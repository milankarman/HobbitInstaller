using System;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using IWshRuntimeLibrary;
using File = System.IO.File;

namespace HobbitInstaller
{
    public partial class MainWindow : Window
    {
        private const string version = "1.0";
        private const string hobbitGamePatchedUrl = "https://hobbitspeedruns.com/HobbitGamePatched.zip";
        private const string dxWndUrl = "https://hobbitspeedruns.com/DxWnd.zip";

        private string hobbitInstallPath = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
        private string dxWndInstallPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private string hstInstallPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        //private string hobbitInstallPath = AppDomain.CurrentDomain.BaseDirectory;
        //private string dxWndInstallPath = AppDomain.CurrentDomain.BaseDirectory;
        //private string hstInstallPath = AppDomain.CurrentDomain.BaseDirectory;

        public MainWindow()
        {
            InitializeComponent();
            Title += $" {version}";
        }

        private async void btnInstall_Click(object sender, RoutedEventArgs e)
        {
            txtStatus.Text = "Status (1/6): Downloading The Hobbit";
            await DownloadHobbitGame();

            prbProgress.Value = 0;
            txtStatus.Text = "Status (2/6): Installing The Hobbit";
            await Task.Run(() => InstallHobbitGame());

            txtStatus.Text = "Status (3/6): Downloading DxWnd";
            await DownloadDxWnd();

            prbProgress.Value = 0;
            txtStatus.Text = "Status (4/6): Installing DxWnd";
            await Task.Run(() => InstallDxWnd());

            txtStatus.Text = "Status (5/6): Downloading HobbitSpeedrunTools";
            await DownloadHobbitSpeedrunTools();
            prbProgress.Value = 0;

            txtStatus.Text = "Status (6/6): Installing HobbitSpeedrunTools";
            await Task.Run(() => InstallHobbitSpeedrunTools());

            MessageBox.Show("Done");
        }

        private async Task DownloadHobbitGame()
        {
            using (var client = new HttpClientDownloadWithProgress(hobbitGamePatchedUrl, "HobbitGamePatched.zip"))
            {
                client.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) =>
                {
                    prbProgress.Value = progressPercentage ?? 0;
                };

                await client.StartDownload();
            }
        }

        private void InstallHobbitGame()
        {
            if (Directory.Exists(Path.Join(hobbitInstallPath, "Sierra")))
            {
                Directory.Delete(Path.Join(hobbitInstallPath, "Sierra"), true);
            }

            ZipFile.ExtractToDirectory("HobbitGamePatched.zip", hobbitInstallPath);
        }

        private async Task DownloadDxWnd()
        {
            using (var client = new HttpClientDownloadWithProgress(dxWndUrl, "DxWnd.zip"))
            {
                client.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) =>
                {
                    prbProgress.Value = progressPercentage ?? 0;
                };

                await client.StartDownload();
            }
        }

        private void InstallDxWnd()
        {
            if (Directory.Exists(Path.Join(dxWndInstallPath, "DxWnd")))
            {
                Directory.Delete(Path.Join(dxWndInstallPath, "DxWnd"), true);
            }

            ZipFile.ExtractToDirectory("DxWnd.zip", dxWndInstallPath);
            File.Delete(Path.Join(dxWndInstallPath, "dxwnd.ini"));
            File.Copy(Path.Join("resources", "dxwnd.ini"), Path.Join(dxWndInstallPath, "DxWnd", "dxwnd.ini"));

            string shortcutPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "The Hobbit - DxWnd.lnk");
            string targetDir = Path.Join(dxWndInstallPath, "DxWnd");
            string targetFile = "DxWnd.exe";

            CreateShortcut(shortcutPath, targetDir, targetFile);
        }

        private async Task DownloadHobbitSpeedrunTools()
        {
            HttpClient apiClient = new HttpClient();

            apiClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("HobbitInstaller", version));
            apiClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("(+https://github.com/milankarman/HobbitInstaller)"));

            HttpResponseMessage response = await apiClient.GetAsync("https://api.github.com/repos/milankarman/hobbitspeedruntools/releases/latest");
            response.EnsureSuccessStatusCode();

            JObject json = JObject.Parse(await response.Content.ReadAsStringAsync());
            string downloadUrl = (string)json.SelectToken("assets[0].browser_download_url");

            using (var client = new HttpClientDownloadWithProgress(downloadUrl, "HobbitSpeedrunTools.zip"))
            {
                client.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) =>
                {
                    prbProgress.Value = progressPercentage ?? 0;
                };

                await client.StartDownload();
            }
        }

        private void InstallHobbitSpeedrunTools()
        {
            if (Directory.Exists(Path.Join(hstInstallPath, "HobbitSpeedrunTools")))
            {
                Directory.Delete(Path.Join(hstInstallPath, "HobbitSpeedrunTools"), true);
            }

            ZipFile.ExtractToDirectory("HobbitSpeedrunTools.zip", hstInstallPath);

            string shortcutPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "HobbitSpeedrunTools.lnk");
            string targetDir = Path.Join(hstInstallPath, "HobbitSpeedrunTools");
            string targetFile = "HobbitSpeedrunTools.exe";

            CreateShortcut(shortcutPath, targetDir, targetFile);
        }

        private void CreateShortcut(string shortcutPath, string targetDir, string targetFile)
        {
            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = Path.Join(targetDir, targetFile);
            shortcut.WorkingDirectory = targetDir;

            shortcut.Save();
        }
    }
}
