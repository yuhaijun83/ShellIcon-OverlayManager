using System;
using System.Collections.Generic;
using System.Globalization;
using System.Resources;
using System.Windows.Forms;
using ShellIconOverlayManager.Data;

namespace ShellIconOverlayManager.Localization
{
    /// <summary>
    /// 多语言管理器：简体中文 / 繁体中文 / 日文 / 英文 / 韩文，运行时即时切换。
    /// </summary>
    public static class Localizer
    {
        private static readonly ResourceManager Resources =
            new ResourceManager("ShellIconOverlayManager.Resources.Strings", typeof(Localizer).Assembly);

        public static readonly string[] SupportedCultures = { "en", "zh-CN", "zh-TW", "ja-JP", "ko-KR" };

        private static readonly Dictionary<string, string> DisplayNames =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "en", "English" },
                { "zh-CN", "简体中文" },
                { "zh-TW", "繁體中文" },
                { "ja-JP", "日本語" },
                { "ko-KR", "한국어" }
            };

        /// <summary>当前语言名称（如 zh-CN）。</summary>
        public static string Current { get; private set; }

        /// <summary>当前语言对应的区域信息。</summary>
        public static CultureInfo Culture { get; private set; }

        /// <summary>语言切换后触发，窗体据此刷新界面文字。</summary>
        public static event EventHandler LanguageChanged;

        public static void Initialize()
        {
            SetCulture(AppDatabase.GetSetting("Language", DetectDefaultCulture()), false);
        }

        public static void SetCulture(string cultureName, bool persist = true)
        {
            var name = Normalize(cultureName);
            if (name == null)
            {
                name = "en";
            }

            CultureInfo culture;
            try
            {
                culture = CultureInfo.GetCultureInfo(name);
            }
            catch (CultureNotFoundException)
            {
                culture = CultureInfo.GetCultureInfo("en");
                name = "en";
            }

            Current = name;
            Culture = culture;
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.CurrentCulture = culture;

            if (persist)
            {
                try
                {
                    AppDatabase.SetSetting("Language", name);
                }
                catch (Exception)
                {
                    // 保存失败不影响界面切换
                }
            }

            var handler = LanguageChanged;
            if (handler != null)
            {
                handler(null, EventArgs.Empty);
            }
        }

        /// <summary>取指定语言的翻译文本，找不到时返回键名。</summary>
        public static string T(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            try
            {
                var text = Resources.GetString(key, Culture);
                return text ?? key;
            }
            catch (MissingManifestResourceException)
            {
                return key;
            }
        }

        public static string Format(string key, params object[] args)
        {
            try
            {
                return string.Format(Culture, T(key), args);
            }
            catch (FormatException)
            {
                return T(key);
            }
        }

        /// <summary>仅当资源中存在该键时才返回，避免误改非资源标记（如语言代码）。</summary>
        public static bool TryGet(string key, out string value)
        {
            value = null;
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            try
            {
                value = Resources.GetString(key, Culture);
            }
            catch (MissingManifestResourceException)
            {
                return false;
            }

            return value != null;
        }

        public static string CultureDisplayName(string cultureName)
        {
            string name;
            return DisplayNames.TryGetValue(cultureName, out name) ? name : cultureName;
        }

        /// <summary>根据系统语言推断默认语言。</summary>
        public static string DetectDefaultCulture()
        {
            var name = CultureInfo.CurrentCulture.Name;
            if (string.IsNullOrEmpty(name))
            {
                return "en";
            }

            if (name.StartsWith("ja", StringComparison.OrdinalIgnoreCase))
            {
                return "ja-JP";
            }

            if (name.StartsWith("ko", StringComparison.OrdinalIgnoreCase))
            {
                return "ko-KR";
            }

            if (name.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
            {
                return IsTraditionalChinese(name) ? "zh-TW" : "zh-CN";
            }

            return "en";
        }

        private static bool IsTraditionalChinese(string name)
        {
            return name.IndexOf("TW", StringComparison.OrdinalIgnoreCase) >= 0
                   || name.IndexOf("HK", StringComparison.OrdinalIgnoreCase) >= 0
                   || name.IndexOf("MO", StringComparison.OrdinalIgnoreCase) >= 0
                   || name.IndexOf("Hant", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string Normalize(string cultureName)
        {
            if (string.IsNullOrEmpty(cultureName))
            {
                return null;
            }

            foreach (var supported in SupportedCultures)
            {
                if (string.Equals(supported, cultureName, StringComparison.OrdinalIgnoreCase))
                {
                    return supported;
                }
            }

            return cultureName;
        }

        #region 界面文字应用

        /// <summary>递归设置控件文本：控件 Tag 保存资源键。</summary>
        public static void ApplyTo(Control control)
        {
            if (control == null)
            {
                return;
            }

            string text;
            var key = control.Tag as string;
            if (!string.IsNullOrEmpty(key) && TryGet(key, out text))
            {
                control.Text = text;
            }

            var grid = control as DataGridView;
            if (grid != null)
            {
                foreach (DataGridViewColumn column in grid.Columns)
                {
                    var columnKey = column.Tag as string;
                    if (!string.IsNullOrEmpty(columnKey) && TryGet(columnKey, out text))
                    {
                        column.HeaderText = text;
                    }
                }
            }

            var strip = control as ToolStrip;
            if (strip != null)
            {
                ApplyToMenu(strip.Items);
            }

            foreach (Control child in control.Controls)
            {
                ApplyTo(child);
            }
        }

        public static void ApplyToMenu(ToolStripItemCollection items)
        {
            if (items == null)
            {
                return;
            }

            foreach (ToolStripItem item in items)
            {
                string text;
                var key = item.Tag as string;
                if (!string.IsNullOrEmpty(key) && TryGet(key, out text))
                {
                    item.Text = text;
                }

                var dropDown = item as ToolStripDropDownItem;
                if (dropDown != null)
                {
                    ApplyToMenu(dropDown.DropDownItems);
                }
            }
        }

        /// <summary>刷新整个窗体（含菜单）的文字。</summary>
        public static void ApplyToForm(Form form)
        {
            ApplyTo(form);
            if (form.MainMenuStrip != null)
            {
                ApplyToMenu(form.MainMenuStrip.Items);
            }
        }

        #endregion
    }
}
