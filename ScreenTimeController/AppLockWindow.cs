using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ScreenTimeController;

[SupportedOSPlatform("windows")]
public class AppLockWindow : Form
{
    private readonly string _appName;
    private readonly string _appIdentifier;
    private readonly int _limitMinutes;
    private readonly int _usedMinutes;
    private readonly int _exceededMinutes;
    private readonly AppTimeLimit _limit;
    private readonly Process? _targetProcess;
    private readonly SettingsManager _settingsManager;
    private readonly TimeTracker _timeTracker;

    private Label? _labelTitle;
    private Label? _labelMessage;
    private Label? _labelAppName;
    private Label? _labelTimeInfo;
    private Button? _buttonClose;
    private Button? _buttonGracePeriod;
    private ComboBox? _comboBoxBonusTime;
    private PictureBox? _pictureBoxIcon;
    private IContainer? components;

    private Timer? _positionTimer;
    private Timer? _zOrderTimer;
    private bool _isDragging = false;
    private Point _dragOffset;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool CanDismiss { get; set; } = false;

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool MoveWindowToDesktop(IntPtr hWnd, IntPtr hDesktop);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetThreadDesktop(uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
    private static readonly IntPtr HWND_TOP = new IntPtr(0);

    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public AppLockWindow(string appName, string appIdentifier, int limitMinutes, int usedMinutes, int exceededMinutes, AppTimeLimit limit, Process? targetProcess, SettingsManager settingsManager, TimeTracker timeTracker)
    {
        _appName = appName;
        _appIdentifier = appIdentifier;
        _limitMinutes = limitMinutes;
        _usedMinutes = usedMinutes;
        _exceededMinutes = exceededMinutes;
        _limit = limit;
        _targetProcess = targetProcess;
        _settingsManager = settingsManager;
        _timeTracker = timeTracker;

        InitializeComponent();
        ApplyLanguage();
        SetupPositionTimer();
        SetupZOrderTimer();

        this.Load += AppLockWindow_Load;
        LanguageManager.LanguageChanged += OnLanguageChanged;
    }

    private void AppLockWindow_Load(object? sender, EventArgs e)
    {
        UpdatePositionImmediately();
        BringToFront();
    }

    private void UpdatePositionImmediately()
    {
        if (_targetProcess == null || _targetProcess.HasExited)
        {
            return;
        }

        try
        {
            IntPtr hWnd = _targetProcess.MainWindowHandle;
            if (hWnd == IntPtr.Zero || !IsWindowVisible(hWnd))
            {
                return;
            }

            if (GetWindowRect(hWnd, out RECT rect))
            {
                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;
                int x = rect.Left;
                int y = rect.Top;

                Screen? screen = Screen.FromHandle(hWnd);
                if (screen == null) screen = Screen.PrimaryScreen;

                if (x < screen.WorkingArea.Left)
                {
                    x = screen.WorkingArea.Left;
                }
                if (y < screen.WorkingArea.Top)
                {
                    y = screen.WorkingArea.Top;
                }
                if (x + width > screen.WorkingArea.Right)
                {
                    x = screen.WorkingArea.Right - width;
                }
                if (y + height > screen.WorkingArea.Bottom)
                {
                    y = screen.WorkingArea.Bottom - height;
                }

                if (x != rect.Left || y != rect.Top)
                {
                    MoveWindow(hWnd, x, y, width, height, true);
                }

                this.Bounds = new Rectangle(x, y, width, height);
            }
        }
        catch
        {
        }
    }

    private void SetupPositionTimer()
    {
        _positionTimer = new Timer
        {
            Interval = 50
        };
        _positionTimer.Tick += UpdatePosition;
        _positionTimer.Start();
    }

    private void SetupZOrderTimer()
    {
        _zOrderTimer = new Timer
        {
            Interval = 100
        };
        _zOrderTimer.Tick += EnsureTopMost;
        _zOrderTimer.Start();
    }

    private void EnsureTopMost(object? sender, EventArgs e)
    {
        if (_targetProcess == null || _targetProcess.HasExited)
        {
            return;
        }

        try
        {
            IntPtr hWnd = _targetProcess.MainWindowHandle;
            if (hWnd != IntPtr.Zero && IsWindowVisible(hWnd))
            {
                SetWindowPos(hWnd, this.Handle, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
            }
        }
        catch
        {
        }
    }

    private void UpdatePosition(object? sender, EventArgs e)
    {
        if (_targetProcess == null || _targetProcess.HasExited)
        {
            CheckAndClose();
            return;
        }

        try
        {
            IntPtr hWnd = _targetProcess.MainWindowHandle;
            if (hWnd == IntPtr.Zero || !IsWindowVisible(hWnd))
            {
                CheckAndClose();
                return;
            }

            if (GetWindowRect(hWnd, out RECT rect))
            {
                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;
                int x = rect.Left;
                int y = rect.Top;

                MoveToSameDesktop(hWnd);

                if (!_isDragging)
                {
                    Screen? screen = Screen.FromHandle(hWnd);
                    if (screen == null) screen = Screen.PrimaryScreen;

                    if (x < screen.WorkingArea.Left)
                    {
                        x = screen.WorkingArea.Left;
                    }
                    if (y < screen.WorkingArea.Top)
                    {
                        y = screen.WorkingArea.Top;
                    }
                    if (x + width > screen.WorkingArea.Right)
                    {
                        x = screen.WorkingArea.Right - width;
                    }
                    if (y + height > screen.WorkingArea.Bottom)
                    {
                        y = screen.WorkingArea.Bottom - height;
                    }

                    if (x != rect.Left || y != rect.Top)
                    {
                        MoveWindow(hWnd, x, y, width, height, true);
                    }

                    if (this.Bounds.X != x || this.Bounds.Y != y ||
                        this.Bounds.Width != width || this.Bounds.Height != height)
                    {
                        this.Bounds = new Rectangle(x, y, width, height);
                    }
                }
            }
        }
        catch
        {
            CheckAndClose();
        }
    }

    private void CheckAndClose()
    {
        if (_targetProcess == null || _targetProcess.HasExited)
        {
            CanDismiss = true;
            Close();
        }
    }

    private void MoveToSameDesktop(IntPtr targetHWnd)
    {
        try
        {
            uint targetThreadId = GetWindowThreadProcessId(targetHWnd, out _);
            uint currentThreadId = GetCurrentThreadId();

            if (targetThreadId != 0 && targetThreadId != currentThreadId)
            {
                IntPtr targetDesktop = GetThreadDesktop(targetThreadId);
                IntPtr currentDesktop = GetThreadDesktop(currentThreadId);

                if (targetDesktop != IntPtr.Zero && targetDesktop != currentDesktop)
                {
                    MoveWindowToDesktop(this.Handle, targetDesktop);
                }
            }
        }
        catch
        {
        }
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        ApplyLanguage();
    }

    private void ApplyLanguage()
    {
        Text = LanguageManager.GetString("ApplicationLocked");
        _labelTitle!.Text = LanguageManager.GetString("TimeLimitExceeded");
        _labelMessage!.Text = LanguageManager.GetString("AppTimeLimitReached");
        _buttonClose!.Text = LanguageManager.GetString("CloseApp");
        _buttonGracePeriod!.Text = LanguageManager.GetString("AddTime");

        UpdateTimeInfo();
    }

    private void UpdateTimeInfo()
    {
        string limitStr = $"{_limitMinutes / 60}h {_limitMinutes % 60}m";
        string usedStr = $"{_usedMinutes / 60}h {_usedMinutes % 60}m";
        string exceededStr = $"{_exceededMinutes}m";

        _labelAppName!.Text = _appName;
        _labelTimeInfo!.Text = string.Format(
            LanguageManager.GetString("AppLockTimeInfo"),
            limitStr, usedStr, exceededStr);
    }

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        this.FormBorderStyle = FormBorderStyle.None;
        this.WindowState = FormWindowState.Normal;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.FromArgb(30, 30, 30);
        this.ForeColor = Color.White;
        this.TopMost = false;
        this.ShowInTaskbar = true;
        this.KeyPreview = true;
        this.ControlBox = false;
        this.Size = new Size(800, 600);

        TableLayoutPanel mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));

