using System;
using System.Windows.Forms;
using System.Drawing;

namespace ScreenTimeController
{
    public partial class PasswordForm : Form
    {
        private readonly SettingsManager _settingsManager;
        private Label _labelPassword;
        private TextBox _textBoxPassword;
        private Button _buttonOK;
        private Button _buttonCancel;
        private int _attemptsLeft;
        private bool _isClosing;

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

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            this.Text = LanguageManager.GetString("EnterPassword");
            _labelPassword.Text = LanguageManager.GetString("Password");
            _buttonOK.Text = LanguageManager.GetString("OK");
            _buttonCancel.Text = LanguageManager.GetString("Cancel");
        }

        private void OnOKClick(object sender, EventArgs e)
        {
            if (_isClosing) return;

            if (_settingsManager.VerifyPassword(_textBoxPassword.Text))
            {
                IsPasswordCorrect = true;
                _isClosing = true;
                LanguageManager.LanguageChanged -= OnLanguageChanged;
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                _attemptsLeft--;
                if (_attemptsLeft > 0)
                {
                    MessageBox.Show(string.Format(LanguageManager.GetString("IncorrectPassword"), _attemptsLeft), LanguageManager.GetString("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _textBoxPassword.Clear();
                    _textBoxPassword.Focus();
                }
                else
                {
                    MessageBox.Show(LanguageManager.GetString("TooManyAttempts"), LanguageManager.GetString("Error"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _isClosing = true;
                    LanguageManager.LanguageChanged -= OnLanguageChanged;
                    DialogResult = DialogResult.Cancel;
                    Close();
                }
            }
        }

        private void OnCancelClick(object sender, EventArgs e)
        {
            if (_isClosing) return;
            _isClosing = true;
            LanguageManager.LanguageChanged -= OnLanguageChanged;
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void PasswordForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isClosing) return;
            _isClosing = true;
            LanguageManager.LanguageChanged -= OnLanguageChanged;
            if (DialogResult == DialogResult.None)
            {
                DialogResult = DialogResult.Cancel;
            }
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(600, 300);
            this.Text = "Enter Password";
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(240, 240, 240);
            this.FormClosing += PasswordForm_FormClosing;

            _labelPassword = new Label
            {
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Location = new Point(40, 60),
                Size = new Size(150, 40),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(_labelPassword);

            _textBoxPassword = new TextBox
            {
                PasswordChar = '*',
                Location = new Point(200, 58),
                Size = new System.Drawing.Size(340, 40),
                Font = new Font("Segoe UI", 14)
            };
            this.Controls.Add(_textBoxPassword);

            _buttonOK = new Button
            {
                Font = new Font("Segoe UI", 12),
                Location = new Point(150, 180),
                Size = new System.Drawing.Size(120, 50),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _buttonOK.FlatAppearance.BorderSize = 0;
            _buttonOK.Click += OnOKClick;
            this.Controls.Add(_buttonOK);

            _buttonCancel = new Button
            {
                Font = new Font("Segoe UI", 12),
                Location = new Point(320, 180),
                Size = new System.Drawing.Size(120, 50),
                BackColor = Color.FromArgb(200, 200, 200),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat
            };
            _buttonCancel.FlatAppearance.BorderSize = 0;
            _buttonCancel.Click += OnCancelClick;
            this.Controls.Add(_buttonCancel);
        }

        private System.ComponentModel.IContainer components;
    }
}
