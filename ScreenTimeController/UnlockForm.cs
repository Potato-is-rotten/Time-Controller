using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScreenTimeController;

public class UnlockForm : Form
{
    private readonly SettingsManager _settingsManager;
    private readonly TimeTracker _timeTracker;
    private Panel? _centerPanel;
    private Panel? _passwordPanel;
    private Panel? _bonusPanel;
    private Label? _labelTitle;
    private Label? _labelExpired;
    private Label? _labelPassword;
    private TextBox? _textBoxPassword;
    private Button? _buttonUnlock;
    private Label? _labelSelectTime;
    private ComboBox? _comboBoxBonusTime;
    private Button? _buttonAddTime;
    private int _attemptsLeft;
    private bool _isClosing;
    private bool _showingBonusOptions;
    private IContainer? components;

    public bool IsPasswordCorrect { get; private set; }
    public TimeSpan BonusTimeAdded { get; private set; }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    public UnlockForm(SettingsManager settingsManager, TimeTracker timeTracker)
    {
        _isClosing = false;
        _showingBonusOptions = false;
        _settingsManager = settingsManager;
        _timeTracker = timeTracker;
        _attemptsLeft = 3;
        IsPasswordCorrect = false;
        BonusTimeAdded = TimeSpan.Zero;
        InitializeComponent();
        ApplyLanguage();
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Maximized;
        BackColor = Color.FromArgb(30, 30, 30);
        TopMost = true;
        ShowInTaskbar = false;
        Load += UnlockForm_Load;
        Shown += UnlockForm_Shown;
    }

    private void UnlockForm_Load(object? sender, EventArgs e)
    {
        SetFullScreen();

        Screen? screen = Screen.FromPoint(Cursor.Position);
        if (screen == null)
        {
            screen = Screen.PrimaryScreen;
        }
        float scaleFactor = Math.Min(screen.WorkingArea.Width / 1920f, screen.WorkingArea.Height / 1080f);
        scaleFactor = Math.Max(scaleFactor, 0.75f);

        int panelWidth = (int)(500 * scaleFactor);
        int panelHeight = (int)(350 * scaleFactor);
        _centerPanel!.Size = new Size(panelWidth, panelHeight);

        int titleSize = Math.Max(12, (int)(12 * scaleFactor));
        _labelTitle!.Font = new Font("Segoe UI", titleSize, FontStyle.Bold);
        _labelTitle.Size = new Size(panelWidth, (int)(50 * scaleFactor));
        _labelTitle.Location = new Point(0, (int)(20 * scaleFactor));

        _labelExpired!.Font = new Font("Segoe UI", Math.Max(11, (int)(14 * scaleFactor)));
        _labelExpired.Size = new Size(panelWidth, (int)(40 * scaleFactor));
        _labelExpired.Location = new Point(0, (int)(80 * scaleFactor));

        int innerPanelWidth = (int)(440 * scaleFactor);
        int innerPanelHeight = (int)(200 * scaleFactor);
        _passwordPanel!.Size = new Size(innerPanelWidth, innerPanelHeight);
        _passwordPanel.Location = new Point((panelWidth - innerPanelWidth) / 2, (int)(130 * scaleFactor));

        int fontSize = Math.Max(11, (int)(14 * scaleFactor));
        _labelPassword!.Font = new Font("Segoe UI", fontSize);
        _labelPassword.Size = new Size(innerPanelWidth, (int)(40 * scaleFactor));

        _textBoxPassword!.Font = new Font("Segoe UI", fontSize);
        _textBoxPassword.Size = new Size(innerPanelWidth, (int)(40 * scaleFactor));
        _textBoxPassword.Location = new Point(0, (int)(50 * scaleFactor));

        int buttonWidth = (int)(200 * scaleFactor);
        int buttonHeight = (int)(50 * scaleFactor);
        _buttonUnlock!.Font = new Font("Segoe UI", fontSize);
        _buttonUnlock.Size = new Size(buttonWidth, buttonHeight);
        _buttonUnlock.Location = new Point((innerPanelWidth - buttonWidth) / 2, (int)(110 * scaleFactor));

        _bonusPanel!.Size = new Size(innerPanelWidth, innerPanelHeight);
        _bonusPanel.Location = _passwordPanel.Location;

        _labelSelectTime!.Font = new Font("Segoe UI", fontSize);
        _labelSelectTime.Size = new Size(innerPanelWidth, (int)(40 * scaleFactor));

        _comboBoxBonusTime!.Font = new Font("Segoe UI", fontSize);
        _comboBoxBonusTime.Size = new Size(innerPanelWidth, (int)(40 * scaleFactor));
        _comboBoxBonusTime.Location = new Point(0, (int)(50 * scaleFactor));

        _buttonAddTime!.Font = new Font("Segoe UI", fontSize);
        _buttonAddTime.Size = new Size(buttonWidth, buttonHeight);
        _buttonAddTime.Location = new Point((innerPanelWidth - buttonWidth) / 2, (int)(110 * scaleFactor));

        CenterThePanel();
    }

