# Screen Time Controller

English | [简体中文](README.md)

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

## System Requirements

- Windows 10 1607 and above
- Windows Server 2012 R2 SP1 and above
- .NET Runtime 5.0~10.0 supported

## Installation and Running

### Direct Run
1. Copy the `ScreenTimeController` folder to the target location
2. Double-click `ScreenTimeController.exe` to run the application
3. Ensure `Resources\AppIcon.ico` file exists
4. Ensure `ScreenTimeControllerWatchdog.exe` and its dependency files exist

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

## Process Guardian (Watchdog)

Screen Time Controller includes a Watchdog daemon to protect the main program from being forcibly closed:

### Features
- **Auto Restart**: When main program is closed by Task Manager, Watchdog will automatically restart it
- **Mutual Monitoring**: Main program also monitors Watchdog process, ensuring both are running
- **Single Instance Limit**: Watchdog can only run one instance
- **Quick Response**: 500ms detection interval, quick response to process termination

## Technical Implementation

### Core Components
- **MainForm**: Main application window, uses TabControl to separate overview and app list
- **SettingsForm**: Settings window for configuring daily time limits and password
- **PasswordForm**: Password input window for user authentication
- **ChangePasswordForm**: Password change window
- **TimeTracker**: Time tracker, records screen usage time and app-level usage time
- **SettingsManager**: Settings manager, saves and loads application settings
- **WindowHelper**: Windows API wrapper for getting window info and locking screen
- **Watchdog**: Daemon manager, starts and monitors Watchdog process

### Data Storage
- Settings saved in `%AppData%\ScreenTimeController\settings.txt`
- Total time usage saved in `%AppData%\ScreenTimeController\usage.txt`
- App-level time usage saved in `%AppData%\ScreenTimeController\app_usage.txt`
- Password encrypted with PBKDF2 + random salt

### Security Features
- **Password Security**: PBKDF2 with 100,000 iterations, random salt
- **Account Lockout**: 5 failed attempts = 15 minute lockout
- **IPC Security**: Authentication token for all IPC commands
- **Data Protection**: Windows DPAPI encryption, registry backup
- **File Monitoring**: Real-time detection of file tampering

## Troubleshooting

### Application Won't Start
1. Ensure application files are complete
2. Check application permissions
3. Ensure .NET 5.0 or higher runtime is installed
4. Check error messages in Windows Event Viewer

### Time Limit Not Working
1. Check if settings are saved correctly
2. Ensure application is running (visible in system tray)
3. Restart application

### Forgot Password
1. Close application
2. Delete `%AppData%\ScreenTimeController\settings.txt` file
3. Restart application and set new password

### Reset Usage Time Data
1. Close application
2. Delete `%AppData%\ScreenTimeController\usage.txt` file
3. Delete `%AppData%\ScreenTimeController\app_usage.txt` file
4. Restart application

## License

Apache-2.0 License

## Changelog

### v1.3.0 (Security Update)
- **Enhanced Password Security**
  - PBKDF2 algorithm replaces simple SHA256 hashing
  - Random salt for each password
  - 100,000 iterations for increased cracking difficulty
- **Account Lockout Mechanism**
  - 15-minute lockout after 5 failed attempts
  - Persistent lockout state
- **IPC Communication Security**
  - Authentication token verification
  - All IPC commands require valid token
- **Enhanced Data Protection**
  - Windows DPAPI data encryption
  - Registry backup storage
  - File monitoring for tamper detection
  - File hiding and permission restrictions
- **Windows Service Support**
  - Background service for continuous timing
  - Service continues after GUI closes
  - Auto-start service
- Added lockout prompt text in 10 languages

### v1.2.0
- Use TabControl to separate Overview and Applications interface
- Add application icon display feature
- Implement icon caching mechanism
- Only track windowed applications
- Add single instance detection
- Fix multiple potential bugs and resource leaks
- Optimize UI layout and font sizes
- Improve thread safety

### v1.1.0
- Add app-level time tracking feature
- Today's usage time not reset after settings change
- Optimize system tray notifications
- Add password protection feature

### v1.0.0
- Initial version
- Implement basic screen time control features
- Add system tray integration
- Support different time limits for each day
- Add 5-minute warning notification
