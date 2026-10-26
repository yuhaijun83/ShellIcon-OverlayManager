using System;
using System.Globalization;
using System.Text;
using Microsoft.Win32;

namespace ShellIconOverlayManager.Models
{
    /// <summary>
    /// 注册表键值快照：用于把任意类型的注册表值序列化成文本保存到 SQLite，并在恢复时还原。
    /// </summary>
    public sealed class RegValueSnapshot
    {
        private const char MultiStringSeparator = '\u001F';

        /// <summary>值名称，空字符串表示“(默认)”。</summary>
        public string Name { get; set; }

        /// <summary>值类型（RegistryValueKind 的数值）。</summary>
        public int Kind { get; set; }

        /// <summary>序列化后的值数据。</summary>
        public string Data { get; set; }

        public RegistryValueKind ValueKind
        {
            get { return (RegistryValueKind)Kind; }
        }

        /// <summary>用于界面显示的文本。</summary>
        public string DisplayText
        {
            get
            {
                var text = Data ?? string.Empty;
                if (ValueKind == RegistryValueKind.MultiString)
                {
                    text = text.Replace(MultiStringSeparator, ' ');
                }
                return text;
            }
        }

        public static RegValueSnapshot FromKey(RegistryKey key, string valueName)
        {
            var kind = key.GetValueKind(valueName);
            return new RegValueSnapshot
            {
                Name = valueName ?? string.Empty,
                Kind = (int)kind,
                Data = Serialize(kind, key.GetValue(valueName))
            };
        }

        public static RegValueSnapshot Create(string name, RegistryValueKind kind, object data)
        {
            return new RegValueSnapshot
            {
                Name = name ?? string.Empty,
                Kind = (int)kind,
                Data = Serialize(kind, data)
            };
        }

        /// <summary>把注册表值对象转为可存入 SQLite 的文本。</summary>
        public static string Serialize(RegistryValueKind kind, object data)
        {
            if (data == null)
            {
                return string.Empty;
            }

            switch (kind)
            {
                case RegistryValueKind.Binary:
                    var bytes = data as byte[];
                    return bytes != null ? Convert.ToBase64String(bytes) : Convert.ToString(data, CultureInfo.InvariantCulture);

                case RegistryValueKind.MultiString:
                    var array = data as string[];
                    return array != null ? string.Join(MultiStringSeparator.ToString(), array) : Convert.ToString(data, CultureInfo.InvariantCulture);

                case RegistryValueKind.DWord:
                    return Convert.ToInt32(data).ToString("G", CultureInfo.InvariantCulture);

                case RegistryValueKind.QWord:
                    return Convert.ToInt64(data).ToString("G", CultureInfo.InvariantCulture);

                default:
                    return Convert.ToString(data, CultureInfo.InvariantCulture);
            }
        }

        /// <summary>把 SQLite 中的文本还原为注册表值对象。</summary>
        public object Deserialize()
        {
            var text = Data ?? string.Empty;

            switch (ValueKind)
            {
                case RegistryValueKind.Binary:
                    try
                    {
                        return Convert.FromBase64String(text);
                    }
                    catch (FormatException)
                    {
                        return new byte[0];
                    }

                case RegistryValueKind.MultiString:
                    return text.Length == 0 ? new string[0] : text.Split(MultiStringSeparator);

                case RegistryValueKind.DWord:
                    return ParseInteger(text, false);

                case RegistryValueKind.QWord:
                    return ParseInteger(text, true);

                default:
                    return text;
            }
        }

        private static object ParseInteger(string text, bool isQWord)
        {
            text = (text ?? string.Empty).Trim();
            bool hex = text.StartsWith("0x", StringComparison.OrdinalIgnoreCase);

            try
            {
                if (isQWord)
                {
                    return hex
                        ? Convert.ToInt64(text.Substring(2), 16)
                        : Convert.ToInt64(text, CultureInfo.InvariantCulture);
                }

                var value = hex
                    ? Convert.ToInt64(text.Substring(2), 16)
                    : Convert.ToInt64(text, CultureInfo.InvariantCulture);
                return unchecked((int)value);
            }
            catch (Exception)
            {
                return isQWord ? (object)0L : (object)0;
            }
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append(string.IsNullOrEmpty(Name) ? "(默认)" : Name);
            builder.Append(" = ");
            builder.Append(DisplayText);
            return builder.ToString();
        }
    }
}
