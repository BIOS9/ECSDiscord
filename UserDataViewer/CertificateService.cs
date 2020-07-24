using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace UserDataViewer
{
    public class CertificateService
    {
        public X509Certificate2 Certificate { get; private set; }

        public CertificateService() { }

        public CertificateService(X509Certificate2 certificate)
        {
            Certificate = certificate;
        }

        public void LoadCertificateFromFile(string path, SecureString password)
        {
            Certificate = new X509Certificate2(path, password);
        }

        public void LoadCertificateFromFile(string path, string password)
        {
            Certificate = new X509Certificate2(path, password);
        }

        public void SavePublicKeyToPemFile(string path, bool overwrite = false)
        {
            using (FileStream fs = new FileStream(path, (overwrite ? FileMode.Create : FileMode.CreateNew), FileAccess.Write, FileShare.None))
            using (StreamWriter sw = new StreamWriter(fs))
            {
                sw.Write(ExportPublicKeyToPem());
            }
        }

        public void SavePrivateKeyToPkcs12File(string path, SecureString password)
        {
            Certificate.Export(X509ContentType.Pkcs12, password);
        }

        public void SavePrivateKeyToPkcs12File(string path, string password)
        {
            Certificate.Export(X509ContentType.Pkcs12, password);
        }

        public string ExportPublicKeyToPem()
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("-----BEGIN CERTIFICATE-----");
            builder.AppendLine(Convert.ToBase64String(Certificate.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks));
            builder.AppendLine("-----END CERTIFICATE-----");

            return builder.ToString();
        }

        public void GenerateCertificate()
        {

        }
    }
}
