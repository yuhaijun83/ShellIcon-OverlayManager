using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;
using ShellIconOverlayManager.Models;

namespace ShellIconOverlayManager.Services
{
    /// <summary>
    /// 读写 ShellIconOverlayIdentifiers 的注册表服务。
    /// </summary>
    public static class RegistryService
    {
        public const string SubPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\ShellIconOverlayIdentifiers";
        public const string Wow64SubPath = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Explorer\ShellIconOverlayIdentifiers";

        /// <summary>两条路径都在 HKEY_LOCAL_MACHINE 下，分别对应 64 位与 32 位覆盖标识符。</summary>
        public static string GetSubPath(RootKind kind)
        {
            return kind == RootKind.Wow64Node ? Wow64SubPath : SubPath;
        }

        public const string HiveName = "HKEY_LOCAL_MACHINE";

        /// <summary>用于界面/履历展示的根键名称（区分 64 位与 WOW6432Node）。</summary>
        public static string GetRootName(RootKind kind)
        {
            return kind == RootKind.Wow64Node ? HiveName + " (WOW6432Node)" : HiveName;
        }

        public static string GetRootDisplayPath(RootKind kind)
        {
            return HiveName + "\\" + GetSubPath(kind);
        }

        private static RegistryHive GetHive(RootKind kind)
        {
            return RegistryHive.LocalMachine;
        }

        /// <summary>
        /// 64 位系统上使用 64 位视图，保证看到的是资源管理器（64 位）实际使用的键值。
        /// </summary>
        private static RegistryView View
        {
            get { return Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32; }
        }

        private static RegistryKey OpenHive(RootKind kind)
        {
            return RegistryKey.OpenBaseKey(GetHive(kind), View);
        }

        /// <summary>打开根键；writable 为 true 且不存在时自动创建。</summary>
        public static RegistryKey OpenRoot(RootKind kind, bool writable)
        {
            var hive = OpenHive(kind);
            var key = hive.OpenSubKey(GetSubPath(kind), writable);
            if (key == null && writable)
            {
                key = hive.CreateSubKey(GetSubPath(kind));
            }
            return key;
        }

        public static bool RootExists(RootKind kind)
        {
            using (var key = OpenRoot(kind, false))
            {
                return key != null;
            }
        }

        /// <summary>读取指定根键下的全部子健，按名称排序（与资源管理器优先级顺序一致）。</summary>
        public static List<OverlayItem> Load(RootKind kind)
        {
            var result = new List<OverlayItem>();

            using (var root = OpenRoot(kind, false))
            {
                if (root == null)
                {
                    return result;
                }

                var names = root.GetSubKeyNames();
                Array.Sort(names, StringComparer.OrdinalIgnoreCase);

                var order = 1;
                foreach (var name in names)
                {
                    var item = new OverlayItem { Root = kind, Name = name, Order = order++ };

                    using (var subKey = root.OpenSubKey(name, false))
                    {
                        if (subKey == null)
                        {
                            continue;
                        }

                        item.Values = Snapshot(subKey);
                        var defaultValue = item.DefaultValue;
                        if (defaultValue != null)
                        {
                            item.Value = defaultValue.DisplayText;
                            item.ValueKind = defaultValue.ValueKind;
                        }
                    }

                    result.Add(item);
                }
            }

            return result;
        }

        public static List<RegValueSnapshot> Snapshot(RegistryKey key)
        {
            var list = new List<RegValueSnapshot>();
            foreach (var valueName in key.GetValueNames())
            {
                list.Add(RegValueSnapshot.FromKey(key, valueName));
            }
            return list;
        }

        /// <summary>读取指定子健的全部键值（删除前备份用）。</summary>
        public static List<RegValueSnapshot> SnapshotValues(RootKind kind, string keyName)
        {
            using (var root = OpenRoot(kind, false))
            {
                if (root == null)
                {
                    return new List<RegValueSnapshot>();
                }

                using (var subKey = root.OpenSubKey(keyName, false))
                {
                    return subKey == null ? new List<RegValueSnapshot>() : Snapshot(subKey);
                }
            }
        }

        public static bool Exists(RootKind kind, string keyName)
        {
            using (var root = OpenRoot(kind, false))
            {
                if (root == null)
                {
                    return false;
                }

                using (var subKey = root.OpenSubKey(keyName, false))
                {
                    return subKey != null;
                }
            }
        }

        public static void DeleteKey(RootKind kind, string keyName)
        {
            using (var root = OpenRoot(kind, true))
            {
                if (root == null)
                {
                    throw new InvalidOperationException(GetRootDisplayPath(kind));
                }

                root.DeleteSubKeyTree(keyName, false);
            }
        }

