using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;
using ShellIconOverlayManager.Services;

namespace ShellIconOverlayManager.Models
{
    /// <summary>
    /// ShellIconOverlayIdentifiers 下的一个子健。
    /// </summary>
    public sealed class OverlayItem
    {
        public RootKind Root { get; set; }

        /// <summary>子健名称。</summary>
        public string Name { get; set; }

        /// <summary>默认值（通常是 CLSID）。</summary>
        public string Value { get; set; }

        public RegistryValueKind ValueKind { get; set; }

        /// <summary>按名称排序的序号，Windows 资源管理器按该顺序取前 15 个。</summary>
        public int Order { get; set; }

        /// <summary>该子健下的全部键值（含默认值）。</summary>
        public List<RegValueSnapshot> Values { get; set; }

        public OverlayItem()
        {
            Name = string.Empty;
            Value = string.Empty;
            ValueKind = RegistryValueKind.String;
            Values = new List<RegValueSnapshot>();
        }

        public string ValueKindName
        {
            get
            {
                switch (ValueKind)
                {
                    case RegistryValueKind.ExpandString: return "REG_EXPAND_SZ";
                    case RegistryValueKind.Binary: return "REG_BINARY";
                    case RegistryValueKind.DWord: return "REG_DWORD";
                    case RegistryValueKind.MultiString: return "REG_MULTI_SZ";
                    case RegistryValueKind.QWord: return "REG_QWORD";
                    case RegistryValueKind.String: return "REG_SZ";
                    default: return ValueKind.ToString();
                }
            }
        }

        public RegValueSnapshot DefaultValue
        {
            get { return Values == null ? null : Values.FirstOrDefault(v => string.IsNullOrEmpty(v.Name)); }
        }

        /// <summary>完整注册表路径（界面显示用）。</summary>
        public string FullPath
        {
            get { return RegistryService.GetRootDisplayPath(Root) + "\\" + Name; }
        }
    }
}
