using IniParser;
using IniParser.Model;
using IWshRuntimeLibrary;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using File = System.IO.File;

namespace HobbitInstaller
{
    public partial class MainWindow : Window
    {
        // Download URLs
        private const string hobbitGamePatchedUrl = "https://hobbitspeedruns.com/HobbitGamePatched.zip";
        private const string dxWndUrl = "https://hobbitspeedruns.com/DxWnd.zip";
        private const string hstReleasesUrl = "https://api.github.com/repos/milankarman/hobbitspeedruntools/releases/latest";

        private const string installWarningMessage = "It's possible your system doesn't allow the installer to write to the chosen folder," +
                    $"if you think this might be the case then try installing everything to the desktop instead. \n";

        private readonly string version = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;

        private string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        private string documentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private string startupMenuPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");

        // Paths to installation locations
        private string defaultHobbitInstallPath = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
        private string defaultDxWndInstallPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private string defaultHstInstallPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        private string hobbitInstallPath = "";
        private string dxWndInstallPath = "";
        private string hstInstallPath = "";

        private bool desktopShortcuts = true;
        private bool startMenuShortcuts = true;

        public MainWindow()
        {
            InitializeComponent();

            // Set intial window properties
            grpOptions.Visibility = Visibility.Hidden;
            Height = 270;
            MinHeight = 270;
            Title += $" {version}";

            txtHobbitFolder.Text = defaultHobbitInstallPath;
            txtDxWndFolder.Text = defaultDxWndInstallPath;
            txtHSTFolder.Text = defaultHstInstallPath;
        }

        private async void btnInstall_Click(object sender, RoutedEventArgs e)
        {
            // Set the installation paths and check if they are all available
            hobbitInstallPath = txtHobbitFolder.Text;
            dxWndInstallPath = txtDxWndFolder.Text;
            hstInstallPath = txtHSTFolder.Text;

            if (!Directory.Exists(hobbitInstallPath))
            {
                System.Windows.MessageBox.Show("Couldn't find hobbit installation location at: " + hobbitInstallPath);
                return;
            }

            if (!Directory.Exists(dxWndInstallPath))
            {
                System.Windows.MessageBox.Show("Couldn't find DxWnd installation location at: " + hobbitInstallPath);
                return;
            }

            if (!Directory.Exists(hstInstallPath))
            {
                System.Windows.MessageBox.Show("Couldn't find HST installation location at: " + hobbitInstallPath);
                return;
            }

            desktopShortcuts = cbxDesktopShortcuts.IsChecked ?? true;
            startMenuShortcuts = cbxStartmenuShortcuts.IsChecked ?? true;

            btnInstall.IsEnabled = false;
            grpOptions.IsEnabled = false;

            // Start every step of the installation process and show progress
            txtStatus.Text = "Status (1/6): Downloading The Hobbit";
            try
            {
                await DownloadHobbitGame();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to download The Hobbit. Error: \n{ex.Message}");
                throw;
            }

            prbProgress.Value = 0;
            txtStatus.Text = "Status (2/6): Installing The Hobbit";

            await Task.Run(() =>
            {
                try
                {
                    InstallHobbitGame();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to install The Hobbit.\n{installWarningMessage} Error: \n{ex.Message}");
                    throw;
                }
            });

            txtStatus.Text = "Status (3/6): Downloading DxWnd";

            try
            {
                await DownloadDxWnd();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to download DxWnd. Error: \n{ex.Message}");
                throw;
            }

            prbProgress.Value = 0;
            txtStatus.Text = "Status (4/6): Installing DxWnd";

            await Task.Run(() =>
            {
                try
                {
                    InstallDxWnd();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to install DxWnd.\n{installWarningMessage}\nError: \n{ex.Message}");
                    throw;
                }
            });

            txtStatus.Text = "Status (5/6): Downloading HobbitSpeedrunTools";

            try
            {
                await DownloadHobbitSpeedrunTools();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to download HobbitSpeedrunTools. Error: \n{ex.Message}");
                throw;
            }

            prbProgress.Value = 0;
            txtStatus.Text = "Status (6/6): Installing HobbitSpeedrunTools";

            await Task.Run(() =>
            {
                try
                {
                    InstallHobbitSpeedrunTools();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to install HobbitSpeedrunTools.\n{installWarningMessage}\nError: \n{ex.Message}");
                    throw;
                }
            });

            prbProgress.Value = 100;
            txtIntro.Text = "Status: Done";

            Success successWindow = new();
            successWindow.Show();
        }

