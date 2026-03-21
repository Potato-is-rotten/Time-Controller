# Screen Time Controller

English | [简体中文](README.md)

# Safety

<img width="1420" height="2234" alt="Screenshot_2026-03-13_10-12-26" src="https://github.com/user-attachments/assets/62e026fc-caa6-4689-8eed-9aeafc994749" />

A Windows 11 screen time management application that helps you control and manage your screen usage time.

## Features

- **Daily Screen Time Limits**: Set different screen time limits for each day
- **App-Level Time Tracking**: Distinguish usage time between different applications, detailed recording of each windowed application's usage time
- **Application Icon Display**: Show application icons for intuitive display of usage
- **Tab Interface**: Overview tab shows summary, Applications tab shows app details
- **Time Limit Warning**: Send warning notification 5 minutes before reaching time limit
- **Screen Lock**: Automatically lock screen when time limit is reached
- **Password Protection**: Set password protection to prevent unauthorized settings changes
- **System Tray Integration**: Minimize to system tray without disturbing normal work
- **Real-time Time Tracking**: Real-time display of used time and remaining time
- **Single Instance Running**: Prevent multiple instances from running simultaneously
- **Process Guardian**: Watchdog daemon prevents program from being forcibly closed
- **Windows Service**: Background service continues timing, runs even when GUI is closed
- **Enhanced Security**: DPAPI encryption, registry backup, file monitoring and more
- **Password Lockout**: Auto lock after 5 failed password attempts until next day 00:00 (toggleable)
- **Data Integrity Protection**: SHA256 hash verification, tamper detection and auto-recovery

## System Requirements

- Windows 10 1607 and above
- Windows Server 2012 R2 SP1 and above
- .NET Runtime 5.0~10.0 supported

## Installation and Running

### Direct Run
1. Extract `ScreenTimeController-win-x64.zip` to target location
2. Right-click `setup_protection.bat` and select "Run as administrator"
3. Or double-click `ScreenTimeController.exe` to run directly

### Build from Source
1. Clone or download the project code
2. Install .NET 5.0 SDK or higher
3. Open terminal, navigate to project directory
4. Run build command:
   ```powershell
   dotnet build --configuration Release
   ```
