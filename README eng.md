# Language
中文:
[此处](https://github.com/Potato-is-rotten/Time-Controller/blob/main/README.md)

# Screen Time Controller

Windows 11 / macOS Screen Time Controller application that helps you manage and control screen usage time.

## Supported Platforms

- **Windows**: Windows 10 1607 or later (x64, ARM64)
- **macOS**: macOS 12.0 (Monterey) or later

## Key Features

- **Daily Screen Time Limits**: Set different screen time limits for each day
- **App-Level Usage Tracking**: Distinguish usage time by application, recording detailed usage time for each windowed app
- **App Icon Display**: Shows application icons for an intuitive overview of usage
- **Tabbed Interface**: Overview tab displays summary stats; Applications tab shows app-specific details
- **Time Limit Alerts**: Receive warning notifications 5 minutes before reaching time limits
- **Screen Lock**: Automatically locks the screen upon reaching time limits
- **Password Protection**: Set a password to prevent unauthorized setting changes
- **System Tray/Menu Bar Integration**: Minimizes to the system tray/menu bar without disrupting normal work
- **Real-Time Usage Tracking**: Displays elapsed and remaining time in real time
- **Single Instance Operation**: Prevents multiple instances from running simultaneously
- **Process Protection** (Windows): Watchdog daemon prevents the program from being forcefully closed

## System Requirements

### Windows
- Windows 10 1607 or later
- Windows Server 2012 R2 SP1 or later
- Supports .NET Runtime 5.0–10.0

### macOS
- macOS 12.0 (Monterey) or later
- Xcode 14.0 or later (for building)

## Installation and Operation

### Windows

#### Direct Execution
1. Download `ScreenTimeController-win-x64.zip` or `ScreenTimeController-win-arm64.zip`
2. Extract to target location
3. Double-click `ScreenTimeController.exe` to launch the application
4. Ensure `Resources\AppIcon.ico` file exists
5. Ensure `ScreenTimeControllerWatchdog.exe` and its dependency files exist

#### Build from Source
1. Clone or download the project code
2. Install .NET 5.0 SDK or later
3. Open terminal, navigate to project directory
4. Run build command:
   ```powershell
   dotnet build --configuration Release
   ```
5. Find generated executable files in `bin\Release\net5.0-windows\` folder

### macOS

#### Using DMG Installer (Recommended)
1. Download `ScreenTimeController-macOS.zip`
2. Extract and open the DMG file
3. Drag `ScreenTimeController.app` to the `Applications` folder
4. Open `Applications` folder and double-click to run the application

#### Build from Source
1. Clone or download the project code
2. Open `ScreenTimeController-macOS/ScreenTimeController.xcodeproj`
3. Press `Cmd+R` to run the project

## Usage Guide

### First Launch
1. The application will automatically start and minimize to the system tray/menu bar
2. Click the system tray/menu bar icon to open the main window
3. Click "Settings" button to open settings window
4. Set daily screen time limits
5. Configure password protection (optional)
6. Click "OK" to save settings

### Daily Usage
- **View Status**: Click the system tray/menu bar icon to open the main window
  - **Overview Tab**: Displays daily limit, time used, remaining time, and progress bar
  - **Applications Tab**: Shows usage time and icons for each application
- **Modify Settings**: Click "Settings" button to modify settings (password required)
- **Exit the app**: Right-click the system tray/menu bar icon and select "Exit"

### Time Limit Settings
- Set different time limits for each day
- Use "Apply to All Days" button to apply current settings to all days
- Time limits are set in hours and minutes
- Supports setting, modifying, or removing password protection

## Process Protection (Watchdog) - Windows Only

Screen Time Controller includes a Watchdog daemon to protect the main program from being forcefully closed:

### Features
- **Auto Restart**: When the main program is closed via Task Manager, Watchdog automatically restarts it
- **Mutual Monitoring**: The main program also monitors the Watchdog process, ensuring both are running
- **Single Instance**: Watchdog can only run one instance
- **Fast Response**: 500ms detection interval for quick response to process termination

### File Description
- `ScreenTimeController.exe` - Main program
- `ScreenTimeControllerWatchdog.exe` - Watchdog daemon
- `ScreenTimeController.dll` - Main program dependency
- `ScreenTimeControllerWatchdog.dll` - Watchdog dependency
- `*.runtimeconfig.json` - Runtime configuration files
- `*.deps.json` - Dependency configuration files

### Log Files
- `%AppData%\ScreenTimeController\watchdog_monitor.log` - Main program monitoring log
- `%AppData%\ScreenTimeController\watchdog_external.log` - Watchdog log

## Technical Implementation

### Windows Core Components
- **MainForm**: Main application window, using TabControl to separate Overview and Application List
- **SettingsForm**: Configuration window for daily time limits and passwords
- **PasswordForm**: Password input window for user authentication
- **ChangePasswordForm**: Password modification window
- **TimeTracker**: Time tracker recording screen usage and app-level usage
- **SettingsManager**: Settings manager for saving and loading application configurations
- **WindowHelper**: Windows API wrapper for retrieving window information and locking the screen
- **Watchdog**: Daemon manager for starting and monitoring Watchdog process

### macOS Core Components
- **ScreenTimeControllerApp**: Application entry point
- **AppDelegate**: Application delegate, manages menu bar and lifecycle
- **MainView**: Main interface with Overview and Applications tabs
- **SettingsView**: Settings window
- **TimeTracker**: Time tracker
- **SettingsManager**: Settings manager

### Data Storage
- Settings are saved in `%AppData%\ScreenTimeController\` (Windows) or `~/Library/Application Support/ScreenTimeController/` (macOS)
- Passwords are stored using SHA256 encryption

## Troubleshooting

### Application fails to launch
1. Ensure application files are intact
2. Verify application permissions
3. Ensure .NET 5.0 or later runtime is installed (Windows)
4. Check error messages in Windows Event Viewer or Console.app (macOS)

### Time limit not enforced
1. Confirm settings are saved correctly
2. Ensure application is running (visible in system tray/menu bar)
3. Restart the application

### Forgotten Password
1. Close the application
2. Delete the settings file
3. Restart the application and set a new password

### Watchdog Not Working (Windows)
1. Ensure `ScreenTimeControllerWatchdog.exe` and its dependency files exist
2. Check log file `%AppData%\ScreenTimeController\watchdog_external.log`
3. Ensure multiple Watchdog instances are not running

## License

Apache-2.0 License

## Changelog

### v2.2.0
- Added macOS version support
- Multi-platform release support (Windows x64, Windows ARM64, macOS)
- macOS version supports menu bar integration
- macOS version supports DMG installer
- Lowered macOS system requirement to 12.0 (Monterey)

### v1.3.0
- Added Watchdog process protection feature
- Implemented mutual monitoring between main program and Watchdog
- Added Watchdog single instance restriction
- Fixed multiple thread safety issues
- Optimized process detection interval (500ms)
- Removed self-contained deployment to reduce size

### v1.2.0
- Used TabControl to separate Overview and Applications interface
- Added application icon display feature
- Implemented icon caching mechanism, icons still visible after app closes
- Only track windowed applications
- Added single instance detection to prevent multiple instances
- Fixed multiple potential bugs and resource leaks
- Optimized UI layout and font sizes
- Improved thread safety

### v1.1.0
- Added app-level time tracking feature
- Today's usage time does not reset after changing settings
- Optimized system tray notifications
- Added password protection feature

### v1.0.0
- Initial version
- Implemented basic screen time control features
- Added system tray integration
- Support for different time limits per day
- Added 5-minute warning notification
