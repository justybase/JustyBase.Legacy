using AppBase.Common;
using AppBase.Common.Interfaces;
using AppBase.Common.WindowManagement;
using AppBase.Data;
using AppBase.Services;
using CommunityToolkit.Mvvm.Messaging;
using JustData.Application.Login;
using JustData.ViewModels;
using JustyBaseLegacy.UI.Login;
using DatabaseDataGridView.WinForms;
using JustyBaseLegacy.UI.Controls;
using DatabaseDataGridView.WinForms.Coloring;
using JustyBaseLegacy.UI.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JustyBaseLegacy.UI
{
    public partial class LoginForm : Form
    {
        private readonly ICredentialStore _credentialStore;
        private readonly IApplicationSettingsContext _applicationSettingsContext;
        private readonly IUiHelperService _uiHelperService;
        private readonly ILoginDataValidator _loginDataValidator;
        private readonly LoginViewModel _viewModel;
        public bool SuppressBlurOverlay { get; set; }

        /// <summary>When set, the form saves a PNG of itself after the load animation and closes.</summary>
        public string? DocumentationScreenshotPath { get; set; }

        public LoginForm(
            IApplicationSettingsContext applicationSettingsContext,
            IUiHelperService uiHelperService,
            ICredentialStore credentialStore,
            IApplicationSession applicationSession,
            IMessenger messenger,
            ILoginDataValidator loginDataValidator)
        {
            _applicationSettingsContext = applicationSettingsContext ?? throw new ArgumentNullException(nameof(applicationSettingsContext));
            _uiHelperService = uiHelperService ?? throw new ArgumentNullException(nameof(uiHelperService));
            _credentialStore = credentialStore ?? throw new ArgumentNullException(nameof(credentialStore));
            _loginDataValidator = loginDataValidator ?? throw new ArgumentNullException(nameof(loginDataValidator));
            InitializeComponent();
            SetupModernUI();
            ApplyThemeColors();


            this.rememberAsDefaultCheckBox.Checked = rememberAsDefault;
            this.checkBoxFastLogin.Checked = _applicationSettingsContext.Config.FastLogin;
            string path = $"{_applicationSettingsContext.ConfigDirectory}\\credentials.json";
            this._credentialPath = path;
            _viewModel = new LoginViewModel(
                new LegacyConnectionProfileRepository(_credentialStore, path, loginDataValidator),
                new LegacyDatabaseCatalogService(_applicationSettingsContext.Config.ConnectionTimeout),
                applicationSession ?? throw new ArgumentNullException(nameof(applicationSession)),
                messenger ?? throw new ArgumentNullException(nameof(messenger)),
                new JustData.Mvvm.WindowsFormsUiDispatcher(this));
            _viewModel.FastLogin = checkBoxFastLogin.Checked;
        }



        private static void BackupCorruptCredentials(string path)
        {
            try
            {
                string backupPath = $"{path}.corrupt-{DateTime.UtcNow:yyyyMMddHHmmssfff}.bak";
                File.Move(path, backupPath, false);
            }
            catch (Exception)
            {
                // A locked credentials file should not prevent the safe reset path.
            }
        }

        private void Reset()
        {
            LoginDataList = new List<LoginData>();
            LoginDataList.Add(new LoginData()
            {
                Name = "New",
                Driver = "NetezzaSQL",
                Server = "server ip/network name",
                UserName = "\"login\"",
                Password = "password",
                Database = "TEST",
                //LicenseAccept = false,
                //Conn2 = "123",
                //Pass2 = "xyz",
                //Conn2Type = "DB2",
                DefaultIndex = 0
            });
        }

        private void SyncControlsFromViewModel()
        {
            LoginDataList = _viewModel.Profiles.Select(LegacyConnectionProfileRepository.Map).ToList();
            connectionSelectorComboBox.Items.Clear();
            foreach (var profile in LoginDataList) connectionSelectorComboBox.Items.Add(profile.Name);
            var index = _viewModel.SelectedProfile is null ? 0 : _viewModel.Profiles.IndexOf(_viewModel.SelectedProfile);
            connectionSelectorComboBox.SelectedIndex = Math.Clamp(index, 0, LoginDataList.Count - 1);
        }

        private void SyncViewModelFromControls()
        {
            var index = connectionSelectorComboBox.SelectedIndex;
            if (index < 0 || index >= _viewModel.Profiles.Count) return;
            var profile = _viewModel.Profiles[index];
            profile.Name = nameTextBox.Text; profile.Driver = DriverComboBox.Text; profile.Server = serverTextBox.Text;
            profile.UserName = userNameTextBox.Text; profile.Password = passwordTextBox.Text; profile.Database = databaseComboBox.Text;
            _viewModel.SelectedProfile = profile; _viewModel.FastLogin = checkBoxFastLogin.Checked;
            LoginDataList[index] = LegacyConnectionProfileRepository.Map(profile);
            _viewModel.ValidateSelectedProfile();
        }

        private bool rememberAsDefault = true;

        private void Reorder(List<int> newOrder, int defaultConnection)
        {
            SyncViewModelFromControls();
            _viewModel.Reorder(newOrder, defaultConnection);
            SyncControlsFromViewModel();
        }

        public void SetRememberAsDefaultToFalse()
        {
            rememberAsDefault = false;
            rememberAsDefaultCheckBox.Checked = false;
        }

        private readonly string _credentialPath;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<LoginData> LoginDataList { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Driver { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Server { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string User { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Pass { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Database { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool FastLogin { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Conn2 { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Pass2 { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Type2 { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string ConnectionName { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int DefaultIndex { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public LoginSelection? Result => _viewModel.Result;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IReadOnlyList<ConnectionProfile> Profiles => _viewModel.Profiles.Select(profile => profile.Clone()).ToArray();

        private void SelectDatabaseButton_Click(object sender, EventArgs e)
        {
            DoSave();
            _ = ObserveUiOperationAsync(nameof(SelectDatabaseButton_Click), GoLoginAsync);
        }

        public async Task<bool> ChoseFirstAsync()
        {
            // Fast login is invoked directly by AppBootstrapper, so the form is
            // never shown and OnLoad (which normally initializes the VM) is not
            // raised. Initialize explicitly before reading the selected profile.
            if (_viewModel.Profiles.Count == 0)
            {
                await _viewModel.InitializeAsync();
            }

            // InitializeAsync selects the saved default profile.
            if (_viewModel.SelectedProfile is null)
            {
                return false;
            }

            _viewModel.FastLogin = checkBoxFastLogin.Checked;
            SyncControlsFromViewModel();
            SyncViewModelFromControls();
            if (rememberAsDefaultCheckBox.Checked) _viewModel.SetDefaultCommand.Execute(null);
            if (!_viewModel.AcceptCommand.CanExecute(null)) return false;
            _viewModel.AcceptCommand.Execute(null);
            ApplySelection(_viewModel.Result!);
            DialogResult = DialogResult.OK;
            Hide();
            return true;
        }

        /// <summary>
        /// Compatibility boundary for the pre-message-loop fast-login path.
        /// The actual initialization is asynchronous; this small startup pump
        /// keeps dispatcher callbacks flowing before Application.Run starts.
        /// </summary>
        public bool ChoseFirst()
        {
            Task<bool> loginTask = ChoseFirstAsync();
            while (!loginTask.IsCompleted)
                Application.DoEvents();

            return loginTask.GetAwaiter().GetResult();
        }

        private void ConnectionSelectorComboBox_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            int indx = this.connectionSelectorComboBox.SelectedIndex;
            if (indx < 0 || indx >= LoginDataList.Count)
            {
                return;
            }

            this.DriverComboBox.Text = LoginDataList[indx].Driver;
            this.serverTextBox.Text = LoginDataList[indx].Server;
            this.userNameTextBox.Text = LoginDataList[indx].UserName;
            this.passwordTextBox.Text = LoginDataList[indx].Password;
            databaseComboBox.Items.Clear();
            this.databaseComboBox.Text = LoginDataList[indx].Database;
            //this.cbAcceptLicense.Checked = loginData[indx].LicenseAccept;
            this.nameTextBox.Text = LoginDataList[indx].Name;
        }

        void DoSave()
        {
            int indx = this.connectionSelectorComboBox.SelectedIndex;
            if (indx == -1)
            {
                MessageBox.Show("Add a connection first.", "No connection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (rememberAsDefaultCheckBox.Checked)
            {
                LoginDataList[0].DefaultIndex = indx;
            }

            LoginDataList[indx].Driver = this.DriverComboBox.Text;
            LoginDataList[indx].Server = this.serverTextBox.Text;
            LoginDataList[indx].UserName = this.userNameTextBox.Text;
            LoginDataList[indx].Password = this.passwordTextBox.Text;
            LoginDataList[indx].Database = this.databaseComboBox.Text;
            LoginDataList[indx].Name = this.nameTextBox.Text;

            int id = connectionSelectorComboBox.SelectedIndex;
            connectionSelectorComboBox.Items.RemoveAt(id);
            connectionSelectorComboBox.Items.Insert(id, this.nameTextBox.Text);
            connectionSelectorComboBox.SelectedIndex = id;
            connectionSelectorComboBox.Invalidate();
            FastLogin = checkBoxFastLogin.Checked;
            SyncViewModelFromControls();
        }

        private void btSave_Click(object sender, EventArgs e)
        {
            DoSave();
            _ = ObserveUiOperationAsync(nameof(btSave_Click), () => _viewModel.SaveAsync());
        }
        private async void LoginForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!cancel)
            {
                e.Cancel = true;
                try
                {
                    SyncViewModelFromControls();
                    await _viewModel.SaveAsync();
                }
                catch (Exception)
                {
                    MessageBox.Show(
                        "The connection settings could not be saved. Check the configuration directory permissions and try again.",
                        "Credentials not saved",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
                finally
                {
                    cancel = true;
                    BeginInvoke(() => Close());
                }
            }
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {

            if (MessageBox.Show("Delete the selected connection?", "Confirm deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes)
            {
                int nr = connectionSelectorComboBox.SelectedIndex;
                if (nr == -1)
                {
                    return;
                }
                if (connectionSelectorComboBox.Items.Count == 1)
                {
                    MessageBox.Show("You cannot delete the last connection.", "Delete connection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                SyncViewModelFromControls();
                _viewModel.DeleteCommand.Execute(null);
                SyncControlsFromViewModel();
            }

        }

        private async Task GoLoginAsync()
        {
            SyncViewModelFromControls();
            if (rememberAsDefaultCheckBox.Checked) _viewModel.SetDefaultCommand.Execute(null);
            if (!_viewModel.AcceptCommand.CanExecute(null))
            {
                MessageBox.Show("Complete the required connection fields.", "Invalid connection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            await _viewModel.SaveAsync();
            if (!string.IsNullOrWhiteSpace(_viewModel.ErrorMessage))
            {
                MessageBox.Show(
                    _viewModel.ErrorMessage,
                    "Credentials not saved",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            _viewModel.AcceptCommand.Execute(null);
            ApplySelection(_viewModel.Result!);
            cancel = true;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void ApplySelection(LoginSelection selection)
        {
            Driver = selection.Profile.Driver;
            Database = selection.Profile.Database;
            Server = selection.Profile.Server;
            User = selection.Profile.UserName;
            Pass = selection.Profile.Password;
            FastLogin = selection.FastLogin;
            int selectedIndex = _viewModel.Profiles.IndexOf(_viewModel.SelectedProfile!);
            DefaultIndex = selectedIndex >= 0 ? selectedIndex : connectionSelectorComboBox.SelectedIndex;
            ConnectionName = selection.Profile.Name;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Return)
            {
                BeginInvoke((Action)(() => SelectDatabaseButton_Click(selectDatabaseButton, EventArgs.Empty)));
                return true;
            }
            else if (keyData == Keys.Escape)
            {
                _viewModel.CancelCommand.Execute(null);
                this.DialogResult = DialogResult.Cancel;
                this.Hide();
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }



        private void LoginForm_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                WindowNativeMethods.ReleaseCapture();
                WindowNativeMethods.SendMessage(Handle, WindowConstants.WM_NCLBUTTONDOWN, WindowConstants.HT_CAPTION, 0);
            }
        }


        private void LoginForm_Load(object sender, EventArgs e)
        {
            checkBox1.Checked = false;
            passwordTextBox.UseSystemPasswordChar = true;
            this.TopMost = true;
            this.Enabled = true;
            toolTip1.SetToolTip(selectDatabaseButton, "save before selecting if you want");
            toolTip1.SetToolTip(saveBt, "save credential and other login info to encrypted file");
            toolTip1.SetToolTip(DriverComboBox, "odbc driver name NetezzaSQL");
            toolTip1.SetToolTip(databaseComboBox, "name of database (not server)");
            toolTip1.SetToolTip(serverTextBox, "server ip");
            toolTip1.SetToolTip(userNameTextBox, "username");
            toolTip1.SetToolTip(passwordTextBox, "password to database");
            toolTip1.SetToolTip(nameTextBox, "your name of connection like: my best database");

            toolTip1.SetToolTip(this.addNewButton, "add new connection\r\n to clone use context menu on \"Login Data\" block");
            toolTip1.SetToolTip(this.deleteButton, "delete connection");

            toolTip1.SetToolTip(this.checkBoxFastLogin, "check only if you saved connection !");
        }

        private void tsmiDbClick_Click(object sender, EventArgs e)
        {
            if (sender == tsmiDB2)
            {
                this.userNameTextBox.Text = "user";
                this.passwordTextBox.Text = "password";
                this.serverTextBox.Text = "111.111.111.111:50000";
                this.databaseComboBox.Text = "SAMPLE";
                this.DriverComboBox.Text = "DB2";
            }
            else if (sender == tsmiNz)
            {
                this.userNameTextBox.Text = "user";
                this.passwordTextBox.Text = "password";
                this.serverTextBox.Text = "111.111.111.111:5480";
                this.databaseComboBox.Text = "SYSTEM";
                this.DriverComboBox.Text = "NetezzaSQL";
            }
            else if (sender == tsmiOracle)
            {
                this.userNameTextBox.Text = "user";
                this.passwordTextBox.Text = "password";
                this.serverTextBox.Text = "111.111.111.111:1521";
                this.databaseComboBox.Text = "XEPDB1";
                this.DriverComboBox.Text = "Oracle";
            }
            else if (sender == tsmiAccess)
            {
                this.userNameTextBox.Text = "";
                this.passwordTextBox.Text = "";
                this.serverTextBox.Text = "C:\\directory";
                this.databaseComboBox.Text = "filename.accdb";
                this.DriverComboBox.Text = "Microsoft.ACE.OLEDB.12.0";
            }
            else if (sender == tsmiMsSqlStandard)
            {
                this.userNameTextBox.Text = "SA";
                this.passwordTextBox.Text = "password";
                this.serverTextBox.Text = "111.111.111.111";
                this.databaseComboBox.Text = "";
                this.DriverComboBox.Text = "MsSqlStd";
            }
            else if (sender == tsmiMsSqlTrusted)
            {
                this.userNameTextBox.Text = "";
                this.passwordTextBox.Text = "";
                this.serverTextBox.Text = @"server ip/name";
                this.databaseComboBox.Text = "";
                this.DriverComboBox.Text = "MsSqlTrusted";
            }
            else if (sender == tsmiPostgres)
            {
                this.userNameTextBox.Text = "postgres";
                this.passwordTextBox.Text = "password";
                this.serverTextBox.Text = @"111.111.111.111:5432";
                this.databaseComboBox.Text = "";
                this.DriverComboBox.Text = "Postgres";
            }
            else if (sender == tsmiSQLite)
            {
                this.userNameTextBox.Text = "";
                this.passwordTextBox.Text = "";
                this.serverTextBox.Text = "C:\\directory";
                this.databaseComboBox.Text = "databaseFile.db";
                this.DriverComboBox.Text = "SQLite";
            }
            else if (sender == tsmiMySql)
            {
                this.userNameTextBox.Text = "root";
                this.passwordTextBox.Text = "password";
                this.serverTextBox.Text = "localhost";
                this.databaseComboBox.Text = "employees";
                this.DriverComboBox.Text = "MySql";
            }
        }

        private bool cancel = false;
        private void XButton_Click(object sender, EventArgs e)
        {
            cancel = true;
            _viewModel.CancelCommand.Execute(null);
            this.Close();
        }

        private void BtReorder_Click(object sender, EventArgs e)
        {
            List<string> list = new List<string>();

            foreach (var item in LoginDataList)
            {
                list.Add(item.Name);
            }
            string defName = LoginDataList[LoginDataList[0].DefaultIndex].Name;

            var f = new DbForms.SortConnections(_uiHelperService, list, defName);

            f.StartPosition = FormStartPosition.CenterParent;
            if (f.ShowDialog() == DialogResult.OK)
            {
                Reorder(f.NewOrder, f.DefNum);
            }

        }

        private void databaseComboBox_DropDown(object sender, EventArgs e)
        {
            _ = ObserveUiOperationAsync(nameof(databaseComboBox_DropDown), FetchDatabasesAsync);
        }

        private async Task FetchDatabasesAsync()
        {
            SyncViewModelFromControls();
            await _viewModel.FetchDatabasesCommand.ExecuteAsync(null);
            if (IsDisposed || Disposing)
                return;

            databaseComboBox.Items.Clear();
            databaseComboBox.Items.AddRange(_viewModel.Databases.ToArray());
            if (!string.IsNullOrWhiteSpace(_viewModel.ErrorMessage))
            {
                MessageBox.Show(_viewModel.ErrorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task ObserveUiOperationAsync(string operationName, Func<Task> operation)
        {
            try
            {
                await operation();
            }
            catch (OperationCanceledException)
            {
                // Closing the login dialog or replacing a fetch is expected.
            }
            catch (Exception exception)
            {
                System.Diagnostics.Trace.WriteLine($"{operationName} failed: {exception.GetType().Name}");
                if (!IsDisposed && !Disposing)
                {
                    MessageBox.Show(
                        this,
                        "The operation could not be completed. Check the connection settings and try again.",
                        "Login error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
        }

        private void AddNewButton_Click(object sender, EventArgs e)
        {
            SyncViewModelFromControls();
            _viewModel.AddCommand.Execute(null);
            SyncControlsFromViewModel();
        }

        private void cloneConnectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SyncViewModelFromControls();
            _viewModel.CloneCommand.Execute(null);
            SyncControlsFromViewModel();
        }

        private void CheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            passwordTextBox.UseSystemPasswordChar = !checkBox1.Checked;
        }

        private void SetupModernUI()
        {
            // Setup modern form styling
            FormBorderStyle = FormBorderStyle.None;

            // Add drop shadow effect
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);

            // Style existing buttons with modern flat design
            StyleModernButtons();
            SetRoundedButtonCorners();
            if (!_applicationSettingsContext.Config.FastLogin && !SuppressBlurOverlay)
            {
                ShowBlurOverlay();
            }

            // Set rounded corners
            SetRoundedCorners(CornerRadius);
        }

        private void ApplyThemeColors()
        {
            var theme = new ColorTheme(_applicationSettingsContext.Config);
            theme.InitColors();
            bool dark = _applicationSettingsContext.Config.UseSpecialColoring;

            BackColor = dark ? theme.MainBack : Color.FromArgb(248, 249, 250);
            Color border = DarkChromeHelper.SoftBorder(BackColor, dark);
            // Lift fields slightly above the form so combo text stays readable in dark mode.
            Color fieldBack = dark
                ? ControlPaint.Light(theme.MainBack, 0.10f)
                : theme.TextBoxBackColor;
            Color fieldFore = dark
                ? Color.FromArgb(240, 240, 240)
                : theme.TextBoxForeColor;

            foreach (TextBox textBox in new[] { userNameTextBox, passwordTextBox, serverTextBox, nameTextBox })
            {
                DarkChromeHelper.ApplyTextBox(textBox, fieldBack, fieldFore, border);
            }

            foreach (ComboBox comboBox in new[] { connectionSelectorComboBox, databaseComboBox, DriverComboBox })
            {
                DarkChromeHelper.ApplyComboBox(comboBox, fieldBack, fieldFore, ownerDrawItems: dark);
            }

            Color labelColor = dark ? theme.MainFore : Color.FromArgb(108, 117, 125);
            foreach (Label label in new[] { label1, label2, label3, label4, label5, label6, label7 })
            {
                label.ForeColor = labelColor;
            }

            programName.ForeColor = dark ? Color.FromArgb(236, 236, 236) : Color.FromArgb(33, 37, 41);
            label7.ForeColor = dark ? Color.FromArgb(168, 168, 168) : Color.FromArgb(120, 120, 120);

            foreach (CheckBox checkBox in new[] { checkBoxFastLogin, rememberAsDefaultCheckBox, checkBox1 })
            {
                checkBox.ForeColor = dark ? theme.MainFore : SystemColors.ControlText;
                checkBox.BackColor = Color.Transparent;
            }

            DarkChromeHelper.ApplyGroupBox(
                groupBox1,
                BackColor,
                dark ? theme.MainFore : Color.FromArgb(52, 58, 64),
                border,
                drawChildFieldBorders: true);
            DarkChromeHelper.AttachChildBorders(this, border);
            GridThemingHelper.ApplyScrollbarThemeRecursive(this, dark);
        }

        private int CornerRadius => DpiScale.Scale(20, DeviceDpi);

        private void SetRoundedCorners(int radius)
        {
            this.Region = new Region(GetRoundedRectPath(this.ClientRectangle, radius));
        }

        private GraphicsPath GetRoundedRectPath(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            bounds.Width--;
            bounds.Height--;
            path.AddArc(bounds.X, bounds.Y, radius, radius, 180, 90);
            path.AddArc(bounds.Right - radius, bounds.Y, radius, radius, 270, 90);
            path.AddArc(bounds.Right - radius, bounds.Bottom - radius, radius, radius, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - radius, radius, radius, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (var pen = new Pen(Color.FromArgb(0, 122, 204), DpiScale.Scale(4, DeviceDpi)))
            {
                using (var path = GetRoundedRectPath(this.ClientRectangle, CornerRadius))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            SetRoundedCorners(CornerRadius);
            SetRoundedButtonCorners();
        }

        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            base.OnDpiChanged(e);
            SetRoundedCorners(CornerRadius);
            SetRoundedButtonCorners();
            Invalidate();
        }

        private void SetRoundedButtonCorners()
        {
            int radius = DpiScale.Scale(5, DeviceDpi);
            foreach (Button button in new[] { selectDatabaseButton, saveBt, addNewButton, deleteButton, xButton, btReorder })
            {
                if (button.ClientSize.Width <= 1 || button.ClientSize.Height <= 1)
                {
                    continue;
                }

                button.Region?.Dispose();
                using var path = GetRoundedRectPath(button.ClientRectangle, radius);
                button.Region = new Region(path);
            }
        }

        private void StyleModernButtons()
        {
            // Style selectDatabaseButton
            selectDatabaseButton.BackColor = Color.FromArgb(0, 122, 204);
            selectDatabaseButton.ForeColor = Color.White;
            selectDatabaseButton.FlatStyle = FlatStyle.Flat;
            selectDatabaseButton.FlatAppearance.BorderSize = 0;
            selectDatabaseButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(28, 151, 234);
            selectDatabaseButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 102, 184);
            selectDatabaseButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            selectDatabaseButton.Height = 40;
            selectDatabaseButton.Cursor = Cursors.Hand;

            // Style saveBt
            saveBt.BackColor = Color.FromArgb(108, 117, 125);
            saveBt.ForeColor = Color.White;
            saveBt.FlatStyle = FlatStyle.Flat;
            saveBt.FlatAppearance.BorderSize = 0;
            saveBt.FlatAppearance.MouseOverBackColor = Color.FromArgb(128, 137, 145);
            saveBt.FlatAppearance.MouseDownBackColor = Color.FromArgb(88, 97, 105);
            saveBt.Font = new Font("Segoe UI", 10F);
            saveBt.Height = 40;
            saveBt.Cursor = Cursors.Hand;

            // Style addNewButton
            addNewButton.BackColor = Color.FromArgb(40, 167, 69);
            addNewButton.ForeColor = Color.White;
            addNewButton.FlatStyle = FlatStyle.Flat;
            addNewButton.FlatAppearance.BorderSize = 0;
            addNewButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 187, 89);
            addNewButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(20, 147, 49);
            addNewButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            addNewButton.Height = 40;
            addNewButton.Cursor = Cursors.Hand;

            // Style deleteButton
            deleteButton.BackColor = Color.FromArgb(220, 53, 69);
            deleteButton.ForeColor = Color.White;
            deleteButton.FlatStyle = FlatStyle.Flat;
            deleteButton.FlatAppearance.BorderSize = 0;
            deleteButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 73, 89);
            deleteButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(200, 33, 49);
            deleteButton.Font = new Font("Segoe UI", 10F);
            deleteButton.Height = 40;
            deleteButton.Cursor = Cursors.Hand;

            // Style testConnectionBt in groupBox1 if it exists
            var testBtn = groupBox1.Controls.OfType<Button>().FirstOrDefault(b => b.Name == "testConnectionBt");
            if (testBtn != null)
            {
                testBtn.BackColor = Color.FromArgb(255, 193, 7);
                testBtn.ForeColor = Color.FromArgb(33, 37, 41);
                testBtn.FlatStyle = FlatStyle.Flat;
                testBtn.FlatAppearance.BorderSize = 0;
                testBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 213, 47);
                testBtn.FlatAppearance.MouseDownBackColor = Color.FromArgb(235, 173, 0);
                testBtn.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                testBtn.Height = 40;
                testBtn.Cursor = Cursors.Hand;
            }

            // GroupBox chrome is applied in ApplyThemeColors (soft border in dark mode).
            groupBox1.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        }

        private BlurOverlay _blurOverlay;
        private void ShowBlurOverlay()
        {
            _blurOverlay = new BlurOverlay();
            _blurOverlay.Show();
            _blurOverlay.BringToFront();
            BringToFront();
            TopMost = true;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _blurOverlay?.Close();
            _viewModel.Dispose();
            base.OnFormClosed(e);
        }

        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            await _viewModel.InitializeAsync();
            SyncControlsFromViewModel();

            // Start position for the animation (slightly below the final position)
            var startY = this.Location.Y + 30;
            var endY = this.Location.Y;
            this.Location = new Point(this.Location.X, startY);
            this.Opacity = 0;

            // Animation settings
            const int duration = 300; // ms
            const int steps = 30;
            var stepInterval = duration / steps;

            for (int i = 0; i <= steps; i++)
            {
                var progress = (double)i / steps;
                var easedProgress = Easing(progress);

                // Update opacity
                this.Opacity = easedProgress;

                // Update position
                var newY = startY - (int)((startY - endY) * easedProgress);
                this.Location = new Point(this.Location.X, newY);

                await Task.Delay(stepInterval);
            }

            // Ensure final state is set correctly
            this.Opacity = 1;
            this.Location = new Point(this.Location.X, endY);

            if (!string.IsNullOrWhiteSpace(DocumentationScreenshotPath))
            {
                connectionSelectorComboBox.Invalidate(true);
                databaseComboBox.Invalidate(true);
                DriverComboBox.Invalidate(true);
                Refresh();
                Application.DoEvents();
                SaveDocumentationScreenshot(DocumentationScreenshotPath);
                BeginInvoke(Close);
            }
        }

        internal void SaveDocumentationScreenshot(string filePath)
        {
            string? directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var bitmap = new Bitmap(ClientSize.Width, ClientSize.Height);
            DrawToBitmap(bitmap, new Rectangle(Point.Empty, ClientSize));
            bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
        }

        private static double Easing(double t)
        {
            // Simple Ease-Out-Cubic easing function
            return 1 - Math.Pow(1 - t, 3);
        }
    }
}
