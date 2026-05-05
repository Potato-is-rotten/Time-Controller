using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ScreenTimeController;

/// <summary>
/// User control for managing application time limits.
/// </summary>
public class AppLimitSettingsControl : UserControl
{
    private SettingsManager? _settingsManager;
    private TimeTracker? _timeTracker;
    private DataGridView? _dataGridViewApps;
    private Button? _buttonAdd;
    private Button? _buttonEdit;
    private Button? _buttonDelete;
    private CheckBox? _checkBoxEnableAppLock;
    private ComboBox? _comboBoxLockMode;
    private Label? _labelLockMode;
    private Label? _labelAppList;
    private Panel? _panelButtons;
    private IContainer? components;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public SettingsManager? SettingsManager
    {
        get => _settingsManager;
        set
        {
            _settingsManager = value;
            if (_settingsManager != null)
            {
                LoadSettings();
            }
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TimeTracker? TimeTracker
    {
        get => _timeTracker;
        set
        {
            _timeTracker = value;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AppLimitSettingsControl"/> class.
    /// </summary>
    public AppLimitSettingsControl()
    {
        InitializeComponent();
        ApplyLanguage();
        LanguageManager.LanguageChanged += OnLanguageChanged;
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        ApplyLanguage();
    }

    private void ApplyLanguage()
    {
        _labelLockMode!.Text = LanguageManager.GetString("LockMode") + ":";
        _labelAppList!.Text = LanguageManager.GetString("ApplicationList") + ":";
        _checkBoxEnableAppLock!.Text = LanguageManager.GetString("EnableAppLock");
        _buttonAdd!.Text = LanguageManager.GetString("Add");
        _buttonEdit!.Text = LanguageManager.GetString("Edit");
        _buttonDelete!.Text = LanguageManager.GetString("Delete");

        if (_dataGridViewApps != null && _dataGridViewApps.Columns.Count > 0)
        {
            _dataGridViewApps.Columns[0].HeaderText = LanguageManager.GetString("Application");
            _dataGridViewApps.Columns[1].HeaderText = LanguageManager.GetString("DailyLimit");
            _dataGridViewApps.Columns[2].HeaderText = LanguageManager.GetString("RemainingTime");
            _dataGridViewApps.Columns[3].HeaderText = LanguageManager.GetString("Status");
        }

        if (_comboBoxLockMode != null)
        {
            _comboBoxLockMode.Items.Clear();
            _comboBoxLockMode.Items.Add(LanguageManager.GetString("FullScreenLock"));
            _comboBoxLockMode.Items.Add(LanguageManager.GetString("PerAppLock"));
            if (_settingsManager != null)
            {
                _comboBoxLockMode.SelectedIndex = (int)_settingsManager.CurrentLockMode;
            }
        }
    }

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        this.Dock = DockStyle.Fill;
        this.BackColor = Color.FromArgb(240, 240, 240);
        this.Padding = new Padding(10);

        TableLayoutPanel mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            AutoScroll = true
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        TableLayoutPanel modePanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1
        };
        modePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120f));
        modePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200f));
        modePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        _labelLockMode = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };
        modePanel.Controls.Add(_labelLockMode, 0, 0);

        _comboBoxLockMode = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 11f)
        };
        _comboBoxLockMode.SelectedIndexChanged += OnLockModeChanged;
        modePanel.Controls.Add(_comboBoxLockMode, 1, 0);

        mainLayout.Controls.Add(modePanel, 0, 0);

        _checkBoxEnableAppLock = new CheckBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 11f),
            Checked = false,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _checkBoxEnableAppLock.CheckedChanged += OnEnableAppLockChanged;
        mainLayout.Controls.Add(_checkBoxEnableAppLock, 0, 1);

        _labelAppList = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };
        mainLayout.Controls.Add(_labelAppList, 0, 2);

        TableLayoutPanel contentPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        contentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        contentPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        contentPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));

        _dataGridViewApps = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            RowHeadersVisible = false,
            Font = new Font("Segoe UI", 10f),
            EnableHeadersVisualStyles = false,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            },
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                SelectionBackColor = Color.FromArgb(0, 122, 204),
                SelectionForeColor = Color.White
            },
            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(245, 245, 245)
            }
        };

        _dataGridViewApps.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "AppName",
            HeaderText = "Application",
            FillWeight = 30
        });
        _dataGridViewApps.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "TimeLimit",
            HeaderText = "Daily Limit",
            FillWeight = 20
        });
        _dataGridViewApps.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "RemainingTime",
            HeaderText = "Remaining",
            FillWeight = 20
        });
        _dataGridViewApps.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Status",
            HeaderText = "Status",
            FillWeight = 15
        });

        _dataGridViewApps.DoubleClick += OnDataGridViewDoubleClick;
        contentPanel.Controls.Add(_dataGridViewApps, 0, 0);

        _panelButtons = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(240, 240, 240)
        };

        FlowLayoutPanel buttonFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 5, 0, 5)
        };

        _buttonAdd = new Button
        {
            Font = new Font("Segoe UI", 10f),
            Size = new Size(100, 35),
            BackColor = Color.FromArgb(0, 122, 204),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(5, 0, 5, 0),
            AccessibleName = "AddAppButton"
        };
        _buttonAdd.FlatAppearance.BorderSize = 0;
        _buttonAdd.Click += OnAddClick;
        buttonFlow.Controls.Add(_buttonAdd);

        _buttonEdit = new Button
        {
            Font = new Font("Segoe UI", 10f),
            Size = new Size(100, 35),
            BackColor = Color.FromArgb(0, 122, 204),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(5, 0, 5, 0),
            AccessibleName = "EditAppButton"
        };
        _buttonEdit.FlatAppearance.BorderSize = 0;
        _buttonEdit.Click += OnEditClick;
        buttonFlow.Controls.Add(_buttonEdit);

        _buttonDelete = new Button
        {
            Font = new Font("Segoe UI", 10f),
            Size = new Size(100, 35),
            BackColor = Color.FromArgb(220, 53, 69),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(5, 0, 5, 0),
            AccessibleName = "DeleteAppButton"
        };
        _buttonDelete.FlatAppearance.BorderSize = 0;
        _buttonDelete.Click += OnDeleteClick;
        buttonFlow.Controls.Add(_buttonDelete);

        _panelButtons.Controls.Add(buttonFlow);
        contentPanel.Controls.Add(_panelButtons, 0, 1);

        mainLayout.Controls.Add(contentPanel, 0, 3);

        this.Controls.Add(mainLayout);
    }

    private void LoadSettings()
    {
        if (_settingsManager == null) return;

        _comboBoxLockMode!.SelectedIndex = (int)_settingsManager.CurrentLockMode;
        _checkBoxEnableAppLock!.Checked = _settingsManager.CurrentLockMode == LockMode.PerApp;
        RefreshAppList();
    }

    /// <summary>
    /// Refreshes the application list from settings.
    /// </summary>
    public void RefreshAppList()
    {
        if (_settingsManager == null || _dataGridViewApps == null) return;

        _dataGridViewApps.Rows.Clear();

        TimeTracker? timeTracker = _timeTracker;
        if (timeTracker == null)
        {
            timeTracker = new TimeTracker(_settingsManager);
        }

        List<AppTimeLimit> limits = _settingsManager.AppTimeLimits;
        foreach (AppTimeLimit limit in limits)
        {
            string timeStr = $"{(int)limit.DailyLimit.TotalHours}h {limit.DailyLimit.Minutes}m";
            string statusStr = limit.IsEnabled ? LanguageManager.GetString("Enabled") : LanguageManager.GetString("Disabled");

            TimeSpan usedTime = timeTracker.GetAppUsageToday(limit.AppIdentifier);
            TimeSpan remainingTime = limit.DailyLimit - usedTime;
            string remainingStr = remainingTime > TimeSpan.Zero
                ? $"{(int)remainingTime.TotalHours}h {remainingTime.Minutes}m"
                : "0h 0m";

            _dataGridViewApps.Rows.Add(
                limit.DisplayName,
                timeStr,
                remainingStr,
                statusStr
            );

            int rowIndex = _dataGridViewApps.Rows.Count - 1;
            _dataGridViewApps.Rows[rowIndex].Tag = limit;
            _dataGridViewApps.Rows[rowIndex].Cells[3].Style.ForeColor = limit.IsEnabled
                ? Color.FromArgb(40, 167, 69)
                : Color.FromArgb(108, 117, 125);
        }
    }

    private void OnLockModeChanged(object? sender, EventArgs e)
    {
        if (_settingsManager == null || _comboBoxLockMode == null) return;

        LockMode newMode = (LockMode)_comboBoxLockMode.SelectedIndex;
        _settingsManager.CurrentLockMode = newMode;
        _settingsManager.SaveSettings();

        _checkBoxEnableAppLock!.Checked = newMode == LockMode.PerApp;
    }

    private void OnEnableAppLockChanged(object? sender, EventArgs e)
    {
        if (_settingsManager == null || _checkBoxEnableAppLock == null) return;

        LockMode newMode = _checkBoxEnableAppLock.Checked ? LockMode.PerApp : LockMode.FullScreen;
        _settingsManager.CurrentLockMode = newMode;
        _comboBoxLockMode!.SelectedIndex = (int)newMode;
        _settingsManager.SaveSettings();
    }

    private void OnAddClick(object? sender, EventArgs e)
    {
        if (_settingsManager == null) return;

        using AddAppDialog dialog = new(_settingsManager);
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            RefreshAppList();
        }
    }

    private void OnEditClick(object? sender, EventArgs e)
    {
        if (_settingsManager == null || _dataGridViewApps == null) return;

        if (_dataGridViewApps.SelectedRows.Count == 0)
        {
            MessageBox.Show(
                LanguageManager.GetString("PleaseSelectApp"),
                LanguageManager.GetString("Info"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        AppTimeLimit? selectedLimit = _dataGridViewApps.SelectedRows[0].Tag as AppTimeLimit;
        if (selectedLimit == null) return;

        using AddAppDialog dialog = new(_settingsManager, selectedLimit);
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            RefreshAppList();
        }
    }

    private void OnDeleteClick(object? sender, EventArgs e)
    {
        if (_settingsManager == null || _dataGridViewApps == null) return;

        if (_dataGridViewApps.SelectedRows.Count == 0)
        {
            MessageBox.Show(
                LanguageManager.GetString("PleaseSelectApp"),
                LanguageManager.GetString("Info"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        AppTimeLimit? selectedLimit = _dataGridViewApps.SelectedRows[0].Tag as AppTimeLimit;
        if (selectedLimit == null) return;

        DialogResult result = MessageBox.Show(
            string.Format(LanguageManager.GetString("ConfirmDeleteApp"), selectedLimit.DisplayName),
            LanguageManager.GetString("Confirm"),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            _settingsManager.RemoveAppTimeLimit(selectedLimit.AppIdentifier);
            RefreshAppList();
        }
    }

    private void OnDataGridViewDoubleClick(object? sender, EventArgs e)
    {
        OnEditClick(sender, e);
    }

    /// <summary>
    /// Saves the current settings.
    /// </summary>
    public void SaveSettings()
    {
        _settingsManager?.SaveSettings();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            LanguageManager.LanguageChanged -= OnLanguageChanged;
            components?.Dispose();
        }
        base.Dispose(disposing);
    }
}