        Panel iconPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent
        };

        _pictureBoxIcon = new PictureBox
        {
            Size = new Size(80, 80),
            BackColor = Color.Transparent,
            Anchor = AnchorStyles.None
        };

        try
        {
            if (!string.IsNullOrEmpty(_limit.IconPath) && System.IO.File.Exists(_limit.IconPath))
            {
                using Icon? icon = Icon.ExtractAssociatedIcon(_limit.IconPath);
                if (icon != null)
                {
                    _pictureBoxIcon.Image = new Bitmap(icon.ToBitmap(), new Size(64, 64));
                }
            }
        }
        catch { }

        if (_pictureBoxIcon.Image == null)
        {
            _pictureBoxIcon.Image = CreateDefaultIcon();
        }

        iconPanel.Controls.Add(_pictureBoxIcon);
        _pictureBoxIcon.Left = (iconPanel.Width - _pictureBoxIcon.Width) / 2;
        _pictureBoxIcon.Top = (iconPanel.Height - _pictureBoxIcon.Height) / 2;
        iconPanel.Resize += (s, e) =>
        {
            _pictureBoxIcon.Left = (iconPanel.Width - _pictureBoxIcon.Width) / 2;
            _pictureBoxIcon.Top = (iconPanel.Height - _pictureBoxIcon.Height) / 2;
        };

        mainLayout.Controls.Add(iconPanel, 0, 0);

        Panel contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            AutoSize = true
        };

        _labelTitle = new Label
        {
            Dock = DockStyle.Top,
            Font = new Font("Segoe UI", 24f, FontStyle.Bold),
            ForeColor = Color.FromArgb(255, 100, 100),
            TextAlign = ContentAlignment.MiddleCenter,
            AutoSize = true,
            Padding = new Padding(0, 10, 0, 5)
        };
        contentPanel.Controls.Add(_labelTitle);

        _labelAppName = new Label
        {
            Dock = DockStyle.Top,
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoSize = true,
            Padding = new Padding(0, 5, 0, 5)
        };
        contentPanel.Controls.Add(_labelAppName);

        _labelMessage = new Label
        {
            Dock = DockStyle.Top,
            Font = new Font("Segoe UI", 14f),
            ForeColor = Color.FromArgb(200, 200, 200),
            TextAlign = ContentAlignment.MiddleCenter,
            AutoSize = true,
            Padding = new Padding(0, 5, 0, 5)
        };
        contentPanel.Controls.Add(_labelMessage);

        _labelTimeInfo = new Label
        {
            Dock = DockStyle.Top,
            Font = new Font("Segoe UI", 12f),
            ForeColor = Color.FromArgb(180, 180, 180),
            TextAlign = ContentAlignment.MiddleCenter,
            AutoSize = true,
            Padding = new Padding(0, 5, 0, 10)
        };
        contentPanel.Controls.Add(_labelTimeInfo);

        mainLayout.Controls.Add(contentPanel, 0, 1);

        Panel timePanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Height = 80
        };

        Label labelSelectTime = new Label
        {
            Text = LanguageManager.GetString("SelectBonusTime"),
            Font = new Font("Segoe UI", 12f),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 30
        };
        timePanel.Controls.Add(labelSelectTime);

        _comboBoxBonusTime = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 12f),
            Size = new Size(200, 35),
            Anchor = AnchorStyles.None
        };
        _comboBoxBonusTime.Items.Add(LanguageManager.GetString("5Minutes"));
        _comboBoxBonusTime.Items.Add(LanguageManager.GetString("10Minutes"));
        _comboBoxBonusTime.Items.Add(LanguageManager.GetString("15Minutes"));
        _comboBoxBonusTime.Items.Add(LanguageManager.GetString("30Minutes"));
        _comboBoxBonusTime.Items.Add(LanguageManager.GetString("1Hour"));
        _comboBoxBonusTime.SelectedIndex = 0;
        timePanel.Controls.Add(_comboBoxBonusTime);
        _comboBoxBonusTime.Left = (timePanel.Width - _comboBoxBonusTime.Width) / 2;
        _comboBoxBonusTime.Top = 35;
        timePanel.Resize += (s, e) =>
        {
            _comboBoxBonusTime.Left = (timePanel.Width - _comboBoxBonusTime.Width) / 2;
        };

        mainLayout.Controls.Add(timePanel, 0, 2);

        Panel buttonPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Height = 80
        };

        FlowLayoutPanel buttonFlowLayout = new FlowLayoutPanel
        {
            Dock = DockStyle.None,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Size = new Size(420, 50),
            Anchor = AnchorStyles.None
        };

        _buttonGracePeriod = new Button
        {
            Font = new Font("Segoe UI", 12f),
            Size = new Size(200, 50),
            BackColor = Color.FromArgb(40, 167, 69),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            AccessibleName = "GracePeriodButton",
            Margin = new Padding(5, 0, 5, 0)
        };
        _buttonGracePeriod.FlatAppearance.BorderSize = 0;
        _buttonGracePeriod.Click += OnGracePeriodClick;
        buttonFlowLayout.Controls.Add(_buttonGracePeriod);

        _buttonClose = new Button
        {
            Font = new Font("Segoe UI", 12f),
            Size = new Size(200, 50),
            BackColor = Color.FromArgb(220, 53, 69),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            AccessibleName = "CloseButton",
            Margin = new Padding(5, 0, 5, 0)
        };
        _buttonClose.FlatAppearance.BorderSize = 0;
        _buttonClose.Click += OnCloseClick;
        buttonFlowLayout.Controls.Add(_buttonClose);

        buttonPanel.Controls.Add(buttonFlowLayout);
        buttonFlowLayout.Left = (buttonPanel.Width - buttonFlowLayout.Width) / 2;
        buttonFlowLayout.Top = (buttonPanel.Height - buttonFlowLayout.Height) / 2;
        buttonPanel.Resize += (s, e) =>
        {
            buttonFlowLayout.Left = (buttonPanel.Width - buttonFlowLayout.Width) / 2;
            buttonFlowLayout.Top = (buttonPanel.Height - buttonFlowLayout.Height) / 2;
        };

        mainLayout.Controls.Add(buttonPanel, 0, 3);

        this.Controls.Add(mainLayout);

        AddDragHandlersToControl(mainLayout);

        this.KeyDown += OnKeyDown;
        this.MouseDown += OnFormMouseDown;
        this.MouseMove += OnFormMouseMove;
        this.MouseUp += OnFormMouseUp;
    }

    private void AddDragHandlersToControl(Control control)
    {
        control.MouseDown += OnFormMouseDown;
        control.MouseMove += OnFormMouseMove;
        control.MouseUp += OnFormMouseUp;

        foreach (Control child in control.Controls)
        {
            if (child is not Button && child is not ComboBox && child is not TextBox)
            {
                AddDragHandlersToControl(child);
            }
        }
    }

    private void OnFormMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _isDragging = true;
            _positionTimer?.Stop();
            
            _dragOffset = new Point(Cursor.Position.X - this.Left, Cursor.Position.Y - this.Top);
            
            this.Capture = true;
        }
    }

    private void OnFormMouseMove(object? sender, MouseEventArgs e)
    {
        if (_isDragging && _targetProcess != null && !_targetProcess.HasExited)
        {
            try
            {
                IntPtr hWnd = _targetProcess.MainWindowHandle;
                if (hWnd != IntPtr.Zero)
                {
                    if (GetWindowRect(hWnd, out RECT rect))
                    {
                        int width = rect.Right - rect.Left;
                        int height = rect.Bottom - rect.Top;
                        
                        int newX = Cursor.Position.X - _dragOffset.X;
                        int newY = Cursor.Position.Y - _dragOffset.Y;

                        Screen? screen = Screen.FromHandle(hWnd);
                        if (screen == null) screen = Screen.PrimaryScreen;
                        
                        if (newX < screen.WorkingArea.Left) newX = screen.WorkingArea.Left;
                        if (newY < screen.WorkingArea.Top) newY = screen.WorkingArea.Top;
                        if (newX + width > screen.WorkingArea.Right) newX = screen.WorkingArea.Right - width;
                        if (newY + height > screen.WorkingArea.Bottom) newY = screen.WorkingArea.Bottom - height;
                        
                        MoveWindow(hWnd, newX, newY, width, height, true);
                        
                        this.Bounds = new Rectangle(newX, newY, width, height);
                    }
                }
            }
            catch
            {
            }
        }
    }

    private void OnFormMouseUp(object? sender, MouseEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            this.Capture = false;
            _positionTimer?.Start();
        }
    }

    private Bitmap CreateDefaultIcon()
    {
        Bitmap bmp = new(64, 64);
        using Graphics g = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);

        using Pen pen = new(Color.FromArgb(255, 100, 100), 3);
        g.DrawEllipse(pen, 5, 5, 54, 54);

        using Font font = new("Segoe UI", 28f, FontStyle.Bold);
        using Brush brush = new SolidBrush(Color.FromArgb(255, 100, 100));
        StringFormat sf = new() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString("!", font, brush, new RectangleF(0, 0, 64, 64), sf);

        return bmp;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (!CanDismiss)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    private void OnGracePeriodClick(object? sender, EventArgs e)
    {
        if (_comboBoxBonusTime == null) return;

        int bonusMinutes = _comboBoxBonusTime.SelectedIndex switch
        {
            0 => 5,
            1 => 10,
            2 => 15,
            3 => 30,
            4 => 60,
            _ => 5,
        };

        if (_settingsManager.HasPassword())
        {
            using PasswordInputDialog passwordDialog = new PasswordInputDialog(_settingsManager);
            if (passwordDialog.ShowDialog() == DialogResult.OK && passwordDialog.IsPasswordCorrect)
            {
                _timeTracker.AddAppBonusTime(_appIdentifier, TimeSpan.FromMinutes(bonusMinutes));
                CanDismiss = true;
                Close();
            }
        }
        else
        {
            _timeTracker.AddAppBonusTime(_appIdentifier, TimeSpan.FromMinutes(bonusMinutes));
            CanDismiss = true;
            Close();
        }
    }

    private void OnCloseClick(object? sender, EventArgs e)
    {
        if (_targetProcess != null && !_targetProcess.HasExited)
        {
            try
            {
                _targetProcess.Kill();
            }
            catch { }
        }

        CanDismiss = true;
        Close();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!CanDismiss && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
        }
        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            LanguageManager.LanguageChanged -= OnLanguageChanged;
            _positionTimer?.Stop();
            _positionTimer?.Dispose();
            _zOrderTimer?.Stop();
            _zOrderTimer?.Dispose();
            components?.Dispose();
        }
        base.Dispose(disposing);
    }
}

