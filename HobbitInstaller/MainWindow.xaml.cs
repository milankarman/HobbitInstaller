﻿using System;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using IWshRuntimeLibrary;
using IniParser;
using IniParser.Model;
using File = System.IO.File;

namespace HobbitInstaller
{
    public partial class MainWindow : Window
    {
        private const string version = "1.0";

        // Download URLs
        private const string hobbitGamePatchedUrl = "https://hobbitspeedruns.com/HobbitGamePatched.zip";
        private const string dxWndUrl = "https://hobbitspeedruns.com/DxWnd.zip";
        private const string hstReleasesUrl = " https://api.github.com/repos/milankarman/hobbitspeedruntools/releases/latest";
       
        // Paths to installation locations
        private string hobbitInstallPath = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
        private string dxWndInstallPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private string hstInstallPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        public MainWindow()
        {
            InitializeComponent();
            Title += $" {version}";
        }

        private async void btnInstall_Click(object sender, RoutedEventArgs e)
        {
            btnInstall.IsEnabled = false;

            // Start every step of the installation process and show progress
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
            prbProgress.Value = 1;

            txtIntro.Text = "Status: Done";
            MessageBox.Show("Done");
        }

        // Downloads the patched hobbit game files to the current directory
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
            using (var client = new HttpClientDownloadWithProgress(dxWndUrl, "DxWnd.zip"))
            {
                client.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) =>
                {
                    prbProgress.Value = progressPercentage ?? 0;
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

            parser.WriteFile(Path.Join("resources", "dxwnd_custom.ini"), data);

            // Move the new config to the dxwnd folder and remove temporary files
            File.Move(Path.Join("resources", "dxwnd_custom.ini"), Path.Join(dxWndInstallPath, "DxWnd", "dxwnd.ini"));

            if (File.Exists(Path.Join("resources", "dxwnd_custom.ini")))
            {
                File.Delete(Path.Join("resources", "dxwnd_custom.ini"));
            }

            // Create the DxWnd shortcut
            string shortcutPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "The Hobbit - DxWnd.lnk");
            string targetDir = Path.Join(dxWndInstallPath, "DxWnd");
            string targetFile = "DxWnd.exe";

            CreateShortcut(shortcutPath, targetDir, targetFile);
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
            using (var client = new HttpClientDownloadWithProgress(downloadUrl, "HobbitSpeedrunTools.zip"))
            {
                client.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) =>
                {
                    prbProgress.Value = progressPercentage ?? 0;
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
            string shortcutPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "HobbitSpeedrunTools.lnk");
            string targetDir = Path.Join(hstInstallPath, "HobbitSpeedrunTools");
            string targetFile = "HobbitSpeedrunTools.exe";

            CreateShortcut(shortcutPath, targetDir, targetFile);
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
    }
}
