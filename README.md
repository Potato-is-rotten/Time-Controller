# Language
English:
[Here](https://github.com/Potato-is-rotten/Time-Controller/blob/main/README%20eng.md)

# Screen Time Controller

Windows 11 / macOS 屏幕时间管控器应用程序，帮助您控制和管理屏幕使用时间。

## 支持平台

- **Windows**: Windows 10 1607 及以上 (x64, ARM64)
- **macOS**: macOS 12.0 (Monterey) 及以上

## 功能特性

- **每日屏幕时间限制**：为每天设置不同的屏幕时间限制
- **应用级别时间记录**：区分不同应用的使用时间，详细记录每个有窗口应用的使用时间
- **应用图标显示**：显示应用程序图标，直观展示各应用使用情况
- **选项卡界面**：Overview选项卡显示总览，Applications选项卡显示应用详情
- **时间限制警告**：在达到时间限制前5分钟发送警告通知
- **屏幕锁定**：当达到时间限制时自动锁定屏幕
- **密码保护**：设置密码保护，防止未经授权修改设置
- **系统托盘/菜单栏集成**：最小化到系统托盘/菜单栏，不干扰正常工作
- **实时时间跟踪**：实时显示已使用时间和剩余时间
- **单实例运行**：防止多个实例同时运行
- **进程守护** (Windows): Watchdog守护进程，防止程序被强制关闭

## 系统要求

### Windows
- Windows 10 1607 及以上
- Windows Server 2012 R2 SP1 及以上
- 支持.NET运行时 5.0~10.0

### macOS
- macOS 12.0 (Monterey) 及以上
- Xcode 14.0 及以上（用于构建）

## 安装与运行

### Windows

#### 直接运行
1. 下载 `ScreenTimeController-win-x64.zip` 或 `ScreenTimeController-win-arm64.zip`
2. 解压到目标位置
3. 双击 `ScreenTimeController.exe` 运行应用程序
4. 确保 `Resources\AppIcon.ico` 文件存在
5. 确保 `ScreenTimeControllerWatchdog.exe` 及其依赖文件存在

#### 从源码构建
1. 克隆或下载项目代码
2. 安装 .NET 5.0 SDK 或更高版本
3. 打开终端，导航到项目目录
4. 运行构建命令：
   ```powershell
   dotnet build --configuration Release
   ```
5. 在 `bin\Release\net5.0-windows\` 文件夹中找到生成的可执行文件

### macOS

#### 使用DMG安装包（推荐）
1. 下载 `ScreenTimeController-macOS.zip`
2. 解压后打开DMG文件
3. 将 `ScreenTimeController.app` 拖拽到 `Applications` 文件夹
4. 打开 `Applications` 文件夹，双击运行应用程序

#### 从源码构建
1. 克隆或下载项目代码
2. 打开 `ScreenTimeController-macOS/ScreenTimeController.xcodeproj`
3. 按 `Cmd+R` 运行项目

## 使用方法

### 首次启动
1. 应用程序将自动启动并最小化到系统托盘/菜单栏
2. 点击系统托盘/菜单栏图标打开主窗口
3. 点击 "Settings" 按钮打开设置窗口
4. 设置每天的屏幕时间限制
5. 设置密码保护（可选）
6. 点击 "OK" 保存设置

### 日常使用
- **查看状态**：点击系统托盘/菜单栏图标打开主窗口
  - **Overview选项卡**：显示每日限制、已用时间、剩余时间和进度条
  - **Applications选项卡**：显示各应用使用时间及图标
- **修改设置**：点击 "Settings" 按钮修改设置（需要输入密码）
- **退出应用**：右键点击系统托盘/菜单栏图标，选择 "Exit"

### 时间限制设置
- 可以为每天设置不同的时间限制
- 使用 "Apply to All Days" 按钮将当前设置应用到所有天
- 时间限制以小时和分钟为单位
- 支持设置、修改或删除密码保护

## 进程守护 (Watchdog) - Windows Only

Screen Time Controller 包含一个 Watchdog 守护进程，用于保护主程序不被强制关闭：

### 功能特点
- **自动重启**：当主程序被任务管理器关闭时，Watchdog会自动重启主程序
- **相互监控**：主程序也会监控Watchdog进程，确保两者都在运行
- **单实例限制**：Watchdog只能运行一个实例
- **快速响应**：500ms检测间隔，快速响应进程终止

### 文件说明
- `ScreenTimeController.exe` - 主程序
- `ScreenTimeControllerWatchdog.exe` - Watchdog守护进程
- `ScreenTimeController.dll` - 主程序依赖
- `ScreenTimeControllerWatchdog.dll` - Watchdog依赖
- `*.runtimeconfig.json` - 运行时配置文件
- `*.deps.json` - 依赖配置文件

### 日志文件
- `%AppData%\ScreenTimeController\watchdog_monitor.log` - 主程序监控日志
- `%AppData%\ScreenTimeController\watchdog_external.log` - Watchdog日志

## 技术实现

### Windows 核心组件
- **MainForm**：主应用程序窗口，使用TabControl分离总览和应用列表
- **SettingsForm**：设置窗口，用于配置每天的时间限制和密码
- **PasswordForm**：密码输入窗口，验证用户身份
- **ChangePasswordForm**：密码修改窗口
- **TimeTracker**：时间跟踪器，记录屏幕使用时间和应用级别使用时间
- **SettingsManager**：设置管理器，保存和加载应用程序设置
- **WindowHelper**：Windows API 包装器，用于获取窗口信息和锁定屏幕
- **Watchdog**：守护进程管理器，启动和监控Watchdog进程

### macOS 核心组件
- **ScreenTimeControllerApp**：应用程序入口点
- **AppDelegate**：应用程序代理，管理菜单栏和生命周期
- **MainView**：主界面，包含Overview和Applications选项卡
- **SettingsView**：设置窗口
- **TimeTracker**：时间跟踪器
- **SettingsManager**：设置管理器

### 数据存储
- 设置保存在 `%AppData%\ScreenTimeController\` (Windows) 或 `~/Library/Application Support/ScreenTimeController/` (macOS)
- 密码使用 SHA256 加密存储

## 故障排除

### 应用程序无法启动
1. 确保应用程序文件完整
2. 检查应用程序权限
3. 确保已安装 .NET 5.0 或更高版本运行时 (Windows)
4. 查看 Windows 事件查看器或 Console.app (macOS) 中的错误信息

### 时间限制不生效
1. 检查设置是否正确保存
2. 确保应用程序正在运行（在系统托盘/菜单栏中可见）
3. 重启应用程序

### 忘记密码
1. 关闭应用程序
2. 删除设置文件
3. 重新启动应用程序，设置新密码

### Watchdog不工作 (Windows)
1. 确保 `ScreenTimeControllerWatchdog.exe` 及其依赖文件存在
2. 检查日志文件 `%AppData%\ScreenTimeController\watchdog_external.log`
3. 确保没有多个Watchdog实例在运行

## 许可证

Apache-2.0 License

## 更新日志

### v2.2.0
- 添加macOS版本支持
- 支持多平台发布 (Windows x64, Windows ARM64, macOS)
- macOS版本支持菜单栏集成
- macOS版本支持DMG安装包
- 降低macOS系统要求到12.0 (Monterey)

### v1.3.0
- 添加Watchdog进程守护功能
- 实现主程序与Watchdog相互监控
- 添加Watchdog单实例限制
- 修复多个线程安全问题
- 优化进程检测间隔（500ms）
- 移除自包含发布，减小体积

### v1.2.0
- 使用TabControl分离Overview和Applications界面
- 添加应用程序图标显示功能
- 实现图标缓存机制，应用关闭后图标仍可显示
- 只计时有窗口的应用程序
- 添加单实例检测，防止多开
- 修复多个潜在bug和资源泄漏问题
- 优化UI布局和字体大小
- 改进线程安全性

### v1.1.0
- 添加应用级别时间记录功能
- 更改设置后今日使用时间不重置
- 优化系统托盘通知
- 添加密码保护功能

### v1.0.0
- 初始版本
- 实现基本的屏幕时间管控功能
- 添加系统托盘集成
- 支持每天不同的时间限制设置
- 添加5分钟警告通知
