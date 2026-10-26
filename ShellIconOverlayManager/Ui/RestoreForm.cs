using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ShellIconOverlayManager.Data;
using ShellIconOverlayManager.Localization;
using ShellIconOverlayManager.Models;
using ShellIconOverlayManager.Services;

namespace ShellIconOverlayManager.Ui
{
    /// <summary>
    /// 操作履历：显示 SQLite 中保存的删除/修改记录，可选择一个或多个恢复。
    /// </summary>
    public sealed class RestoreForm : DpiAwareForm
    {
        private readonly DataGridView _grid;
        private readonly CheckBox _onlyUnrestored;
        private readonly Button _restoreButton;
        private readonly Button _deleteButton;
        private readonly Button _refreshButton;
        private readonly Button _closeButton;

        private List<HistoryEntry> _entries = new List<HistoryEntry>();

        public RestoreForm()
        {
            _grid = new DataGridView
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

            // 明细表格用略大的内容字号
            UiFont.Apply(_grid);

            var checkColumn = new DataGridViewCheckBoxColumn { Tag = "ColSelect", Width = 46, SortMode = DataGridViewColumnSortMode.NotSortable, AutoSizeMode = DataGridViewAutoSizeColumnMode.None };
            var timeColumn = new DataGridViewTextBoxColumn { Tag = "ColTime", Width = 150, ReadOnly = true, SortMode = DataGridViewColumnSortMode.NotSortable };
            var rootColumn = new DataGridViewTextBoxColumn { Tag = "ColRoot", Width = 180, ReadOnly = true, SortMode = DataGridViewColumnSortMode.NotSortable };
            var operationColumn = new DataGridViewTextBoxColumn { Tag = "ColOperation", Width = 90, ReadOnly = true, SortMode = DataGridViewColumnSortMode.NotSortable };
            var keyColumn = new DataGridViewTextBoxColumn { Tag = "ColKey", Width = 240, ReadOnly = true, SortMode = DataGridViewColumnSortMode.NotSortable };
            var detailColumn = new DataGridViewTextBoxColumn { Tag = "ColDetail", Width = 220, ReadOnly = true, SortMode = DataGridViewColumnSortMode.NotSortable, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill };
            var statusColumn = new DataGridViewTextBoxColumn { Tag = "ColStatus", Width = 100, ReadOnly = true, SortMode = DataGridViewColumnSortMode.NotSortable };

            _grid.Columns.Add(checkColumn);
            _grid.Columns.Add(timeColumn);
            _grid.Columns.Add(rootColumn);
            _grid.Columns.Add(operationColumn);
            _grid.Columns.Add(keyColumn);
            _grid.Columns.Add(detailColumn);
            _grid.Columns.Add(statusColumn);

            _onlyUnrestored = new CheckBox { Tag = "LblOnlyUnrestored", AutoSize = true, Checked = true, Dock = DockStyle.Left, Margin = new Padding(6, 4, 0, 0) };

            var topPanel = new Panel { Dock = DockStyle.Top, Height = 30 };
            topPanel.Controls.Add(_onlyUnrestored);

            _restoreButton = new Button { Tag = "BtnRestoreSelected", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(110, 0), Margin = new Padding(4, 0, 0, 0) };
            _deleteButton = new Button { Tag = "BtnDeleteHistory", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(110, 0), Margin = new Padding(4, 0, 0, 0) };
            _refreshButton = new Button { Tag = "BtnRefresh", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(96, 0), Margin = new Padding(4, 0, 0, 0) };
            _closeButton = new Button { Tag = "BtnClose", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(96, 0), Margin = new Padding(4, 0, 0, 0) };

            var buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 48, Padding = new Padding(10, 6, 10, 12) };
            var buttonFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false
            };
            buttonFlow.Controls.Add(_restoreButton);
            buttonFlow.Controls.Add(_deleteButton);
            buttonFlow.Controls.Add(_refreshButton);
            buttonFlow.Controls.Add(_closeButton);
            buttonPanel.Controls.Add(buttonFlow);

