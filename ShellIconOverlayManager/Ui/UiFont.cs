using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ShellIconOverlayManager.Ui
{
    /// <summary>
    /// 界面内容字号：明细表格、页签、按钮排使用比系统默认（9pt）略大的字号，便于阅读。
    /// 该字号不随 DPI 缩放（与资源管理器一致），可通过"工具 → 字体大小"菜单调整，
    /// 选择会持久化到 SQLite，对所有窗体立即生效。
    /// </summary>
    internal static class UiFont
    {
        /// <summary>默认内容字号（磅）。</summary>
        public const float DefaultSize = 11f;

        /// <summary>字号档位（磅），与"字体大小"菜单一一对应。</summary>
        public static readonly float[] Sizes = { 10f, 11f, 13f, 15f };

        private static readonly List<WeakReference> _targets = new List<WeakReference>();

        /// <summary>当前内容字号（磅）。</summary>
        public static float ContentSize { get; private set; } = DefaultSize;

        /// <summary>设置内容字号（不立即刷新界面，随后调用 RefreshAll）。</summary>
        public static void SetContentSize(float size)
        {
            ContentSize = size <= 0 ? DefaultSize : size;
        }

        /// <summary>把控件字体改为内容字号（保留字体族与样式），并登记以便字号变化时刷新。</summary>
        public static void Apply(Control control)
        {
            control.Font = Create(control.Font);
            _targets.RemoveAll(reference => !reference.IsAlive);
            _targets.Add(new WeakReference(control));
        }

        /// <summary>字号变化后，把所有已登记控件刷成新字号。</summary>
        public static void RefreshAll()
        {
            _targets.RemoveAll(reference => !reference.IsAlive);
            foreach (var reference in _targets)
            {
                var control = (Control)reference.Target;
                if (control.IsDisposed)
                {
                    continue;
                }

                control.Font = Create(control.Font);
            }
        }

        /// <summary>按内容字号生成字体。</summary>
        public static Font Create(Font source)
        {
            return new Font(source.FontFamily, ContentSize, source.Style, source.Unit);
        }
    }
}
