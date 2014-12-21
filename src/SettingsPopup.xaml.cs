using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using Mount_and_Blade_Server_Panel.ServerSettingsDataSetTableAdapters;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace Mount_and_Blade_Server_Panel
{
    /// <summary>
    /// Interaction logic for SettingsPopup.xaml
    /// </summary>
    public partial class SettingsPopup
    {
        private readonly ServerSettingsDataSet _settingsAdapter = new ServerSettingsDataSet();
        private readonly ServerSettingsTableAdapter _settingsTableAdapter = new ServerSettingsTableAdapter();

        public SettingsPopup()
        {
            InitializeComponent();
            _settingsTableAdapter.Fill(_settingsAdapter.ServerSettings);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Populate_SettingsCombo();
            SettingCombo_Changed(this, new RoutedEventArgs());
        }

        private void Populate_SettingsCombo()
        {
            foreach (DataRowView row in _settingsAdapter.ServerSettings.DefaultView)
            {
                var comboItem = new ComboBoxItem {Content = row["Name"], Tag = row["Type"]};
                SettingsComboBox.Items.Add(comboItem);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    
        private void Add_Click(object sender, RoutedEventArgs e)
        {
            foreach (var window in Application.Current.Windows)
            {
                if (window.GetType() == typeof (MainWindow))
                {
                    foreach (DataRowView row in _settingsAdapter.ServerSettings.DefaultView)
                    {
                        if (row["Name"].ToString() == SettingsComboBox.SelectionBoxItem.ToString())
                        {
                            var mainWindow = window as MainWindow;
                            if (mainWindow != null)
                            {
                                if (row["Type"].ToString() == "BOOL")
                                {
                                    row["Value"] = SettingValueComboBox.SelectionBoxItem.ToString();
                                }
                                else if(row["Type"].ToString() == "INT")
                                {
                                    int parseTry;
                                    if (Int32.TryParse(SettingValueTextBox.Text, out parseTry))
                                    {
                                        row["Value"] = parseTry;
                                    }
                                    else
                                    {
                                        MessageBox.Show("Please enter a number for this setting!");
                                        return;
                                    }

                                }
                                else
                                {
                                    if (!string.IsNullOrEmpty(SettingValueTextBox.Text))
                                    {
                                        row["Value"] = SettingValueTextBox.Text;
                                    }
                                    else
                                    {
                                        MessageBox.Show("Please enter a value for this setting!");
                                        return;
                                    }
                                }
                                mainWindow.SettingsListView.Items.Add(row);
                            }
                        }
                    }
                }
                SettingValueTextBox.Text = "";
            }
        }

        private void SettingCombo_Changed(object sender, EventArgs e)
        {
            foreach (DataRowView row in _settingsAdapter.ServerSettings.DefaultView)
            {
                if (row["Name"].ToString() == SettingsComboBox.SelectionBoxItem.ToString())
                {
                    if (row["Type"].ToString() == "BOOL")
                    {
                        SettingValueComboBox.Visibility = Visibility.Visible;
                        SettingValueTextBox.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        SettingValueTextBox.Visibility = Visibility.Visible;
                        SettingValueComboBox.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }
    }
} 