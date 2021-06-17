using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace nxfw.WebClient
{
    public class CertificateWebClient : System.Net.WebClient
    {
        private readonly X509Certificate2 certificate;

        public CertificateWebClient(X509Certificate2 cert)
        {
            certificate = cert;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest request = (HttpWebRequest)base.GetWebRequest(address);

            ServicePointManager.ServerCertificateValidationCallback = delegate
            {
                return true;
            };

            request.ClientCertificates.Add(certificate);
            return request;
        }
    }
}