        // Downloads the patched hobbit game files to the current directory
        private async Task DownloadHobbitGame()
        {
            using (ProgressDownloader client = new ProgressDownloader(hobbitGamePatchedUrl, "HobbitGamePatched.zip"))
            {
                client.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) =>
                {
                    prbProgress.Value = progressPercentage ?? 0;
                    txtStatus.Text = $"Status (1/6): Downloading The Hobbit - {totalBytesDownloaded / 1000000}mb / {totalFileSize / 1000000}mb";
                };

                await client.StartDownload();
            }
        }

        // Unzips the patched hobbit game files in the installation location and creates a desktop shortcut
        private void InstallHobbitGame()
        {
            // Removes The Hobbit folder if it already exists and unzips the downloaded version there
            if (Directory.Exists(Path.Join(hobbitInstallPath, "Sierra")))
            {
                Directory.Delete(Path.Join(hobbitInstallPath, "Sierra"), true);
            }

            ZipFile.ExtractToDirectory("HobbitGamePatched.zip", hobbitInstallPath);
        }

        // Downloads DxWnd files to the current directory
        private async Task DownloadDxWnd()
        {
            using (ProgressDownloader client = new ProgressDownloader(dxWndUrl, "DxWnd.zip"))
            {
                client.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) =>
                {
                    prbProgress.Value = progressPercentage ?? 0;
                    txtStatus.Text = $"Status (3/6): Downloading DxWnd - {totalBytesDownloaded / 1000000}mb / {totalFileSize / 1000000}mb";
                };

                await client.StartDownload();
            }
        }

        // Unzips DxWnd in the installation location, updates the config and creates a desktop shortcut
        private void InstallDxWnd()
        {
            // Remove DxWnd folder if it already exists then unzip there
            if (Directory.Exists(Path.Join(dxWndInstallPath, "DxWnd")))
            {
                Directory.Delete(Path.Join(dxWndInstallPath, "DxWnd"), true);
            }

            ZipFile.ExtractToDirectory("DxWnd.zip", dxWndInstallPath);

            // Load the ini file template
            FileIniDataParser parser = new();
            IniData data = parser.ReadFile(Path.Join("resources", "dxwnd.ini"));

            // Convert the current screen resolution to a 4:3 aspect ratio
            int resY = (int)Math.Round(SystemParameters.PrimaryScreenHeight);
            int resX = (int)Math.Round(SystemParameters.PrimaryScreenWidth / 4 * 3);

            // Write the new 4:3 resolution to the DxWnd config
            data["target"]["initresw0"] = resX.ToString();
            data["target"]["sizx0"] = resX.ToString();
            data["target"]["initresh0"] = resY.ToString();
            data["target"]["sizy0"] = resY.ToString();

            data["target"]["path0"] = Path.Join(hobbitInstallPath, "Sierra", "The Hobbit(TM)", "Meridian.exe");
            data["target"]["path1"] = Path.Join(hobbitInstallPath, "Sierra", "The Hobbit(TM)", "Meridian.exe");

            parser.WriteFile(Path.Join("resources", "dxwnd_custom.ini"), data);

            // Move the new config to the dxwnd folder and remove temporary files
            File.Move(Path.Join("resources", "dxwnd_custom.ini"), Path.Join(dxWndInstallPath, "DxWnd", "dxwnd.ini"));

            if (File.Exists(Path.Join("resources", "dxwnd_custom.ini")))
            {
                File.Delete(Path.Join("resources", "dxwnd_custom.ini"));
            }

            // Create the DxWnd shortcut
            string desktopShortcutPath = Path.Join(desktopPath, "The Hobbit - DxWnd.lnk");
            string startMenuShortcutPath = Path.Join(startupMenuPath, "The Hobbit - DxWnd.lnk");
            string targetDir = Path.Join(dxWndInstallPath, "DxWnd");
            string targetFile = "DxWnd.exe";

            if (desktopShortcuts) CreateShortcut(desktopShortcutPath, targetDir, targetFile);
            if (startMenuShortcuts) CreateShortcut(startMenuShortcutPath, targetDir, targetFile);
        }

        // Queries GitHub API for the latest HST version and downloads it
        private async Task DownloadHobbitSpeedrunTools()
        {
            HttpClient apiClient = new HttpClient();

            // Add user agent headers so we can call the GitHub API
            apiClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("HobbitInstaller", version));
            apiClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("(+https://github.com/milankarman/HobbitInstaller)"));

            HttpResponseMessage response = await apiClient.GetAsync(hstReleasesUrl);
            response.EnsureSuccessStatusCode();

            // Read the downloadUrl for the latest release from the GitHub API
            JObject json = JObject.Parse(await response.Content.ReadAsStringAsync());
            string downloadUrl = (string)json.SelectToken("assets[0].browser_download_url");

            // Download the latest version of HST
            using (ProgressDownloader client = new ProgressDownloader(downloadUrl, "HobbitSpeedrunTools.zip"))
            {
                client.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) =>
                {
                    prbProgress.Value = progressPercentage ?? 0;
                    txtStatus.Text = $"Status (5/6): Downloading HobbitSpeedrunTools - {totalBytesDownloaded / 1000000}mb / {totalFileSize / 1000000}mb";
                };

                await client.StartDownload();
            }
        }

        // Installs HST in the right location
        private void InstallHobbitSpeedrunTools()
        {
            // Removes HST folder if it already exists and unzips the downloaded version there
            if (Directory.Exists(Path.Join(hstInstallPath, "HobbitSpeedrunTools")))
            {
                Directory.Delete(Path.Join(hstInstallPath, "HobbitSpeedrunTools"), true);
            }

            ZipFile.ExtractToDirectory("HobbitSpeedrunTools.zip", hstInstallPath);

            // Create shortcut
            string desktopShortcutPath = Path.Join(desktopPath, "HobbitSpeedrunTools.lnk");
            string startMenuShortcutPath = Path.Join(startupMenuPath, "HobbitSpeedrunTools.lnk");
            string targetDir = Path.Join(hstInstallPath, "HobbitSpeedrunTools");
            string targetFile = "HobbitSpeedrunTools.exe";

            if (desktopShortcuts) CreateShortcut(desktopShortcutPath, targetDir, targetFile);
            if (startMenuShortcuts) CreateShortcut(startMenuShortcutPath, targetDir, targetFile);
        }

        // Method that creates a shortcut to a given path
        private void CreateShortcut(string shortcutPath, string targetDir, string targetFile)
        {
            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = Path.Join(targetDir, targetFile);
            shortcut.WorkingDirectory = targetDir;

            shortcut.Save();
        }

        private void cbxOptions_Checked(object sender, RoutedEventArgs e)
        {
            grpOptions.Visibility = Visibility.Visible;
            MinHeight = 583;
            Height = 583;
        }

        private void cbxOptions_Unchecked(object sender, RoutedEventArgs e)
        {
            grpOptions.Visibility = Visibility.Hidden;
            MinHeight = 270;
            Height = 270;
        }

        private void btnSelectHobbitFolder_Click(object sender, RoutedEventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    txtHobbitFolder.Text = fbd.SelectedPath;
                }
            }
        }

        private void btnSelectDxWndFolder_Click(object sender, RoutedEventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    txtDxWndFolder.Text = fbd.SelectedPath;
                }
            }
        }

        private void btnSelectHSTFolder_Click(object sender, RoutedEventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    txtHSTFolder.Text = fbd.SelectedPath;
                }
            }
        }
    }
}
