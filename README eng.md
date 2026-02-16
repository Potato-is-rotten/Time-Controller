# Language
中文:
[此处](https://github.com/Potato-is-rotten/Time-Controller/blob/main/README.md)

# Screen Time Controller

The Windows 11 Screen Time Controller app helps you manage and control screen usage time.

## Key Features

- **Daily Screen Time Limits**: Set different screen time limits for each day
- **App-Level Usage Tracking**: Distinguish usage time by application, recording detailed usage time for each windowed app
- **App Icon Display**: Shows application icons for an intuitive overview of usage
- **Tabbed Interface**: Overview tab displays summary stats; Applications tab shows app-specific details
- **Time Limit Alerts**: Receive warning notifications 5 minutes before reaching time limits
- **Screen Lock**: Automatically locks the screen upon reaching time limits
- **Password Protection**: Set a password to prevent unauthorized setting changes
- **System Tray Integration**: Minimizes to the system tray without disrupting normal work
- **Real-Time Usage Tracking**: Displays elapsed and remaining time in real time
- **Single Instance Operation**: Prevents multiple instances from running simultaneously
- **Grace Period**: Allows password entry after time expires
- **Full-Screen Restriction**: Requires password input to enable full-screen mode after time expires

## System Requirements

- Windows 10 1607 or later
- Windows Server 2012 R2 SP1 or later
- Supports .NET Framework 5.0–10.0

## Installation and Operation

### Direct Execution
1. Copy the `ScreenTimeController` folder to the target location
2. Double-click `ScreenTimeController.exe` to launch the application

## Usage Guide

### First Launch
1. The application will automatically start and minimize to the system tray
2. Double-click the system tray icon to open the main window
3. Click the “Settings” button to open the settings window
4. Set daily screen time limits
5. Configure password protection (optional)
6. Click “OK” to save settings

### Daily Usage
- **View Status**: Double-click the system tray icon to open the main window
  - **Overview Tab**: Displays daily limit, time used, remaining time, and progress bar
  - **Applications Tab**: Shows usage time and icons for each application
- **Modify Settings**: Click the “Settings” button to modify settings (password required)
- **Minimize**: Click the minimize or close button to hide the app in the system tray
- **Exit the app**: Right-click the system tray icon and select “Exit”

### Time Limit Settings
- Set different time limits for each day
- Use the “Apply to All Days” button to apply current settings to all days
- Time limits are set in hours and minutes
- Supports setting, modifying, or removing password protection

## Technical Implementation

### Core Components
- **MainForm**: Main application window, using TabControl to separate Overview and Application List
- **SettingsForm**: Configuration window for daily time limits and passwords
- **PasswordForm**: Password input window for user authentication
- **ChangePasswordForm**: Password modification window
- **TimeTracker**: Time tracker recording screen usage and app-level usage
- **SettingsManager**: Settings manager for saving and loading application configurations
- **WindowHelper**: Windows API wrapper for retrieving window information and locking the screen

### Data Storage
- Settings are saved in the `%AppData%\ScreenTimeController\settings.txt` file
- Total time usage records are saved in the `%AppData%\ScreenTimeController\usage.txt` file
- Application-level time usage records are saved in the `%AppData%\ScreenTimeController\app_usage.txt` file
- Passwords are stored using SHA256 encryption

### Key Technologies
- Windows Forms application development
- Windows API integration (user32.dll)
- Time tracking and management
- System tray integration (NotifyIcon)
- Single instance detection (Mutex)
- Self-contained deployment
- Icon caching mechanism
- Thread-safe design

## Troubleshooting

### Application fails to launch
1. Ensure application files are intact
2. Verify application permissions
3. Check error messages in Windows Event Viewer

### Time limit not enforced
1. Confirm settings are saved correctly
2. Ensure application is running (visible in system tray)
3. Restart the application

### Forgotten Password
1. Close the application
2. Delete the `%AppData%\ScreenTimeController\settings.txt` file
3. Restart the application and set a new password

### Reset Usage Data
1. Close the application
2. Delete the `%AppData%\ScreenTimeController\usage.txt` file
3. Delete the `%AppData%\ScreenTimeController\app_usage.txt` file
4. Restart the application

### Application Icon Not Displaying
1. Ensure the `Resources\AppIcon.ico` file exists
2. Check if the icon file is corrupted
3. The application will automatically use the system default icon as a fallback

## License

Apache-2.0 License
