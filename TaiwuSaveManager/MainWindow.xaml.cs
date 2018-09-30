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
            setStatusText("Ready");
        }

        private void setStatusText(string status, int state = 0) {
            labelStatus.Content = status;
            SolidColorBrush brush = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            if (state == 1) {
                brush.Color = Color.FromRgb(255, 0, 0);
            } else if (state == 2) {
                brush.Color = Color.FromRgb(0, 255, 0);
            }
            labelStatus.Foreground = brush;
        }

        private void onSavefilePathChanged(object sender, TextChangedEventArgs e) {
            TextBox textbox = sender as TextBox;
            if (textbox != null) {
                Properties.Settings.Default.SavefilePath = textbox.Text;
                Properties.Settings.Default.Save();
            }
        }

        private void CheckSaveID(object sender, RoutedEventArgs e) {
            int id = Int32.Parse((sender as RadioButton).Tag.ToString());
            Properties.Settings.Default.SaveID = id;
            Properties.Settings.Default.Save();
        }

        private string SavefilePath {
            get {
                return Properties.Settings.Default.SavefilePath + $@"\Date_{Properties.Settings.Default.SaveID}";
            }
        }

        private string BackupPath {
            get {
                return System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + @"\TaiwuBackups";
            }
        }

        private void RefreshBackupList() {
            listBackups.Items.Clear();
            if (!Directory.Exists(BackupPath)) {
                return;
            }
            DirectoryInfo dir = new DirectoryInfo(BackupPath);
            FileInfo[] files = dir.GetFiles("backup-?-????-??-??-??-??.zip");
            foreach (FileInfo file in files) {
                ListViewItem item = new ListViewItem();
                item.Content = file.Name;
                item.Tag = file;
                listBackups.Items.Add(item);
            }
        }

        private bool filelock = false;

        private async void Backup(object sender, RoutedEventArgs e) {
            if (filelock) {
                return;
            }
            filelock = true;
            setStatusText("...");
            if (Directory.Exists(SavefilePath)) {
                string backupFile = BackupPath + $@"\backup-{Properties.Settings.Default.SaveID}-{DateTime.Now.ToString("yyyy-MM-dd-HH-mm")}.zip";
                Directory.CreateDirectory(BackupPath);
                if (File.Exists(backupFile)) {
                    File.Delete(backupFile);
                    await Task.Run(() => {
                        ZipFile.CreateFromDirectory(SavefilePath, backupFile);
                    });
                    setStatusText("Overwrite");
                } else {
                    await Task.Run(() => {
                        ZipFile.CreateFromDirectory(SavefilePath, backupFile);
                    });
                    setStatusText("OK", 2);
                }
            } else {
                setStatusText("X Save", 1);
            }
            filelock = false;
            RefreshBackupList();
        }

        private async void Restore(object sender, RoutedEventArgs e) {
            if (filelock) {
                return;
            }
            filelock = true;
            setStatusText("...");
            if (Directory.Exists(Properties.Settings.Default.SavefilePath)) {
                string backupFile = ((listBackups.SelectedItem as ListViewItem).Tag as FileInfo).FullName;
                if (File.Exists(backupFile)) {
                    if (Directory.Exists(SavefilePath)) {
                        Directory.Delete(SavefilePath, true);
                    }
                    await Task.Run(() => {
                        ZipFile.ExtractToDirectory(backupFile, SavefilePath);
                    });
                    setStatusText("OK", 2);
                } else {
                    setStatusText("X File", 1);
                }
            } else {
                setStatusText("X Path", 1);
            }
            filelock = false;
            RefreshBackupList();
        }
    }
}
