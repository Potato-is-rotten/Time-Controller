using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;
using Microsoft.Win32.TaskScheduler;

namespace ProtectionService
{
    public class ProtectionService : ServiceBase
    {
        private const string ServiceNameConst = "ScreenTimeController_ProtectionService";
        private const string TaskName = "ScreenTimeController_Restart";
        private const string MainProcessName = "ScreenTimeController";
        private const string WatchdogProcessName = "WatchdogMonitor";
        private const int CheckIntervalMs = 30000;

        private static string? _watchdogPath;
        private static string? _mainExePath;
        private static string _logPath = "";
        private Thread? _workerThread;
        private bool _isRunning;

        public ProtectionService()
        {
            ServiceName = ServiceNameConst;
            CanStop = true;
            CanPauseAndContinue = false;
            AutoLog = true;
        }

        protected override void OnStart(string[] args)
        {
            Log("Protection Service starting (ServiceBase mode)...");
            StartWorker();
            Log("Protection Service started");
        }

        protected override void OnStop()
        {
            Log("Protection Service stopping...");
            StopWorker();
            Log("Protection Service stopped");
        }

        private static void InitializePaths()
        {
            string? currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _watchdogPath = Path.Combine(currentDir ?? "", "WatchdogMonitor.exe");
            _mainExePath = Path.Combine(currentDir ?? "", "ScreenTimeController.exe");
            _logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ScreenTimeController", "protection_service.log");
        }

        private void StartWorker()
        {
            _isRunning = true;
            _workerThread = new Thread(WorkerLoop);
            _workerThread.IsBackground = true;
            _workerThread.Start();
        }

        private void StopWorker()
        {
            _isRunning = false;
            if (_workerThread != null && _workerThread.IsAlive)
            {
                _workerThread.Join(5000);
            }
        }

        private void WorkerLoop()
        {
            Thread.Sleep(5000);
            
            while (_isRunning)
            {
                try
                {
                    PerformChecks();
                }
                catch (Exception ex)
                {
                    Log($"Error during checks: {ex.Message}");
                }

                Thread.Sleep(CheckIntervalMs);
            }
        }

        private void PerformChecks()
        {
            EnsureScheduledTaskExists();
            EnsureWatchdogRunning();
            EnsureMainProgramRunning();
        }

