/* 
 * LauncherShyax, un launcher pour World Of Warcraft avec téléchargement de mise à jour
  Copyright (C) 2011 Shyax — Tous droits réservés.
  
  Ce programme est un logiciel libre ; vous pouvez le redistribuer ou le
  modifier suivant les termes de la “GNU General Public License” telle que
  publiée par la Free Software Foundation : soit la version 3 de cette
  licence, soit (à votre gré) toute version ultérieure.
  
  Ce programme est distribué dans l’espoir qu’il vous sera utile, mais SANS
  AUCUNE GARANTIE : sans même la garantie implicite de COMMERCIALISABILITÉ
  ni d’ADÉQUATION À UN OBJECTIF PARTICULIER. Consultez la Licence Générale
  Publique GNU pour plus de détails.
  
  Vous devriez avoir reçu une copie de la Licence Générale Publique GNU avec
  ce programme ; si ce n’est pas le cas, consultez :
  <http://www.gnu.org/licenses/>.
 * */

using System;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using Microsoft.Win32;

namespace LauncherShyax
{
    public partial class Launcher : Form
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        #region Variables
        public bool isAllowedToDeleteCache
        {
            get => Configuration.Default.IsAllowedToDeleteCache;
            set => Configuration.Default.IsAllowedToDeleteCache = value;
        }

        public string hostAddress => Configuration.Default.HostAddress;
        public string siteAddress => Configuration.Default.SiteAddress;
        public string forumAddress => Configuration.Default.ForumAddress;
        public string bugTrackerAddress => Configuration.Default.BugTrackerAddress;
        public int realmPort => Configuration.Default.RealmServerPort;
        public int worldPort => Configuration.Default.WorldServerPort;

        public string _wowDir
        {
            get => Configuration.Default.WoWDirectory;
            set => Configuration.Default.WoWDirectory = value;
        }
        #endregion

        public Launcher()
        {
            InitializeComponent();
        }

        private async void LauncherBox_Load(object sender, EventArgs e)
        {
            FindWoWDir();
            await VerifyVersionAsync();
            await VerifyStatusAsync();
            ChangeRealmlist();
            AddLinks();
            checkBoxCache.Checked = Configuration.Default.IsAllowedToDeleteCache;
        }

        private void FindWoWDir()
        {
            const string wowExe = "Wow.exe";
            if (!string.IsNullOrWhiteSpace(_wowDir) && File.Exists(Path.Combine(_wowDir, wowExe)))
            {
                return;
            }

            var localPath = Path.GetDirectoryName(AppContext.BaseDirectory) ?? Environment.CurrentDirectory;
            if (File.Exists(Path.Combine(localPath, wowExe)))
            {
                _wowDir = localPath;
                return;
            }

            var installPath = ReadWoWInstallPathFromRegistry();
            if (!string.IsNullOrWhiteSpace(installPath) && File.Exists(Path.Combine(installPath, wowExe)))
            {
                _wowDir = installPath;
                return;
            }

            MessageBox.Show("World Of Warcraft non trouvé.");
        }

        private string? ReadWoWInstallPathFromRegistry()
        {
            const string wowRegistryPath = @"SOFTWARE\Blizzard Entertainment\World of Warcraft";
            using var baseKey = Registry.LocalMachine.OpenSubKey(wowRegistryPath);
            if (baseKey is not null)
            {
                return baseKey.GetValue("InstallPath")?.ToString();
            }

            if (!Environment.Is64BitOperatingSystem)
            {
                return null;
            }

            const string wow64RegistryPath = @"SOFTWARE\Wow6432Node\Blizzard Entertainment\World of Warcraft";
            using var wow64Key = Registry.LocalMachine.OpenSubKey(wow64RegistryPath);
            return wow64Key?.GetValue("InstallPath")?.ToString();
        }

        private async Task VerifyVersionAsync()
        {
            if (string.IsNullOrWhiteSpace(_wowDir))
            {
                return;
            }

            var versionUrl = new Uri($"http://{hostAddress}/version.xml");
            XDocument xmlDoc;
            try
            {
                var xmlContent = await HttpClient.GetStringAsync(versionUrl);
                xmlDoc = XDocument.Parse(xmlContent);
            }
            catch
            {
                MessageBox.Show("XML de MAJ non trouvé ou invalide !");
                return;
            }

            const string dataDir = "Data";
            var dataPath = Path.Combine(_wowDir, dataDir);
            var existingFiles = Directory.Exists(dataPath)
                ? Directory.GetFiles(dataPath, "patch-*.mpq", SearchOption.TopDirectoryOnly)
                : Array.Empty<string>();

            var versions = xmlDoc.Descendants("version")
                .Select(node => new VersionEntry(
                    node.Value.Trim(),
                    node.Attribute("nom")?.Value ?? node.Value.Trim(),
                    node.Attribute("desc")?.Value ?? string.Empty,
                    node.Attribute("lien")?.Value ?? string.Empty))
                .Where(entry => !string.IsNullOrWhiteSpace(entry.FileName))
                .ToList();

            if (!versions.Any())
            {
                return;
            }

            var missingEntry = versions
                .FirstOrDefault(entry => !existingFiles
                    .Select(Path.GetFileName)
                    .Contains(entry.FileName, StringComparer.OrdinalIgnoreCase));

            if (missingEntry is null)
            {
                return;
            }

            using var dlBox = new DownloaderBox();
            dlBox.Show(this);
            await dlBox.DownloadAsync(versions, existingFiles, dataPath);
        }

