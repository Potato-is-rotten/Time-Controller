using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace ScreenTimeController;

public class ChangePasswordForm : Form
{
    private readonly SettingsManager _settingsManager;
    private Label? _label1;
    private Label? _label2;
    private TextBox? _textBoxNewPassword;
    private TextBox? _textBoxConfirmPassword;
    private Button? _buttonOK;
    private Button? _buttonCancel;
    private Button? _buttonRemovePassword;
    private IContainer? components;

    public ChangePasswordForm(SettingsManager settingsManager)
    {
        InitializeComponent();
        _settingsManager = settingsManager;
        ApplyLanguage();
        LanguageManager.LanguageChanged += OnLanguageChanged;
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        ApplyLanguage();
    }

    private void ApplyLanguage()
    {
        Text = LanguageManager.GetString("ChangePassword");
        _label1!.Text = LanguageManager.GetString("NewPassword");
        _label2!.Text = LanguageManager.GetString("ConfirmPassword");
        _buttonOK!.Text = LanguageManager.GetString("OK");
        _buttonCancel!.Text = LanguageManager.GetString("Cancel");
        _buttonRemovePassword!.Text = LanguageManager.GetString("RemovePassword");
    }

    private void OnOKClick(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_textBoxNewPassword!.Text))
        {
            MessageBox.Show(LanguageManager.GetString("PasswordEmpty"), LanguageManager.GetString("Error"), MessageBoxButtons.OK, MessageBoxIcon.Hand);
            return;
        }
        if (_textBoxNewPassword.Text != _textBoxConfirmPassword!.Text)
        {
            MessageBox.Show(LanguageManager.GetString("PasswordMismatch"), LanguageManager.GetString("Error"), MessageBoxButtons.OK, MessageBoxIcon.Hand);
            return;
        }
        _settingsManager.SetPassword(_textBoxNewPassword.Text);
        _settingsManager.SaveSettings();
        MessageBox.Show(LanguageManager.GetString("PasswordChanged"), LanguageManager.GetString("Success"), MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
        LanguageManager.LanguageChanged -= OnLanguageChanged;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void OnCancelClick(object? sender, EventArgs e)
    {
        LanguageManager.LanguageChanged -= OnLanguageChanged;
        DialogResult = DialogResult.Cancel;
        Close();
    }

    private void OnRemovePasswordClick(object? sender, EventArgs e)
    {
        if (MessageBox.Show(LanguageManager.GetString("ConfirmRemovePassword"), LanguageManager.GetString("Confirm"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            _settingsManager.SetPassword("");
            _settingsManager.SaveSettings();
            MessageBox.Show(LanguageManager.GetString("PasswordRemoved"), LanguageManager.GetString("Success"), MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            LanguageManager.LanguageChanged -= OnLanguageChanged;
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    private void ChangePasswordForm_KeyDown(object? sender, KeyEventArgs e)
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

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        ClientSize = new Size(700, 400);
        Text = "Change Password";
        FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(240, 240, 240);
        KeyPreview = true;
        KeyDown += new KeyEventHandler(ChangePasswordForm_KeyDown);

        _label1 = new Label
        {
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            Location = new Point(40, 60),
            Size = new Size(200, 40),
            TextAlign = ContentAlignment.MiddleLeft
        };
        Controls.Add(_label1);

        _textBoxNewPassword = new TextBox
        {
            PasswordChar = '*',
            Location = new Point(260, 58),
            Size = new Size(400, 40),
            Font = new Font("Segoe UI", 14f)
        };
        Controls.Add(_textBoxNewPassword);

        _label2 = new Label
        {
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            Location = new Point(40, 140),
            Size = new Size(200, 40),
            TextAlign = ContentAlignment.MiddleLeft
        };
        Controls.Add(_label2);

        _textBoxConfirmPassword = new TextBox
        {
            PasswordChar = '*',
            Location = new Point(260, 138),
            Size = new Size(400, 40),
            Font = new Font("Segoe UI", 14f)
        };
        Controls.Add(_textBoxConfirmPassword);

        _buttonOK = new Button
        {
            Font = new Font("Segoe UI", 13f),
            Location = new Point(100, 260),
            Size = new Size(130, 50),
            BackColor = Color.FromArgb(0, 122, 204),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            AccessibleName = "OKButton"
        };
        _buttonOK.FlatAppearance.BorderSize = 0;
        _buttonOK.Click += new EventHandler(OnOKClick);
        Controls.Add(_buttonOK);

        _buttonCancel = new Button
        {
            Font = new Font("Segoe UI", 13f),
            Location = new Point(260, 260),
            Size = new Size(130, 50),
            BackColor = Color.FromArgb(200, 200, 200),
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            AccessibleName = "CancelButton"
        };
        _buttonCancel.FlatAppearance.BorderSize = 0;
        _buttonCancel.Click += new EventHandler(OnCancelClick);
        Controls.Add(_buttonCancel);

        _buttonRemovePassword = new Button
        {
            Font = new Font("Segoe UI", 13f),
            Location = new Point(420, 260),
            Size = new Size(180, 50),
            BackColor = Color.FromArgb(220, 53, 69),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            AccessibleName = "RemovePasswordButton"
        };
        _buttonRemovePassword.FlatAppearance.BorderSize = 0;
        _buttonRemovePassword.Click += new EventHandler(OnRemovePasswordClick);
        Controls.Add(_buttonRemovePassword);
    }
}