    private void UnlockForm_Shown(object? sender, EventArgs e)
    {
        SetFullScreen();
        CenterThePanel();
        SetForegroundWindow(Handle);
        ShowWindow(Handle, 3);
        if (_textBoxPassword != null && _textBoxPassword.Visible)
        {
            _textBoxPassword.Focus();
        }
    }

    private void SetFullScreen()
    {
        Screen? screen = Screen.FromPoint(Cursor.Position);
        if (screen == null)
        {
            screen = Screen.PrimaryScreen;
        }
        Bounds = screen!.Bounds;
        WindowState = FormWindowState.Maximized;
        TopMost = true;
        Focus();
        BringToFront();
    }

    private void CenterThePanel()
    {
        _centerPanel!.Location = new Point((ClientSize.Width - _centerPanel.Width) / 2, (ClientSize.Height - _centerPanel.Height) / 2);
    }

    private void ApplyLanguage()
    {
        _labelTitle!.Text = LanguageManager.GetString("AppTitle");
        _labelExpired!.Text = LanguageManager.GetString("TimeExpired");
        _labelPassword!.Text = LanguageManager.GetString("EnterPasswordToUnlock");
        _buttonUnlock!.Text = LanguageManager.GetString("Unlock");
        _labelSelectTime!.Text = LanguageManager.GetString("SelectBonusTime");
        _buttonAddTime!.Text = LanguageManager.GetString("AddBonusTime");
    }

