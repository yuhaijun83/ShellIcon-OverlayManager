# ShellIcon-OverlayManager
탐색기 아이콘 오버레이(배지) 표시를 관리하는 프로그램입니다.

Windows의 아이콘 오버레이 처리 프로그램은 자리가 제한되어 있어 기본적으로 앞의 15개만 표시됩니다. 시스템에 Google Driver, One Drive, Resilio Sync, Baidu Netdisk, TortoiseSVN 등 배지를 표시해야 하는 아이콘이 너무 많으면, 꼭 표시하고 싶은 중요한 배지가 표시되지 않아 레지스트리 위치를 직접 수정해야 합니다. 이 도구는 레지스트리를 직접 조작할 수 있어 수동 작업의 번거로움을 없애 줍니다. 변경 후에는 탐색기를 다시 시작하거나 컴퓨터를 재부팅해야 합니다.

- HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\ShellIconOverlayIdentifiers
- HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Explorer\ShellIconOverlayIdentifiers

[简体中文](README.md) · [繁體中文](README.zh-TW.md) · [日本語](README.ja-JP.md) · [English](README.en-US.md) · **한국어**

【빌드 환경】：

       Windows 11 Enterprise 26H2 (Cn) + Visusl Studio 2026 Enterprise + .Net FrameWork 4.8

【실행 요구 사항】：

   .Net FrameWork 4.7.2 이상이 설치되어 있어야 합니다 (Windows 10 LTSC 2019/2021에는 기본으로 포함되어 있음; 더 오래된 시스템은 직접 설치해야 함)

   Download Url: [.NET Framework 4.7.2 다운로드 페이지](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net472)

【사용 방법】：
  
       1.간체 중국어, 번체 중국어, 일본어, 한국어, 영어 등 5개 언어를 지원합니다.
       
       2.레지스트리 하위 키를 수동으로 삭제하거나 이름을 바꿔 표시 순서를 조정할 수 있습니다. 삭제한 기록은 자동으로 저장되어 나중에 복원하는 데 사용할 수 있습니다.
 
【버전 업데이트】：

      1.v26.10
         첫 버전 배포