        private void EnsureScheduledTaskExists()
        {
            try
            {
                using TaskService taskService = new();
                Task? task = taskService.FindTask(TaskName);

                if (task == null)
                {
                    Log("Scheduled task not found. Recreating...");
                    CreateScheduledTask();
                }
                else if (!task.Enabled)
                {
                    Log("Scheduled task is disabled. Re-enabling...");
                    task.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                Log($"Error checking scheduled task: {ex.Message}");
            }
        }

        private void CreateScheduledTask()
        {
            try
            {
                if (string.IsNullOrEmpty(_watchdogPath) || !File.Exists(_watchdogPath))
                {
                    Log("WatchdogMonitor.exe not found. Cannot create scheduled task.");
                    return;
                }

                using TaskService taskService = new();
                TaskDefinition taskDefinition = taskService.NewTask();
                taskDefinition.RegistrationInfo.Description = "Automatically restart Screen Time Controller when it exits unexpectedly";
                taskDefinition.Settings.Enabled = true;
                taskDefinition.Settings.Hidden = true;
                taskDefinition.Settings.StartWhenAvailable = true;
                taskDefinition.Settings.DisallowStartIfOnBatteries = false;
                taskDefinition.Settings.StopIfGoingOnBatteries = false;
                taskDefinition.Settings.ExecutionTimeLimit = TimeSpan.Zero;

                taskDefinition.Triggers.Add(new BootTrigger { Delay = TimeSpan.FromSeconds(30) });
                taskDefinition.Triggers.Add(new LogonTrigger { Delay = TimeSpan.FromSeconds(10) });
                taskDefinition.Triggers.Add(new DailyTrigger { DaysInterval = 1, StartBoundary = DateTime.Today.AddMinutes(1) });

                ExecAction execAction = new(_watchdogPath);
                taskDefinition.Actions.Add(execAction);

                taskService.RootFolder.RegisterTaskDefinition(TaskName, taskDefinition);
                Log("Scheduled task recreated successfully.");
            }
            catch (Exception ex)
            {
                Log($"Failed to create scheduled task: {ex.Message}");
            }
        }

        private void EnsureWatchdogRunning()
        {
            try
            {
                Process[] processes = Process.GetProcessesByName(WatchdogProcessName);
                if (processes.Length == 0)
                {
                    Log("WatchdogMonitor not running. Triggering scheduled task to start in user session...");
                    TriggerScheduledTask();
                }
                else
                {
                    foreach (Process p in processes)
                    {
                        p.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error checking watchdog: {ex.Message}");
            }
        }

        private void EnsureMainProgramRunning()
        {
            try
            {
                Process[] processes = Process.GetProcessesByName(MainProcessName);
                if (processes.Length == 0)
                {
                    string cleanExitFile = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                        "ScreenTimeController", "clean_exit.txt");

                    bool shouldRestart = true;

                    if (File.Exists(cleanExitFile))
                    {
                        try
                        {
                            string content = File.ReadAllText(cleanExitFile).Trim();
                            if (DateTime.TryParse(content, out DateTime cleanExitDate))
                            {
                                if (cleanExitDate.Date == DateTime.Today || cleanExitDate.Date == DateTime.Today.AddDays(-1))
                                {
                                    Log($"Clean exit detected ({cleanExitDate:yyyy-MM-dd}). Not restarting main program.");
                                    shouldRestart = false;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log($"Error reading clean exit file: {ex.Message}");
                        }
                    }

                    if (shouldRestart)
                    {
                        Log("Main program not running and no clean exit. Triggering watchdog restart...");
                        TriggerWatchdogRestart();
                    }
                }
                else
                {
                    foreach (Process p in processes)
                    {
                        p.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error checking main program: {ex.Message}");
            }
        }

        private void TriggerWatchdogRestart()
        {
            try
            {
                Process[] watchdogProcesses = Process.GetProcessesByName(WatchdogProcessName);
                if (watchdogProcesses.Length == 0)
                {
                    Log("WatchdogMonitor not running. Triggering scheduled task to start in user session...");
                    TriggerScheduledTask();
                }
                else
                {
                    Log("WatchdogMonitor already running. It will handle restart.");
                    foreach (Process p in watchdogProcesses)
                    {
                        p.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error triggering watchdog restart: {ex.Message}");
            }
        }

        private static void TriggerScheduledTask()
        {
            try
            {
                using TaskService taskService = new();
                Task? task = taskService.FindTask(TaskName);
                if (task != null)
                {
                    task.Run();
                    Log($"Triggered scheduled task: {TaskName}");
                }
                else
                {
                    Log("Scheduled task not found. Cannot trigger watchdog.");
                }
            }
            catch (Exception ex)
            {
                Log($"Error triggering scheduled task: {ex.Message}");
            }
        }

        private static void RecordAbnormalExit()
        {
            try
            {
                string commonDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ScreenTimeController");
                if (!Directory.Exists(commonDataDir))
                {
                    Directory.CreateDirectory(commonDataDir);
                }

                string abnormalExitFile = Path.Combine(commonDataDir, "abnormal_exits.txt");
                string today = DateTime.Today.ToString("yyyy-MM-dd");
                int count = 0;

                if (File.Exists(abnormalExitFile))
                {
                    string[] lines = File.ReadAllLines(abnormalExitFile);
                    if (lines.Length > 0 && lines[0].StartsWith(today))
                    {
                        if (lines.Length > 1 && int.TryParse(lines[1], out count))
                        {
                            count++;
                        }
                        else
                        {
                            count = 1;
                        }
                    }
                    else
                    {
                        count = 1;
                    }
                }
                else
                {
                    count = 1;
                }

                File.WriteAllLines(abnormalExitFile, new string[] { today, count.ToString() });
                Log($"Recorded abnormal exit. Today's count: {count}");
            }
            catch (Exception ex)
            {
                Log($"Error recording abnormal exit: {ex.Message}");
            }
        }

        private static void StartProcess(string? path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                Log($"Cannot start process: path not found - {path}");
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
                Log($"Started: {path}");
            }
            catch (Exception ex)
            {
                Log($"Failed to start {path}: {ex.Message}");
            }
        }

        private static void Log(string message)
        {
            try
            {
                string? logDir = Path.GetDirectoryName(_logPath);
                if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
                File.AppendAllText(_logPath, logEntry);
            }
            catch { }
        }

        #region Native Windows Service API

        private const int SERVICE_WIN32_OWN_PROCESS = 0x10;
        private const int SERVICE_ACCEPT_STOP = 0x1;
        private const int SERVICE_RUNNING = 0x4;
        private const int SERVICE_START_PENDING = 0x2;
        private const int SERVICE_STOP_PENDING = 0x3;
        private const int SERVICE_STOPPED = 0x1;

        [StructLayout(LayoutKind.Sequential)]
        private struct SERVICE_STATUS
        {
            public int dwServiceType;
            public int dwCurrentState;
            public int dwControlsAccepted;
            public int dwWin32ExitCode;
            public int dwServiceSpecificExitCode;
            public int dwCheckPoint;
            public int dwWaitHint;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SERVICE_TABLE_ENTRY
        {
            public IntPtr lpServiceName;
            public IntPtr lpServiceProc;
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(IntPtr handle, ref SERVICE_STATUS status);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr RegisterServiceCtrlHandler(string serviceName, ServiceControlHandlerProc handler);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool StartServiceCtrlDispatcher(IntPtr serviceTable);

        private delegate void ServiceControlHandlerProc(int controlCode);
        private delegate void ServiceMainProc(int argc, IntPtr argv);

        private static IntPtr _serviceStatusHandle = IntPtr.Zero;
        private static bool _isRunningNative = false;
        private static ServiceControlHandlerProc? _controlHandler;

        private static void ReportServiceStatus(int currentState, int controlsAccepted = 0, int waitHint = 0, int checkPoint = 0)
        {
            if (_serviceStatusHandle == IntPtr.Zero) return;

            SERVICE_STATUS status = new()
            {
                dwServiceType = SERVICE_WIN32_OWN_PROCESS,
                dwCurrentState = currentState,
                dwControlsAccepted = controlsAccepted,
                dwWin32ExitCode = 0,
                dwServiceSpecificExitCode = 0,
                dwCheckPoint = checkPoint,
                dwWaitHint = waitHint
            };
            SetServiceStatus(_serviceStatusHandle, ref status);
        }

        private static void NativeServiceControlHandler(int controlCode)
        {
            switch (controlCode)
            {
                case 0x1:
                    ReportServiceStatus(SERVICE_STOP_PENDING, 0, 5000, 1);
                    _isRunningNative = false;
                    ReportServiceStatus(SERVICE_STOPPED, 0);
                    break;
                case 0x5:
                    ReportServiceStatus(_isRunningNative ? SERVICE_RUNNING : SERVICE_STOPPED, SERVICE_ACCEPT_STOP);
                    break;
            }
        }

        private static void NativeServiceMain(int argc, IntPtr argv)
        {
            Log("NativeServiceMain called");

            _controlHandler = NativeServiceControlHandler;
            _serviceStatusHandle = RegisterServiceCtrlHandler(ServiceNameConst, _controlHandler);

            if (_serviceStatusHandle == IntPtr.Zero)
            {
                Log($"Failed to register service handler: {Marshal.GetLastWin32Error()}");
                return;
            }

            ReportServiceStatus(SERVICE_START_PENDING, 0, 3000, 1);
            Log("Protection Service starting (native mode)...");

            ReportServiceStatus(SERVICE_START_PENDING, 0, 3000, 2);
            Log("Starting worker thread...");

            _isRunningNative = true;
            Thread workerThread = new(NativeWorkerLoop);
            workerThread.IsBackground = true;
            workerThread.Start();

            ReportServiceStatus(SERVICE_RUNNING, SERVICE_ACCEPT_STOP);
            Log("Protection Service started (native mode)");

            while (_isRunningNative)
            {
                Thread.Sleep(1000);
            }

            Log("Native service exiting...");
        }

        private static void NativeWorkerLoop()
        {
            while (_isRunningNative)
            {
                try
                {
                    using TaskService taskService = new();
                    Task? task = taskService.FindTask(TaskName);
                    if (task == null)
                    {
                        Log("Scheduled task not found.");
                    }

                    Process[] watchdogProcesses = Process.GetProcessesByName(WatchdogProcessName);
                    if (watchdogProcesses.Length == 0)
                    {
                        Log("WatchdogMonitor not running. Triggering scheduled task...");
                        TriggerScheduledTask();
                    }
                    foreach (var p in watchdogProcesses) p.Dispose();

                    Process[] mainProcesses = Process.GetProcessesByName(MainProcessName);
                    if (mainProcesses.Length == 0)
                    {
                        bool shouldRestart = true;
                        string cleanExitFile = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                            "ScreenTimeController", "clean_exit.txt");

                        if (File.Exists(cleanExitFile))
                        {
                            try
                            {
                                string content = File.ReadAllText(cleanExitFile).Trim();
                                if (DateTime.TryParse(content, out DateTime cleanExitDate))
                                {
                                    if (cleanExitDate.Date == DateTime.Today || cleanExitDate.Date == DateTime.Today.AddDays(-1))
                                    {
                                        Log($"Clean exit detected ({cleanExitDate:yyyy-MM-dd}) (native mode). Not restarting main program.");
                                        shouldRestart = false;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Log($"Error reading clean exit file (native): {ex.Message}");
                            }
                        }

                        if (shouldRestart)
                        {
                            Log("Main program not running and no clean exit (native mode). Triggering watchdog restart...");
                        }
                    }
                    foreach (var p in mainProcesses) p.Dispose();
                }
                catch (Exception ex)
                {
                    Log($"Error: {ex.Message}");
                }

                Thread.Sleep(CheckIntervalMs);
            }
        }

        private static void RunNativeService()
        {
            Log("RunNativeService: Setting up service table...");

            IntPtr serviceNamePtr = Marshal.StringToHGlobalUni(ServiceNameConst);
            IntPtr serviceProcPtr = Marshal.GetFunctionPointerForDelegate(new ServiceMainProc(NativeServiceMain));

            try
            {
                SERVICE_TABLE_ENTRY[] table = new SERVICE_TABLE_ENTRY[2];
                table[0].lpServiceName = serviceNamePtr;
                table[0].lpServiceProc = serviceProcPtr;
                table[1].lpServiceName = IntPtr.Zero;
                table[1].lpServiceProc = IntPtr.Zero;

                int size = Marshal.SizeOf(typeof(SERVICE_TABLE_ENTRY));
                IntPtr tablePtr = Marshal.AllocHGlobal(size * 2);

                try
                {
                    Marshal.StructureToPtr(table[0], tablePtr, false);
                    Marshal.StructureToPtr(table[1], tablePtr + size, false);

                    Log("RunNativeService: Calling StartServiceCtrlDispatcher...");

                    if (!StartServiceCtrlDispatcher(tablePtr))
                    {
                        int error = Marshal.GetLastWin32Error();
                        Log($"StartServiceCtrlDispatcher failed: {error}");
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(tablePtr);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(serviceNamePtr);
            }
        }

        #endregion

        static void Main(string[] args)
        {
            InitializePaths();

            if (args.Length > 0 && args[0].Equals("--console", StringComparison.OrdinalIgnoreCase))
            {
                RunConsoleMode();
                return;
            }

            Log($"ProtectionService starting. CLR: {Environment.Version}, OS: {Environment.OSVersion}");

            try
            {
                Log("Attempting ServiceBase.Run...");
                ServiceBase[] servicesToRun = new ServiceBase[]
                {
                    new ProtectionService()
                };
                ServiceBase.Run(servicesToRun);
            }
            catch (PlatformNotSupportedException ex)
            {
                Log($"ServiceBase not supported: {ex.Message}");
                Log("Using native Windows service API...");
                RunNativeService();
            }
            catch (Exception ex)
            {
                Log($"ServiceBase error: {ex.GetType().Name}: {ex.Message}");
                Log("Falling back to native Windows service API...");
                RunNativeService();
            }
        }

        private static void RunConsoleMode()
        {
            Log("Running in console mode...");
            Console.WriteLine("Protection Service running in console mode.");
            Console.WriteLine("Press any key to stop...");

            bool isRunning = true;
            Thread workerThread = new(() =>
            {
                while (isRunning)
                {
                    try
                    {
                        using TaskService taskService = new();
                        Task? task = taskService.FindTask(TaskName);
                        if (task == null)
                        {
                            Log("Scheduled task not found.");
                        }

                        Process[] watchdogProcesses = Process.GetProcessesByName(WatchdogProcessName);
                        if (watchdogProcesses.Length == 0)
                        {
                            Log("WatchdogMonitor not running. Triggering scheduled task...");
                            TriggerScheduledTask();
                        }
                        foreach (var p in watchdogProcesses) p.Dispose();

                        Process[] mainProcesses = Process.GetProcessesByName(MainProcessName);
                        if (mainProcesses.Length == 0)
                        {
                            bool shouldRestart = true;
                            string cleanExitFile = Path.Combine(
                                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                                "ScreenTimeController", "clean_exit.txt");

                            if (File.Exists(cleanExitFile))
                            {
                                try
                                {
                                    string content = File.ReadAllText(cleanExitFile).Trim();
                                    if (DateTime.TryParse(content, out DateTime cleanExitDate))
                                    {
                                        if (cleanExitDate.Date == DateTime.Today || cleanExitDate.Date == DateTime.Today.AddDays(-1))
                                        {
                                            Log($"Clean exit detected ({cleanExitDate:yyyy-MM-dd}) (console mode). Not restarting main program.");
                                            shouldRestart = false;
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log($"Error reading clean exit file (console): {ex.Message}");
                                }
                            }

                            if (shouldRestart)
                            {
                                Log("Main program not running and no clean exit (console mode). Triggering watchdog restart...");
                            }
                        }
                        foreach (var p in mainProcesses) p.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Log($"Error: {ex.Message}");
                    }

                    Thread.Sleep(CheckIntervalMs);
                }
            });
            workerThread.IsBackground = true;
            workerThread.Start();

            Console.ReadKey();
            isRunning = false;
            Log("Console mode stopped.");
        }
    }
}
