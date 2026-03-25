using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ScreenTimeController;

public class SettingsForm : Form
{
    private readonly SettingsManager _settingsManager;
    private bool _isPasswordVerified;
    private bool _isClosing;

    private Label? _labelSunday;
    private NumericUpDown? _numericUpDownSundayHours;
    private NumericUpDown? _numericUpDownSundayMinutes;
    private Label? _labelSundayHours;
    private Label? _labelSundayMinutes;

    private Label? _labelMonday;
    private NumericUpDown? _numericUpDownMondayHours;
    private NumericUpDown? _numericUpDownMondayMinutes;
    private Label? _labelMondayHours;
    private Label? _labelMondayMinutes;

    private Label? _labelTuesday;
    private NumericUpDown? _numericUpDownTuesdayHours;
    private NumericUpDown? _numericUpDownTuesdayMinutes;
    private Label? _labelTuesdayHours;
    private Label? _labelTuesdayMinutes;

    private Label? _labelWednesday;
    private NumericUpDown? _numericUpDownWednesdayHours;
    private NumericUpDown? _numericUpDownWednesdayMinutes;
    private Label? _labelWednesdayHours;
    private Label? _labelWednesdayMinutes;

    private Label? _labelThursday;
    private NumericUpDown? _numericUpDownThursdayHours;
    private NumericUpDown? _numericUpDownThursdayMinutes;
    private Label? _labelThursdayHours;
    private Label? _labelThursdayMinutes;

    private Label? _labelFriday;
    private NumericUpDown? _numericUpDownFridayHours;
    private NumericUpDown? _numericUpDownFridayMinutes;
    private Label? _labelFridayHours;
    private Label? _labelFridayMinutes;

    private Label? _labelSaturday;
    private NumericUpDown? _numericUpDownSaturdayHours;
    private NumericUpDown? _numericUpDownSaturdayMinutes;
    private Label? _labelSaturdayHours;
    private Label? _labelSaturdayMinutes;

    private Label? _labelLanguage;
    private ComboBox? _comboBoxLanguage;
    private CheckBox? _checkBoxEnablePasswordLock;
    private Button? _buttonOK;
    private Button? _buttonCancel;
    private Button? _buttonChangePassword;
    private Button? _buttonApplyToAll;
    private IContainer? components;

    public SettingsForm(SettingsManager settingsManager)
    {
        _isClosing = false;
        InitializeComponent();
        _settingsManager = settingsManager;
        _isPasswordVerified = false;
        LoadSettings();
        ApplyLanguage();
        LanguageManager.LanguageChanged += OnLanguageChanged;
        ShowPasswordPrompt();
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        ApplyLanguage();
    }

    private void ApplyLanguage()
    {
        Text = LanguageManager.GetString("ScreenTimeSettings");
        _labelSunday!.Text = LanguageManager.GetString("Sunday") + ":";
        _labelMonday!.Text = LanguageManager.GetString("Monday") + ":";
        _labelTuesday!.Text = LanguageManager.GetString("Tuesday") + ":";
        _labelWednesday!.Text = LanguageManager.GetString("Wednesday") + ":";
        _labelThursday!.Text = LanguageManager.GetString("Thursday") + ":";
        _labelFriday!.Text = LanguageManager.GetString("Friday") + ":";
        _labelSaturday!.Text = LanguageManager.GetString("Saturday") + ":";
        _labelSundayHours!.Text = LanguageManager.GetString("Hours");
        _labelMondayHours!.Text = LanguageManager.GetString("Hours");
        _labelTuesdayHours!.Text = LanguageManager.GetString("Hours");
        _labelWednesdayHours!.Text = LanguageManager.GetString("Hours");
        _labelThursdayHours!.Text = LanguageManager.GetString("Hours");
        _labelFridayHours!.Text = LanguageManager.GetString("Hours");
        _labelSaturdayHours!.Text = LanguageManager.GetString("Hours");
        _labelSundayMinutes!.Text = LanguageManager.GetString("Minutes");
        _labelMondayMinutes!.Text = LanguageManager.GetString("Minutes");
        _labelTuesdayMinutes!.Text = LanguageManager.GetString("Minutes");
        _labelWednesdayMinutes!.Text = LanguageManager.GetString("Minutes");
        _labelThursdayMinutes!.Text = LanguageManager.GetString("Minutes");
        _labelFridayMinutes!.Text = LanguageManager.GetString("Minutes");
        _labelSaturdayMinutes!.Text = LanguageManager.GetString("Minutes");
        _labelLanguage!.Text = LanguageManager.GetString("Language") + ":";
        _checkBoxEnablePasswordLock!.Text = LanguageManager.GetString("EnablePasswordLock");
        _buttonOK!.Text = LanguageManager.GetString("OK");
        _buttonCancel!.Text = LanguageManager.GetString("Cancel");
        _buttonApplyToAll!.Text = LanguageManager.GetString("ApplyToAllDays");
        _buttonChangePassword!.Text = LanguageManager.GetString("PasswordSettings");
    }

