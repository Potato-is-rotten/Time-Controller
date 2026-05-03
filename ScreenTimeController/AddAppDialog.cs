using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ScreenTimeController;

/// <summary>
/// Dialog for adding or editing application time limits.
/// </summary>
public class AddAppDialog : Form
{
    private readonly SettingsManager _settingsManager;
    private readonly AppTimeLimit? _editingLimit;
    private readonly bool _isEditMode;

    private Label? _labelAppName;
    private TextBox? _textBoxAppName;
    private Button? _buttonBrowse;
    private Button? _buttonSelectProcess;
    private Label? _labelTimeLimit;
    private NumericUpDown? _numericUpDownHours;
    private Label? _labelHours;
    private NumericUpDown? _numericUpDownMinutes;
    private Label? _labelMinutes;
    private CheckBox? _checkBoxEnabled;
    private Button? _buttonOK;
    private Button? _buttonCancel;
    private IContainer? components;

    private string? _selectedAppPath;
    private string? _selectedProcessName;

    /// <summary>
    /// Gets the resulting AppTimeLimit after dialog closes.
    /// </summary>
    public AppTimeLimit? Result { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AddAppDialog"/> class for adding a new app.
    /// </summary>
    /// <param name="settingsManager">The settings manager instance.</param>
    public AddAppDialog(SettingsManager settingsManager) : this(settingsManager, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AddAppDialog"/> class for editing an existing app.
    /// </summary>
    /// <param name="settingsManager">The settings manager instance.</param>
    /// <param name="existingLimit">The existing limit to edit.</param>
    public AddAppDialog(SettingsManager settingsManager, AppTimeLimit? existingLimit)
    {
        _settingsManager = settingsManager;
        _editingLimit = existingLimit;
        _isEditMode = existingLimit != null;

        InitializeComponent();
        ApplyLanguage();

        if (_isEditMode && _editingLimit != null)
        {
            LoadExistingLimit();
        }

        LanguageManager.LanguageChanged += OnLanguageChanged;
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        ApplyLanguage();
    }

    private void ApplyLanguage()
    {
        Text = _isEditMode 
            ? LanguageManager.GetString("EditApplication") 
            : LanguageManager.GetString("AddApplication");

        _labelAppName!.Text = LanguageManager.GetString("ApplicationName") + ":";
        _labelTimeLimit!.Text = LanguageManager.GetString("DailyTimeLimit") + ":";
        _labelHours!.Text = LanguageManager.GetString("Hours");
        _labelMinutes!.Text = LanguageManager.GetString("Minutes");
        _checkBoxEnabled!.Text = LanguageManager.GetString("EnableTimeLimit");
        _buttonBrowse!.Text = LanguageManager.GetString("Browse");
        _buttonSelectProcess!.Text = LanguageManager.GetString("SelectProcess");
        _buttonOK!.Text = LanguageManager.GetString("OK");
        _buttonCancel!.Text = LanguageManager.GetString("Cancel");
    }

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        this.ClientSize = new Size(550, 350);
        this.MinimumSize = new Size(450, 300);
        this.Text = "Add Application";
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = Color.FromArgb(240, 240, 240);

        TableLayoutPanel mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(20)
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));

