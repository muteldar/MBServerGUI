using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Mount_and_Blade_Server_Panel.GameModesDataSetTableAdapters;
using Mount_and_Blade_Server_Panel.Properties;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Path = System.IO.Path;

namespace Mount_and_Blade_Server_Panel
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly Process _serverProcess = new Process();
        private string _serverConfig = String.Empty;
        private SettingsPopup _settingsPopup;
        private readonly List<string> _serverFilesList;
        private readonly BackgroundWorker _worker;
        private Brush _serverInstallStatusColor = Brushes.Red;
        private string _serverInstallStatus = "No Server Installed";

        public MainWindow()
        {
            InitializeComponent();
            GetModules();
            Populate_GameModes();
            UIVersionTextBlock.Content = "UI Version " + Assembly.GetExecutingAssembly().GetName().Version;
            Settings.Default.InstallFolder =
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mount and Blade Server Panel");
            Settings.Default.PropertyChanged += SettingsChanged;
            _serverFilesList = GetServerFiles();
            _worker = new BackgroundWorker();
            _worker.DoWork += Worker_DoWork;
            _worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            _worker.ProgressChanged += Worker_ProgressChanged;
            _worker.WorkerSupportsCancellation = true;
            _worker.WorkerReportsProgress = true;
        }

        /// <summary>
        /// Watch for ServerExeLocation settings change.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "ServerExeLocation":
                    if (Settings.Default.ServerVersion != 0)
                    {
                        _serverInstallStatus = Settings.Default.ServerVersion == 9999
                                    ? "Server Installed Unknown Version"
                                    : "Server Version " + Settings.Default.ServerVersion + " Installed";
                        _serverInstallStatusColor = Brushes.GreenYellow;
                    }
                    else
                    {
                        _serverInstallStatus = "No Server Installed";
                        _serverInstallStatusColor = Brushes.Red;
                    }
                    Dispatcher.Invoke(() =>
                    {
                        if (Settings.Default.ServerVersion != 0)
                        {
                            InstalledServerTextBlock.Content = ServerVersionTextLabel.Content = _serverInstallStatus;
                            InstalledServerTextBlock.Foreground = ServerVersionTextLabel.Foreground = _serverInstallStatusColor;
                            ServerEXETextBox.Text = Settings.Default.ServerExeLocation;
                            UninstallServerButton.IsEnabled = true;
                        }
                        else
                        {
                            InstalledServerTextBlock.Content = ServerVersionTextLabel.Content = _serverInstallStatus;
                            InstalledServerTextBlock.Foreground = ServerVersionTextLabel.Foreground = _serverInstallStatusColor;
                            ServerEXETextBox.Text = "Double Click to select";
                            UninstallServerButton.IsEnabled = false;
                        }
                    },
                    DispatcherPriority.Normal);
                    break;
                case "ModulesLocation":
                    ModulesTextBox.Clear();
                    ModulesTextBox.Text = Settings.Default.ModulesLocation;
                    break;
            }
        }

        /// <summary>
        /// Set Server EXE on double click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ServerEXETextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var dialog = new OpenFileDialog { DefaultExt = ".exe", Filter = "Executables (.exe)|*.exe"};
            var result = dialog.ShowDialog();
            if (result != true) return;
            var fileInfo = FileVersionInfo.GetVersionInfo(dialog.FileName);
            if(fileInfo.ProductName.Contains("Mount&Blade: Warband"))
            {
                Settings.Default.ServerVersion = 9999;
                ServerEXETextBox.Text = dialog.FileName;
                Settings.Default.ServerExeLocation = ServerEXETextBox.Text;
            }
            else
            {
                MessageBox.Show("Please choose the correct EXE file");
                Settings.Default.ServerVersion = 0;
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
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            ModulesTextBox.Text = Settings.Default.ModulesLocation = dialog.SelectedPath;
            GetModules();
        }

        /// <summary>
        /// Find Modules with the selected directory. Search for module.ini as an indicator there are modules present.
        /// </summary>
        private void GetModules()
        {
            ModuleComboBox.Items.Clear();
            if (!Directory.Exists(Settings.Default.ModulesLocation))
            {
                GameModeComboBox.IsEnabled = false;
                return;
            }
            foreach (var dir in Directory.GetDirectories(Settings.Default.ModulesLocation))
            {
                foreach (var file in Directory.GetFiles(dir))
                {
                    if (file.Contains("module.ini"))
                    {
                        ModuleComboBox.Items.Add(new DirectoryInfo(dir).Name);
                    }
                }
            }
            GameModeComboBox.IsEnabled = true;
        }

        /// <summary>
        /// Grab Server Files from FTP
        /// </summary>
        /// <returns>List of strings of Dedicated Server Files</returns>
        private static List<string> GetServerFiles()
        {
            var request = (FtpWebRequest) WebRequest.Create(Settings.Default.FilesURI);
            request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
            request.Credentials = new NetworkCredential("anonymous", "anonymous");
            var itemsList = new List<string>();

            using (var response = (FtpWebResponse) request.GetResponse())
            {
                using (var responseStream = response.GetResponseStream())
                {
                    char[] splitter = {' '};

                    if (responseStream == null) return itemsList;
                    using (var reader = new StreamReader(responseStream))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            var items = line.Split(splitter, StringSplitOptions.RemoveEmptyEntries);
                            itemsList.AddRange(
                                items.Where(item => item.Contains("zip") && item.Contains("mb_warband_dedicated")));
                        }
                    }
                }
            }

            return itemsList;
        }

        /// <summary>
        /// Download server files with button click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ServerDownload_Click(object sender, RoutedEventArgs e)
        {
            if (ServerFilesComboBox.SelectedValue != null)
            {
                ServerFilesButton.IsEnabled = false;
                CancelDownloadButton.IsEnabled = true;
                _worker.RunWorkerAsync(ServerFilesComboBox.SelectedValue.ToString());
                ServerVersionTextLabel.Content = InstalledServerTextBlock.Content = "Installing Server";
                ServerVersionTextLabel.Foreground = InstalledServerTextBlock.Foreground = Brushes.GreenYellow; 
            }
        }

        /// <summary>
        /// Cancel download server files with button click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ServerCancelDownload_Click(object sender, RoutedEventArgs e)
        {
            _worker.CancelAsync();
            ServerFilesButton.IsEnabled = true;
            CancelDownloadButton.IsEnabled = false;
        }

        /// <summary>
        /// Uninstall local server files
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private void UninstallServer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Uninstall_Server();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                throw;
            }

        }

        private void Uninstall_Server()
        {
            if (Directory.Exists(Settings.Default.InstallFolder))
            {
                Directory.Delete(Settings.Default.InstallFolder, true);
                Settings.Default.ServerVersion = 0;
                Settings.Default.ServerExeLocation = "";
            }
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            var steam = false;
            Dispatcher.Invoke(() =>
            {
                steam = SteamEdition.IsChecked != null && (bool) SteamEdition.IsChecked;
                SteamEdition.IsEnabled = false;
            }, DispatcherPriority.Normal);
            InstallServer((string) e.Argument, steam);
        }

        private void Worker_RunWorkerCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (!_worker.CancellationPending)
            {
                ServerFilesButton.IsEnabled = true;
                CancelDownloadButton.IsEnabled = false;
                ProgressBarDownload.Value = 100;
            }
            else
            {
                ServerFilesButton.IsEnabled = true;
                CancelDownloadButton.IsEnabled = false;
                ProgressBarDownload.Value = 0;
            }

            Dispatcher.Invoke(() =>
            {
                SteamEdition.IsEnabled = true;
                SteamEdition.IsChecked = false;
            }, DispatcherPriority.Normal);
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressBarDownload.Value = e.ProgressPercentage;
        }

        /// <summary>
        /// Download and Install Server files
        /// </summary>
        /// <param name="file">File name to download</param>
        /// <param name="steam">Install Steam server or not</param>
        private void InstallServer(string file, bool steam)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                MessageBox.Show("No Network Connection");
                return;
            }

            var fileParts = file.Split('_');
            var serverVersion = 0;
            if (fileParts[fileParts.Length - 1] != null)
            {
                try
                {
                    Int32.TryParse(fileParts[fileParts.Length - 1].Split('.')[0], out serverVersion);
                }
                catch
                {
                    MessageBox.Show("Server Version not recognized. Please try to reinstall");
                }
            }
            else
            {
                MessageBox.Show("Server Version not recognized. Please try to reinstall");
            }

            Uninstall_Server();

            Settings.Default.ServerVersion = serverVersion;

            long fileSize;
            var networkCred = new NetworkCredential("anonymous", "anonymous");
            var sizeRequest = (FtpWebRequest) WebRequest.Create(Settings.Default.FilesURI + file);
            sizeRequest.Method = WebRequestMethods.Ftp.GetFileSize;
            sizeRequest.Credentials = networkCred;
            using (var sizeResponse = sizeRequest.GetResponse())
            {
                fileSize = sizeResponse.ContentLength;
            }

            var request = (FtpWebRequest) WebRequest.Create(Settings.Default.FilesURI + file);

            request.Method = WebRequestMethods.Ftp.DownloadFile;
            request.UseBinary = true;
            request.Credentials = networkCred;

            using (var response = (FtpWebResponse) request.GetResponse())
            {
                var responseStream = response.GetResponseStream();
                using (var writer = new FileStream(file, FileMode.Create))
                {
                    const int bufferSize = 32*1024;

                    var buffer = new byte[bufferSize];

                    if (responseStream != null)
                    {
                        var readCount = responseStream.Read(buffer, 0, bufferSize);
                        while (readCount > 0)
                        {
                            if(!_worker.CancellationPending)
                            {
                                writer.Write(buffer, 0, readCount);
                                readCount = responseStream.Read(buffer, 0, bufferSize);
                                _worker.ReportProgress((int) Math.Round(100*((float) writer.Length/fileSize), 0));
                            }
                            else
                            {
                                Settings.Default.ServerVersion = 0;
                                return;
                            }
                        }
                        responseStream.Close();
                    }
                }
            }

            ZipFile.ExtractToDirectory(file, Settings.Default.InstallFolder);

            var subDirs = Directory.GetDirectories(Settings.Default.InstallFolder);

            var exeType = steam ? "mb_warband_dedicated.exe" : "mb_warband_dedicated_steam.exe";

            foreach (var dir in subDirs)
            {
                if (dir.Contains("Modules"))
                {
                    Settings.Default.ModulesLocation = dir;
                }
                var files = Directory.GetFiles(dir);
                foreach (var item in files)
                {
                    if (item.Contains(exeType))
                    {
                        Settings.Default.ServerExeLocation = item;
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// validate and start server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LaunchButton_Click(object sender, RoutedEventArgs e)
        {  
            Build_Settings();
            if (Settings.Default.Debug)
            {
                MessageBox.Show(_serverConfig);
            }
            else
            {
                _serverProcess.StartInfo.FileName = Settings.Default.ServerExeLocation;
                if (_serverConfig != String.Empty)
                {
                    _serverProcess.StartInfo.Arguments = "-r " + _serverConfig + " -m " + ModuleComboBox.SelectedItem;
                    _serverProcess.EnableRaisingEvents = true;
                    _serverProcess.Exited += ServerProcess_Exited;
                    try
                    {
                        WindowState = WindowState.Minimized;
                        _serverProcess.Start();
                        LaunchButton.IsEnabled = false;
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
                    () =>
                    {
                        WindowState = WindowState.Normal;
                        LaunchButton.IsEnabled = AddSettingButton.IsEnabled = RemoveSettingButton.IsEnabled = true;
                    }, DispatcherPriority.Normal);
            }
        }

        /// <summary>
        /// Show ServerFiles Flyout for downloading current serverfiles.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ServerFiles_Click(object sender, RoutedEventArgs e)
        {
            if (ServerSettingsFlyout.IsOpen)
                ServerSettingsFlyout.IsOpen = false;
            ServerInstallFlyout.IsOpen = true;
            if (ServerFilesComboBox.Items.Count > 0) return;
            foreach (var file in _serverFilesList)
            {
                ServerFilesComboBox.Items.Add(file);
            }
            ServerFilesComboBox.SelectedIndex = ServerFilesComboBox.Items.Count - 1;
            ServerVersionTextLabel.Content = _serverInstallStatus;
            ServerVersionTextLabel.Foreground = _serverInstallStatusColor;
        }

        /// <summary>
        /// Show FileSettings Flyout for setting the server exe locations.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FileSettings_Click(object sender, RoutedEventArgs e)
        {
            if (ServerInstallFlyout.IsOpen)
                ServerInstallFlyout.IsOpen = false;
            ServerSettingsFlyout.IsOpen = true;
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
        /// Start setting dialog popup
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Add_SettingClick(object sender, RoutedEventArgs e)
        {
            if (_settingsPopup == null)
            {
                _settingsPopup = new SettingsPopup();
                _settingsPopup.Show();
                _settingsPopup.Closed += SettingsPop_Closed;
            }
            else if (_settingsPopup != null)
            {
                _settingsPopup.Activate();
            }
        }

        /// <summary>
        /// Remove highlighted setting
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
            if (Settings.Default.ServerExeLocation != "" || Settings.Default.SteamServerExeLocation != "")
            {
                if (Settings.Default.ModulesLocation != "")
                {
                    if (Directory.Exists(Path.GetDirectoryName(Settings.Default.ServerExeLocation)))
                    {
                        using (
                            var write =
                                new StreamWriter(Path.GetDirectoryName(Settings.Default.ServerExeLocation) + "/" +
                                                 Settings.Default.ConfigName))
                        {
                            write.WriteLine("set_mission " + GameModeComboBox.SelectedValue);
                            foreach (DataRowView row in SettingsListView.Items)
                            {
                                write.WriteLine(row["Name"] + " " + row["Value"]);
                            }
                            write.WriteLine("start");
                        }
                        _serverConfig = "serverconfig.mbconf";
                    }
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
            _settingsPopup = null;
        }
    }
}