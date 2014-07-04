using System;
using System.Collections;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using Mount_and_Blade_Server_Panel.GameModesDataSetTableAdapters;
using Mount_and_Blade_Server_Panel.Properties;
using Mount_and_Blade_Server_Panel.ServerSettingsDataSetTableAdapters;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace Mount_and_Blade_Server_Panel
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Process _serverProcess = new Process();
        public string serverConfig = String.Empty;

        public MainWindow()
        {
            InitializeComponent();
            GetModules();
            Populate_GameModes();
            Populate_ListGrid();
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
                var fileName = new FileInfo(dialog.FileName);
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
                Properties.Settings.Default.ModulesLocation = ModulesTextBox.Text;
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
            var dbConn = new SQLiteConnection("Data Source=Settings.db; Version=3;");

            if (Settings.Default.ServerExeLocation != "")
            {
                ServerEXETextBox.Text = Properties.Settings.Default.ServerExeLocation;
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
                MessageBox.Show(serverConfig);
            }
            else
            {
                _serverProcess.StartInfo.FileName = Settings.Default.ServerExeLocation;

                if (serverConfig != String.Empty)
                {
                    _serverProcess.StartInfo.Arguments = "-r " + serverConfig + " -m " + ModuleComboBox.SelectedItem;
                    _serverProcess.EnableRaisingEvents = true;
                    _serverProcess.Exited += ServerProcess_Exited;
                    try
                    {
                        WindowState = WindowState.Minimized;
                        _serverProcess.Start();
                        StartButton.IsEnabled = false;
                    }
                    catch
                    {
                        MessageBox.Show("ServerProcess didn't Start :" + _serverProcess.ProcessName);
                    }
                }
            }
        }

        private void ServerProcess_Exited(object sender, EventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(
                    () => WindowState = WindowState.Normal, DispatcherPriority.Normal);
                Dispatcher.Invoke(
                    () => StartButton.IsEnabled = true, DispatcherPriority.Normal);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Mount and Blade: Server Panel v" + Assembly.GetExecutingAssembly().GetName().Version);
        }

        /// <summary>
        /// 
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
        private void Populate_ListGrid()
        {
            var settingsAdapter = new ServerSettingsDataSet();
            var settingsTableAdapter = new ServerSettingsTableAdapter();
            settingsTableAdapter.Fill(settingsAdapter.ServerSettings);
            foreach (DataRowView row in settingsAdapter.ServerSettings.DefaultView)
            {
                SettingsListView.Items.Add(row);
                //if (row["Type"].ToString().ToLower() == "bool")
                //{
                //    SettingsListView.Items.Add(row);
                //}
            }
            //SettingsListView.ItemsSource = settingsAdapter.ServerSettings.DefaultView;
        }

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
                            write.WriteLine(row["Name"].ToString() + " " + row["Value"].ToString());
                        }
                    }
                    serverConfig = "serverconfig.mbconf";
                }
            }
        }
    }
}
