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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LauncherShyax
{
    public partial class DownloaderBox : Form
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        public DownloaderBox()
        {
            InitializeComponent();
        }

        private void DownloaderBox_Load(object sender, EventArgs e)
        {
        }

        public async Task DownloadAsync(IEnumerable<VersionEntry> versionEntries, IEnumerable<string> mpqFiles, string dataDir)
        {
            var missingEntries = versionEntries
                .Where(entry => !mpqFiles
                    .Select(Path.GetFileName)
                    .Contains(entry.FileName, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (!missingEntries.Any())
            {
                Close();
                return;
            }

            var result = MessageBox.Show("Une nouvelle mise à jour a été trouvée !\r\n Télécharger ?", "Avertissement", MessageBoxButtons.OKCancel);
            if (result == DialogResult.Cancel)
            {
                Close();
                return;
            }

            foreach (var entry in missingEntries)
            {
                labelDl.Text = "Téléchargement de la mise à jour : " + entry.Name;
                if (!string.IsNullOrWhiteSpace(entry.DescriptionUri))
                {
                    try
                    {
                        labelMaj.Text = await HttpClient.GetStringAsync(entry.DescriptionUri);
                    }
                    catch
                    {
                        labelMaj.Text = string.Empty;
                    }
                }

                var outputPath = Path.Combine(dataDir, entry.FileName);
                await DownloadFileWithProgressAsync(entry.DownloadUri, outputPath, new Progress<int>(value => progressBarDl.Value = value));
            }

            buttonClose.Visible = true;
        }

        private static async Task DownloadFileWithProgressAsync(string sourceUrl, string destinationPath, IProgress<int> progress)
        {
            using var response = await HttpClient.GetAsync(sourceUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            await using var contentStream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;
            while ((bytesRead = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                totalRead += bytesRead;
                if (totalBytes > 0)
                {
                    progress.Report((int)(totalRead * 100 / totalBytes));
                }
            }

            progress.Report(100);
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
