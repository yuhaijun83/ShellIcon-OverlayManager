# ShellIcon-OverlayManager 资源管理器图标-覆盖管理器

## 开发要求
- 使用C#语言开发，在Visual Studio 2026的基础上构建，.net Framework最低要求支持4.7
- 工具要能够支持简体中文、繁体中文、日文、英文、韩文五种语言，通过系统托盘菜单自由切换。
- 工具的设置数据（比如语言的记忆），或者工具用户操作用的数据，要保存到SQLite中
- 窗体以及按钮等所有控件，要能够根据DPI自动计算，以保证在所有分辨率显示器上的清晰度显示

## 功能设计
- 分别显示下面两条注册表的子健内容
HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\ShellIconOverlayIdentifiers
HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Explorer\ShellIconOverlayIdentifiers
- 所有子健可以通过点击checkbox进行多个选择，并提供删除按钮。删除前要弹出确认删除按钮，确定好，可以删除所选择的子健
- 删除操作时，所删除的内容，要能够按照操作时间的顺序，及时保存到SQLite中，以便可以回溯
- 提供恢复删除的功能，根据保存到SQLite中的操作履历，可以手动恢复一个或多个删除过的内容。
- 要有能够修改子健和子健值的功能
- 标题栏要能够显示版本号，格式为：ShellIcon-OverlayManager v26.10
- SQLite的db文件放到应用程序的安装目录下，文件名为：ShellIcon-OverlayManager.db
- 五种语言的资源文件，统一放到一个文件夹下，文件名为：ShellIcon-OverlayManager.resources.dll
- 给应用程序设置图标，图标文件名为：ShellIcon-OverlayManager.ico
- Release发行版编译的时候，不要pdb文件
- Release发行版编译的时候，不要App.config文件
- Release发行版编译的时候，不要ico文件
- Release发行版编译的时候，把LICENSE文件和5个readme的md文件，放到安装目录的Readme文件夹下
- 关于窗体，上部左上角显示程序图标，上部右侧显示标题和版本号。下部中央显示 github opensource 的链接，用默认浏览器打开。
https://github.com/yuhaijun83/ShellIcon-OverlayManager
- 主窗体上添加一个按钮，名为重启资源管理器，点击后可自动重启资源管理器。
- 点击右上角关闭按钮，不要自动隐藏到系统托盘里。要提示是否真退出，点击确定可以关闭应用程序。
- 启动的时候，窗口默认应该在屏幕中间
