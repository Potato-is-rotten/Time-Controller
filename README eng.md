# Screen Time Controller

The Windows 11 Screen Time Controller application helps you manage and control screen usage time.

## Key Features

- **Daily Screen Time Limits**: Set different screen time limits for each day
- **Windows Service Mode**: Runs as a Windows service for automatic startup at boot
- **Full Window Timing Logs**: Records all window open times (regardless of focus)
- **Application-Level Time Tracking**: Distinguish usage time by application, detailing focus time for each app
- **Time Limit Warning**: Send warning notifications 5 minutes before reaching the time limit
- **Screen Lock**: Automatically lock the screen when the time limit is reached
- **Password Protection**: Set a password to prevent unauthorized changes to settings
- **System Tray Integration**: Minimizes to the system tray without interrupting normal work
- **Real-Time Usage Tracking**: Displays elapsed and remaining time in real time
- **Settings Persistence**: Today's usage time remains unchanged after settings adjustments

## Installation Guide

### System Requirements
- Windows 11
- .NET 10.0 or later

### Installation Steps
1. Download the project
2. Run the program ‘ScreenTimeController.exe’

## Usage Guide

### First Launch
1. The application will automatically launch and minimize to the system tray
2. Right-click the system tray icon and select “Settings” to open the configuration window
3. Set daily screen time limits
4. Configure password protection (optional)
5. Click “OK” to save settings

### Daily Use
- **View Status**: Right-click the system tray icon and select “Show” to view current usage
- **Modify Settings**: Right-click the system tray icon and select “Settings” to modify settings (password required)
- **Exit Application**: Right-click the system tray icon and select “Exit” to close the application

### Time Limit Settings
- Set different time limits for each day
- Use the “Apply to All” button to apply current settings to all days
- Time limits are specified in hours and minutes

## Technical Implementation

### Core Components
- **MainForm**: Main application window displaying time usage and control options
- **ScreenTimeService**: Windows service class enabling background operation
- **SettingsForm**: Configuration window for daily time limits and passwords
- **TimeTracker**: Records screen time and application-level usage
- **SettingsManager**: Saves and loads application settings
- **WindowHelper**: Windows API wrapper for retrieving window information and locking screens

### Data Storage
- Settings stored in `%AppData%\ScreenTimeController\settings.txt`
- Total usage logs stored in `%AppData%\ScreenTimeController\usage.txt`
- Application-level usage logs stored in `%AppData%\ScreenTimeController\app_usage.txt`
- Passwords stored using SHA256 encryption

### Key Technologies
- Windows Forms application development
- Windows service development
- Windows API integration
- Time tracking and management
- System tray integration
- File-based data storage
- Password encryption

## Troubleshooting

### Application Fails to Launch
1. Ensure .NET 10.0 or later is installed
2. Verify application permissions
3. Check error messages in Windows Event Viewer

### Time Limit Not Taking Effect
1. Confirm settings are saved correctly
2. Ensure application is running (visible in system tray)
3. Restart the application

### Forgot Password
1. Close the application
2. Delete the `%AppData%\ScreenTimeController\settings.txt` file
3. Restart the application and set a new password

### Service Fails to Start
1. Ensure the service was installed with administrator privileges
2. Check service dependencies
3. Review service error messages in the Windows Event Viewer

## License

Apache License 2.0