        private async Task VerifyStatusAsync()
        {
            if (string.IsNullOrWhiteSpace(hostAddress))
            {
                return;
            }

            IPAddress[] addresses;
            try
            {
                addresses = await Dns.GetHostAddressesAsync(hostAddress);
            }
            catch
            {
                SetServerStatus(false, pictureBoxRealm);
                SetServerStatus(false, pictureBoxWorld);
                return;
            }

            if (addresses.Length == 0)
            {
                SetServerStatus(false, pictureBoxRealm);
                SetServerStatus(false, pictureBoxWorld);
                return;
            }

            await CheckServerPortAsync(addresses[0], realmPort, pictureBoxRealm);
            await CheckServerPortAsync(addresses[0], worldPort, pictureBoxWorld);
        }

        private async Task CheckServerPortAsync(IPAddress address, int port, PictureBox pictureBox)
        {
            try
            {
                using var tcpClient = new TcpClient();
                var connectTask = tcpClient.ConnectAsync(address, port);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(3));
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                SetServerStatus(completedTask == connectTask && tcpClient.Connected, pictureBox);
            }
            catch
            {
                SetServerStatus(false, pictureBox);
            }
        }

        private static void SetServerStatus(bool isOnline, PictureBox pictureBox)
        {
            pictureBox.Image = isOnline
                ? LauncherShyax.Properties.Resources.up
                : LauncherShyax.Properties.Resources.down;
            pictureBox.Visible = true;
        }

        private void ChangeRealmlist()
        {
            const string dataDir = "Data";
            const string localeDir = "frFR";
            const string realmFile = "realmlist.wtf";

            var realmPath = Path.Combine(_wowDir, dataDir, localeDir, realmFile);
            var realmDirectory = Path.GetDirectoryName(realmPath);
            if (!string.IsNullOrEmpty(realmDirectory))
            {
                Directory.CreateDirectory(realmDirectory);
            }

            File.WriteAllText(realmPath, $"set realmlist {hostAddress}:{worldPort}", Encoding.UTF8);
        }

        private void AddLinks()
        {
            linkLabelSite.Links.Add(0, linkLabelSite.Text.Length, siteAddress);
            linkLabelForum.Links.Add(0, linkLabelForum.Text.Length, forumAddress);
            linkLabelBugTracker.Links.Add(0, linkLabelBugTracker.Text.Length, bugTrackerAddress);
        }

        private void DeleteCache()
        {
            const string cacheDir = "Cache";
            var cachePath = Path.Combine(_wowDir, cacheDir);
            if (Directory.Exists(cachePath))
            {
                Directory.Delete(cachePath, true);
            }
        }

        private void LaunchGame()
        {
            const string wowExe = "Wow.exe";
            var wowPath = Path.Combine(_wowDir, wowExe);
            if (!File.Exists(wowPath))
            {
                MessageBox.Show("Le jeu World of Warcraft est introuvable.");
                return;
            }

            var wowStartInfo = new ProcessStartInfo(wowPath)
            {
                UseShellExecute = true
            };
            Process.Start(wowStartInfo);
            Close();
        }

        #region Events
        private void pictureBoxLancer_Click(object sender, EventArgs e)
        {
            Configuration.Default.IsAllowedToDeleteCache = checkBoxCache.Checked;
            Configuration.Default.Save();
            if (checkBoxCache.Checked)
            {
                DeleteCache();
            }

            LaunchGame();
        }

        private void pictureBoxClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void pictureBoxHelp_Click(object sender, EventArgs e)
        {
            using var box = new HelpBox();
            box.ShowDialog();
        }

        private void linkLabelSite_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            linkLabelSite.LinkVisited = true;
            Process.Start(new ProcessStartInfo(siteAddress) { UseShellExecute = true });
        }

        private void linkLabelForum_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            linkLabelForum.LinkVisited = true;
            Process.Start(new ProcessStartInfo(forumAddress) { UseShellExecute = true });
        }

        private void linkLabelBugTracker_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            linkLabelBugTracker.LinkVisited = true;
            Process.Start(new ProcessStartInfo(bugTrackerAddress) { UseShellExecute = true });
        }
        #endregion
    }
}
