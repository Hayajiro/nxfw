using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using LibHac.Common.Keys;
using LibHac.FsSystem.NcaUtils;
using nxfw.WebClient;

namespace nxfw
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 6)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("nxfw /path/to/prod.keys /path/to/cert.pfx /path/to/systemVersion /path/to/output/directory DeviceId EnvironmentId");
                Environment.Exit(1);
            }

            string prodkeys = args[0];
            string cert = args[1];
            string sysver = args[2];
            string outdir = args[3];
            string did = args[4];
            string eid = args[5];

            KeySet keySet = ExternalKeyReader.ReadKeyFile(prodkeys);
            X509Certificate2 certificate = new X509Certificate2(cert);
            TitleVersion currentVersion = new TitleVersion(uint.Parse(File.ReadAllText(sysver)), true);
            SystemUpdateDownloader systemUpdateDownloader = new SystemUpdateDownloader(keySet, currentVersion, did, eid, certificate);
            CdnVersionCheckResult latestVersion = systemUpdateDownloader.CheckSystemUpdate();

            if (latestVersion == null)
            {
                Console.WriteLine("No new system update found.");
                return;
            }

            Console.WriteLine($"Found new system update {latestVersion.SystemUpdateMetas[0].TitleVersion}");
            systemUpdateDownloader.DownloadSystemUpdate(latestVersion.SystemUpdateMetas[0].TitleId,
                new TitleVersion(latestVersion.SystemUpdateMetas[0].TitleVersion, true),
                outdir);
        }
    }
}