        TableLayoutPanel contentPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 4,
            AutoSize = true
        };
        contentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        contentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        contentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        contentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        contentPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 45f));
        contentPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
        contentPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 45f));
        contentPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 45f));

        _labelAppName = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            Margin = new Padding(0, 0, 10, 0)
        };
        contentPanel.Controls.Add(_labelAppName, 0, 0);

        _textBoxAppName = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 11f),
            Margin = new Padding(0, 0, 10, 0)
        };
        contentPanel.Controls.Add(_textBoxAppName, 1, 0);

        _buttonBrowse = new Button
        {
            Size = new Size(100, 32),
            Font = new Font("Segoe UI", 10f),
            BackColor = Color.FromArgb(0, 122, 204),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            AccessibleName = "BrowseButton",
            Margin = new Padding(0, 0, 5, 0)
        };
        _buttonBrowse.FlatAppearance.BorderSize = 0;
        _buttonBrowse.Click += OnBrowseClick;
        contentPanel.Controls.Add(_buttonBrowse, 2, 0);

        contentPanel.SetColumnSpan(_textBoxAppName, 1);

        _buttonSelectProcess = new Button
        {
            Size = new Size(150, 28),
            Font = new Font("Segoe UI", 9f),
            BackColor = Color.FromArgb(108, 117, 125),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            AccessibleName = "SelectProcessButton"
        };
        _buttonSelectProcess.FlatAppearance.BorderSize = 0;
        _buttonSelectProcess.Click += OnSelectProcessClick;
        contentPanel.Controls.Add(_buttonSelectProcess, 1, 1);

        _labelTimeLimit = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            Margin = new Padding(0, 0, 10, 0)
        };
        contentPanel.Controls.Add(_labelTimeLimit, 0, 2);

        TableLayoutPanel timePanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            AutoSize = true
        };
        timePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70f));
        timePanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        timePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70f));
        timePanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _numericUpDownHours = new NumericUpDown
        {
            Size = new Size(70, 30),
            Font = new Font("Segoe UI", 11f),
            Minimum = 0,
            Maximum = 24,
            Value = 1,
            Margin = new Padding(0, 0, 5, 0)
        };
        timePanel.Controls.Add(_numericUpDownHours, 0, 0);

        _labelHours = new Label
        {
            Size = new Size(50, 30),
            Font = new Font("Segoe UI", 10f),
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 0, 10, 0)
        };
        timePanel.Controls.Add(_labelHours, 1, 0);

        _numericUpDownMinutes = new NumericUpDown
        {
            Size = new Size(70, 30),
            Font = new Font("Segoe UI", 11f),
            Minimum = 0,
            Maximum = 59,
            Value = 0,
            Margin = new Padding(0, 0, 5, 0)
        };
        timePanel.Controls.Add(_numericUpDownMinutes, 2, 0);

        _labelMinutes = new Label
        {
            Size = new Size(50, 30),
            Font = new Font("Segoe UI", 10f),
            TextAlign = ContentAlignment.MiddleLeft
        };
        timePanel.Controls.Add(_labelMinutes, 3, 0);

        contentPanel.Controls.Add(timePanel, 1, 2);
        contentPanel.SetColumnSpan(timePanel, 3);

        _checkBoxEnabled = new CheckBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 11f),
            Checked = true
        };
        contentPanel.Controls.Add(_checkBoxEnabled, 1, 3);
        contentPanel.SetColumnSpan(_checkBoxEnabled, 3);

        mainLayout.Controls.Add(contentPanel, 0, 0);

        FlowLayoutPanel buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 10, 0, 0)
        };

        _buttonCancel = new Button
        {
            Font = new Font("Segoe UI", 11f),
            Size = new Size(100, 40),
            BackColor = Color.FromArgb(200, 200, 200),
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(5),
            AccessibleName = "CancelButton"
        };
        _buttonCancel.FlatAppearance.BorderSize = 0;
        _buttonCancel.Click += OnCancelClick;
        buttonPanel.Controls.Add(_buttonCancel);

        _buttonOK = new Button
        {
            Font = new Font("Segoe UI", 11f),
            Size = new Size(100, 40),
            BackColor = Color.FromArgb(0, 122, 204),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(5),
            AccessibleName = "OKButton"
        };
        _buttonOK.FlatAppearance.BorderSize = 0;
        _buttonOK.Click += OnOKClick;
        buttonPanel.Controls.Add(_buttonOK);

        mainLayout.Controls.Add(buttonPanel, 0, 1);

        this.Controls.Add(mainLayout);
    }

    private void LoadExistingLimit()
    {
        if (_editingLimit == null) return;

        _textBoxAppName!.Text = _editingLimit.DisplayName;
        _numericUpDownHours!.Value = Math.Min((int)_editingLimit.DailyLimit.TotalHours, 24);
        _numericUpDownMinutes!.Value = _editingLimit.DailyLimit.Minutes;
        _checkBoxEnabled!.Checked = _editingLimit.IsEnabled;
        _selectedAppPath = _editingLimit.IconPath;
        _selectedProcessName = _editingLimit.AppIdentifier;
    }

    private void OnBrowseClick(object? sender, EventArgs e)
    {
        using OpenFileDialog openFileDialog = new()
        {
            Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*",
            Title = LanguageManager.GetString("SelectApplication"),
            RestoreDirectory = true
        };

        if (openFileDialog.ShowDialog() == DialogResult.OK)
        {
            _selectedAppPath = openFileDialog.FileName;
            _selectedProcessName = Path.GetFileNameWithoutExtension(_selectedAppPath);
            _textBoxAppName!.Text = _selectedProcessName;
        }
    }

    private void OnSelectProcessClick(object? sender, EventArgs e)
    {
        using ProcessSelectDialog dialog = new();
        if (dialog.ShowDialog() == DialogResult.OK && dialog.SelectedProcess != null)
        {
            _selectedProcessName = dialog.SelectedProcess.ProcessName;
            _textBoxAppName!.Text = dialog.SelectedProcess.MainWindowTitle;
            if (string.IsNullOrEmpty(_textBoxAppName.Text))
            {
                _textBoxAppName.Text = _selectedProcessName;
            }
            try
            {
                _selectedAppPath = dialog.SelectedProcess.MainModule?.FileName;
            }
            catch
            {
                _selectedAppPath = null;
            }
        }
    }

    private void OnOKClick(object? sender, EventArgs e)
    {
        string appName = _textBoxAppName!.Text.Trim();
        if (string.IsNullOrEmpty(appName))
        {
            MessageBox.Show(
                LanguageManager.GetString("PleaseEnterAppName"),
                LanguageManager.GetString("Warning"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrEmpty(_selectedProcessName) && string.IsNullOrEmpty(_selectedAppPath))
        {
            _selectedProcessName = appName;
        }

        string appIdentifier = _selectedProcessName ?? appName;

        TimeSpan dailyLimit = new(
            (int)_numericUpDownHours!.Value,
            (int)_numericUpDownMinutes!.Value,
            0
        );

        if (dailyLimit.TotalMinutes <= 0)
        {
            MessageBox.Show(
                LanguageManager.GetString("PleaseSetTimeLimit"),
                LanguageManager.GetString("Warning"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        Result = new AppTimeLimit
        {
            AppIdentifier = appIdentifier,
            DisplayName = appName,
            DailyLimit = dailyLimit,
            IsEnabled = _checkBoxEnabled!.Checked,
            IconPath = _selectedAppPath
        };

        _settingsManager.AddAppTimeLimit(Result);

        DialogResult = DialogResult.OK;
        Close();
    }

    private void OnCancelClick(object? sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
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

/// <summary>
/// Dialog for selecting a running process.
/// </summary>
internal class ProcessSelectDialog : Form
{
    private ListView? _listViewProcesses;
    private Button? _buttonOK;
    private Button? _buttonCancel;
    private ColumnHeader? _columnProcessName;
    private ColumnHeader? _columnWindowTitle;
    private IContainer? components;

    /// <summary>
    /// Gets the selected process.
    /// </summary>
    public Process? SelectedProcess { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessSelectDialog"/> class.
    /// </summary>
    public ProcessSelectDialog()
    {
        InitializeComponent();
        LoadProcesses();
    }

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        this.ClientSize = new Size(500, 400);
        this.MinimumSize = new Size(400, 300);
        this.Text = LanguageManager.GetString("SelectRunningProcess");
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = Color.FromArgb(240, 240, 240);

        TableLayoutPanel mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(15)
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));

        _listViewProcesses = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            MultiSelect = false,
            GridLines = true,
            BackColor = Color.White,
            Font = new Font("Segoe UI", 10f)
        };

        _columnProcessName = new ColumnHeader
        {
            Text = LanguageManager.GetString("ProcessName"),
            Width = 150
        };
        _listViewProcesses.Columns.Add(_columnProcessName);

        _columnWindowTitle = new ColumnHeader
        {
            Text = LanguageManager.GetString("WindowTitle"),
            Width = 300
        };
        _listViewProcesses.Columns.Add(_columnWindowTitle);

        _listViewProcesses.DoubleClick += (s, e) =>
        {
            if (_listViewProcesses.SelectedItems.Count > 0)
            {
                OnOKClick(s, e);
            }
        };

        mainLayout.Controls.Add(_listViewProcesses, 0, 0);

        FlowLayoutPanel buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };

        _buttonCancel = new Button
        {
            Font = new Font("Segoe UI", 10f),
            Size = new Size(90, 35),
            BackColor = Color.FromArgb(200, 200, 200),
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(5),
            AccessibleName = "CancelButton"
        };
        _buttonCancel.FlatAppearance.BorderSize = 0;
        _buttonCancel.Click += OnCancelClick;
        buttonPanel.Controls.Add(_buttonCancel);

        _buttonOK = new Button
        {
            Font = new Font("Segoe UI", 10f),
            Size = new Size(90, 35),
            BackColor = Color.FromArgb(0, 122, 204),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(5),
            AccessibleName = "OKButton"
        };
        _buttonOK.FlatAppearance.BorderSize = 0;
        _buttonOK.Click += OnOKClick;
        buttonPanel.Controls.Add(_buttonOK);

        mainLayout.Controls.Add(buttonPanel, 0, 1);

        this.Controls.Add(mainLayout);
    }

    private void LoadProcesses()
    {
        _listViewProcesses!.Items.Clear();

        Process[] processes = Process.GetProcesses();
        foreach (Process process in processes)
        {
            try
            {
                if (!string.IsNullOrEmpty(process.ProcessName) && process.ProcessName != "System")
                {
                    ListViewItem item = new(process.ProcessName);
                    try
                    {
                        item.SubItems.Add(process.MainWindowTitle ?? "");
                    }
                    catch
                    {
                        item.SubItems.Add("");
                    }
                    item.Tag = process;
                    _listViewProcesses.Items.Add(item);
                }
            }
            catch
            {
            }
        }
    }

    private void OnOKClick(object? sender, EventArgs e)
    {
        if (_listViewProcesses!.SelectedItems.Count == 0)
        {
            MessageBox.Show(
                LanguageManager.GetString("PleaseSelectProcess"),
                LanguageManager.GetString("Info"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        SelectedProcess = _listViewProcesses.SelectedItems[0].Tag as Process;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void OnCancelClick(object? sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }
        base.Dispose(disposing);
    }
}
