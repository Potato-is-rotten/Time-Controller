# Screen Time Controller v1.3.0 (Security Update)

## New Features

### Enhanced Password Security
- PBKDF2 algorithm replaces simple SHA256 hashing
- Random salt for each password
- 100,000 iterations for increased cracking difficulty

### Account Lockout Mechanism
- 15-minute lockout after 5 failed attempts
- Persistent lockout state

### IPC Communication Security
- Authentication token verification
- All IPC commands require valid token

### Enhanced Data Protection
- Windows DPAPI data encryption
- Registry backup storage
- File monitoring for tamper detection
- File hiding and permission restrictions

### Windows Service Support
- Background service for continuous timing
- Service continues after GUI closes
- Auto-start service

### Other Improvements
- Added lockout prompt text in 10 languages

## System Requirements
- Windows 10 1607 and above
- .NET Runtime 5.0~10.0

## Installation
1. Download `ScreenTimeController-win-x64.zip`
2. Extract to your preferred location
3. Run `ScreenTimeController.exe`
