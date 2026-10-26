namespace ShellIconOverlayManager.Models
{
    /// <summary>
    /// 支持的注册表根键。
    /// </summary>
    public enum RootKind
    {
        /// <summary>HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\ShellIconOverlayIdentifiers</summary>
        LocalMachine = 0,

        /// <summary>HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Explorer\ShellIconOverlayIdentifiers</summary>
        Wow64Node = 1
    }

    /// <summary>
    /// 记录到 SQLite 的操作类型。
    /// </summary>
    public enum HistoryOperation
    {
        Delete = 0,
        Create = 1,
        Rename = 2,
        EditValue = 3
    }
}
