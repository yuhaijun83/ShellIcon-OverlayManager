using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;
using ShellIconOverlayManager.Data;
using ShellIconOverlayManager.Localization;
using ShellIconOverlayManager.Models;
using ShellIconOverlayManager.Services;

namespace ShellIconOverlayManager.Ui
{
    /// <summary>
    /// 主窗体：分别显示 HKLM / HKCU 下 ShellIconOverlayIdentifiers 的子健，
    /// 支持多选删除（带确认）、修改、新建，以及基于 SQLite 履历的恢复。
    /// </summary>
    public sealed class MainForm : DpiAwareForm
    {
        // 96 DPI（100%）下的设计尺寸，实际尺寸由 OnScalingApplied 按系数换算
        private const int DesignCheckWidth = 46;
        private const int DesignOrderWidth = 56;
        private const int DesignNameWidth = 320;
        private const int DesignValueWidth = 320;
        private const int DesignTypeWidth = 110;
        private const int DesignMinWidth = 720;
        private const int DesignMinHeight = 460;

        private sealed class RootView
        {
            public RootKind Root;
            public TabPage Page;
            public DataGridView Grid;
            public TextBox SearchBox;
            public List<OverlayItem> Items = new List<OverlayItem>();
        }

        private readonly MenuStrip _menuStrip;
        private readonly ToolStripMenuItem _fileMenu;
        private readonly ToolStripMenuItem _toolsMenu;
        private readonly ToolStripMenuItem _helpMenu;
        private readonly ToolStripMenuItem _languageMenu;
        private readonly ToolStripMenuItem _refreshMenuItem;
        private readonly ToolStripMenuItem _exitMenuItem;
        private readonly ToolStripMenuItem _newMenuItem;
        private readonly ToolStripMenuItem _restoreMenuItem;
        private readonly ToolStripMenuItem _openDataMenuItem;
        private readonly ToolStripMenuItem _fontSizeMenu;
        private readonly List<ToolStripMenuItem> _fontSizeItems = new List<ToolStripMenuItem>();
        private readonly ToolStripMenuItem _aboutMenuItem;

        private readonly Panel _statusPanel;
        private readonly Label _statusMessage;
        private readonly Label _statusPrivilege;
        private readonly Label _statusTip;

        private readonly TabControl _tabControl;
        private readonly Panel _buttonPanel;
        private readonly Panel _leftButtonFlow;
        private readonly Panel _rightButtonFlow;
        private readonly List<Button> _barButtons = new List<Button>();
        private readonly Button _btnSelectAll;
        private readonly Button _btnClearSelection;
        private readonly Button _btnInvert;
        private readonly Button _btnDelete;
        private readonly Button _btnEdit;
        private readonly Button _btnNew;
        private readonly Button _btnRestore;
        private readonly Button _btnRefresh;
        private readonly Button _btnRestartExplorer;

        private readonly NotifyIcon _trayIcon;
        private readonly ContextMenuStrip _trayMenu;
        private readonly ToolStripMenuItem _trayShowItem;
        private readonly ToolStripMenuItem _trayLanguageItem;
        private readonly ToolStripMenuItem _trayExitItem;

        private readonly List<RootView> _views = new List<RootView>();
        private readonly Dictionary<TabPage, RootView> _pageMap = new Dictionary<TabPage, RootView>();
        private readonly List<ContextMenuStrip> _gridMenus = new List<ContextMenuStrip>();

        private readonly bool _isAdministrator;
        private bool _allowClose;

