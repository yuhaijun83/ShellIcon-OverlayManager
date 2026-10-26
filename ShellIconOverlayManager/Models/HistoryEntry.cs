using System;
using System.Collections.Generic;

namespace ShellIconOverlayManager.Models
{
    /// <summary>
    /// 保存在 SQLite 中的操作履历，用于回溯与恢复。
    /// </summary>
    public sealed class HistoryEntry
    {
        public long Id { get; set; }

        public RootKind Root { get; set; }

        /// <summary>操作涉及的子健名称（删除/新建时为最终名称）。</summary>
        public string KeyName { get; set; }

        public HistoryOperation Operation { get; set; }

        public string OldKeyName { get; set; }

        public string NewKeyName { get; set; }

        /// <summary>操作详情，例如旧值 -> 新值。</summary>
        public string Summary { get; set; }

        public DateTime OperatedAt { get; set; }

        public bool IsRestored { get; set; }

        public DateTime? RestoredAt { get; set; }

        /// <summary>操作前的键值快照，恢复时使用。</summary>
        public List<RegValueSnapshot> Values { get; set; }

        public HistoryEntry()
        {
            KeyName = string.Empty;
            Summary = string.Empty;
            Values = new List<RegValueSnapshot>();
        }
    }
}
