using System;
using System.Windows.Forms;
using System.Drawing;

namespace ScreenTimeController
{
    public partial class SettingsForm : Form
    {
        private readonly SettingsManager _settingsManager;
        private bool _isPasswordVerified;
        private bool _isClosing;
        
        private Label _labelSunday;
        private NumericUpDown _numericUpDownSundayHours;
        private NumericUpDown _numericUpDownSundayMinutes;
        private Label _labelSundayHours;
        private Label _labelSundayMinutes;
        
        private Label _labelMonday;
        private NumericUpDown _numericUpDownMondayHours;
        private NumericUpDown _numericUpDownMondayMinutes;
        private Label _labelMondayHours;
        private Label _labelMondayMinutes;
        
        private Label _labelTuesday;
        private NumericUpDown _numericUpDownTuesdayHours;
        private NumericUpDown _numericUpDownTuesdayMinutes;
        private Label _labelTuesdayHours;
        private Label _labelTuesdayMinutes;
        
        private Label _labelWednesday;
        private NumericUpDown _numericUpDownWednesdayHours;
        private NumericUpDown _numericUpDownWednesdayMinutes;
        private Label _labelWednesdayHours;
        private Label _labelWednesdayMinutes;
        
        private Label _labelThursday;
        private NumericUpDown _numericUpDownThursdayHours;
        private NumericUpDown _numericUpDownThursdayMinutes;
        private Label _labelThursdayHours;
        private Label _labelThursdayMinutes;
        
        private Label _labelFriday;
        private NumericUpDown _numericUpDownFridayHours;
        private NumericUpDown _numericUpDownFridayMinutes;
        private Label _labelFridayHours;
        private Label _labelFridayMinutes;
        
        private Label _labelSaturday;
        private NumericUpDown _numericUpDownSaturdayHours;
        private NumericUpDown _numericUpDownSaturdayMinutes;
        private Label _labelSaturdayHours;
        private Label _labelSaturdayMinutes;
        
        private Label _labelLanguage;
        private ComboBox _comboBoxLanguage;
        
        private Button _buttonOK;
        private Button _buttonCancel;
        private Button _buttonChangePassword;
        private Button _buttonApplyToAll;

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

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            this.Text = LanguageManager.GetString("ScreenTimeSettings");
            _labelSunday.Text = LanguageManager.GetString("Sunday") + ":";
            _labelMonday.Text = LanguageManager.GetString("Monday") + ":";
            _labelTuesday.Text = LanguageManager.GetString("Tuesday") + ":";
            _labelWednesday.Text = LanguageManager.GetString("Wednesday") + ":";
            _labelThursday.Text = LanguageManager.GetString("Thursday") + ":";
            _labelFriday.Text = LanguageManager.GetString("Friday") + ":";
            _labelSaturday.Text = LanguageManager.GetString("Saturday") + ":";
            _labelSundayHours.Text = LanguageManager.GetString("Hours");
            _labelMondayHours.Text = LanguageManager.GetString("Hours");
            _labelTuesdayHours.Text = LanguageManager.GetString("Hours");
            _labelWednesdayHours.Text = LanguageManager.GetString("Hours");
            _labelThursdayHours.Text = LanguageManager.GetString("Hours");
            _labelFridayHours.Text = LanguageManager.GetString("Hours");
            _labelSaturdayHours.Text = LanguageManager.GetString("Hours");
            _labelSundayMinutes.Text = LanguageManager.GetString("Minutes");
            _labelMondayMinutes.Text = LanguageManager.GetString("Minutes");
            _labelTuesdayMinutes.Text = LanguageManager.GetString("Minutes");
            _labelWednesdayMinutes.Text = LanguageManager.GetString("Minutes");
            _labelThursdayMinutes.Text = LanguageManager.GetString("Minutes");
            _labelFridayMinutes.Text = LanguageManager.GetString("Minutes");
            _labelSaturdayMinutes.Text = LanguageManager.GetString("Minutes");
            _labelLanguage.Text = LanguageManager.GetString("Language") + ":";
            _buttonOK.Text = LanguageManager.GetString("OK");
            _buttonCancel.Text = LanguageManager.GetString("Cancel");
            _buttonApplyToAll.Text = LanguageManager.GetString("ApplyToAllDays");
            _buttonChangePassword.Text = LanguageManager.GetString("PasswordSettings");
        }

