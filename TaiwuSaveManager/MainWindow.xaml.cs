using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.IO.Compression;

namespace TaiwuSaveManager {
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
            // set radio button
            string tag = Properties.Settings.Default.SaveID.ToString();
            panelSaveSelection.Children.OfType<RadioButton>().ToList().ForEach(r => {
                if (r.Tag.ToString() == tag) {
                    r.IsChecked = true;
                }
            });
            // load datagrid for backups
            RefreshBackupList();
            SetStatusText(Properties.Resources.Ready);
        }

        private void SetStatusText(string status, int state = 0) {
            SolidColorBrush brush = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            if (state == 1) {
                brush.Color = Color.FromRgb(255, 0, 0);
                status = $"{Properties.Resources.Error}: {status}";
            } else if (state == 2) {
                brush.Color = Color.FromRgb(0, 0, 255);
                status = $"{Properties.Resources.Success}: {status}";
            }
            tbStatus.Text = status;
            tbStatus.Foreground = brush;
        }

        private void OnSavefilePathChanged(object sender, TextChangedEventArgs e) {
            if (sender is TextBox textbox) {
                Properties.Settings.Default.GameDir = textbox.Text;
                Properties.Settings.Default.Save();
            }
        }

        private void CheckSaveID(object sender, RoutedEventArgs e) {
            int id = Int32.Parse((sender as RadioButton).Tag.ToString());
            Properties.Settings.Default.SaveID = id;
            Properties.Settings.Default.Save();
        }

        private string SavefilesPath {
            get {
                return Properties.Settings.Default.GameDir + $@"\The Scroll Of Taiwu Alpha V1.0_Data\SaveFiles";
            }
        }

        private string SavefilePath {
            get {
                return SavefilesPath + $@"\Date_{Properties.Settings.Default.SaveID}";
            }
        }

        private string BackupPath {
            get {
                return System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + @"\TaiwuBackups";
            }
        }

        private void RefreshBackupList() {
            if (!Directory.Exists(BackupPath)) {
                return;
            }
            DirectoryInfo dir = new DirectoryInfo(BackupPath);
            FileInfo[] files = dir.GetFiles("backup-?-????-??-??-??-??.zip");
            listBackups.Items.Clear();
            int index = 0;
            foreach (FileInfo file in files) {
                ++index;
                listBackups.Items.Add(new ListViewItem {
                    Content = $"{Properties.Resources.Seperator}{index.ToString("D3")} {Properties.Resources.SaveSlot}{file.Name[7]} {file.Name.Substring(9, 10)} {file.Name.Substring(20, 5).Replace('-', ':')}",
                    Tag = file
                });
            }
        }

        private void DisableButtons() {
            foreach (Button btn in gridButtons.Children) {
                btn.IsEnabled = false;
            }
        }

        private void EnableButtons() {
            foreach (Button btn in gridButtons.Children) {
                btn.IsEnabled = true;
            }
        }

        private async void Backup(object sender, RoutedEventArgs e) {
            DisableButtons();
            SetStatusText(Properties.Resources.Processing);
            if (Directory.Exists(SavefilesPath)) {
                if (Directory.Exists(SavefilePath) && Directory.EnumerateFileSystemEntries(SavefilePath).Any()) {
                    string backupFile = BackupPath + $@"\backup-{Properties.Settings.Default.SaveID}-{DateTime.Now.ToString("yyyy-MM-dd-HH-mm")}.zip";
                    Directory.CreateDirectory(BackupPath);
                    if (File.Exists(backupFile)) {
                        File.Delete(backupFile);
                        await Task.Run(() => {
                            ZipFile.CreateFromDirectory(SavefilePath, backupFile);
                        });
                        SetStatusText(Properties.Resources.OverwirteBackups);
                    } else {
                        await Task.Run(() => {
                            ZipFile.CreateFromDirectory(SavefilePath, backupFile);
                        });
                        SetStatusText(Properties.Resources.Finished, 2);
                    }
                } else {
                    SetStatusText(Properties.Resources.NoSavefile, 1);
                }
            } else {
                SetStatusText(Properties.Resources.InvalidGameDir, 1);
            }
            RefreshBackupList();
            EnableButtons();
        }

        private async void Restore(object sender, RoutedEventArgs e) {
            DisableButtons();
            SetStatusText(Properties.Resources.Processing);
            if (Directory.Exists(SavefilesPath)) {
                string backupFile = "";
                if (listBackups.SelectedItems.Count != 0) {
                    backupFile = ((listBackups.SelectedItem as ListViewItem).Tag as FileInfo).FullName;
                }
                if (listBackups.SelectedItems.Count != 0 && File.Exists(backupFile)) {
                    if (Directory.Exists(SavefilePath)) {
                        Directory.Delete(SavefilePath, true);
                    }
                    await Task.Run(() => {
                        ZipFile.ExtractToDirectory(backupFile, SavefilePath);
                    });
                    SetStatusText(Properties.Resources.Finished, 2);
                } else {
                    SetStatusText(Properties.Resources.InvalidBackup, 1);
                }
            } else {
                SetStatusText(Properties.Resources.InvalidGameDir, 1);
            }
            RefreshBackupList();
            EnableButtons();
        }
    }
}