    private void ShowPasswordPrompt()
    {
        if (!_settingsManager.HasPassword())
        {
            _isPasswordVerified = true;
            return;
        }
        using PasswordForm passwordForm = new(_settingsManager);
        if (passwordForm.ShowDialog() == DialogResult.OK && passwordForm.IsPasswordCorrect)
        {
            _isPasswordVerified = true;
            return;
        }
        _isClosing = true;
        DialogResult = DialogResult.Cancel;
        Close();
    }

    private void LoadSettings()
    {
        LoadDaySetting(_settingsManager.SundayLimit, _numericUpDownSundayHours!, _numericUpDownSundayMinutes!);
        LoadDaySetting(_settingsManager.MondayLimit, _numericUpDownMondayHours!, _numericUpDownMondayMinutes!);
        LoadDaySetting(_settingsManager.TuesdayLimit, _numericUpDownTuesdayHours!, _numericUpDownTuesdayMinutes!);
        LoadDaySetting(_settingsManager.WednesdayLimit, _numericUpDownWednesdayHours!, _numericUpDownWednesdayMinutes!);
        LoadDaySetting(_settingsManager.ThursdayLimit, _numericUpDownThursdayHours!, _numericUpDownThursdayMinutes!);
        LoadDaySetting(_settingsManager.FridayLimit, _numericUpDownFridayHours!, _numericUpDownFridayMinutes!);
        LoadDaySetting(_settingsManager.SaturdayLimit, _numericUpDownSaturdayHours!, _numericUpDownSaturdayMinutes!);
        _comboBoxLanguage!.Items.Clear();
        foreach (Language value in Enum.GetValues(typeof(Language)))
        {
            _comboBoxLanguage.Items.Add(LanguageManager.GetLanguageName(value));
        }
        _comboBoxLanguage.SelectedIndex = (int)_settingsManager.Language;
        _checkBoxEnablePasswordLock!.Checked = _settingsManager.EnablePasswordLock;
    }

    private void LoadDaySetting(TimeSpan limit, NumericUpDown hoursControl, NumericUpDown minutesControl)
    {
        hoursControl.Value = Math.Min(limit.Hours, 24);
        minutesControl.Value = limit.Minutes;
    }

