# ShellIcon-OverlayManager
エクスプローラーのアイコン角標（オーバーレイ）表示を管理するプログラムです。

Windows のアイコンオーバーレイハンドラーには枠に限りがあり、既定では先頭の 15 個しか表示されません。システムに Google Driver、One Drive、Resilio Sync、Baidu Netdisk、TortoiseSVN など角標を表示するアイコンが多すぎると、本当に表示したい重要な角標が表示されなくなり、レジストリを手動で書き換えて順番を調整する必要があります。本ツールはレジストリを直接操作できるため、手動操作の煩雑さを回避できます。変更後はエクスプローラーの再起動、またはパソコンの再起動が必要です。

- HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\ShellIconOverlayIdentifiers
- HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Explorer\ShellIconOverlayIdentifiers

[简体中文](README.md) · [繁體中文](README.zh-TW.md) · **日本語** · [English](README.en-US.md) · [한국어](README.ko-KR.md)

【ビルド環境】：

       Windows 11 Enterprise 26H2 (Cn) + Visusl Studio 2026 Enterprise + .Net FrameWork 4.8

【動作要件】：

   .Net FrameWork 4.7.2 以上がインストールされている必要があります（Windows 10 LTSC 2019/2021 には標準搭載されています；より古いシステムでは手動でインストールしてください）

   Download Url: [.NET Framework 4.7.2 ダウンロードページ](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net472)

【使い方】：
  
       1.簡体字中国語、繁体字中国語、日本語、韓国語、英語の5つの言語に対応しています。
       
       2.レジストリのサブキーを手動で削除したり、名前を変更して順序を調整できます。削除した記録は自動的に保存され、復元に利用できます。
 
【バージョン履歴】：

      1.v26.10
        初版リリース
