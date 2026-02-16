# Language
English:
[Here](https://github.com/Potato-is-rotten/Time-Controller/blob/main/README%20eng.md)

# 紧急通知！！！
我们测试下来发现火绒会报TrojanSpy，经过VirusTotal的检查，只有火绒会报病毒。
<img width="1418" height="982" alt="Screenshot_2026-02-16_15-37-05" src="https://github.com/user-attachments/assets/06c71b5e-54d0-47b8-826e-c2abf7736ede" />

# Screen Time Controller

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
- **宽限时间**：到时间后输入密码宽限时间
- **全屏限制**：到达时间后开启需输入密码

## 系统要求

- Windows 10 1607 及以上
- Windows server 2012 R2 SP1 及以上
- 支持.NET运行时5.0~10.0

## 安装与运行

### 直接运行
1. 将 `ScreenTimeController` 文件夹复制到目标位置
2. 双击 `ScreenTimeController.exe` 运行应用程序

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

## 技术实现

### 核心组件
- **MainForm**：主应用程序窗口，使用TabControl分离总览和应用列表
- **SettingsForm**：设置窗口，用于配置每天的时间限制和密码
- **PasswordForm**：密码输入窗口，验证用户身份
- **ChangePasswordForm**：密码修改窗口
- **TimeTracker**：时间跟踪器，记录屏幕使用时间和应用级别使用时间
- **SettingsManager**：设置管理器，保存和加载应用程序设置
- **WindowHelper**：Windows API 包装器，用于获取窗口信息和锁定屏幕

### 数据存储
- 设置保存在 `%AppData%\ScreenTimeController\settings.txt` 文件中
- 总时间使用记录保存在 `%AppData%\ScreenTimeController\usage.txt` 文件中
- 应用级别时间使用记录保存在 `%AppData%\ScreenTimeController\app_usage.txt` 文件中
- 密码使用 SHA256 加密存储

### 关键技术
- Windows Forms 应用程序开发
- Windows API 集成 (user32.dll)
- 时间跟踪和管理
- 系统托盘集成 (NotifyIcon)
- 单实例检测 (Mutex)
- 自包含发布 (Self-contained deployment)
- 图标缓存机制
- 线程安全设计

## 故障排除

### 应用程序无法启动
1. 确保应用程序文件完整
2. 检查应用程序权限
3. 查看 Windows 事件查看器中的错误信息

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

## 许可证

Apache-2.0 Lincense