        public static void CreateKey(RootKind kind, string keyName, IEnumerable<RegValueSnapshot> values)
        {
            using (var root = OpenRoot(kind, true))
            {
                if (root == null)
                {
                    throw new InvalidOperationException(GetRootDisplayPath(kind));
                }

                using (var subKey = root.CreateSubKey(keyName))
                {
                    WriteValues(subKey, values);
                }
            }
        }

        /// <summary>重命名子健：创建新键 -&gt; 复制键值与子健 -&gt; 删除旧键。</summary>
        public static void RenameKey(RootKind kind, string oldName, string newName)
        {
            using (var root = OpenRoot(kind, true))
            {
                if (root == null)
                {
                    throw new InvalidOperationException(GetRootDisplayPath(kind));
                }

                using (var source = root.OpenSubKey(oldName, false))
                {
                    if (source == null)
                    {
                        throw new InvalidOperationException(oldName);
                    }

                    using (var target = root.CreateSubKey(newName))
                    {
                        CopyKey(source, target);
                    }
                }

                root.DeleteSubKeyTree(oldName, false);
            }
        }

        /// <summary>写入（覆盖）指定子健的键值集合。</summary>
        public static void WriteKeyValues(RootKind kind, string keyName, IEnumerable<RegValueSnapshot> values)
        {
            using (var root = OpenRoot(kind, true))
            {
                if (root == null)
                {
                    throw new InvalidOperationException(GetRootDisplayPath(kind));
                }

                using (var subKey = root.OpenSubKey(keyName, true) ?? root.CreateSubKey(keyName))
                {
                    WriteValues(subKey, values);
                }
            }
        }

        /// <summary>仅修改默认值（最常用）。</summary>
        public static void SetDefaultValue(RootKind kind, string keyName, RegistryValueKind valueKind, object data)
        {
            WriteKeyValues(kind, keyName, new[] { RegValueSnapshot.Create(string.Empty, valueKind, data) });
        }

        private static void WriteValues(RegistryKey key, IEnumerable<RegValueSnapshot> values)
        {
            if (values == null)
            {
                return;
            }

            foreach (var snapshot in values)
            {
                key.SetValue(snapshot.Name, snapshot.Deserialize(), snapshot.ValueKind);
            }
        }

        private static void CopyKey(RegistryKey source, RegistryKey target)
        {
            foreach (var valueName in source.GetValueNames())
            {
                target.SetValue(valueName, source.GetValue(valueName), source.GetValueKind(valueName));
            }

            foreach (var subName in source.GetSubKeyNames())
            {
                using (var subSource = source.OpenSubKey(subName, false))
                using (var subTarget = target.CreateSubKey(subName))
                {
                    if (subSource != null && subTarget != null)
                    {
                        CopyKey(subSource, subTarget);
                    }
                }
            }
        }

        /// <summary>
        /// 根据履历恢复注册表内容。
        /// </summary>
        public static bool Restore(HistoryEntry entry, out string message)
        {
            message = null;

            try
            {
                switch (entry.Operation)
                {
                    case HistoryOperation.Delete:
                        if (Exists(entry.Root, entry.KeyName))
                        {
                            message = Localization.Localizer.Format("MsgRestoreSkip", entry.KeyName);
                            return false;
                        }
                        CreateKey(entry.Root, entry.KeyName, entry.Values);
                        return true;

                    case HistoryOperation.Create:
                        if (!Exists(entry.Root, entry.KeyName))
                        {
                            return true;
                        }
                        DeleteKey(entry.Root, entry.KeyName);
                        return true;

                    case HistoryOperation.Rename:
                        if (string.IsNullOrEmpty(entry.OldKeyName) || string.IsNullOrEmpty(entry.NewKeyName))
                        {
                            message = Localization.Localizer.T("MsgRestoreSkip");
                            return false;
                        }
                        if (!Exists(entry.Root, entry.NewKeyName) || Exists(entry.Root, entry.OldKeyName))
                        {
                            message = Localization.Localizer.Format("MsgRestoreSkip", entry.NewKeyName);
                            return false;
                        }
                        RenameKey(entry.Root, entry.NewKeyName, entry.OldKeyName);
                        return true;

                    case HistoryOperation.EditValue:
                        if (!Exists(entry.Root, entry.KeyName))
                        {
                            message = Localization.Localizer.Format("MsgRestoreSkip", entry.KeyName);
                            return false;
                        }
                        WriteKeyValues(entry.Root, entry.KeyName, entry.Values);
                        return true;

                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }
    }
}
