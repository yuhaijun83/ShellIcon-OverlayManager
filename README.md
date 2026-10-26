# ShellIcon-OverlayManager
一个资源管理器图标角标显示的管理程序。

Windows的图标覆盖处理程序名额有限，默认只显示前15个。当系统里Google Driver、One Drive、Resilio Sync、Baidu Netdisk、TortoiseSVN等需要显示角标的图标过多时，重要的想显示的角标无法被现实，需要手动修改一下注册表位置。此工具可直接操作注册表，避免了手动操作的各种繁琐。改后需重启资源管理器或电脑。

- HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\ShellIconOverlayIdentifiers
- HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Explorer\ShellIconOverlayIdentifiers

**简体中文** · [繁體中文](README.zh-TW.md) · [日本語](README.ja-JP.md) · [English](README.en-US.md) · [한국어](README.ko-KR.md)

【编译环境】：

       Windows 11 Enterprise 26H2 (Cn) + Visusl Studio 2026 Enterprise + .Net FrameWork 4.8

【运行要求】：

   需安装 .Net FrameWork 4.7.2 及以上（Windows 10 LTSC 2019/2021 系统自带；更老的系统需自行安装）

   Download Url: [.NET Framework 4.7.2 下载页面](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net472)

【使用说明】：
  
       1.对简体中文、繁体中文、日本语、韩国语、英语等五种语言文字提供支持。
       
       2.手动删除注册表子健，或改名以调整顺序。可自动保存删除记录用备恢复所用。
 
【版本更新】：

      1.v26.10
        初版发布