        public MainForm()
        {
            _isAdministrator = IsRunningAsAdministrator();

            _menuStrip = new MenuStrip();

            _refreshMenuItem = new ToolStripMenuItem { Tag = "MenuRefresh", ShortcutKeys = Keys.F5 };
            _exitMenuItem = new ToolStripMenuItem { Tag = "MenuExit" };
            _fileMenu = new ToolStripMenuItem { Tag = "MenuFile" };
            _fileMenu.DropDownItems.Add(_refreshMenuItem);
            _fileMenu.DropDownItems.Add(new ToolStripSeparator());
            _fileMenu.DropDownItems.Add(_exitMenuItem);

            _languageMenu = new ToolStripMenuItem { Tag = "MenuLanguage" };

            _newMenuItem = new ToolStripMenuItem { Tag = "MenuNew" };
            _restoreMenuItem = new ToolStripMenuItem { Tag = "MenuRestore" };
            _openDataMenuItem = new ToolStripMenuItem { Tag = "MenuOpenDataFolder" };

            // 字体大小：小 / 标准 / 大 / 特大，持久化到 SQLite，对所有窗体立即生效
            _fontSizeMenu = new ToolStripMenuItem { Tag = "MenuFontSize" };
            var sizeKeys = new[] { "FontSizeSmall", "FontSizeNormal", "FontSizeLarge", "FontSizeXLarge" };
            for (var i = 0; i < sizeKeys.Length; i++)
            {
                var index = i;
                var item = new ToolStripMenuItem { Tag = sizeKeys[i], CheckOnClick = true };
                item.Click += (sender, e) => ChangeContentSize(index, item);
                _fontSizeItems.Add(item);
                _fontSizeMenu.DropDownItems.Add(item);
            }

            _toolsMenu = new ToolStripMenuItem { Tag = "MenuTools" };
            _toolsMenu.DropDownItems.Add(_newMenuItem);
            _toolsMenu.DropDownItems.Add(_restoreMenuItem);
            _toolsMenu.DropDownItems.Add(new ToolStripSeparator());
            _toolsMenu.DropDownItems.Add(_openDataMenuItem);
            _toolsMenu.DropDownItems.Add(new ToolStripSeparator());
            _toolsMenu.DropDownItems.Add(_fontSizeMenu);

            // 按当前字号勾选对应档位
            for (var i = 0; i < _fontSizeItems.Count; i++)
            {
                _fontSizeItems[i].Checked = Math.Abs(UiFont.Sizes[i] - UiFont.ContentSize) < 0.01f;
            }

            _aboutMenuItem = new ToolStripMenuItem { Tag = "MenuAbout" };
            _helpMenu = new ToolStripMenuItem { Tag = "MenuHelp" };
            _helpMenu.DropDownItems.Add(_aboutMenuItem);

            _menuStrip.Items.Add(_fileMenu);
            _menuStrip.Items.Add(_languageMenu);
            _menuStrip.Items.Add(_toolsMenu);
            _menuStrip.Items.Add(_helpMenu);
            MainMenuStrip = _menuStrip;

            _tabControl = new TabControl { Dock = DockStyle.Fill };
            // 页签文字同样用略大的内容字号
            UiFont.Apply(_tabControl);
            _views.Add(CreateView(RootKind.LocalMachine, "TabHklm"));
            _views.Add(CreateView(RootKind.Wow64Node, "TabWow64"));
            foreach (var view in _views)
            {
                _pageMap[view.Page] = view;
                _tabControl.TabPages.Add(view.Page);
            }

            _btnSelectAll = CreateButton("BtnSelectAll");
            _btnClearSelection = CreateButton("BtnClearSelection");
            _btnInvert = CreateButton("BtnInvert");
            _btnDelete = CreateButton("BtnDelete");
            _btnEdit = CreateButton("BtnEdit");
            _btnNew = CreateButton("BtnNew");
            _btnRestore = CreateButton("BtnRestore");
            _btnRefresh = CreateButton("BtnRefresh");
            _btnRestartExplorer = CreateButton("BtnRestartExplorer");

            _buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 50, Padding = new Padding(10, 6, 10, 10) };
            var buttonFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false
            };
            buttonFlow.Controls.Add(_btnSelectAll);
            buttonFlow.Controls.Add(_btnClearSelection);
            buttonFlow.Controls.Add(_btnInvert);
            buttonFlow.Controls.Add(new Label { Width = 12, Height = 1 });
            buttonFlow.Controls.Add(_btnDelete);
            buttonFlow.Controls.Add(_btnEdit);
            buttonFlow.Controls.Add(_btnNew);
            buttonFlow.Controls.Add(new Label { Width = 12, Height = 1 });
            buttonFlow.Controls.Add(_btnRestore);
            buttonFlow.Controls.Add(_btnRefresh);
            _buttonPanel.Controls.Add(buttonFlow);
            _rightButtonFlow = buttonFlow;

