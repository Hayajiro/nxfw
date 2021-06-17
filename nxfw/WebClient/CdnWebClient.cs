using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace nxfw.WebClient
{
    public class CdnWebClient
    {
        private readonly System.Net.WebClient _webClient;
        private readonly string _deviceId;
        private readonly string _environmentId;

        public CdnWebClient(string did, string eid, string firmwareVersion, X509Certificate2 certificate)
        {
            _deviceId = did;
            _environmentId = eid;
            
            _webClient = new CertificateWebClient(certificate);

            _webClient.Headers.Clear();
            _webClient.Headers.Add(HttpRequestHeader.UserAgent,
                $"NintendoSDK Firmware/{firmwareVersion} (platform:NX; did:{did}; eid:{eid})");
        }

        public CdnVersionCheckResult GetLatestSystemVersion()
        {
            return CdnVersionCheckResult.FromJson(_webClient.DownloadString(
                $"https://sun.hac.{_environmentId}.d4c.nintendo.net/v1/system_update_meta?device_id={_deviceId}"));
        }

        public CdnDownloadResult DownloadMeta(ulong titleId, uint version, char type = 'c', bool isSystemTitle = true)
        {
            string titleIdStr = titleId.ToString("x16");
            return DownloadMeta(titleIdStr, version, type, isSystemTitle);
        }

        public CdnDownloadResult DownloadMeta(string titleId, uint version, char type = 'c', bool isSystemTitle = true)
        {
            byte[] data = _webClient.DownloadData(
                    $"https://atum{(isSystemTitle ? "n" : "")}.hac.{_environmentId}.d4c.nintendo.net/t/{type}/{titleId}/{version}?device_id={_deviceId}");
            return new CdnDownloadResult
            {
                ContentHash = _webClient.ResponseHeaders?.Get("X-Nintendo-Content-Hash"),
                NcaId = _webClient.ResponseHeaders?.Get("X-Nintendo-Content-ID"),
                Data = data
            };
        }

        public CdnDownloadResult DownloadContent(byte[] ncaId, char type = 'c', bool isSystemTitle = true)
        {
            return DownloadContent(ByteArrayToString(ncaId), type, isSystemTitle);
        }

        public CdnDownloadResult DownloadContent(string ncaId, char type = 'c', bool isSystemTitle = true)
        {
            byte[] data = _webClient.DownloadData(
                $"https://atum{(isSystemTitle ? "n" : "")}.hac.{_environmentId}.d4c.nintendo.net/c/{type}/{ncaId}");

            return new CdnDownloadResult
            {
                ContentHash = _webClient.ResponseHeaders?.Get("X-Nintendo-Content-Hash"),
                NcaId = _webClient.ResponseHeaders?.Get("X-Nintendo-Content-ID"),
                Data = data
            };
        }

        private string ByteArrayToString(byte[] ba)
        { 
            return BitConverter.ToString(ba).Replace("-","").ToLower();
        }
    }
}