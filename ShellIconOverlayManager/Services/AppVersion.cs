using System;
using System.Globalization;

namespace ShellIconOverlayManager.Services
{
    /// <summary>
    /// 版本信息：标题栏显示格式为 "ShellIcon-OverlayManager v26.10"（年.月）。
    /// </summary>
    public static class AppVersion
    {
        public const string ProductName = "ShellIcon-OverlayManager";

        /// <summary>版本号，如 26.10。</summary>
        public static string Version
        {
            get { return DateTime.Now.ToString("yy.MM", CultureInfo.InvariantCulture); }
        }

        /// <summary>标题栏文字，如 ShellIcon-OverlayManager v26.10。</summary>
        public static string Title
        {
            get { return ProductName + " v" + Version; }
        }
    }
}
