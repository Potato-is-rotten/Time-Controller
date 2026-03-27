# [中文](README.md) | [English](README%20eng.md)

# Screen Time Controller

一个 Windows 屏幕时间管理工具，帮助您监控和控制电脑使用时间。

## 功能特性

- **时间追踪** - 自动追踪每日屏幕使用时间
- **时间限制** - 设置每日使用时间上限
- **密码保护** - 使用密码锁定设置，防止绕过
- **数据保护** - 保护配置数据，防止篡改
- **异常退出检测** - 检测非正常关闭并记录
- **多语言支持** - 支持中文和英文界面
- **便携版本** - 无需安装，可直接运行

## 系统要求

- Windows 10 或更高版本
- .NET 8.0 Runtime

## 项目结构

```
Time-Controller/
├── ScreenTimeController/     # 主应用程序
├── ProtectionService/        # 数据保护服务
├── WatchdogMonitor/          # 监控守护进程
└── ScreenTimeController.sln  # 解决方案文件
```

## 构建方法

1. 安装 .NET 8.0 SDK
2. 克隆仓库：
   ```bash
   git clone https://github.com/Potato-is-rotten/Time-Controller.git
   ```
3. 构建项目：
   ```bash
   cd Time-Controller
   dotnet build ScreenTimeController.sln
   ```

## 使用方法

1. 运行 `ScreenTimeController.exe`
2. 首次运行时设置管理员密码
3. 在设置中配置每日时间限制
4. 应用程序将在后台监控使用时间

## 许可证

本项目采用 Apache 许可证 - 详见 [LICENSE](LICENSE) 文件。

## 作者

Potato-is-rotten