        private void ShowPasswordPrompt()
        {
            if (!_settingsManager.HasPassword())
            {
                _isPasswordVerified = true;
                return;
            }

            using (var passwordForm = new PasswordForm(_settingsManager))
            {
                var result = passwordForm.ShowDialog();
                
                if (result == DialogResult.OK && passwordForm.IsPasswordCorrect)
                {
                    _isPasswordVerified = true;
                }
                else
                {
                    _isClosing = true;
                    DialogResult = DialogResult.Cancel;
                    Close();
                }
            }
        }

        private void LoadSettings()
        {
            LoadDaySetting(_settingsManager.SundayLimit, _numericUpDownSundayHours, _numericUpDownSundayMinutes);
            LoadDaySetting(_settingsManager.MondayLimit, _numericUpDownMondayHours, _numericUpDownMondayMinutes);
            LoadDaySetting(_settingsManager.TuesdayLimit, _numericUpDownTuesdayHours, _numericUpDownTuesdayMinutes);
            LoadDaySetting(_settingsManager.WednesdayLimit, _numericUpDownWednesdayHours, _numericUpDownWednesdayMinutes);
            LoadDaySetting(_settingsManager.ThursdayLimit, _numericUpDownThursdayHours, _numericUpDownThursdayMinutes);
            LoadDaySetting(_settingsManager.FridayLimit, _numericUpDownFridayHours, _numericUpDownFridayMinutes);
            LoadDaySetting(_settingsManager.SaturdayLimit, _numericUpDownSaturdayHours, _numericUpDownSaturdayMinutes);

            _comboBoxLanguage.Items.Clear();
            foreach (Language lang in System.Enum.GetValues(typeof(Language)))
            {
                _comboBoxLanguage.Items.Add(LanguageManager.GetLanguageName(lang));
            }
            _comboBoxLanguage.SelectedIndex = (int)_settingsManager.Language;
        }

        private void LoadDaySetting(TimeSpan limit, NumericUpDown hoursControl, NumericUpDown minutesControl)
        {
            hoursControl.Value = Math.Min(limit.Hours, 24);
            minutesControl.Value = limit.Minutes;
        }

