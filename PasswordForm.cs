using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace ScreenTimeController;

public class PasswordForm : Form
{
    private readonly SettingsManager _settingsManager;
    private Label? _labelPassword;
    private TextBox? _textBoxPassword;
    private Button? _buttonOK;
    private Button? _buttonCancel;
    private int _attemptsLeft;
    private bool _isClosing;
    private IContainer? components;

    public bool IsPasswordCorrect { get; private set; }

    public PasswordForm(SettingsManager settingsManager)
    {
        _isClosing = false;
        InitializeComponent();
        _settingsManager = settingsManager;
        _attemptsLeft = 3;
        IsPasswordCorrect = false;
        ApplyLanguage();
        LanguageManager.LanguageChanged += OnLanguageChanged;
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        ApplyLanguage();
    }

    private void ApplyLanguage()
    {
        Text = LanguageManager.GetString("EnterPassword");
        _labelPassword!.Text = LanguageManager.GetString("Password");
        _buttonOK!.Text = LanguageManager.GetString("OK");
        _buttonCancel!.Text = LanguageManager.GetString("Cancel");
    }

    private void OnOKClick(object? sender, EventArgs e)
    {
        if (_isClosing)
        {
            return;
        }
        if (_settingsManager.VerifyPassword(_textBoxPassword!.Text))
        {
            IsPasswordCorrect = true;
            _isClosing = true;
            LanguageManager.LanguageChanged -= OnLanguageChanged;
            DialogResult = DialogResult.OK;
            Close();
            return;
        }
        _attemptsLeft--;
        if (_attemptsLeft > 0)
        {
            MessageBox.Show(string.Format(LanguageManager.GetString("IncorrectPassword"), _attemptsLeft), LanguageManager.GetString("Error"), MessageBoxButtons.OK, MessageBoxIcon.Hand);
            _textBoxPassword.Clear();
            _textBoxPassword.Focus();
        }
        else
        {
            MessageBox.Show(LanguageManager.GetString("TooManyAttempts"), LanguageManager.GetString("Error"), MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            _isClosing = true;
            LanguageManager.LanguageChanged -= OnLanguageChanged;
            DialogResult = DialogResult.Cancel;
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

    private void PasswordForm_KeyDown(object? sender, KeyEventArgs e)
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

    private void PasswordForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!_isClosing)
        {
            _isClosing = true;
            LanguageManager.LanguageChanged -= OnLanguageChanged;
            if (DialogResult == DialogResult.None)
            {
                DialogResult = DialogResult.Cancel;
            }
        }
    }

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        ClientSize = new System.Drawing.Size(600, 300);
        Text = "Enter Password";
        FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        BackColor = System.Drawing.Color.FromArgb(240, 240, 240);
        KeyPreview = true;
        KeyDown += new System.Windows.Forms.KeyEventHandler(PasswordForm_KeyDown);
        FormClosing += new System.Windows.Forms.FormClosingEventHandler(PasswordForm_FormClosing);

        _labelPassword = new Label
        {
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            Location = new Point(40, 60),
            Size = new Size(150, 40),
            TextAlign = ContentAlignment.MiddleLeft
        };
        Controls.Add(_labelPassword);

        _textBoxPassword = new TextBox
        {
            PasswordChar = '*',
            Location = new Point(200, 58),
            Size = new Size(340, 40),
            Font = new Font("Segoe UI", 14f)
        };
        Controls.Add(_textBoxPassword);

        _buttonOK = new Button
        {
            Font = new Font("Segoe UI", 12f),
            Location = new Point(150, 180),
            Size = new Size(120, 50),
            BackColor = Color.FromArgb(0, 122, 204),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _buttonOK.FlatAppearance.BorderSize = 0;
        _buttonOK.Click += new EventHandler(OnOKClick);
        Controls.Add(_buttonOK);

        _buttonCancel = new Button
        {
            Font = new Font("Segoe UI", 12f),
            Location = new Point(320, 180),
            Size = new Size(120, 50),
            BackColor = Color.FromArgb(200, 200, 200),
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat
        };
        _buttonCancel.FlatAppearance.BorderSize = 0;
        _buttonCancel.Click += new EventHandler(OnCancelClick);
        Controls.Add(_buttonCancel);
    }
}
