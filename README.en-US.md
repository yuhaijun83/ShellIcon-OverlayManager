# ShellIcon-OverlayManager
A management tool for the icon overlay (badge) display in Windows Explorer.

Windows allows only a limited number of icon overlay handlers and shows just the first 15 by default. When too many applications need overlays — Google Driver, One Drive, Resilio Sync, Baidu Netdisk, TortoiseSVN and so on — the overlays you really want may end up not being displayed, and you have to edit the registry by hand. This tool operates directly on the registry, avoiding all the hassle of manual editing. After making changes, restart Explorer or reboot the computer.

- HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\ShellIconOverlayIdentifiers
- HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Explorer\ShellIconOverlayIdentifiers

[简体中文](README.md) · [繁體中文](README.zh-TW.md) · [日本語](README.ja-JP.md) · **English** · [한국어](README.ko-KR.md)

【Build Environment】：

       Windows 11 Enterprise 26H2 (Cn) + Visusl Studio 2026 Enterprise + .Net FrameWork 4.8

【Requirements】：

   .Net FrameWork 4.7.2 or above must be installed (Windows 10 LTSC 2019/2021 ships with it; older systems need a manual install)

   Download Url: [.NET Framework 4.7.2 download page](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net472)

【Usage】：
  
       1.Supports five languages: Simplified Chinese, Traditional Chinese, Japanese, Korean and English.
       
       2.Registry subkeys can be deleted manually or renamed to adjust the display order. Deletion records are saved automatically so they can be restored later.
 
【Version History】：

      1.v26.10
        Initial release
