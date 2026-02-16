using System;
using System.Windows.Forms;
using System.Drawing;

namespace ScreenTimeController
{
    public partial class ChangePasswordForm : Form
    {
        private readonly SettingsManager _settingsManager;
        private Label _label1;
        private Label _label2;
        private TextBox _textBoxNewPassword;
        private TextBox _textBoxConfirmPassword;
        private Button _buttonOK;
        private Button _buttonCancel;
        private Button _buttonRemovePassword;

        public ChangePasswordForm(SettingsManager settingsManager)
        {
            InitializeComponent();
            _settingsManager = settingsManager;
            ApplyLanguage();
            LanguageManager.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            this.Text = LanguageManager.GetString("ChangePassword");
            _label1.Text = LanguageManager.GetString("NewPassword");
            _label2.Text = LanguageManager.GetString("ConfirmPassword");
            _buttonOK.Text = LanguageManager.GetString("OK");
            _buttonCancel.Text = LanguageManager.GetString("Cancel");
            _buttonRemovePassword.Text = LanguageManager.GetString("RemovePassword");
        }

        private void OnOKClick(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_textBoxNewPassword.Text))
            {
                MessageBox.Show(LanguageManager.GetString("PasswordEmpty"), LanguageManager.GetString("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (_textBoxNewPassword.Text != _textBoxConfirmPassword.Text)
            {
                MessageBox.Show(LanguageManager.GetString("PasswordMismatch"), LanguageManager.GetString("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _settingsManager.SetPassword(_textBoxNewPassword.Text);
            _settingsManager.SaveSettings();
            MessageBox.Show(LanguageManager.GetString("PasswordChanged"), LanguageManager.GetString("Success"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            LanguageManager.LanguageChanged -= OnLanguageChanged;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnCancelClick(object sender, EventArgs e)
        {
            LanguageManager.LanguageChanged -= OnLanguageChanged;
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void OnRemovePasswordClick(object sender, EventArgs e)
        {
            var result = MessageBox.Show(LanguageManager.GetString("ConfirmRemovePassword"), LanguageManager.GetString("Confirm"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                _settingsManager.SetPassword("");
                _settingsManager.SaveSettings();
                MessageBox.Show(LanguageManager.GetString("PasswordRemoved"), LanguageManager.GetString("Success"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                LanguageManager.LanguageChanged -= OnLanguageChanged;
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(700, 400);
            this.Text = "Change Password";
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(240, 240, 240);

            _label1 = new Label
            {
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Location = new Point(40, 60),
                Size = new Size(200, 40),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(_label1);

            _textBoxNewPassword = new TextBox
            {
                PasswordChar = '*',
                Location = new Point(260, 58),
                Size = new System.Drawing.Size(400, 40),
                Font = new Font("Segoe UI", 14)
            };
            this.Controls.Add(_textBoxNewPassword);

            _label2 = new Label
            {
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Location = new Point(40, 140),
                Size = new Size(200, 40),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(_label2);

            _textBoxConfirmPassword = new TextBox
            {
                PasswordChar = '*',
                Location = new Point(260, 138),
                Size = new System.Drawing.Size(400, 40),
                Font = new Font("Segoe UI", 14)
            };
            this.Controls.Add(_textBoxConfirmPassword);

            _buttonOK = new Button
            {
                Font = new Font("Segoe UI", 13),
                Location = new Point(100, 260),
                Size = new System.Drawing.Size(130, 50),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _buttonOK.FlatAppearance.BorderSize = 0;
            _buttonOK.Click += OnOKClick;
            this.Controls.Add(_buttonOK);

            _buttonCancel = new Button
            {
                Font = new Font("Segoe UI", 13),
                Location = new Point(260, 260),
                Size = new System.Drawing.Size(130, 50),
                BackColor = Color.FromArgb(200, 200, 200),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat
            };
            _buttonCancel.FlatAppearance.BorderSize = 0;
            _buttonCancel.Click += OnCancelClick;
            this.Controls.Add(_buttonCancel);

            _buttonRemovePassword = new Button
            {
                Font = new Font("Segoe UI", 13),
                Location = new Point(420, 260),
                Size = new System.Drawing.Size(180, 50),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _buttonRemovePassword.FlatAppearance.BorderSize = 0;
            _buttonRemovePassword.Click += OnRemovePasswordClick;
            this.Controls.Add(_buttonRemovePassword);
        }

        private System.ComponentModel.IContainer components;
    }
}