        private void OnOKClick(object sender, EventArgs e)
        {
            if (_isClosing) return;
            _isClosing = true;
            SaveSettings();
            LanguageManager.LanguageChanged -= OnLanguageChanged;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnCancelClick(object sender, EventArgs e)
        {
            if (_isClosing) return;
            _isClosing = true;
            LanguageManager.LanguageChanged -= OnLanguageChanged;
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void OnApplyToAllClick(object sender, EventArgs e)
        {
            decimal hours = _numericUpDownSundayHours.Value;
            decimal minutes = _numericUpDownSundayMinutes.Value;
            
            _numericUpDownMondayHours.Value = hours;
            _numericUpDownMondayMinutes.Value = minutes;
            
            _numericUpDownTuesdayHours.Value = hours;
            _numericUpDownTuesdayMinutes.Value = minutes;
            
            _numericUpDownWednesdayHours.Value = hours;
            _numericUpDownWednesdayMinutes.Value = minutes;
            
            _numericUpDownThursdayHours.Value = hours;
            _numericUpDownThursdayMinutes.Value = minutes;
            
            _numericUpDownFridayHours.Value = hours;
            _numericUpDownFridayMinutes.Value = minutes;
            
            _numericUpDownSaturdayHours.Value = hours;
            _numericUpDownSaturdayMinutes.Value = minutes;

            MessageBox.Show(LanguageManager.GetString("AllDaysSet"), LanguageManager.GetString("Info"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnChangePasswordClick(object sender, EventArgs e)
        {
            using (var changePasswordForm = new ChangePasswordForm(_settingsManager))
            {
                changePasswordForm.ShowDialog();
            }
        }

        private void SaveSettings()
        {
            _settingsManager.SundayLimit = GetTimeFromControls(_numericUpDownSundayHours, _numericUpDownSundayMinutes);
            _settingsManager.MondayLimit = GetTimeFromControls(_numericUpDownMondayHours, _numericUpDownMondayMinutes);
            _settingsManager.TuesdayLimit = GetTimeFromControls(_numericUpDownTuesdayHours, _numericUpDownTuesdayMinutes);
            _settingsManager.WednesdayLimit = GetTimeFromControls(_numericUpDownWednesdayHours, _numericUpDownWednesdayMinutes);
            _settingsManager.ThursdayLimit = GetTimeFromControls(_numericUpDownThursdayHours, _numericUpDownThursdayMinutes);
            _settingsManager.FridayLimit = GetTimeFromControls(_numericUpDownFridayHours, _numericUpDownFridayMinutes);
            _settingsManager.SaturdayLimit = GetTimeFromControls(_numericUpDownSaturdayHours, _numericUpDownSaturdayMinutes);
            
            _settingsManager.Language = (Language)_comboBoxLanguage.SelectedIndex;
            
            _settingsManager.SaveSettings();
        }

        private TimeSpan GetTimeFromControls(NumericUpDown hoursControl, NumericUpDown minutesControl)
        {
            int hours = (int)hoursControl.Value;
            int minutes = (int)minutesControl.Value;
            return new TimeSpan(hours, minutes, 0);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1100, 950);
            this.Text = "Screen Time Settings";
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(240, 240, 240);

            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(50)
            };

            int yPos = 50;
            int rowHeight = 70;
            int labelWidth = 200;
            int numWidth = 100;
            int unitWidth = 100;

            _labelSunday = new Label
            {
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Location = new Point(50, yPos),
                Size = new Size(labelWidth, 50),
                TextAlign = ContentAlignment.MiddleLeft
            };
            mainPanel.Controls.Add(_labelSunday);

            _numericUpDownSundayHours = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 24,
                Font = new Font("Segoe UI", 15),
                Location = new Point(280, yPos + 5),
                Size = new Size(numWidth, 40)
            };
            mainPanel.Controls.Add(_numericUpDownSundayHours);

            _labelSundayHours = new Label
            {
                Font = new Font("Segoe UI", 15),
                Location = new Point(400, yPos),
                Size = new Size(unitWidth, 50),
                TextAlign = ContentAlignment.MiddleLeft
            };
            mainPanel.Controls.Add(_labelSundayHours);

            _numericUpDownSundayMinutes = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 59,
                Increment = 1,
                Font = new Font("Segoe UI", 15),
                Location = new Point(520, yPos + 5),
                Size = new Size(numWidth, 40)
            };
            mainPanel.Controls.Add(_numericUpDownSundayMinutes);

            _labelSundayMinutes = new Label
            {
                Font = new Font("Segoe UI", 15),
                Location = new Point(640, yPos),
                Size = new Size(unitWidth, 50),
                TextAlign = ContentAlignment.MiddleLeft
            };
            mainPanel.Controls.Add(_labelSundayMinutes);

            yPos += rowHeight;

            _labelMonday = new Label
            {
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Location = new Point(50, yPos),
                Size = new Size(labelWidth, 50),
                TextAlign = ContentAlignment.MiddleLeft
            };
            mainPanel.Controls.Add(_labelMonday);

            _numericUpDownMondayHours = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 24,
                Font = new Font("Segoe UI", 15),
                Location = new Point(280, yPos + 5),
                Size = new Size(numWidth, 40)
            };
            mainPanel.Controls.Add(_numericUpDownMondayHours);

            _labelMondayHours = new Label
            {
                Font = new Font("Segoe UI", 15),
                Location = new Point(400, yPos),
                Size = new Size(unitWidth, 50),
                TextAlign = ContentAlignment.MiddleLeft
            };
            mainPanel.Controls.Add(_labelMondayHours);

            _numericUpDownMondayMinutes = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 59,
                Increment = 1,
                Font = new Font("Segoe UI", 15),
                Location = new Point(520, yPos + 5),
                Size = new Size(numWidth, 40)
            };
            mainPanel.Controls.Add(_numericUpDownMondayMinutes);

            _labelMondayMinutes = new Label
            {
                Font = new Font("Segoe UI", 15),
                Location = new Point(640, yPos),
                Size = new Size(unitWidth, 50),
                TextAlign = ContentAlignment.MiddleLeft
            };
            mainPanel.Controls.Add(_labelMondayMinutes);

            yPos += rowHeight;

            _labelTuesday = new Label
            {
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Location = new Point(50, yPos),
                Size = new Size(labelWidth, 50),
                TextAlign = ContentAlignment.MiddleLeft
            };
            mainPanel.Controls.Add(_labelTuesday);

