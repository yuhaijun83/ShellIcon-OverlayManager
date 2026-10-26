using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ShellIconOverlayManager.Ui
{
    /// <summary>
    /// DPI 缩放器：以 96 DPI（100%）为设计基准，按当前显示器 DPI 重新计算
    /// 控件字体、尺寸、边距、表格列宽与分隔条位置，保证高分屏下清晰且不变形。
    /// </summary>
    public sealed class DpiScaler
    {
        private sealed class DesignInfo
        {
            public Control Control;
            public Size Size;
            public Point Location;
            public float FontSize;
            public Padding Margin;
            public Padding Padding;
        }

        private readonly List<DesignInfo> _designs = new List<DesignInfo>();
        private readonly List<KeyValuePair<DataGridViewColumn, int>> _gridColumns =
            new List<KeyValuePair<DataGridViewColumn, int>>();
        private readonly List<KeyValuePair<SplitContainer, int>> _splitters =
            new List<KeyValuePair<SplitContainer, int>>();

        private bool _captured;

        /// <summary>
        /// 字号是否随 DPI 放大。默认 false——与资源管理器等原生程序一致：
        /// 只放大布局尺寸，字号保持不变（由更高分辨率渲染得更清晰），
        /// 否则高 DPI 下文字会比系统其它程序大一圈。
        /// </summary>
        public bool ScaleFont { get; set; }

        /// <summary>记录设计基准（96 DPI）下的原始尺寸。</summary>
        public void Capture(Control root)
        {
            if (_captured)
            {
                return;
            }

            Walk(root);
            _captured = true;
        }

        private void Walk(Control control)
        {
            if (control == null)
            {
                return;
            }

            _designs.Add(new DesignInfo
            {
                Control = control,
                Size = control.Size,
                Location = control.Location,
                FontSize = control.Font != null ? control.Font.Size : 0f,
                Margin = control.Margin,
                Padding = control.Padding
            });

            var grid = control as DataGridView;
            if (grid != null)
            {
                foreach (DataGridViewColumn column in grid.Columns)
                {
                    _gridColumns.Add(new KeyValuePair<DataGridViewColumn, int>(column, column.Width));
                }
            }

            var splitter = control as SplitContainer;
            if (splitter != null)
            {
                _splitters.Add(new KeyValuePair<SplitContainer, int>(splitter, splitter.SplitterDistance));
            }

            foreach (Control child in control.Controls)
            {
                Walk(child);
            }
        }

        /// <summary>按缩放系数应用尺寸（基于设计基准值，可重复调用）。</summary>
        public void Apply(Control root, float factor)
        {
            if (!_captured)
            {
                Capture(root);
            }

            if (factor <= 0)
            {
                factor = 1f;
            }

            root.SuspendLayout();

            // 布局尺寸按系数缩放；字号默认保持不变，字体若有加粗等样式需重建时对拷即可
            var fontScale = ScaleFont ? factor : 1f;

            foreach (var design in _designs)
            {
                var control = design.Control;

                if (control is Form)
                {
                    ((Form)control).ClientSize = ScaleSize(design.Size, factor);
                }
                else
                {
                    control.Size = ScaleSize(design.Size, factor);
                    control.Location = ScalePoint(design.Location, factor);
                }

                control.Margin = ScalePadding(design.Margin, factor);
                control.Padding = ScalePadding(design.Padding, factor);

                if (design.FontSize > 0 && control.Font != null && Math.Abs(fontScale - 1f) > 0.01f)
                {
                    control.Font = new Font(
                        control.Font.FontFamily,
                        design.FontSize * fontScale,
                        control.Font.Style,
                        control.Font.Unit);
                }
            }

            foreach (var pair in _gridColumns)
            {
                var column = pair.Key;
                if (column.AutoSizeMode == DataGridViewAutoSizeColumnMode.Fill)
                {
                    continue;
                }

                column.Width = (int)Math.Round(pair.Value * factor);
            }

            foreach (var pair in _splitters)
            {
                var container = pair.Key;
                if (container.Orientation == Orientation.Vertical)
                {
                    var max = container.Width - container.Panel2MinSize - container.SplitterWidth;
                    var distance = (int)Math.Round(pair.Value * factor);
                    if (max > container.Panel1MinSize)
                    {
                        container.SplitterDistance = Math.Min(Math.Max(distance, container.Panel1MinSize), max);
                    }
                }
            }

            root.ResumeLayout(true);
        }

        /// <summary>获取当前显示器相对 96 DPI 的缩放系数。</summary>
        public static float GetFactor(Control control)
        {
            try
            {
                using (var graphics = control.CreateGraphics())
                {
                    var dpi = graphics.DpiX;
                    return dpi <= 0 ? 1f : Math.Max(1f, dpi / 96f);
                }
            }
            catch (Exception)
            {
                return 1f;
            }
        }

        private static Size ScaleSize(Size size, float factor)
        {
            return new Size((int)Math.Round(size.Width * factor), (int)Math.Round(size.Height * factor));
        }

        private static Point ScalePoint(Point point, float factor)
        {
            return new Point((int)Math.Round(point.X * factor), (int)Math.Round(point.Y * factor));
        }

        private static Padding ScalePadding(Padding padding, float factor)
        {
            return new Padding(
                (int)Math.Round(padding.Left * factor),
                (int)Math.Round(padding.Top * factor),
                (int)Math.Round(padding.Right * factor),
                (int)Math.Round(padding.Bottom * factor));
        }
    }

    /// <summary>
    /// 所有窗体的基类：自动完成 DPI 计算与缩放（含跨屏拖动时的 DPI 变化）。
    /// </summary>
    public class DpiAwareForm : Form
    {
        private const int WmDpiChanged = 0x02E0;

        private readonly DpiScaler _scaler = new DpiScaler();
        private float _currentFactor = 1f;
        private bool _startCentered;

        protected DpiAwareForm()
        {
            // 缩放完全由 DpiScaler 计算，禁用 WinForms 的自动缩放以免重复放大
            AutoScaleMode = AutoScaleMode.None;
        }

        protected override void OnLoad(EventArgs e)
        {
            ApplyScaling(this);
            base.OnLoad(e);

            // 缩放会改变窗体尺寸，而 CenterScreen 的位置是按缩放前的尺寸算好的，
            // 尺寸变大后窗口会偏向左上，所以这里按最终尺寸重新居中一次。
            // 只在首次显示时生效，之后用户拖动、跨屏 DPI 变化都不再强制居中。
            if (!_startCentered && StartPosition == FormStartPosition.CenterScreen)
            {
                _startCentered = true;
                CenterToScreen();
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmDpiChanged)
            {
                var dpi = (int)(m.WParam.ToInt64() & 0xFFFF);
                base.WndProc(ref m);
                ApplyScaling(dpi <= 0 ? 1f : dpi / 96f);
                return;
            }

            base.WndProc(ref m);
        }

        private void ApplyScaling(Form form)
        {
            ApplyScaling(DpiScaler.GetFactor(form));
        }

        private void ApplyScaling(float factor)
        {
            if (factor <= 0)
            {
                factor = 1f;
            }

            _currentFactor = factor;
            SuspendLayout();
            _scaler.Apply(this, factor);
            ResumeLayout(true);

            OnScalingApplied(factor);
        }

        /// <summary>当前缩放系数（1 = 96 DPI）。</summary>
        protected float CurrentFactor
        {
            get { return _currentFactor; }
        }

        /// <summary>
        /// 按当前缩放系数重新应用一次缩放（含字号相关的行高、列宽、按钮排等度量）。
        /// 供内容字号等设置变化后立即刷新已打开的窗体。
        /// </summary>
        public void RefreshScaling()
        {
            ApplyScaling(_currentFactor);
        }

        /// <summary>
        /// 缩放完成后调用：子类可在此调整那些由字体决定的尺寸
        /// （如表格行高、列宽、窗体最小尺寸），保证文字不被截断。
        /// </summary>
        protected virtual void OnScalingApplied(float factor)
        {
        }
    }
}
