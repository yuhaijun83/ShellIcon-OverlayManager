using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Microsoft.Win32;
using ShellIconOverlayManager.Localization;
using ShellIconOverlayManager.Models;
using ShellIconOverlayManager.Services;

namespace ShellIconOverlayManager.Ui
{
    /// <summary>
    /// 新建 / 修改子健的名称与子健值。
    /// </summary>
    public sealed class EditEntryForm : DpiAwareForm
    {
        private sealed class KindItem
        {
            public RegistryValueKind Kind { get; set; }
            public string Text { get; set; }

            public override string ToString()
            {
                return Text;
            }
        }

        private readonly RootKind _root;
        private readonly OverlayItem _item;

        private readonly TableLayoutPanel _layout;
        private readonly Label _rootLabel;
        private readonly TextBox _rootBox;
        private readonly Label _nameLabel;
        private readonly TextBox _nameBox;
        private readonly Label _valueLabel;
        private readonly TextBox _valueBox;
        private readonly Label _kindLabel;
        private readonly ComboBox _kindBox;
        private readonly Button _okButton;
        private readonly Button _cancelButton;

        public string KeyName { get; private set; }
        public RegistryValueKind ValueKind { get; private set; }
        public object Value { get; private set; }

        public EditEntryForm(RootKind root, OverlayItem item)
        {
            _root = root;
            _item = item;

            _rootLabel = new Label { Tag = "LblRoot", AutoSize = true, Anchor = AnchorStyles.Left, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 4, 6, 0) };
            _rootBox = new TextBox { Dock = DockStyle.Fill, ReadOnly = true, BackColor = SystemColors.Control, Margin = new Padding(0, 1, 0, 1) };
            _nameLabel = new Label { Tag = "LblName", AutoSize = true, Anchor = AnchorStyles.Left, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 4, 6, 0) };
            _nameBox = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(0, 1, 0, 1) };
            _valueLabel = new Label { Tag = "LblValue", AutoSize = true, Anchor = AnchorStyles.Left, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 4, 6, 0) };
            _valueBox = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(0, 1, 0, 1) };
            _kindLabel = new Label { Tag = "LblValueKind", AutoSize = true, Anchor = AnchorStyles.Left, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 4, 6, 0) };
            _kindBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 1, 0, 1) };
            _okButton = new Button { Tag = "BtnOk", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(88, 0), Margin = new Padding(4, 0, 0, 0) };
            _cancelButton = new Button { Tag = "BtnCancel", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(88, 0), DialogResult = DialogResult.Cancel, Margin = new Padding(4, 0, 0, 0) };

            _layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5,
                Padding = new Padding(12, 12, 12, 6)
            };
            _layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (var i = 0; i < 5; i++)
            {
                _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            _layout.Controls.Add(_rootLabel, 0, 0);
            _layout.Controls.Add(_rootBox, 1, 0);
            _layout.Controls.Add(_nameLabel, 0, 1);
            _layout.Controls.Add(_nameBox, 1, 1);
            _layout.Controls.Add(_valueLabel, 0, 2);
            _layout.Controls.Add(_valueBox, 1, 2);
            _layout.Controls.Add(_kindLabel, 0, 3);
            _layout.Controls.Add(_kindBox, 1, 3);

            var buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 48, Padding = new Padding(12, 6, 12, 12) };
            var buttonFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false
            };
            buttonFlow.Controls.Add(_okButton);
            buttonFlow.Controls.Add(_cancelButton);
            buttonPanel.Controls.Add(buttonFlow);

            Controls.Add(_layout);
            Controls.Add(buttonPanel);

            AcceptButton = _okButton;
            CancelButton = _cancelButton;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            Icon = AppIcons.App;
            ClientSize = new Size(560, 220);
            Tag = item == null ? "NewTitle" : "EditTitle";

            _kindBox.Items.Add(new KindItem { Kind = RegistryValueKind.String, Text = "REG_SZ" });
            _kindBox.Items.Add(new KindItem { Kind = RegistryValueKind.ExpandString, Text = "REG_EXPAND_SZ" });
            _kindBox.Items.Add(new KindItem { Kind = RegistryValueKind.DWord, Text = "REG_DWORD" });
            _kindBox.Items.Add(new KindItem { Kind = RegistryValueKind.QWord, Text = "REG_QWORD" });
            _kindBox.SelectedIndex = 0;

            _rootBox.Text = RegistryService.GetRootDisplayPath(_root);

            if (_item != null)
            {
                _nameBox.Text = _item.Name;
                _valueBox.Text = _item.Value ?? string.Empty;
                SelectKind(_item.ValueKind);
            }

            KeyName = _item != null ? _item.Name : string.Empty;
            ValueKind = RegistryValueKind.String;
            Value = string.Empty;

            _okButton.Click += OnOkClick;

            Localizer.LanguageChanged += OnLanguageChanged;
            ApplyTexts();
        }

        private void SelectKind(RegistryValueKind kind)
        {
            for (var i = 0; i < _kindBox.Items.Count; i++)
            {
                var kindItem = _kindBox.Items[i] as KindItem;
                if (kindItem != null && kindItem.Kind == kind)
                {
                    _kindBox.SelectedIndex = i;
                    return;
                }
            }
        }

        private void OnOkClick(object sender, EventArgs e)
        {
            var name = _nameBox.Text.Trim();

            if (name.Length == 0)
            {
                ShowMessage(Localizer.T("MsgNameEmpty"));
                DialogResult = DialogResult.None;
                return;
            }

            if (name.IndexOf('\\') >= 0)
            {
                ShowMessage(Localizer.T("MsgNameInvalid"));
                DialogResult = DialogResult.None;
                return;
            }

            if (_item == null || !string.Equals(_item.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                if (RegistryService.Exists(_root, name))
                {
                    ShowMessage(Localizer.Format("MsgNameExists", name));
                    DialogResult = DialogResult.None;
                    return;
                }
            }

            var kindItem = _kindBox.SelectedItem as KindItem;
            var kind = kindItem != null ? kindItem.Kind : RegistryValueKind.String;
            var text = _valueBox.Text;

            object value;
            if (kind == RegistryValueKind.DWord || kind == RegistryValueKind.QWord)
            {
                if (!TryParseNumber(text, kind == RegistryValueKind.QWord, out value))
                {
                    ShowMessage(Localizer.T("MsgNumberInvalid"));
                    DialogResult = DialogResult.None;
                    return;
                }
            }
            else
            {
                value = text;
            }

            KeyName = name;
            ValueKind = kind;
            Value = value;
            DialogResult = DialogResult.OK;
        }

        private static bool TryParseNumber(string text, bool isQWord, out object value)
        {
            value = null;
            text = (text ?? string.Empty).Trim();
            if (text.Length == 0)
            {
                value = isQWord ? (object)0L : (object)0;
                return true;
            }

            try
            {
                if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    var hex = text.Substring(2);
                    value = isQWord
                        ? (object)Convert.ToInt64(hex, 16)
                        : (object)unchecked((int)Convert.ToInt64(hex, 16));
                    return true;
                }

                value = isQWord
                    ? (object)Convert.ToInt64(text, CultureInfo.InvariantCulture)
                    : (object)Convert.ToInt32(text, CultureInfo.InvariantCulture);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void ShowMessage(string message)
        {
            MessageBox.Show(this, message, Localizer.T("MsgInfo"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            ApplyTexts();
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
