# ShellIcon-OverlayManager
一個資源管理器圖標角標顯示的管理程式。

Windows 的圖標覆蓋處理程式名額有限，默認只顯示前 15 個。當系統裡 Google Driver、One Drive、Resilio Sync、Baidu Netdisk、TortoiseSVN 等需要顯示角標的圖標過多時，重要的、想顯示的角標就無法被顯示，需要手動修改註冊表的位置。此工具可直接操作註冊表，避免了手動操作的各種繁瑣。修改後需重啟資源管理器或電腦。

- HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\ShellIconOverlayIdentifiers
- HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Explorer\ShellIconOverlayIdentifiers

[简体中文](README.md) · **繁體中文** · [日本語](README.ja-JP.md) · [English](README.en-US.md) · [한국어](README.ko-KR.md)

【編譯環境】：

       Windows 11 Enterprise 26H2 (Cn) + Visusl Studio 2026 Enterprise + .Net FrameWork 4.8

【運行要求】：

   需安裝 .Net FrameWork 4.7.2 及以上（Windows 10 LTSC 2019/2021 系統自帶；更舊的系統需自行安裝）

   Download Url: [.NET Framework 4.7.2 下載頁面](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net472)

【使用說明】：
  
       1.對簡體中文、繁體中文、日本語、韓國語、英語等五種語言文字提供支援。
       
       2.手動刪除註冊表子鍵，或改名以調整順序。可自動保存刪除記錄用備恢復所用。
 
【版本更新】：

      1.v26.10
        初版發布