public class PasswordInputDialog : Form
{
    private readonly SettingsManager _settingsManager;
    private TextBox? _textBoxPassword;
    private Button? _buttonOK;
    private Button? _buttonCancel;
    private Label? _labelPassword;
    private int _attemptsLeft = 3;

    public bool IsPasswordCorrect { get; private set; } = false;

    public PasswordInputDialog(SettingsManager settingsManager)
    {
        _settingsManager = settingsManager;
        InitializeComponent();
        ApplyLanguage();
    }

    private void InitializeComponent()
    {
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.StartPosition = FormStartPosition.CenterParent;
        this.ClientSize = new Size(450, 200);
        this.Text = LanguageManager.GetString("EnterPassword");
        this.BackColor = Color.FromArgb(50, 50, 50);
        this.ForeColor = Color.White;
        this.ControlBox = false;
        this.MinimizeBox = false;
        this.MaximizeBox = false;

        TableLayoutPanel layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(20, 15, 20, 15)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));

        _labelPassword = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 12f),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = true
        };
        layout.Controls.Add(_labelPassword);

        _textBoxPassword = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 14f),
            PasswordChar = '*'
        };
        layout.Controls.Add(_textBoxPassword);

        FlowLayoutPanel buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 10, 0, 0)
        };

        _buttonCancel = new Button
        {
            Text = LanguageManager.GetString("Cancel"),
            Font = new Font("Segoe UI", 11f),
            Size = new Size(100, 40),
            BackColor = Color.FromArgb(108, 117, 125),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(5, 0, 0, 0)
        };
        _buttonCancel.FlatAppearance.BorderSize = 0;
        _buttonCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        buttonPanel.Controls.Add(_buttonCancel);

        _buttonOK = new Button
        {
            Text = LanguageManager.GetString("OK"),
            Font = new Font("Segoe UI", 11f),
            Size = new Size(100, 40),
            BackColor = Color.FromArgb(0, 122, 204),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(5, 0, 5, 0)
        };
        _buttonOK.FlatAppearance.BorderSize = 0;
        _buttonOK.Click += OnOKClick;
        buttonPanel.Controls.Add(_buttonOK);

        layout.Controls.Add(buttonPanel);

        this.Controls.Add(layout);

        this.AcceptButton = _buttonOK;
        this.CancelButton = _buttonCancel;
    }

    private void ApplyLanguage()
    {
        _labelPassword!.Text = LanguageManager.GetString("EnterPasswordToUnlock");
        _buttonOK!.Text = LanguageManager.GetString("OK");
        _buttonCancel!.Text = LanguageManager.GetString("Cancel");
    }

    private void OnOKClick(object? sender, EventArgs e)
    {
        if (_settingsManager.VerifyPassword(_textBoxPassword!.Text))
        {
            IsPasswordCorrect = true;
            DialogResult = DialogResult.OK;
            Close();
        }
        else
        {
            _attemptsLeft--;
            if (_attemptsLeft > 0)
            {
                MessageBox.Show(
                    string.Format(LanguageManager.GetString("IncorrectPassword"), _attemptsLeft),
                    LanguageManager.GetString("Error"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                _textBoxPassword.Clear();
                _textBoxPassword.Focus();
            }
            else
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }
    }
}
