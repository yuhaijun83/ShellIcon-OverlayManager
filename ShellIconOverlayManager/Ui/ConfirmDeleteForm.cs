using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ShellIconOverlayManager.Localization;
using ShellIconOverlayManager.Models;
using ShellIconOverlayManager.Services;

namespace ShellIconOverlayManager.Ui
{
    /// <summary>
    /// 删除前的确认窗体：列出将要删除的子健，确认后才执行删除。
    /// </summary>
    public sealed class ConfirmDeleteForm : DpiAwareForm
    {
        private readonly Label _captionLabel;
        private readonly Label _titleLabel;
        private readonly ListBox _listBox;
        private readonly Button _okButton;
        private readonly Button _cancelButton;

        public ConfirmDeleteForm(IEnumerable<OverlayItem> items)
        {
            _titleLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 26,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 4, 10, 0)
            };
            _captionLabel = new Label
            {
                Tag = "MsgConfirmDeleteCaption",
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(10, 2, 10, 2)
            };
            _listBox = new ListBox
            {
                Dock = DockStyle.Fill,
                SelectionMode = SelectionMode.None,
                HorizontalScrollbar = true,
                BorderStyle = BorderStyle.Fixed3D
            };
            _okButton = new Button { Tag = "BtnOk", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(96, 0), DialogResult = DialogResult.OK, Margin = new Padding(4, 0, 0, 0) };
            _cancelButton = new Button { Tag = "BtnCancel", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(96, 0), DialogResult = DialogResult.Cancel, Margin = new Padding(4, 0, 0, 0) };

            var buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 48, Padding = new Padding(10, 6, 10, 12) };
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

            var contentPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 0, 10, 0) };
            contentPanel.Controls.Add(_listBox);

            Controls.Add(contentPanel);
            Controls.Add(_captionLabel);
            Controls.Add(_titleLabel);
            Controls.Add(buttonPanel);

            AcceptButton = _okButton;
            CancelButton = _cancelButton;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            Icon = AppIcons.App;
            ClientSize = new Size(620, 360);
            Tag = "MsgConfirmDeleteTitle";

            var list = new List<OverlayItem>(items);
            foreach (var item in list)
            {
                _listBox.Items.Add(RegistryService.GetRootName(item.Root) + @"\..." + item.Name);
            }

            _titleLabel.Text = Localizer.Format("MsgConfirmDelete", list.Count);

            Localizer.LanguageChanged += OnLanguageChanged;
            ApplyTexts();
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