5. Find the generated executable in `bin\Release\net5.0-windows\` folder

## Usage

### First Start
1. Application will automatically start and minimize to system tray
2. Double-click system tray icon to open main window
3. Click "Settings" button to open settings window
4. Set screen time limits for each day
5. Set password protection (optional)
6. Click "OK" to save settings

### Daily Use
- **View Status**: Double-click system tray icon to open main window
  - **Overview Tab**: Shows daily limit, used time, remaining time and progress bar
  - **Applications Tab**: Shows each app's usage time and icon
- **Modify Settings**: Click "Settings" button to modify settings (password required)
- **Minimize**: Click minimize or close button, app will hide to system tray
- **Exit App**: Right-click system tray icon, select "Exit"

### Time Limit Settings
- Can set different time limits for each day
- Use "Apply to All Days" button to apply current settings to all days
- Time limits in hours and minutes
- Support setting, modifying or removing password protection

### Password Lockout Feature
- After 5 incorrect password attempts, account will be locked until next day 00:00
- Can be enabled/disabled in settings
- Correct password will reset the failed attempt counter

## Process Guardian (Watchdog)

Screen Time Controller includes a Watchdog daemon to protect the main program from being forcibly closed:

### Features
- **Auto Restart**: When main program is closed by Task Manager, Watchdog will automatically restart it
- **Mutual Monitoring**: Main program also monitors Watchdog process, ensuring both are running
- **Single Instance Limit**: Watchdog can only run one instance
- **Quick Response**: 500ms detection interval, quick response to process termination

### File Description
- `ScreenTimeController.exe` - Main program
- `ProtectionService.exe` - Windows service program
- `WatchdogMonitor.exe` - Watchdog daemon
- `setup_protection.bat` - Install/uninstall script
- `*.runtimeconfig.json` - Runtime configuration files

### Log Files
- `%ProgramData%\ScreenTimeController\watchdog.log` - Main program monitoring log
- `%ProgramData%\ScreenTimeController\protection_service.log` - Service log

## Technical Implementation

### Core Components
- **MainForm**: Main application window, uses TabControl to separate overview and app list
- **SettingsForm**: Settings window for configuring daily time limits and password
- **PasswordForm**: Password input window for user authentication, includes password lockout logic
- **ChangePasswordForm**: Password change window
- **TimeTracker**: Time tracker, records screen usage time and app-level usage time
- **SettingsManager**: Settings manager, saves and loads application settings
- **DataProtectionManager**: Data protection manager, multi-location storage and integrity verification
- **LoginAttemptManager**: Login attempt manager, password lockout functionality
- **WindowHelper**: Windows API wrapper for getting window info and locking screen
- **Watchdog**: Daemon manager, starts and monitors Watchdog process

### Data Storage
- Settings saved in `%ProgramData%\ScreenTimeController\settings.txt`
- Total time usage saved in `%ProgramData%\ScreenTimeController\usage.txt`
- App-level time usage saved in `%ProgramData%\ScreenTimeController\app_usage.txt`
- Password encrypted with SHA256
- Login attempt records stored in registry and file (dual backup)

### Security Features
- **Password Security**: SHA256 encryption storage
- **Account Lockout**: 5 failed password attempts lock until next day 00:00 (toggleable)
- **Data Protection**: Multi-location storage (main+backup+registry), SHA256 hash verification
- **Directory Permissions**: Config directory only accessible by Admin and SYSTEM
- **Uninstall Cleanup**: Clean all data directories and registry on uninstall

### Key Technologies
- Windows Forms application development
- Windows API integration (user32.dll)
- Time tracking and management
- System tray integration (NotifyIcon)
- Single instance detection (Mutex)
- Process guardian and mutual monitoring
- Icon caching mechanism
- Thread-safe design
- SHA256 hash verification
- Windows ACL permission management

## Troubleshooting

### Application Won't Start
1. Ensure application files are complete
2. Check application permissions (recommended to run as administrator)
3. Ensure .NET 5.0 or higher runtime is installed
4. Check error messages in Windows Event Viewer

### Time Limit Not Working
1. Check if settings are saved correctly
2. Ensure application is running (visible in system tray)
3. Restart application

### Forgot Password
1. Close application
2. Delete `%ProgramData%\ScreenTimeController\settings.txt` file
3. Restart application and set new password

### Reset Usage Time Data
1. Close application
2. Delete `%ProgramData%\ScreenTimeController\usage.txt` file
3. Delete `%ProgramData%\ScreenTimeController\app_usage.txt` file
4. Restart application

### App Icons Not Displaying
1. Check if icon files are corrupted
2. Application will use system default icon as fallback

### Watchdog Not Working
1. Ensure all dependency files exist
2. Check log files
3. Ensure no multiple Watchdog instances are running

### Uninstall Cleanup
1. Right-click `setup_protection.bat` and select "Run as administrator"
2. Select uninstall option
3. Script will clean all data directories, registry entries and scheduled tasks

## License

Apache-2.0 License

## Changelog

### v1.0.1
- **New**: Data protection - multi-location storage, SHA256 hash verification, tamper detection auto-recovery
- **New**: Password lockout - lock after 5 failed attempts until next day 00:00 (toggleable in settings)
- **New**: Config directory permissions - Admin and SYSTEM only access
- **Improved**: Exit speed - removed retry mechanism for faster exit
- **Improved**: Data integrity - File.Replace atomic operation prevents data loss
- **Fixed**: UI font styles - table headers bold, content regular
- **Fixed**: Uninstall cleanup - clean all data directories and registry

### v1.0.0
- Initial official release
- Implement basic screen time control features
- Add system tray integration
- Support different time limits for each day
- Add 5-minute warning notification
- Windows service for continuous timing, runs even when GUI is closed
- Process guardian (WatchdogMonitor) prevents program from being forcibly closed
- Abnormal exit detection and penalty mechanism
- Use CommonApplicationData for data storage, supports cross-account access
- Scheduled task management ensures daemon runs in user session