    private void OnUnlockClick(object? sender, EventArgs e)
    {
        if (_isClosing)
        {
            return;
        }
        if (_settingsManager.HasPassword())
        {
            if (_settingsManager.VerifyPassword(_textBoxPassword!.Text))
            {
                ShowBonusTimeOptions();
                return;
            }
            _attemptsLeft--;
            if (_attemptsLeft > 0)
            {
                MessageBox.Show(string.Format(LanguageManager.GetString("IncorrectPassword"), _attemptsLeft), LanguageManager.GetString("Error"), MessageBoxButtons.OK, MessageBoxIcon.Hand);
                _textBoxPassword.Clear();
                _textBoxPassword.Focus();
                BringToFront();
                Focus();
            }
            else
            {
                _isClosing = true;
                IsPasswordCorrect = false;
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }
        else
        {
            ShowBonusTimeOptions();
        }
    }

    private void ShowBonusTimeOptions()
    {
        _showingBonusOptions = true;
        _passwordPanel!.Visible = false;
        _bonusPanel!.Visible = true;
        _comboBoxBonusTime!.Focus();
    }

    private void OnAddTimeClick(object? sender, EventArgs e)
    {
        if (!_isClosing)
        {
            int num = _comboBoxBonusTime!.SelectedIndex switch
            {
                0 => 5,
                1 => 10,
                2 => 15,
                3 => 30,
                4 => 60,
                _ => 5,
            };
            BonusTimeAdded = TimeSpan.FromMinutes(num);
            _timeTracker.AddBonusTime(BonusTimeAdded);
            IsPasswordCorrect = true;
            _isClosing = true;
            MessageBox.Show(string.Format(LanguageManager.GetString("BonusTimeAdded"), num), LanguageManager.GetString("Success"), MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    private void UnlockForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!_isClosing)
        {
            _isClosing = true;
            if (DialogResult == DialogResult.None)
            {
                DialogResult = DialogResult.Cancel;
            }
        }
    }

    private void UnlockForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Return)
        {
            if (!_showingBonusOptions)
            {
                OnUnlockClick(sender, e);
            }
            else
            {
                OnAddTimeClick(sender, e);
            }
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Escape)
        {
            _isClosing = true;
            IsPasswordCorrect = false;
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        Text = "Unlock";
        StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        KeyPreview = true;
        KeyDown += new KeyEventHandler(UnlockForm_KeyDown);
        FormClosing += new FormClosingEventHandler(UnlockForm_FormClosing);

        _centerPanel = new Panel
        {
            Size = new Size(500, 350),
            BackColor = Color.FromArgb(50, 50, 50)
        };

        _labelTitle = new Label
        {
            Text = LanguageManager.GetString("AppTitle"),
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(0, 20),
            Size = new Size(500, 50)
        };
        _centerPanel.Controls.Add(_labelTitle);

        _labelExpired = new Label
        {
            Text = LanguageManager.GetString("TimeExpired"),
            Font = new Font("Segoe UI", 14f),
            ForeColor = Color.FromArgb(255, 100, 100),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(0, 80),
            Size = new Size(500, 40)
        };
        _centerPanel.Controls.Add(_labelExpired);

        _passwordPanel = new Panel
        {
            Location = new Point(30, 130),
            Size = new Size(440, 200),
            BackColor = Color.FromArgb(50, 50, 50)
        };

        _labelPassword = new Label
        {
            Font = new Font("Segoe UI", 14f),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(0, 0),
            Size = new Size(440, 40)
        };
        _passwordPanel.Controls.Add(_labelPassword);

        _textBoxPassword = new TextBox
        {
            PasswordChar = '*',
            Font = new Font("Segoe UI", 14f),
            Location = new Point(0, 50),
            Size = new Size(440, 40)
        };
        _passwordPanel.Controls.Add(_textBoxPassword);

        _buttonUnlock = new Button
        {
            Font = new Font("Segoe UI", 14f),
            Size = new Size(200, 50),
            Location = new Point(120, 110),
            BackColor = Color.FromArgb(0, 122, 204),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            AccessibleName = "UnlockButton"
        };
        _buttonUnlock.FlatAppearance.BorderSize = 0;
        _buttonUnlock.Click += new EventHandler(OnUnlockClick);
        _passwordPanel.Controls.Add(_buttonUnlock);
        _centerPanel.Controls.Add(_passwordPanel);

        _bonusPanel = new Panel
        {
            Location = new Point(30, 130),
            Size = new Size(440, 200),
            BackColor = Color.FromArgb(50, 50, 50),
            Visible = false
        };

        _labelSelectTime = new Label
        {
            Font = new Font("Segoe UI", 14f),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(0, 0),
            Size = new Size(440, 40)
        };
        _bonusPanel.Controls.Add(_labelSelectTime);

        _comboBoxBonusTime = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 14f),
            Location = new Point(0, 50),
            Size = new Size(440, 40)
        };
        _comboBoxBonusTime.Items.Add(LanguageManager.GetString("5Minutes"));
        _comboBoxBonusTime.Items.Add(LanguageManager.GetString("10Minutes"));
        _comboBoxBonusTime.Items.Add(LanguageManager.GetString("15Minutes"));
        _comboBoxBonusTime.Items.Add(LanguageManager.GetString("30Minutes"));
        _comboBoxBonusTime.Items.Add(LanguageManager.GetString("1Hour"));
        _comboBoxBonusTime.SelectedIndex = 0;
        _bonusPanel.Controls.Add(_comboBoxBonusTime);

        _buttonAddTime = new Button
        {
            Font = new Font("Segoe UI", 14f),
            Size = new Size(200, 50),
            Location = new Point(120, 110),
            BackColor = Color.FromArgb(40, 167, 69),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            AccessibleName = "AddTimeButton"
        };
        _buttonAddTime.FlatAppearance.BorderSize = 0;
        _buttonAddTime.Click += new EventHandler(OnAddTimeClick);
        _bonusPanel.Controls.Add(_buttonAddTime);
        _centerPanel.Controls.Add(_bonusPanel);

        Controls.Add(_centerPanel);
        Resize += delegate { CenterThePanel(); };
    }
}