            _numericUpDownTuesdayHours = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 24,
                Font = new Font("Segoe UI", 15),
                Location = new Point(280, yPos + 5),
                Size = new Size(numWidth, 40)
            };
            mainPanel.Controls.Add(_numericUpDownTuesdayHours);

            _labelTuesdayHours = new Label
            {
                Font = new Font("Segoe UI", 15),
                Location = new Point(400, yPos),
                Size = new Size(unitWidth, 50),
                TextAlign = ContentAlignment.MiddleLeft
            };
            mainPanel.Controls.Add(_labelTuesdayHours);

            _numericUpDownTuesdayMinutes = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 59,
                Increment = 1,
                Font = new Font("Segoe UI", 15),
                Location = new Point(520, yPos + 5),
                Size = new Size(numWidth, 40)
            };
            mainPanel.Controls.Add(_numericUpDownTuesdayMinutes);

            _labelTuesdayMinutes = new Label
            {
                Font = new Font("Segoe UI", 15),
                Location = new Point(640, yPos),
                Size = new Size(unitWidth, 50),
                TextAlign = ContentAlignment.MiddleLeft
            };
            mainPanel.Controls.Add(_labelTuesdayMinutes);

            yPos += rowHeight;

            _labelWednesday = new Label
            {
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Location = new Point(50, yPos),
                Size = new Size(labelWidth, 50),
                TextAlign = ContentAlignment.MiddleLeft
            };
            mainPanel.Controls.Add(_labelWednesday);

            _numericUpDownWednesdayHours = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 24,
                Font = new Font("Segoe UI", 15),
                Location = new Point(280, yPos + 5),
                Size = new Size(numWidth, 40)
            };
            mainPanel.Controls.Add(_numericUpDownWednesdayHours);

            _labelWednesdayHours = new Label
            {
                Font = new Font("Segoe UI", 15),
                Location = new Point(400, yPos),
                Size = new Size(unitWidth, 50),
                TextAlign = ContentAlignment.MiddleLeft
            };
            mainPanel.Controls.Add(_labelWednesdayHours);

            _numericUpDownWednesdayMinutes = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 59,
                Increment = 1,
                Font = new Font("Segoe UI", 15),
                Location = new Point(520, yPos + 5),
                Size = new Size(numWidth, 40)
            };
            mainPanel.Controls.Add(_numericUpDownWednesdayMinutes);

            _labelWednesdayMinutes = new Label
            {
                Font = new Font("Segoe UI", 15),
                Location = new Point(640, yPos),
                Size = new Size(unitWidth, 50),
                TextAlign = ContentAlignment.MiddleLeft
            };
            mainPanel.Controls.Add(_labelWednesdayMinutes);

            yPos += rowHeight;

            _labelThursday = new Label
            {
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Location = new Point(50, yPos),
                Size = new Size(labelWidth, 50),
                TextAlign = ContentAlignment.MiddleLeft
            };
            mainPanel.Controls.Add(_labelThursday);

            _numericUpDownThursdayHours = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 24,
                Font = new Font("Segoe UI", 15),
                Location = new Point(280, yPos + 5),
                Size = new Size(numWidth, 40)
            };
            mainPanel.Controls.Add(_numericUpDownThursdayHours);

            _labelThursdayHours = new Label
            {
                Font = new Font("Segoe UI", 15),
                Location = new Point(400, yPos),
                Size = new Size(unitWidth, 50),
                TextAlign = ContentAlignment.MiddleLeft
            };
            mainPanel.Controls.Add(_labelThursdayHours);

            _numericUpDownThursdayMinutes = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 59,
                Increment = 1,
                Font = new Font("Segoe UI", 15),
                Location = new Point(520, yPos + 5),
                Size = new Size(numWidth, 40)
            };
            mainPanel.Controls.Add(_numericUpDownThursdayMinutes);

            _labelThursdayMinutes = new Label
            {
                Font = new Font("Segoe UI", 15),
                Location = new Point(640, yPos),
                Size = new Size(unitWidth, 50),
                TextAlign = ContentAlignment.MiddleLeft
            };
            mainPanel.Controls.Add(_labelThursdayMinutes);

            yPos += rowHeight;

            _labelFriday = new Label
            {
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Location = new Point(50, yPos),
                Size = new Size(labelWidth, 50),
                TextAlign = ContentAlignment.MiddleLeft
            };
            mainPanel.Controls.Add(_labelFriday);

            _numericUpDownFridayHours = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 24,
                Font = new Font("Segoe UI", 15),
                Location = new Point(280, yPos + 5),
                Size = new Size(numWidth, 40)
            };
            mainPanel.Controls.Add(_numericUpDownFridayHours);

            _labelFridayHours = new Label
            {
                Font = new Font("Segoe UI", 15),
                Location = new Point(400, yPos),
                Size = new Size(unitWidth, 50),
                TextAlign = ContentAlignment.MiddleLeft
            };
            mainPanel.Controls.Add(_labelFridayHours);

            _numericUpDownFridayMinutes = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 59,
                Increment = 1,
                Font = new Font("Segoe UI", 15),
                Location = new Point(520, yPos + 5),
                Size = new Size(numWidth, 40)
            };
            mainPanel.Controls.Add(_numericUpDownFridayMinutes);

            _labelFridayMinutes = new Label
            {
                Font = new Font("Segoe UI", 15),
                Location = new Point(640, yPos),
                Size = new Size(unitWidth, 50),
                TextAlign = ContentAlignment.MiddleLeft
            };
            mainPanel.Controls.Add(_labelFridayMinutes);

            yPos += rowHeight;

            _labelSaturday = new Label
            {
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Location = new Point(50, yPos),
                Size = new Size(labelWidth, 50),
                TextAlign = ContentAlignment.MiddleLeft
            };
            mainPanel.Controls.Add(_labelSaturday);

            _numericUpDownSaturdayHours = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 24,
                Font = new Font("Segoe UI", 15),
                Location = new Point(280, yPos + 5),
                Size = new Size(numWidth, 40)
            };
            mainPanel.Controls.Add(_numericUpDownSaturdayHours);

            _labelSaturdayHours = new Label
            {
                Font = new Font("Segoe UI", 15),
                Location = new Point(400, yPos),
                Size = new Size(unitWidth, 50),
                TextAlign = ContentAlignment.MiddleLeft
            };
            mainPanel.Controls.Add(_labelSaturdayHours);

            _numericUpDownSaturdayMinutes = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 59,
                Increment = 1,
                Font = new Font("Segoe UI", 15),
                Location = new Point(520, yPos + 5),
                Size = new Size(numWidth, 40)
            };
            mainPanel.Controls.Add(_numericUpDownSaturdayMinutes);

            _labelSaturdayMinutes = new Label
            {
                Font = new Font("Segoe UI", 15),
                Location = new Point(640, yPos),
                Size = new Size(unitWidth, 50),
                TextAlign = ContentAlignment.MiddleLeft
            };
            mainPanel.Controls.Add(_labelSaturdayMinutes);

            yPos += rowHeight + 20;

            _labelLanguage = new Label
            {
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Location = new Point(50, yPos),
                Size = new Size(labelWidth, 50),
                TextAlign = ContentAlignment.MiddleLeft
            };
            mainPanel.Controls.Add(_labelLanguage);

            _comboBoxLanguage = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 15),
                Location = new Point(280, yPos + 5),
                Size = new Size(380, 40)
            };
            mainPanel.Controls.Add(_comboBoxLanguage);

            yPos += rowHeight + 30;

            _buttonApplyToAll = new Button
            {
                Font = new Font("Segoe UI", 15),
                Location = new Point(50, yPos),
                Size = new Size(250, 55),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _buttonApplyToAll.FlatAppearance.BorderSize = 0;
            _buttonApplyToAll.Click += OnApplyToAllClick;
            mainPanel.Controls.Add(_buttonApplyToAll);

            _buttonChangePassword = new Button
            {
                Font = new Font("Segoe UI", 15),
                Location = new Point(330, yPos),
                Size = new Size(250, 55),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _buttonChangePassword.FlatAppearance.BorderSize = 0;
            _buttonChangePassword.Click += OnChangePasswordClick;
            mainPanel.Controls.Add(_buttonChangePassword);

            yPos += 80;

            _buttonOK = new Button
            {
                Font = new Font("Segoe UI", 15),
                Location = new Point(350, yPos),
                Size = new Size(150, 55),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _buttonOK.FlatAppearance.BorderSize = 0;
            _buttonOK.Click += OnOKClick;
            mainPanel.Controls.Add(_buttonOK);

            _buttonCancel = new Button
            {
                Font = new Font("Segoe UI", 15),
                Location = new Point(530, yPos),
                Size = new Size(150, 55),
                BackColor = Color.FromArgb(200, 200, 200),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat
            };
            _buttonCancel.FlatAppearance.BorderSize = 0;
            _buttonCancel.Click += OnCancelClick;
            mainPanel.Controls.Add(_buttonCancel);

            this.Controls.Add(mainPanel);
        }

        private System.ComponentModel.IContainer components;
    }
}
