using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace ShellIconOverlayManager.Ui
{
    /// <summary>
    /// 统一提供程序图标。
    /// 优先从程序集内嵌的 ShellIcon-OverlayManager.ico 取（含 16~256 各尺寸帧，
    /// 高 DPI 下不失真）；取不到时回退到 exe 已关联的系统图标。
    /// </summary>
    internal static class AppIcons
    {
        private const string AppIconResource = "ShellIconOverlayManager.AppIcon.ico";

        private static byte[] _iconBytes;

        /// <summary>默认 32x32 图标（窗体标题栏、托盘等常规用途）。</summary>
        public static Icon App
        {
            get { return GetApp(32); }
        }

        /// <summary>按指定像素尺寸取图标。</summary>
        public static Icon GetApp(int size)
        {
            EnsureIconBytes();

            if (_iconBytes != null)
            {
                try
                {
                    using (var memory = new MemoryStream(_iconBytes, false))
                    {
                        return new Icon(memory, size, size);
                    }
                }
                catch (Exception)
                {
                    // 尺寸不受支持时继续回退
                }
            }

            try
            {
                var exePath = Application.ExecutablePath;
                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                {
                    var icon = Icon.ExtractAssociatedIcon(exePath);
                    if (icon != null)
                    {
                        return (Icon)icon.Clone();
                    }
                }
            }
            catch (Exception)
            {
                // 提取失败时退回系统图标
            }

            return Icon.FromHandle(SystemIcons.Application.Handle);
        }

        /// <summary>
        /// 按显示尺寸取最佳帧的图片。
        /// 直接从 ico 内取出对应帧交给 GDI+ 解码——.NET Framework 无法把
        /// PNG 压缩帧的 Icon 转成 Bitmap（Icon.ToBitmap / DrawIcon 在 >48px 时会崩），
        /// 绕开 Icon 类即可避免该问题，同时保留原始高清帧。
        /// </summary>
        public static Image GetAppImage(int size)
        {
            EnsureIconBytes();

            if (_iconBytes != null)
            {
                try
                {
                    int offset;
                    int length;
                    if (TryFindBestFrame(size, out offset, out length))
                    {
                        using (var memory = new MemoryStream(_iconBytes, offset, length, false))
                        using (var source = Image.FromStream(memory, true, true))
                        {
                            return new Bitmap(source);
                        }
                    }
                }
                catch (Exception)
                {
                    // 解析失败时回退到 Icon 方式
                }
            }

            var icon = GetApp(Math.Min(size, 48));
            try
            {
                return icon.ToBitmap();
            }
            finally
            {
                icon.Dispose();
            }
        }

        /// <summary>在 ico 目录里找出宽度不小于显示尺寸的最小帧（找不到则取最大帧）。</summary>
        private static bool TryFindBestFrame(int size, out int offset, out int length)
        {
            offset = 0;
            length = 0;

            if (_iconBytes.Length < 6)
            {
                return false;
            }

            var count = _iconBytes[4] | (_iconBytes[5] << 8);
            var bestWidth = -1;

            for (var i = 0; i < count; i++)
            {
                var entry = 6 + i * 16;
                if (entry + 16 > _iconBytes.Length)
                {
                    return false;
                }

                var width = (int)_iconBytes[entry];
                if (width == 0)
                {
                    width = 256;
                }

                var frameLength = _iconBytes[entry + 8]
                                  | (_iconBytes[entry + 9] << 8)
                                  | (_iconBytes[entry + 10] << 16)
                                  | (_iconBytes[entry + 11] << 24);
                var frameOffset = _iconBytes[entry + 12]
                                  | (_iconBytes[entry + 13] << 8)
                                  | (_iconBytes[entry + 14] << 16)
                                  | (_iconBytes[entry + 15] << 24);

                if (frameOffset < 0 || frameLength <= 0 || frameOffset + frameLength > _iconBytes.Length)
                {
                    continue;
                }

                var better = bestWidth < 0
                             || (bestWidth < size && width > bestWidth)
                             || (width >= size && width < bestWidth);
                if (!better)
                {
                    continue;
                }

                bestWidth = width;
                offset = frameOffset;
                length = frameLength;
            }

            return bestWidth > 0;
        }

        private static void EnsureIconBytes()
        {
            if (_iconBytes != null)
            {
                return;
            }

            try
            {
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(AppIconResource))
                {
                    if (stream == null)
                    {
                        return;
                    }

                    using (var memory = new MemoryStream())
                    {
                        stream.CopyTo(memory);
                        _iconBytes = memory.ToArray();
                    }
                }
            }
            catch (Exception)
            {
                _iconBytes = null;
            }
        }
    }
}