            // 重启资源管理器单独放在左侧，与其它操作区分开
            var leftFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false
            };
            leftFlow.Controls.Add(_btnRestartExplorer);
            _buttonPanel.Controls.Add(leftFlow);
            _leftButtonFlow = leftFlow;

            var contentPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 4, 8, 0) };
            contentPanel.Controls.Add(_tabControl);

            // 状态栏使用标签而非 StatusStrip：自动省略过长文字，避免各段相互重叠
            _statusTip = new Label
            {
                Tag = "TipTop15",
                AutoSize = true,
                Dock = DockStyle.Right,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 2, 0)
            };
            _statusPrivilege = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Right,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 14, 0)
            };
            _statusMessage = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft
            };
            _statusMessage.UseCompatibleTextRendering = false;
            _statusPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 24,
                Padding = new Padding(8, 2, 4, 2)
            };
            _statusPanel.Controls.Add(_statusMessage);
            _statusPanel.Controls.Add(_statusPrivilege);
            _statusPanel.Controls.Add(_statusTip);

            _trayShowItem = new ToolStripMenuItem { Tag = "TrayShow" };
            _trayLanguageItem = new ToolStripMenuItem { Tag = "MenuLanguage" };
            _trayExitItem = new ToolStripMenuItem { Tag = "TrayExit" };
            _trayMenu = new ContextMenuStrip();
            _trayMenu.Items.Add(_trayShowItem);
            _trayMenu.Items.Add(_trayLanguageItem);
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add(_trayExitItem);

            _trayIcon = new NotifyIcon { Icon = AppIcons.App, Visible = true, ContextMenuStrip = _trayMenu };

            Controls.Add(contentPanel);
            Controls.Add(_buttonPanel);
            Controls.Add(_statusPanel);
            Controls.Add(_menuStrip);

            // 标题栏显示版本号：ShellIcon-OverlayManager v26.10
            Text = AppVersion.Title;
            Icon = AppIcons.App;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(720, 460);
            ClientSize = new Size(1000, 620);

            _refreshMenuItem.Click += (sender, e) => RefreshAll();
            _exitMenuItem.Click += (sender, e) => ExitApplication();
            _newMenuItem.Click += (sender, e) => NewEntry();
            _restoreMenuItem.Click += (sender, e) => ShowRestoreDialog();
            _openDataMenuItem.Click += (sender, e) => OpenDataFolder();
            _aboutMenuItem.Click += (sender, e) => ShowAbout();

            _btnSelectAll.Click += (sender, e) => SetChecked(CurrentView, true);
            _btnClearSelection.Click += (sender, e) => SetChecked(CurrentView, false);
            _btnInvert.Click += (sender, e) => InvertChecked(CurrentView);
            _btnDelete.Click += (sender, e) => DeleteSelected();
            _btnEdit.Click += (sender, e) => EditSelected();
            _btnNew.Click += (sender, e) => NewEntry();
            _btnRestore.Click += (sender, e) => ShowRestoreDialog();
            _btnRefresh.Click += (sender, e) => RefreshAll();
            _btnRestartExplorer.Click += (sender, e) => RestartExplorer();

            _trayIcon.DoubleClick += (sender, e) => ShowFromTray();
            _trayShowItem.Click += (sender, e) => ShowFromTray();
            _trayExitItem.Click += (sender, e) => ExitApplication();

            Localizer.LanguageChanged += OnLanguageChanged;
            ApplyTexts();
            RefreshAll();
        }

        private Button CreateButton(string resourceKey)
        {
            var button = new Button
            {
                Tag = resourceKey,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(96, 0),
                Padding = new Padding(6, 2, 6, 2),
                Margin = new Padding(3, 0, 3, 0)
            };

            // 按钮文字用比默认略大的内容字号
            UiFont.Apply(button);
            _barButtons.Add(button);
            return button;
        }

        private RootView CreateView(RootKind root, string titleKey)
        {
            var view = new RootView { Root = root };

            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToOrderColumns = false,
                MultiSelect = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                RowHeadersVisible = false,
                BackgroundColor = SystemColors.Window,
                BorderStyle = BorderStyle.None
            };

            // 明细表格用略大的内容字号，行高列宽会随之自动调整
            UiFont.Apply(grid);

            var checkColumn = new DataGridViewCheckBoxColumn
            {
                Tag = "ColSelect",
                Width = DesignCheckWidth,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            };
            // 名称、值两列按剩余宽度自适应，避免高 DPI 下被截断
            var nameColumn = new DataGridViewTextBoxColumn
            {
                Tag = "ColName",
                Width = DesignNameWidth,
                MinimumWidth = DesignNameWidth,
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.Automatic,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 34
            };
            var valueColumn = new DataGridViewTextBoxColumn
            {
                Tag = "ColValue",
                Width = DesignValueWidth,
                MinimumWidth = DesignValueWidth,
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.Automatic,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 66
            };

            grid.Columns.Add(checkColumn);
            grid.Columns.Add(new DataGridViewTextBoxColumn { Tag = "ColOrder", Width = DesignOrderWidth, ReadOnly = true, SortMode = DataGridViewColumnSortMode.NotSortable });
            grid.Columns.Add(nameColumn);
            grid.Columns.Add(valueColumn);
            grid.Columns.Add(new DataGridViewTextBoxColumn { Tag = "ColType", Width = DesignTypeWidth, ReadOnly = true, SortMode = DataGridViewColumnSortMode.Automatic });

            grid.CurrentCellDirtyStateChanged += (sender, e) =>
            {
                if (grid.IsCurrentCellDirty && grid.CurrentCell is DataGridViewCheckBoxCell)
                {
                    grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };
            grid.CellValueChanged += (sender, e) =>
            {
                if (e.ColumnIndex == 0)
                {
                    UpdateStatus();
                }
            };
            grid.CellDoubleClick += (sender, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    EditSelected();
                }
            };
            grid.ColumnHeaderMouseClick += (sender, e) =>
            {
                if (e.ColumnIndex == 0)
                {
                    SetChecked(view, !AllChecked(view));
                }
            };
            grid.CellMouseDown += (sender, e) =>
            {
                if (e.Button == MouseButtons.Right && e.RowIndex >= 0 && e.ColumnIndex >= 0)
                {
                    grid.CurrentCell = grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
                }
            };

            var editItem = new ToolStripMenuItem { Tag = "MenuContextEdit" };
            var deleteItem = new ToolStripMenuItem { Tag = "MenuContextDelete" };
            var copyNameItem = new ToolStripMenuItem { Tag = "MenuContextCopyName" };
            var copyValueItem = new ToolStripMenuItem { Tag = "MenuContextCopyValue" };
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add(editItem);
            contextMenu.Items.Add(deleteItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(copyNameItem);
            contextMenu.Items.Add(copyValueItem);
            grid.ContextMenuStrip = contextMenu;
            _gridMenus.Add(contextMenu);

            editItem.Click += (sender, e) => EditSelected();
            deleteItem.Click += (sender, e) => DeleteSelected();
            copyNameItem.Click += (sender, e) => CopyName(view);
            copyValueItem.Click += (sender, e) => CopyValue(view);

            var searchLabel = new Label
            {
                Tag = "LblSearch",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 4, 6, 0)
            };
            var searchBox = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(0, 1, 0, 1) };

            // 页内的检索区与明细保持一致的字号
            UiFont.Apply(searchLabel);
            UiFont.Apply(searchBox);
            var searchPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0, 4, 0, 6)
            };            searchPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            searchPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            searchPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            searchPanel.Controls.Add(searchLabel, 0, 0);
            searchPanel.Controls.Add(searchBox, 1, 0);
            searchBox.TextChanged += (sender, e) => PopulateGrid(view);

            var page = new TabPage { Tag = titleKey, Padding = new Padding(6, 6, 6, 6) };
            page.Controls.Add(grid);
            page.Controls.Add(searchPanel);

            view.Page = page;
            view.Grid = grid;
            view.SearchBox = searchBox;
            return view;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // 右键菜单不参与控件树，单独同步字体以保证 DPI 缩放一致
            foreach (var view in _views)
            {
                if (view.Grid.ContextMenuStrip != null)
                {
                    view.Grid.ContextMenuStrip.Font = view.Grid.Font;
                }
            }

            if (_trayMenu != null)
            {
                _trayMenu.Font = Font;
            }
        }

        /// <summary>缩放完成后按当前字体重算表格行高、表头高度与列宽。</summary>
        protected override void OnScalingApplied(float factor)
        {
            base.OnScalingApplied(factor);

            foreach (var view in _views)
            {
                ApplyGridMetrics(view, factor);
            }

            ApplyButtonBarMetrics();

            // 最小宽度要能容纳整排按钮，否则窗口缩小时按钮会被裁切
            var needWidth = _leftButtonFlow.Width + _rightButtonFlow.Width
                            + _buttonPanel.Padding.Horizontal + (int)Math.Round(24 * factor);
            MinimumSize = new Size(
                Math.Max((int)Math.Round(DesignMinWidth * factor), needWidth),
                (int)Math.Round(DesignMinHeight * factor));
        }

        /// <summary>按钮排高度按按钮实际需要的高度换算，避免字号变大后被压扁。</summary>
        private void ApplyButtonBarMetrics()
        {
            var barHeight = 0;
            foreach (var button in _barButtons)
            {
                barHeight = Math.Max(barHeight, button.PreferredSize.Height);
            }

            if (barHeight <= 0)
            {
                return;
            }

            _buttonPanel.Height = barHeight + _buttonPanel.Padding.Vertical;
        }

        private static void ApplyGridMetrics(RootView view, float factor)
        {
            var grid = view.Grid;
            if (grid == null || grid.Columns.Count < 5)
            {
                return;
            }

            // 行高与表头高度跟随字体，留出随 DPI 增长的少量边距即可
            var rowPadding = (int)Math.Round(6 * factor);
            var headerPadding = (int)Math.Round(8 * factor);
            var rowHeight = Math.Max(grid.Font.Height + rowPadding, 20);
            var headerHeight = Math.Max(grid.Font.Height + headerPadding, 22);

            grid.RowTemplate.Height = rowHeight;
            grid.ColumnHeadersHeight = headerHeight;
            foreach (DataGridViewRow row in grid.Rows)
            {
                row.Height = rowHeight;
            }

            using (var graphics = grid.CreateGraphics())
            {
                grid.Columns[0].Width = (int)Math.Round(DesignCheckWidth * factor);
                grid.Columns[1].Width = Math.Max(
                    (int)Math.Round(DesignOrderWidth * factor),
                    MeasureText(graphics, grid.Font, "0000") + 20);
                grid.Columns[4].Width = Math.Max(
                    (int)Math.Round(DesignTypeWidth * factor),
                    MeasureText(graphics, grid.Font, "REG_EXPAND_SZ") + 18);

                // 名称/值为自适应列，最小宽度保证常见内容完整显示
                grid.Columns[2].MinimumWidth = Math.Max(
                    (int)Math.Round(DesignNameWidth * 0.75 * factor),
                    MeasureText(graphics, grid.Font, "OneDriveShortcutOverlay") + 16);
                grid.Columns[3].MinimumWidth = Math.Max(
                    (int)Math.Round(DesignValueWidth * factor),
                    MeasureText(graphics, grid.Font, "{BBACC218-34EA-4666-9D7A-C78F2274A524}") + 16);
            }
        }

        private static int MeasureText(Graphics graphics, Font font, string text)
        {
            return TextRenderer.MeasureText(graphics, text, font, Size.Empty, TextFormatFlags.NoPadding).Width;
        }

        private static bool IsRunningAsAdministrator()
        {
            try
            {
                using (var identity = WindowsIdentity.GetCurrent())
                {
                    return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
        #region 界面文字

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            ApplyTexts();
            UpdateStatus();
        }

        private void ApplyTexts()
        {
            Localizer.ApplyToForm(this);

            BuildLanguageMenu(_languageMenu);
            BuildLanguageMenu(_trayLanguageItem);

            _statusPrivilege.Text = _isAdministrator ? Localizer.T("StatusAdmin") : Localizer.T("StatusUser");
            _trayIcon.Text = Localizer.T("AppTitle");

            foreach (var menu in _gridMenus)
            {
                Localizer.ApplyToMenu(menu.Items);
            }

            Localizer.ApplyToMenu(_trayMenu.Items);
        }

        private void BuildLanguageMenu(ToolStripDropDownItem parent)
        {
            if (parent == null)
            {
                return;
            }

            parent.DropDownItems.Clear();

            foreach (var culture in Localizer.SupportedCultures)
            {
                var name = culture;
                var item = new ToolStripMenuItem(Localizer.CultureDisplayName(name))
                {
                    Checked = string.Equals(name, Localizer.Current, StringComparison.OrdinalIgnoreCase)
                };
                item.Click += (sender, e) => Localizer.SetCulture(name);
                parent.DropDownItems.Add(item);
            }
        }

        #endregion

        #region 数据加载

        private RootView CurrentView
        {
            get
            {
                RootView view;
                var page = _tabControl.SelectedTab;
                if (page != null && _pageMap.TryGetValue(page, out view))
                {
                    return view;
                }

                return _views[0];
            }
        }

        private void RefreshAll()
        {
            var missingRoots = new List<string>();

            foreach (var view in _views)
            {
                try
                {
                    view.Items = RegistryService.Load(view.Root);
                    if (view.Items.Count == 0 && !RegistryService.RootExists(view.Root))
                    {
                        missingRoots.Add(RegistryService.GetRootDisplayPath(view.Root));
                    }
                }
                catch (Exception ex)
                {
                    view.Items = new List<OverlayItem>();
                    MessageBox.Show(this, Localizer.Format("MsgLoadFailed", ex.Message), Localizer.T("MsgError"),
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                PopulateGrid(view);
            }

            UpdateStatus();

            if (missingRoots.Count > 0)
            {
                _statusMessage.Text = Localizer.Format("MsgRegistryMissing", string.Join("; ", missingRoots.ToArray()));
            }
        }

        /// <summary>切换内容字号：持久化选择并让所有打开的窗体立即生效。</summary>
        private void ChangeContentSize(int index, ToolStripMenuItem clicked)
        {
            var size = UiFont.Sizes[index];
            UiFont.SetContentSize(size);
            try
            {
                AppDatabase.SetSetting("ContentFontSize", size.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            catch (Exception)
            {
                // 保存失败不影响本次会话的字号切换
            }

            foreach (var item in _fontSizeItems)
            {
                item.Checked = item == clicked;
            }

            // 先刷字体，再让每个打开的窗体按新字体重算行高、列宽、按钮排等度量
            UiFont.RefreshAll();
            foreach (Form form in Application.OpenForms)
            {
                if (form is DpiAwareForm dpiForm && !dpiForm.IsDisposed)
                {
                    dpiForm.RefreshScaling();
                }
            }
        }

        /// <summary>重启 Windows 资源管理器，使图标覆盖的改动立即生效。</summary>
        private void RestartExplorer()
        {
            var confirm = MessageBox.Show(
                this,
                Localizer.T("MsgRestartExplorerConfirm"),
                Localizer.T("MsgConfirmRestartTitle"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            try
            {
                foreach (var process in Process.GetProcessesByName("explorer"))
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit(3000);
                    }
                    catch (Exception)
                    {
                        // 少数受保护的文件管理器进程无法结束，忽略并继续
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }

                // 重新拉起外壳进程，桌面与任务栏随之恢复
                var explorerPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows) + "\\explorer.exe";
                Process.Start(new ProcessStartInfo(explorerPath) { UseShellExecute = false });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, Localizer.Format("MsgRestartExplorerFailed", ex.Message),
                    Localizer.T("MsgError"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _statusMessage.Text = Localizer.T("MsgExplorerRestarted");
        }

        private void PopulateGrid(RootView view)
        {
            var grid = view.Grid;
            var filter = view.SearchBox.Text.Trim();

            grid.SuspendLayout();
            grid.Rows.Clear();

            foreach (var item in view.Items)
            {
                if (filter.Length > 0 &&
                    item.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0 &&
                    (item.Value ?? string.Empty).IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                var index = grid.Rows.Add(false, item.Order, item.Name, item.Value, item.ValueKindName);
                grid.Rows[index].Tag = item;
            }

            grid.ResumeLayout();
        }

        private void UpdateStatus()
        {
            var view = CurrentView;
            _statusMessage.Text = Localizer.Format("StatusCount", view.Items.Count, GetCheckedItems(view).Count);
        }

        #endregion

        #region 选择操作

        private static bool IsChecked(DataGridViewRow row)
        {
            return row.Cells[0].Value is bool && (bool)row.Cells[0].Value;
        }

        private static void SetRowChecked(DataGridViewRow row, bool check)
        {
            row.Cells[0].Value = check;
        }

        private static List<OverlayItem> GetCheckedItems(RootView view)
        {
            var list = new List<OverlayItem>();
            foreach (DataGridViewRow row in view.Grid.Rows)
            {
                if (IsChecked(row))
                {
                    var item = row.Tag as OverlayItem;
                    if (item != null)
                    {
                        list.Add(item);
                    }
                }
            }

            return list;
        }

        private static bool AllChecked(RootView view)
        {
            foreach (DataGridViewRow row in view.Grid.Rows)
            {
                if (!IsChecked(row))
                {
                    return false;
                }
            }

            return view.Grid.Rows.Count > 0;
        }

        private void SetChecked(RootView view, bool check)
        {
            foreach (DataGridViewRow row in view.Grid.Rows)
            {
                SetRowChecked(row, check);
            }

            view.Grid.RefreshEdit();
            UpdateStatus();
        }

        private void InvertChecked(RootView view)
        {
            foreach (DataGridViewRow row in view.Grid.Rows)
            {
                SetRowChecked(row, !IsChecked(row));
            }

            view.Grid.RefreshEdit();
            UpdateStatus();
        }

        #endregion

        #region 删除 / 编辑 / 新建 / 恢复

        private bool EnsureWritable(RootKind root)
        {
            // 两条路径都在 HKEY_LOCAL_MACHINE 下，均需要管理员权限
            if (!_isAdministrator)
            {
                MessageBox.Show(this, Localizer.T("MsgAdminRequired"), Localizer.T("MsgWarning"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private void DeleteSelected()
        {
            var view = CurrentView;
            var items = GetCheckedItems(view);

            if (items.Count == 0)
            {
                MessageBox.Show(this, Localizer.T("MsgNoSelection"), Localizer.T("MsgInfo"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!EnsureWritable(view.Root))
            {
                return;
            }

            using (var dialog = new ConfirmDeleteForm(items))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
            }

            var deleted = 0;
            var errors = new StringBuilder();

            foreach (var item in items)
            {
                try
                {
                    // 删除前先备份完整键值，保证可以回溯恢复
                    var values = RegistryService.SnapshotValues(item.Root, item.Name);
                    RegistryService.DeleteKey(item.Root, item.Name);

                    AppDatabase.AddHistory(new HistoryEntry
                    {
                        Root = item.Root,
                        KeyName = item.Name,
                        Operation = HistoryOperation.Delete,
                        Summary = item.Value ?? string.Empty,
                        OperatedAt = DateTime.UtcNow,
                        Values = values
                    });

                    deleted++;
                }
                catch (Exception ex)
                {
                    errors.AppendLine(item.Name + ": " + ex.Message);
                }
            }

            RefreshAll();

            if (errors.Length > 0)
            {
                MessageBox.Show(this, Localizer.Format("MsgDeleteFailed", errors.ToString()), Localizer.T("MsgError"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                _statusMessage.Text = Localizer.Format("MsgDeleted", deleted);
            }
        }

        private void EditSelected()
        {
            var view = CurrentView;
            var items = GetCheckedItems(view);

            OverlayItem target = null;
            if (items.Count == 1)
            {
                target = items[0];
            }
            else if (items.Count == 0)
            {
                var row = view.Grid.CurrentRow;
                if (row != null)
                {
                    target = row.Tag as OverlayItem;
                }
            }

            if (items.Count > 1)
            {
                MessageBox.Show(this, Localizer.T("MsgSelectOne"), Localizer.T("MsgInfo"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (target == null)
            {
                MessageBox.Show(this, Localizer.T("MsgNoSelection"), Localizer.T("MsgInfo"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!EnsureWritable(view.Root))
            {
                return;
            }

            using (var dialog = new EditEntryForm(view.Root, target))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    var oldName = target.Name;
                    var newName = dialog.KeyName;
                    var valueText = Convert.ToString(dialog.Value, System.Globalization.CultureInfo.InvariantCulture);

                    if (!string.Equals(oldName, newName, StringComparison.Ordinal))
                    {
                        var oldValues = RegistryService.SnapshotValues(view.Root, oldName);
                        RegistryService.RenameKey(view.Root, oldName, newName);

                        AppDatabase.AddHistory(new HistoryEntry
                        {
                            Root = view.Root,
                            KeyName = newName,
                            Operation = HistoryOperation.Rename,
                            OldKeyName = oldName,
                            NewKeyName = newName,
                            Summary = oldName + " -> " + newName,
                            OperatedAt = DateTime.UtcNow,
                            Values = oldValues
                        });
                    }

                    var valueChanged = !string.Equals(target.Value ?? string.Empty, valueText, StringComparison.Ordinal)
                                       || target.ValueKind != dialog.ValueKind;

                    if (valueChanged)
                    {
                        var beforeValues = RegistryService.SnapshotValues(view.Root, newName);
                        RegistryService.SetDefaultValue(view.Root, newName, dialog.ValueKind, dialog.Value);

                        AppDatabase.AddHistory(new HistoryEntry
                        {
                            Root = view.Root,
                            KeyName = newName,
                            Operation = HistoryOperation.EditValue,
                            Summary = (target.Value ?? string.Empty) + " -> " + valueText,
                            OperatedAt = DateTime.UtcNow,
                            Values = beforeValues
                        });
                    }

                    _statusMessage.Text = Localizer.T("MsgSaved");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, Localizer.T("MsgError"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            RefreshAll();
        }

        private void NewEntry()
        {
            var view = CurrentView;
            if (!EnsureWritable(view.Root))
            {
                return;
            }

            using (var dialog = new EditEntryForm(view.Root, null))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    var snapshot = RegValueSnapshot.Create(string.Empty, dialog.ValueKind, dialog.Value);
                    RegistryService.CreateKey(view.Root, dialog.KeyName, new[] { snapshot });

                    AppDatabase.AddHistory(new HistoryEntry
                    {
                        Root = view.Root,
                        KeyName = dialog.KeyName,
                        Operation = HistoryOperation.Create,
                        Summary = Convert.ToString(dialog.Value, System.Globalization.CultureInfo.InvariantCulture),
                        OperatedAt = DateTime.UtcNow,
                        Values = new List<RegValueSnapshot> { snapshot }
                    });

                    _statusMessage.Text = Localizer.T("MsgSaved");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, Localizer.T("MsgError"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            RefreshAll();
        }

        private void ShowRestoreDialog()
        {
            using (var dialog = new RestoreForm())
            {
                dialog.ShowDialog(this);
            }

            RefreshAll();
        }

        private void CopyName(RootView view)
        {
            var item = GetSingleItem(view);
            if (item != null)
            {
                Clipboard.SetText(item.Name);
            }
        }

        private void CopyValue(RootView view)
        {
            var item = GetSingleItem(view);
            if (item != null)
            {
                Clipboard.SetText(item.Value ?? string.Empty);
            }
        }

        private OverlayItem GetSingleItem(RootView view)
        {
            var items = GetCheckedItems(view);
            if (items.Count == 1)
            {
                return items[0];
            }

            var row = view.Grid.CurrentRow;
            return row != null ? row.Tag as OverlayItem : null;
        }

        #endregion

        #region 其它

        private void ShowAbout()
        {
            using (var dialog = new AboutForm())
            {
                dialog.ShowDialog(this);
            }
        }

        private void OpenDataFolder()
        {
            try
            {
                Process.Start("explorer.exe", AppDatabase.DataDirectory);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, Localizer.T("MsgError"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowFromTray()
        {
            if (!Visible)
            {
                Show();
            }

            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
            }

            Activate();
        }

        private void ExitApplication()
        {
            _allowClose = true;
            Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // 点击右上角关闭按钮时不再隐藏到托盘，先让用户确认是否真的退出
            if (!_allowClose && e.CloseReason == CloseReason.UserClosing)
            {
                var answer = MessageBox.Show(
                    this,
                    Localizer.T("MsgConfirmExit"),
                    Localizer.T("MsgConfirmExitTitle"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (answer != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }

            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);

            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Localizer.LanguageChanged -= OnLanguageChanged;
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}