    private void OnOKClick(object? sender, EventArgs e)
    {
        if (!_isClosing)
        {
            _isClosing = true;
            SaveSettings();
            LanguageManager.LanguageChanged -= OnLanguageChanged;
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    private void OnCancelClick(object? sender, EventArgs e)
    {
        if (!_isClosing)
        {
            _isClosing = true;
            LanguageManager.LanguageChanged -= OnLanguageChanged;
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }

    private void SettingsForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Return)
        {
            OnOKClick(sender, e);
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Escape)
        {
            OnCancelClick(sender, e);
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    private void OnApplyToAllClick(object? sender, EventArgs e)
    {
        decimal hours = _numericUpDownSundayHours!.Value;
        decimal minutes = _numericUpDownSundayMinutes!.Value;
        _numericUpDownMondayHours!.Value = hours;
        _numericUpDownMondayMinutes!.Value = minutes;
        _numericUpDownTuesdayHours!.Value = hours;
        _numericUpDownTuesdayMinutes!.Value = minutes;
        _numericUpDownWednesdayHours!.Value = hours;
        _numericUpDownWednesdayMinutes!.Value = minutes;
        _numericUpDownThursdayHours!.Value = hours;
        _numericUpDownThursdayMinutes!.Value = minutes;
        _numericUpDownFridayHours!.Value = hours;
        _numericUpDownFridayMinutes!.Value = minutes;
        _numericUpDownSaturdayHours!.Value = hours;
        _numericUpDownSaturdayMinutes!.Value = minutes;
        MessageBox.Show(LanguageManager.GetString("AllDaysSet"), LanguageManager.GetString("Info"), MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
    }

    private void OnChangePasswordClick(object? sender, EventArgs e)
    {
        using ChangePasswordForm changePasswordForm = new(_settingsManager);
        changePasswordForm.ShowDialog();
    }

    private void SaveSettings()
    {
        _settingsManager.SundayLimit = GetTimeFromControls(_numericUpDownSundayHours!, _numericUpDownSundayMinutes!);
        _settingsManager.MondayLimit = GetTimeFromControls(_numericUpDownMondayHours!, _numericUpDownMondayMinutes!);
        _settingsManager.TuesdayLimit = GetTimeFromControls(_numericUpDownTuesdayHours!, _numericUpDownTuesdayMinutes!);
        _settingsManager.WednesdayLimit = GetTimeFromControls(_numericUpDownWednesdayHours!, _numericUpDownWednesdayMinutes!);
        _settingsManager.ThursdayLimit = GetTimeFromControls(_numericUpDownThursdayHours!, _numericUpDownThursdayMinutes!);
        _settingsManager.FridayLimit = GetTimeFromControls(_numericUpDownFridayHours!, _numericUpDownFridayMinutes!);
        _settingsManager.SaturdayLimit = GetTimeFromControls(_numericUpDownSaturdayHours!, _numericUpDownSaturdayMinutes!);
        _settingsManager.Language = (Language)_comboBoxLanguage!.SelectedIndex;
        _settingsManager.EnablePasswordLock = _checkBoxEnablePasswordLock!.Checked;
        _settingsManager.SaveSettings();
    }

    private static TimeSpan GetTimeFromControls(NumericUpDown hoursControl, NumericUpDown minutesControl)
    {
        int hours = (int)hoursControl.Value;
        int minutes = (int)minutesControl.Value;
        return new TimeSpan(hours, minutes, 0);
    }

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        Rectangle screenBounds = Screen.PrimaryScreen.WorkingArea;
        int maxWidth = (int)(screenBounds.Width * 0.9);
        int maxHeight = (int)(screenBounds.Height * 0.9);
        int windowWidth = Math.Min(800, maxWidth);
        int windowHeight = Math.Min(700, maxHeight);
        ClientSize = new Size(windowWidth, windowHeight);
        MinimumSize = new Size(500, 500);
        Text = "Screen Time Settings";
        FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(240, 240, 240);
        KeyPreview = true;
        KeyDown += new KeyEventHandler(SettingsForm_KeyDown);

        Panel outerPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(15)
        };

        TableLayoutPanel mainTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            AutoScroll = true
        };
        mainTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        mainTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 80f));
        mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 70f));

        TableLayoutPanel daysTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            RowCount = 9,
            AutoScroll = true,
            Padding = new Padding(10)
        };
        daysTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        daysTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18f));
        daysTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12f));
        daysTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18f));
        daysTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12f));
        for (int i = 0; i < 9; i++)
        {
            daysTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 55f));
        }

        AddDayRow(daysTable, 0, ref _labelSunday, ref _numericUpDownSundayHours, ref _labelSundayHours, ref _numericUpDownSundayMinutes, ref _labelSundayMinutes);
        AddDayRow(daysTable, 1, ref _labelMonday, ref _numericUpDownMondayHours, ref _labelMondayHours, ref _numericUpDownMondayMinutes, ref _labelMondayMinutes);
        AddDayRow(daysTable, 2, ref _labelTuesday, ref _numericUpDownTuesdayHours, ref _labelTuesdayHours, ref _numericUpDownTuesdayMinutes, ref _labelTuesdayMinutes);
        AddDayRow(daysTable, 3, ref _labelWednesday, ref _numericUpDownWednesdayHours, ref _labelWednesdayHours, ref _numericUpDownWednesdayMinutes, ref _labelWednesdayMinutes);
        AddDayRow(daysTable, 4, ref _labelThursday, ref _numericUpDownThursdayHours, ref _labelThursdayHours, ref _numericUpDownThursdayMinutes, ref _labelThursdayMinutes);
        AddDayRow(daysTable, 5, ref _labelFriday, ref _numericUpDownFridayHours, ref _labelFridayHours, ref _numericUpDownFridayMinutes, ref _labelFridayMinutes);
        AddDayRow(daysTable, 6, ref _labelSaturday, ref _numericUpDownSaturdayHours, ref _labelSaturdayHours, ref _numericUpDownSaturdayMinutes, ref _labelSaturdayMinutes);

        _labelLanguage = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };
        daysTable.Controls.Add(_labelLanguage, 0, 7);

        _comboBoxLanguage = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 11f)
        };
        daysTable.SetColumnSpan(_comboBoxLanguage, 4);
        daysTable.Controls.Add(_comboBoxLanguage, 1, 7);

        _checkBoxEnablePasswordLock = new CheckBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 11f),
            Checked = true,
            TextAlign = ContentAlignment.MiddleLeft
        };
        daysTable.SetColumnSpan(_checkBoxEnablePasswordLock, 4);
        daysTable.Controls.Add(_checkBoxEnablePasswordLock, 1, 8);

        Label labelPasswordLock = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Text = LanguageManager.GetString("PasswordLock") + ":"
        };
        daysTable.Controls.Add(labelPasswordLock, 0, 8);

        mainTable.Controls.Add(daysTable, 0, 0);

        FlowLayoutPanel buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(10),
            Anchor = AnchorStyles.Left | AnchorStyles.Right
        };

        _buttonApplyToAll = new Button
        {
            Font = new Font("Segoe UI", 11f),
            Size = new Size(180, 45),
            BackColor = Color.FromArgb(0, 122, 204),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(5),
            Anchor = AnchorStyles.Left,
            AccessibleName = "ApplyToAllButton"
        };
        _buttonApplyToAll.FlatAppearance.BorderSize = 0;
        _buttonApplyToAll.Click += new EventHandler(OnApplyToAllClick);
        buttonPanel.Controls.Add(_buttonApplyToAll);

        _buttonChangePassword = new Button
        {
            Font = new Font("Segoe UI", 11f),
            Size = new Size(180, 45),
            BackColor = Color.FromArgb(0, 122, 204),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(5),
            Anchor = AnchorStyles.Left,
            AccessibleName = "ChangePasswordButton"
        };
        _buttonChangePassword.FlatAppearance.BorderSize = 0;
        _buttonChangePassword.Click += new EventHandler(OnChangePasswordClick);
        buttonPanel.Controls.Add(_buttonChangePassword);

        mainTable.Controls.Add(buttonPanel, 0, 1);

        FlowLayoutPanel okCancelPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(10)
        };

        _buttonCancel = new Button
        {
            Font = new Font("Segoe UI", 11f),
            Size = new Size(120, 45),
            BackColor = Color.FromArgb(200, 200, 200),
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(5),
            AccessibleName = "CancelButton"
        };
        _buttonCancel.FlatAppearance.BorderSize = 0;
        _buttonCancel.Click += new EventHandler(OnCancelClick);
        okCancelPanel.Controls.Add(_buttonCancel);

        _buttonOK = new Button
        {
            Font = new Font("Segoe UI", 11f),
            Size = new Size(120, 45),
            BackColor = Color.FromArgb(0, 122, 204),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(5),
            AccessibleName = "OKButton"
        };
        _buttonOK.FlatAppearance.BorderSize = 0;
        _buttonOK.Click += new EventHandler(OnOKClick);
        okCancelPanel.Controls.Add(_buttonOK);

        mainTable.Controls.Add(okCancelPanel, 0, 2);

        outerPanel.Controls.Add(mainTable);
        Controls.Add(outerPanel);

        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        if (IsHandleCreated && !IsDisposed)
        {
            try
            {
                BeginInvoke(new Action(AdjustWindowSize));
            }
            catch { }
        }
    }

    private void AdjustWindowSize()
    {
        try
        {
            Rectangle screenBounds = Screen.PrimaryScreen.WorkingArea;
            int maxWidth = (int)(screenBounds.Width * 0.9);
            int maxHeight = (int)(screenBounds.Height * 0.9);
            int windowWidth = Math.Min(800, maxWidth);
            int windowHeight = Math.Min(700, maxHeight);
            if (Width > maxWidth || Height > maxHeight)
            {
                ClientSize = new Size(windowWidth, windowHeight);
                CenterToScreen();
            }
        }
        catch { }
    }

    private void AddDayRow(TableLayoutPanel table, int row, ref Label? dayLabel, ref NumericUpDown? hoursNum, ref Label? hoursLabel, ref NumericUpDown? minutesNum, ref Label? minutesLabel)
    {
        dayLabel = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };
        table.Controls.Add(dayLabel, 0, row);

        hoursNum = new NumericUpDown
        {
            Dock = DockStyle.Fill,
            Minimum = 0,
            Maximum = 24,
            Font = new Font("Segoe UI", 11f)
        };
        table.Controls.Add(hoursNum, 1, row);

        hoursLabel = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10f),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };
        table.Controls.Add(hoursLabel, 2, row);

        minutesNum = new NumericUpDown
        {
            Dock = DockStyle.Fill,
            Minimum = 0,
            Maximum = 59,
            Font = new Font("Segoe UI", 11f)
        };
        table.Controls.Add(minutesNum, 3, row);

        minutesLabel = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10f),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };
        table.Controls.Add(minutesLabel, 4, row);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            }
            catch { }
            components?.Dispose();
        }
        base.Dispose(disposing);
    }
}