            var contentPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 4, 10, 4) };
            contentPanel.Controls.Add(_grid);

            Controls.Add(contentPanel);
            Controls.Add(topPanel);
            Controls.Add(buttonPanel);

            StartPosition = FormStartPosition.CenterParent;
            Icon = AppIcons.App;
            ClientSize = new Size(1000, 560);
            MinimumSize = new Size(720, 420);
            Tag = "RestoreTitle";

            _grid.CurrentCellDirtyStateChanged += (sender, e) =>
            {
                if (_grid.IsCurrentCellDirty && _grid.CurrentCell is DataGridViewCheckBoxCell)
                {
                    _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };
            _grid.CellDoubleClick += (sender, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    _grid.Rows[e.RowIndex].Cells[0].Value = !IsChecked(_grid.Rows[e.RowIndex]);
                    _grid.RefreshEdit();
                }
            };
            _grid.ColumnHeaderMouseClick += (sender, e) =>
            {
                if (e.ColumnIndex == 0)
                {
                    ToggleAll(!AllChecked());
                }
            };

            _onlyUnrestored.CheckedChanged += (sender, e) => Populate();
            _restoreButton.Click += (sender, e) => RestoreSelected();
            _deleteButton.Click += (sender, e) => DeleteSelected();
            _refreshButton.Click += (sender, e) => LoadHistory();
            _closeButton.Click += (sender, e) => Close();

            Localizer.LanguageChanged += OnLanguageChanged;
            ApplyTexts();
            LoadHistory();
        }

        private void LoadHistory()
        {
            try
            {
                _entries = AppDatabase.GetHistory();
            }
            catch (Exception ex)
            {
                _entries = new List<HistoryEntry>();
                MessageBox.Show(this, ex.Message, Localizer.T("MsgError"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            Populate();
        }

        /// <summary>缩放后按当前表格字体重算行高、表头高度与列宽，保证内容完整显示。</summary>
        protected override void OnScalingApplied(float factor)
        {
            base.OnScalingApplied(factor);

            var rowHeight = Math.Max(_grid.Font.Height + (int)Math.Round(6 * factor), 20);
            var headerHeight = Math.Max(_grid.Font.Height + (int)Math.Round(8 * factor), 22);

            _grid.RowTemplate.Height = rowHeight;
            _grid.ColumnHeadersHeight = headerHeight;
            foreach (DataGridViewRow row in _grid.Rows)
            {
                row.Height = rowHeight;
            }

            // 列宽至少要能完整显示表头文字，否则字号变大后会被截断
            using (var graphics = _grid.CreateGraphics())
            {
                foreach (DataGridViewColumn column in _grid.Columns)
                {
                    if (column.AutoSizeMode == DataGridViewAutoSizeColumnMode.Fill)
                    {
                        continue;
                    }

                    var headerWidth = TextRenderer.MeasureText(
                        graphics, column.HeaderText, _grid.Font, Size.Empty, TextFormatFlags.NoPadding).Width
                        + (int)Math.Round(20 * factor);

                    if (column.Width < headerWidth)
                    {
                        column.Width = headerWidth;
                    }
                }
            }
        }

        private void Populate()
        {
            _grid.SuspendLayout();
            _grid.Rows.Clear();

            foreach (var entry in _entries)
            {
                if (_onlyUnrestored.Checked && entry.IsRestored)
                {
                    continue;
                }

                var index = _grid.Rows.Add(
                    false,
                    AppDatabase.ToDisplayTime(entry.OperatedAt),
                    RegistryService.GetRootName(entry.Root),
                    GetOperationText(entry.Operation),
                    GetKeyText(entry),
                    GetDetailText(entry),
                    entry.IsRestored ? Localizer.T("StatusRestored") : Localizer.T("StatusNotRestored"));

                _grid.Rows[index].Tag = entry;
            }

            _grid.ResumeLayout();
        }

        private static string GetOperationText(HistoryOperation operation)
        {
            switch (operation)
            {
                case HistoryOperation.Delete: return Localizer.T("OpDelete");
                case HistoryOperation.Create: return Localizer.T("OpCreate");
                case HistoryOperation.Rename: return Localizer.T("OpRename");
                case HistoryOperation.EditValue: return Localizer.T("OpEditValue");
                default: return operation.ToString();
            }
        }

        private static string GetKeyText(HistoryEntry entry)
        {
            return entry.KeyName ?? string.Empty;
        }

        private static string GetDetailText(HistoryEntry entry)
        {
            switch (entry.Operation)
            {
                case HistoryOperation.Rename:
                    return (entry.OldKeyName ?? string.Empty) + " -> " + (entry.NewKeyName ?? string.Empty);
                case HistoryOperation.EditValue:
                case HistoryOperation.Delete:
                case HistoryOperation.Create:
                    return entry.Summary ?? string.Empty;
                default:
                    return string.Empty;
            }
        }

        private static bool IsChecked(DataGridViewRow row)
        {
            return row.Cells[0].Value is bool && (bool)row.Cells[0].Value;
        }

        private bool AllChecked()
        {
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (!IsChecked(row))
                {
                    return false;
                }
            }

            return _grid.Rows.Count > 0;
        }

        private void ToggleAll(bool check)
        {
            foreach (DataGridViewRow row in _grid.Rows)
            {
                row.Cells[0].Value = check;
            }

            _grid.RefreshEdit();
        }

        private List<HistoryEntry> GetCheckedEntries()
        {
            var list = new List<HistoryEntry>();
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (IsChecked(row))
                {
                    var entry = row.Tag as HistoryEntry;
                    if (entry != null)
                    {
                        list.Add(entry);
                    }
                }
            }

            return list;
        }

        private void RestoreSelected()
        {
            var selected = GetCheckedEntries();
            if (selected.Count == 0)
            {
                MessageBox.Show(this, Localizer.T("MsgNoHistory"), Localizer.T("MsgInfo"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show(
                this,
                Localizer.Format("MsgRestoreConfirm", selected.Count),
                Localizer.T("MsgConfirmDeleteTitle"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            var succeeded = 0;
            var failed = 0;
            var details = string.Empty;

            foreach (var entry in selected)
            {
                string message;
                if (RegistryService.Restore(entry, out message))
                {
                    AppDatabase.MarkRestored(entry.Id, DateTime.UtcNow);
                    entry.IsRestored = true;
                    succeeded++;
                }
                else
                {
                    failed++;
                    if (!string.IsNullOrEmpty(message) && details.Length == 0)
                    {
                        details = message;
                    }
                }
            }

            LoadHistory();

            MessageBox.Show(
                this,
                Localizer.Format("MsgRestorePartial", succeeded, failed, details),
                Localizer.T("MsgInfo"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void DeleteSelected()
        {
            var selected = GetCheckedEntries();
            if (selected.Count == 0)
            {
                MessageBox.Show(this, Localizer.T("MsgNoHistory"), Localizer.T("MsgInfo"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show(
                this,
                Localizer.Format("MsgConfirmClearHistory", selected.Count),
                Localizer.T("MsgConfirmDeleteTitle"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            var ids = new List<long>();
            foreach (var entry in selected)
            {
                ids.Add(entry.Id);
            }

            AppDatabase.DeleteHistory(ids);
            LoadHistory();

            MessageBox.Show(
                this,
                Localizer.Format("MsgHistoryDeleted", ids.Count),
                Localizer.T("MsgInfo"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            ApplyTexts();
            Populate();
        }

        private void ApplyTexts()
        {
            Localizer.ApplyToForm(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Localizer.LanguageChanged -= OnLanguageChanged;
            }

            base.Dispose(disposing);
        }
    }
}
