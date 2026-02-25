# Screen Time Controller

[English](README%20eng.md) | 简体中文

Windows 11 屏幕时间管控器应用程序，帮助您控制和管理屏幕使用时间。

## 功能特性

- **每日屏幕时间限制**：为每天设置不同的屏幕时间限制
- **应用级别时间记录**：区分不同应用的使用时间，详细记录每个有窗口应用的使用时间
- **应用图标显示**：显示应用程序图标，直观展示各应用使用情况
- **选项卡界面**：Overview选项卡显示总览，Applications选项卡显示应用详情
- **时间限制警告**：在达到时间限制前5分钟发送警告通知
- **屏幕锁定**：当达到时间限制时自动锁定屏幕
- **密码保护**：设置密码保护，防止未经授权修改设置
- **系统托盘集成**：最小化到系统托盘，不干扰正常工作
- **实时时间跟踪**：实时显示已使用时间和剩余时间
- **单实例运行**：防止多个实例同时运行
- **进程守护**：Watchdog守护进程，防止程序被强制关闭
- **Windows服务**：后台服务持续计时，GUI关闭后仍可运行
- **安全性增强**：DPAPI加密、注册表备份、文件监控等多重保护

## 系统要求

- Windows 10 1607 及以上
- Windows Server 2012 R2 SP1 及以上
- 支持.NET运行时 5.0~10.0

## 安装与运行

### 直接运行
1. 将 `ScreenTimeController` 文件夹复制到目标位置
2. 双击 `ScreenTimeController.exe` 运行应用程序
3. 确保 `Resources\AppIcon.ico` 文件存在
4. 确保 `ScreenTimeControllerWatchdog.exe` 及其依赖文件存在

### 从源码构建
1. 克隆或下载项目代码
2. 安装 .NET 5.0 SDK 或更高版本
3. 打开终端，导航到项目目录
4. 运行构建命令：
   ```powershell
   dotnet build --configuration Release
   ```
5. 在 `bin\Release\net5.0-windows\` 文件夹中找到生成的可执行文件

## 使用方法

### 首次启动
1. 应用程序将自动启动并最小化到系统托盘
2. 双击系统托盘图标打开主窗口
3. 点击 "Settings" 按钮打开设置窗口
4. 设置每天的屏幕时间限制
5. 设置密码保护（可选）
6. 点击 "OK" 保存设置

### 日常使用
- **查看状态**：双击系统托盘图标打开主窗口
  - **Overview选项卡**：显示每日限制、已用时间、剩余时间和进度条
  - **Applications选项卡**：显示各应用使用时间及图标
- **修改设置**：点击 "Settings" 按钮修改设置（需要输入密码）
- **最小化**：点击最小化按钮或关闭按钮，应用将隐藏到系统托盘
- **退出应用**：右键点击系统托盘图标，选择 "Exit"

### 时间限制设置
- 可以为每天设置不同的时间限制
- 使用 "Apply to All Days" 按钮将当前设置应用到所有天
- 时间限制以小时和分钟为单位
- 支持设置、修改或删除密码保护

## 进程守护 (Watchdog)

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

### 核心组件
- **MainForm**：主应用程序窗口，使用TabControl分离总览和应用列表
- **SettingsForm**：设置窗口，用于配置每天的时间限制和密码
- **PasswordForm**：密码输入窗口，验证用户身份
- **ChangePasswordForm**：密码修改窗口
- **TimeTracker**：时间跟踪器，记录屏幕使用时间和应用级别使用时间
- **SettingsManager**：设置管理器，保存和加载应用程序设置
- **WindowHelper**：Windows API 包装器，用于获取窗口信息和锁定屏幕
- **Watchdog**：守护进程管理器，启动和监控Watchdog进程

### 数据存储
- 设置保存在 `%AppData%\ScreenTimeController\settings.txt` 文件中
- 总时间使用记录保存在 `%AppData%\ScreenTimeController\usage.txt` 文件中
- 应用级别时间使用记录保存在 `%AppData%\ScreenTimeController\app_usage.txt` 文件中
- 密码使用 PBKDF2 + 随机盐值加密存储

### 关键技术
- Windows Forms 应用程序开发
- Windows API 集成 (user32.dll)
- 时间跟踪和管理
- 系统托盘集成 (NotifyIcon)
- 单实例检测 (Mutex)
- 进程守护与相互监控
- 图标缓存机制
- 线程安全设计
- DPAPI 数据加密
- 注册表备份存储

## 故障排除

### 应用程序无法启动
1. 确保应用程序文件完整
2. 检查应用程序权限
3. 确保已安装 .NET 5.0 或更高版本运行时
4. 查看 Windows 事件查看器中的错误信息

### 时间限制不生效
1. 检查设置是否正确保存
2. 确保应用程序正在运行（在系统托盘中可见）
3. 重启应用程序

### 忘记密码
1. 关闭应用程序
2. 删除 `%AppData%\ScreenTimeController\settings.txt` 文件
3. 重新启动应用程序，设置新密码

### 重置使用时间数据
1. 关闭应用程序
2. 删除 `%AppData%\ScreenTimeController\usage.txt` 文件
3. 删除 `%AppData%\ScreenTimeController\app_usage.txt` 文件
4. 重新启动应用程序

### 应用图标不显示
1. 确保 `Resources\AppIcon.ico` 文件存在
2. 检查图标文件是否损坏
3. 应用程序会自动使用系统默认图标作为后备

### Watchdog不工作
1. 确保 `ScreenTimeControllerWatchdog.exe` 及其依赖文件存在
2. 检查日志文件 `%AppData%\ScreenTimeController\watchdog_external.log`
3. 确保没有多个Watchdog实例在运行

## 许可证

Apache-2.0 License

## 更新日志

### v1.3.0 (安全性更新)
- **密码安全性增强**
  - 使用PBKDF2算法替代简单SHA256哈希
  - 添加随机盐值，每个密码使用唯一盐值
  - 100,000次迭代增加破解难度
- **账户锁定机制**
  - 5次失败尝试后锁定账户15分钟
  - 锁定状态持久化保存
- **IPC通信安全**
  - 添加认证令牌验证
  - 所有IPC命令需要有效令牌
- **数据保护增强**
  - 使用Windows DPAPI加密数据
  - 注册表备份存储
  - 文件监控防篡改
  - 文件隐藏和权限限制
- **Windows服务支持**
  - 后台服务持续计时
  - GUI关闭后服务继续运行
  - 自动启动服务
- 添加10种语言的锁定提示文本

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
