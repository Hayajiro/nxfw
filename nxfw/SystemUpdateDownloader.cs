using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using LibHac;
using LibHac.Common.Keys;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.FsSystem;
using LibHac.FsSystem.NcaUtils;
using LibHac.Util;
using nxfw.WebClient;

namespace nxfw
{
    public class SystemUpdateDownloader
    {
        private readonly KeySet _keySet;
        private readonly CdnWebClient _cdnWebClient;
        private readonly TitleVersion _systemVersion;
        private string _tmpPath;

        public SystemUpdateDownloader(KeySet keySet,
            TitleVersion systemVersion,
            string deviceId,
            string environmentId,
            X509Certificate2 certificate)
        {
            _keySet = keySet;
            _systemVersion = systemVersion;

            _cdnWebClient = new CdnWebClient(deviceId, environmentId,
                $"{_systemVersion.Major}.{_systemVersion.Minor}.{_systemVersion.Patch}-1.0",
                certificate);

            _tmpPath = Path.Join(Path.GetTempPath(), "nxfw");
        }

        public CdnVersionCheckResult CheckSystemUpdate()
        {
            CdnVersionCheckResult onlineVersion = _cdnWebClient.GetLatestSystemVersion();

            if (onlineVersion.SystemUpdateMetas[0].TitleVersion == _systemVersion.Version)
            {
                return null;
            }

            return onlineVersion;
        }
        
        public void DownloadSystemUpdate(string baseTitleId, TitleVersion version, string destinationDirectory)
        {
            if (Directory.Exists(_tmpPath))
            {
                Directory.Delete(_tmpPath, true);
            }
            Directory.CreateDirectory(_tmpPath);

            CdnDownloadResult firmwareResult = _cdnWebClient.DownloadMeta(baseTitleId, version.Version, 's');
            File.WriteAllBytes(Path.Combine(_tmpPath, $"{firmwareResult.NcaId}.cnmt.nca"), firmwareResult.Data);
            Cnmt firmwareCnmt = DecryptCnmt(firmwareResult.Data);

            DownloadFilesForCnmt(firmwareCnmt);
            
            Console.WriteLine("Creating zip archive...");
            string zipFile = CreateZipFile(version, destinationDirectory);
            Directory.Delete(_tmpPath, true);

            Console.WriteLine("Generating MD5 checksum...");
            File.WriteAllText($"{zipFile}.md5sum", $"{CalculateMD5(zipFile)}  {Path.GetFileName(zipFile)}");
        }

        private string CreateZipFile(TitleVersion version, string destinationDirectory)
        {
            string zipFile = Path.Combine(destinationDirectory,
                $"Firmware {version.Major}.{version.Minor}.{version.Patch}.zip");
            ZipFile.CreateFromDirectory(_tmpPath,
                zipFile,
                // Don't compress archive as the files are encrypted and, as such, don't compress well.
                CompressionLevel.NoCompression, false);

            return zipFile;
        }

        private void DownloadFilesForCnmt(Cnmt sourceCnmt)
        {
            foreach (CnmtContentMetaEntry metaEntry in sourceCnmt.MetaEntries)
            {
                try
                {
                    Console.WriteLine($"Downloading title {metaEntry.TitleId:x16}v{metaEntry.Version}");
                    CdnDownloadResult result =
                        _cdnWebClient.DownloadMeta(metaEntry.TitleId, metaEntry.Version.Version, 'a');
                    Cnmt decryptedCnmt = DecryptCnmt(result.Data);
                    File.WriteAllBytes(Path.Combine(_tmpPath, $"{result.NcaId}.cnmt.nca"), result.Data); 
                    DownloadFilesForCnmt(decryptedCnmt);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error downloading meta for title {metaEntry.TitleId:x16}! {e.Message}");
                    Environment.Exit(1);
                }
            }

            foreach (CnmtContentEntry contentEntry in sourceCnmt.ContentEntries)
            {
                try
                {
                    string ncaId = contentEntry.NcaId.ToHexString().ToLower();
                    CdnDownloadResult contentResult = _cdnWebClient.DownloadContent(contentEntry.NcaId);
                    File.WriteAllBytes(Path.Combine(_tmpPath, $"{ncaId}.nca"), contentResult.Data);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error downloading content {contentEntry.NcaId.ToHexString().ToLower()} for title {sourceCnmt.TitleId:x16}! {e.Message}");
                    Environment.Exit(1);
                }
            }
        }

        private Cnmt DecryptCnmt(byte[] data)
        {
            using (IStorage inFile = new StreamStorage(new MemoryStream(data), false))
            {
                var nca = new Nca(_keySet, inFile);

                PartitionFileSystem fileSystem = (PartitionFileSystem)nca.OpenFileSystem(NcaSectionType.Data, IntegrityCheckLevel.None);
                IFile file = fileSystem.OpenFile(fileSystem.Files[0], OpenMode.Read);

                return new Cnmt(file.AsStream());
            }
        }

        private string CalculateMD5(string filename)
        {
            using (MD5 md5 = MD5.Create())
            {
                using (FileStream stream = File.OpenRead(filename))
                {
                    return md5.ComputeHash(stream).ToHexString().ToLower();
                }
            }
        }
    }
}