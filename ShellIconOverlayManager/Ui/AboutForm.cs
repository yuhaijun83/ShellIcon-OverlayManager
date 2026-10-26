using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using ShellIconOverlayManager.Localization;
using ShellIconOverlayManager.Services;

namespace ShellIconOverlayManager.Ui
{
    /// <summary>
    /// 关于窗体：上部左上角显示程序图标、右侧显示标题与版本号；
    /// 下部居中显示开源仓库链接，点击用默认浏览器打开。
    /// 控件位置与尺寸全部由 OnScalingApplied 精确计算（不使用停靠与自动布局，
    /// 避免与 DPI 缩放器互相覆盖导致文字被压缩）。
    /// </summary>
    public sealed class AboutForm : DpiAwareForm
    {
        // 96 DPI（100%）下的边距，随 DPI 缩放
        private const int DesignPadding = 14;
        private const int DesignIconSize = 44;
        private const int DesignIconGap = 12;
        private const int DesignMinWidth = 380;

        /// <summary>项目开源仓库地址。</summary>
        public const string RepositoryUrl = "https://github.com/yuhaijun83/ShellIcon-OverlayManager";

        private readonly PictureBox _iconBox;
        private readonly Label _titleLabel;
        private readonly Label _versionLabel;
        private readonly Label _repoLabel;
        private readonly LinkLabel _repoLink;

        public AboutForm()
        {
            _iconBox = new PictureBox { SizeMode = PictureBoxSizeMode.Zoom };

            // 标题与版本号（上部右侧）
            _titleLabel = new Label
            {
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = Localizer.T("AppTitle")
            };
            _versionLabel = new Label
            {
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = AppVersion.Title
            };

            // 开源仓库标签与链接（下部居中）
            _repoLabel = new Label
            {
                Tag = "AboutRepository",
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter
            };
            _repoLink = new LinkLabel
            {
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = RepositoryUrl,
                LinkBehavior = LinkBehavior.HoverUnderline,
                LinkColor = Color.FromArgb(0, 84, 166)
            };
            _repoLink.Links.Add(0, RepositoryUrl.Length, RepositoryUrl);
            _repoLink.Click += (sender, e) => OpenRepository();

            Controls.Add(_iconBox);
            Controls.Add(_titleLabel);
            Controls.Add(_versionLabel);
            Controls.Add(_repoLabel);
            Controls.Add(_repoLink);

            Text = AppVersion.Title;
            Icon = AppIcons.App;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;

            // 具体位置与尺寸在 OnScalingApplied 里按当前字体计算
            ClientSize = new Size(DesignMinWidth, 200);

            Localizer.ApplyToForm(this);
        }

        /// <summary>缩放后按当前字体计算图标、标题、链接的位置尺寸与窗体大小。</summary>
        protected override void OnScalingApplied(float factor)
        {
            base.OnScalingApplied(factor);

            var padding = (int)Math.Round(DesignPadding * factor);
            var iconSize = (int)Math.Round(DesignIconSize * factor);
            var gap = (int)Math.Round(DesignIconGap * factor);

            // 字号不随 DPI 放大，行高按字体实际高度加少量留白；宽度多留 8px 余量，
            // 抵消不同文本量算方式的细微差异，保证文字不被截断
            var titleSize = new Size(
                MeasureText(_titleLabel.Text, _titleLabel.Font) + 8,
                _titleLabel.Font.Height + 2);
            var versionSize = new Size(
                MeasureText(_versionLabel.Text, _versionLabel.Font) + 8,
                _versionLabel.Font.Height + 2);
            var repoLabelHeight = _repoLabel.Font.Height + 4;
            var linkWidth = MeasureText(RepositoryUrl, _repoLink.Font) + 8;
            var linkHeight = _repoLink.Font.Height + 6;

            var textLeft = padding + iconSize + gap;
            var topNeeded = textLeft + Math.Max(titleSize.Width, versionSize.Width) + padding;
            var width = Math.Max(Math.Max(topNeeded, linkWidth + padding * 2 + 8), DesignMinWidth);

            // 上部：左侧图标 + 右侧标题/版本号
            _iconBox.Location = new Point(padding, padding);
            _iconBox.Size = new Size(iconSize, iconSize);
            if (_iconBox.Image != null)
            {
                _iconBox.Image.Dispose();
            }
            _iconBox.Image = AppIcons.GetAppImage(iconSize);

            _titleLabel.Location = new Point(textLeft, padding);
            _titleLabel.Size = titleSize;
            _versionLabel.Location = new Point(textLeft, padding + titleSize.Height + (int)Math.Round(4 * factor));
            _versionLabel.Size = versionSize;

            var topHeight = Math.Max(iconSize, titleSize.Height + versionSize.Height + (int)Math.Round(4 * factor));

            // 下部：仓库标签与链接，居中显示
            var repoTop = padding + topHeight + (int)Math.Round(16 * factor);
            _repoLabel.Location = new Point(padding, repoTop);
            _repoLabel.Size = new Size(width - padding * 2, repoLabelHeight);
            _repoLink.Location = new Point(padding, repoTop + repoLabelHeight);
            _repoLink.Size = new Size(width - padding * 2, linkHeight);

            ClientSize = new Size(width, repoTop + repoLabelHeight + linkHeight + padding);
        }

        private int MeasureText(string text, Font font)
        {
            using (var graphics = CreateGraphics())
            {
                return TextRenderer.MeasureText(graphics, text, font, Size.Empty, TextFormatFlags.NoPadding).Width;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _iconBox != null && _iconBox.Image != null)
            {
                _iconBox.Image.Dispose();
                _iconBox.Image = null;
            }

            base.Dispose(disposing);
        }

        /// <summary>用系统默认浏览器打开仓库地址。</summary>
        private void OpenRepository()
        {
            try
            {
                Process.Start(new ProcessStartInfo(RepositoryUrl) { UseShellExecute = true });
            }
            catch (Exception)
            {
                // 极少数环境没有关联 http 协议时，退回资源管理器，由其转交默认浏览器
                try
                {
                    Process.Start("explorer.exe", RepositoryUrl);
                }
                catch (Exception fallbackEx)
                {
                    MessageBox.Show(this, fallbackEx.Message, Localizer.T("MsgError"),
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
