using System;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using Mount_and_Blade_Server_Panel.GameModesDataSetTableAdapters;
using Mount_and_Blade_Server_Panel.Properties;
using Mount_and_Blade_Server_Panel.ServerSettingsDataSetTableAdapters;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace Mount_and_Blade_Server_Panel
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly Process _serverProcess = new Process();
        public string ServerConfig = String.Empty;
        private SettingsPopup settingsPopup;

        public MainWindow()
        {
            InitializeComponent();
            GetModules();
            Populate_GameModes();
        }
        /// <summary>
        /// Set Server EXE on double click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ServerEXETextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var dialog = new OpenFileDialog {DefaultExt = ".exe", Filter = "Executables (.exe)|*.exe"};
            var result = dialog.ShowDialog();
            if (result == true)
            {
                ServerEXETextBox.Text = dialog.FileName;
                Settings.Default.ServerExeLocation = ServerEXETextBox.Text;
            }
        }
        /// <summary>
        /// Set Module Directory on double click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ModuleTextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            var result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                ModulesTextBox.Text = dialog.SelectedPath;
                Settings.Default.ModulesLocation = ModulesTextBox.Text;
                GetModules();
            }
        }
        /// <summary>
        /// Find Modules with the selected directory. Search for module.ini as an indicator there are modules present.
        /// </summary>
        private void GetModules()
        {
            ModuleComboBox.Items.Clear();
            if (Directory.Exists(Settings.Default.ModulesLocation))
            {
                var subDirs = Directory.GetDirectories(Settings.Default.ModulesLocation);
                foreach (var dir in subDirs)
                {
                    var files = Directory.GetFiles(dir);
                    foreach (var file in files)
                    {
                        if (file.Contains("module.ini"))
                        {
                            ModuleComboBox.Items.Add(new DirectoryInfo(dir).Name);
                        }
                    }
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Settings.Default.ServerExeLocation != "")
            {
                ServerEXETextBox.Text = Settings.Default.ServerExeLocation;
            }

            if (Settings.Default.ModulesLocation != "")
            {
                ModulesTextBox.Text = Settings.Default.ModulesLocation;
                GetModules();
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {  
            Build_Settings();
            if (Settings.Default.Debug)
            {
                MessageBox.Show(ServerConfig);
            }
            else
            {
                _serverProcess.StartInfo.FileName = Settings.Default.ServerExeLocation;

                if (ServerConfig != String.Empty)
                {
                    _serverProcess.StartInfo.Arguments = "-r " + ServerConfig + " -m " + ModuleComboBox.SelectedItem;
                    _serverProcess.EnableRaisingEvents = true;
                    _serverProcess.Exited += ServerProcess_Exited;
                    try
                    {
                        WindowState = WindowState.Minimized;
                        _serverProcess.Start();
                        StartButton.IsEnabled = false;
                        AddSettingButton.IsEnabled = false;
                        RemoveSettingButton.IsEnabled = false;
                    }
                    catch
                    {
                        MessageBox.Show("ServerProcess didn't Start :" + _serverProcess.ProcessName);
                    }
                }
            }
        }

        /// <summary>
        /// Handle exit of the MB server exe reactivate buttons that have been blocked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ServerProcess_Exited(object sender, EventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(
                    () => WindowState = WindowState.Normal, DispatcherPriority.Normal);
                Dispatcher.Invoke(
                    () => StartButton.IsEnabled = true, DispatcherPriority.Normal);
                Dispatcher.Invoke(
                    () => AddSettingButton.IsEnabled = true, DispatcherPriority.Normal);
                Dispatcher.Invoke(
                    () => RemoveSettingButton.IsEnabled = true, DispatcherPriority.Normal);
            }
        }

        /// <summary>
        /// Show current build version of Assembly as an "About".
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Mount and Blade: Server Panel v" + Assembly.GetExecutingAssembly().GetName().Version);
        }

        /// <summary>
        /// Populate GameMode dropdown from the DB.
        /// </summary>
        private void Populate_GameModes()
        {
            var gameModeAdapter = new GameModesDataSet();
            var gameModeTableAdapter = new GameModesTableAdapter();
            gameModeTableAdapter.Fill(gameModeAdapter.GameModes);
            GameModeComboBox.ItemsSource = gameModeAdapter.GameModes.DefaultView;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Add_SettingClick(object sender, RoutedEventArgs e)
        {
            if (settingsPopup == null)
            {
                settingsPopup = new SettingsPopup();
                settingsPopup.Show();
                settingsPopup.Closed += SettingsPop_Closed;
            }
            else if (settingsPopup != null)
            {
                settingsPopup.Activate();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Remove_SettingClick(object sender, RoutedEventArgs e)
        {
            if (SettingsListView.Items.Count != 0)
            {
                SettingsListView.Items.Remove(SettingsListView.SelectedItem);
            }
        }

        /// <summary>
        /// Build Settings into the config file for server launch.
        /// </summary>
        private void Build_Settings()
        {
            if (Settings.Default.ServerExeLocation != "" && Settings.Default.ModulesLocation != "")
            {
                if (Directory.Exists(Path.GetDirectoryName(Settings.Default.ServerExeLocation)))
                {
                    using (var write = new StreamWriter(Path.GetDirectoryName(Settings.Default.ServerExeLocation) + "/" + Settings.Default.ConfigName))
                    {
                        write.WriteLine("set_mission " + GameModeComboBox.SelectedValue);
                        foreach (DataRowView row in SettingsListView.Items)
                        {
                            write.WriteLine(row["Name"] + " " + row["Value"]);
                        }
                    }
                    ServerConfig = "serverconfig.mbconf";
                }
            }
        }

        /// <summary>
        /// Close any popups that may exist when exiting main window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Exit(object sender, CancelEventArgs e)
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window.GetType() == typeof (SettingsPopup))
                {
                    window.Close();   
                }
            }
        }

        /// <summary>
        /// Fire when settings popup has been closed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SettingsPop_Closed(object sender, EventArgs e)
        {
            settingsPopup = null;
        }
    }
}
