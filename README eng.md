# [中文](README.md) | [English](README%20eng.md)

# Screen Time Controller

A Windows screen time management tool to help you monitor and control computer usage time.

## Features

- **Time Tracking** - Automatically track daily screen usage time
- **Time Limits** - Set daily usage time limits
- **Password Protection** - Lock settings with password to prevent bypassing
- **Data Protection** - Protect configuration data from tampering
- **Abnormal Exit Detection** - Detect and record unexpected shutdowns
- **Multi-language Support** - Supports Chinese and English interface
- **Portable Version** - No installation required, runs directly

## System Requirements

- Windows 10 or later
- .NET 8.0 Runtime

## Project Structure

```
Time-Controller/
├── ScreenTimeController/     # Main application
├── ProtectionService/        # Data protection service
├── WatchdogMonitor/          # Watchdog monitor process
└── ScreenTimeController.sln  # Solution file
```

## Building

1. Install .NET 8.0 SDK
2. Clone the repository:
   ```bash
   git clone https://github.com/Potato-is-rotten/Time-Controller.git
   ```
3. Build the project:
   ```bash
   cd Time-Controller
   dotnet build ScreenTimeController.sln
   ```

## Usage

1. Run `ScreenTimeController.exe`
2. Set administrator password on first run
3. Configure daily time limits in settings
4. The application will monitor usage time in the background

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Author

Potato-is-rotten
