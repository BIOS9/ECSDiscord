using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;
using System;
using System.IO;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace UserDataViewer
{
    public class CertificateService
    {
        public X509Certificate2 Certificate { get; set; }

        public CertificateService() { }

        public CertificateService(X509Certificate2 certificate)
        {
            Certificate = certificate;
        }

        public void LoadCertificateFromFile(string path, SecureString password)
        {
            X509Certificate2 cert = new X509Certificate2(path, password, X509KeyStorageFlags.Exportable);
            if(!cert.HasPrivateKey)
            {
                throw new Exception("The specified certificate bundle does not contain a private key!");
            }
            Certificate = cert;
        }

        public void LoadCertificateFromFile(string path, string password)
        {
            Certificate = new X509Certificate2(path, password, X509KeyStorageFlags.Exportable);
        }

        public void SavePublicKeyToPemFile(string path, bool overwrite = false)
        {
            if (Certificate == null)
                throw new InvalidOperationException("Certificate must be set before it can be exported.");

            using (FileStream fs = new FileStream(path, (overwrite ? FileMode.Create : FileMode.CreateNew), FileAccess.Write, FileShare.None))
            using (StreamWriter sw = new StreamWriter(fs))
            {
                sw.Write(ExportPublicKeyToPem());
            }
        }

        public void SaveToPkcs12File(string path, SecureString password, bool overwrite = false)
        {
            if (Certificate == null)
                throw new InvalidOperationException("Certificate must be set before it can be exported.");

            using (FileStream fs = new FileStream(path, (overwrite ? FileMode.Create : FileMode.CreateNew), FileAccess.Write, FileShare.None))
                fs.Write(Certificate.Export(X509ContentType.Pkcs12, password));
        }

        public void SaveToPkcs12File(string path, string password, bool overwrite = false)
        {
            if (Certificate == null)
                throw new InvalidOperationException("Certificate must be set before it can be exported.");

            using (FileStream fs = new FileStream(path, (overwrite ? FileMode.Create : FileMode.CreateNew), FileAccess.Write, FileShare.None))
                fs.Write(Certificate.Export(X509ContentType.Pkcs12, password));
        }

        public string ExportPublicKeyToPem()
        {
            if (Certificate == null)
                throw new InvalidOperationException("Certificate must be set before it can be exported.");

            StringBuilder builder = new StringBuilder();

            builder.AppendLine("-----BEGIN CERTIFICATE-----");
            builder.AppendLine(Convert.ToBase64String(Certificate.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks));
            builder.AppendLine("-----END CERTIFICATE-----");

            return builder.ToString();
        }

        public void GenerateCertificate()
        {
            string organisation = "ECSDiscordServer";
            string commonName = "ECSDiscordServer";

            var random = new SecureRandom();
            var certificateGenerator = new X509V3CertificateGenerator();

            var serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(int.MaxValue), random);
            certificateGenerator.SetSerialNumber(serialNumber);

            certificateGenerator.SetIssuerDN(new X509Name($"O={organisation}, CN={commonName}"));
            certificateGenerator.SetSubjectDN(new X509Name($"O={organisation}, CN={commonName}"));
            certificateGenerator.SetNotBefore(DateTime.UtcNow.Date);
            certificateGenerator.SetNotAfter(DateTime.UtcNow.Date.AddYears(10));

            const int strength = 2048;
            var keyGenerationParameters = new KeyGenerationParameters(random, strength);
            var keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(keyGenerationParameters);

            var subjectKeyPair = keyPairGenerator.GenerateKeyPair();
            certificateGenerator.SetPublicKey(subjectKeyPair.Public);

            var issuerKeyPair = subjectKeyPair;
            const string signatureAlgorithm = "SHA256WithRSA";
            var signatureFactory = new Asn1SignatureFactory(signatureAlgorithm, issuerKeyPair.Private);
            var bouncyCert = certificateGenerator.Generate(signatureFactory);

            // Lets convert it to X509Certificate2
            X509Certificate2 certificate;

            Pkcs12Store store = new Pkcs12StoreBuilder().Build();
            store.SetKeyEntry($"{commonName}_key", new AsymmetricKeyEntry(subjectKeyPair.Private), new[] { new X509CertificateEntry(bouncyCert) });
            string exportpw = Guid.NewGuid().ToString("x");

            using (var ms = new MemoryStream())
            {
                store.Save(ms, exportpw.ToCharArray(), random);
                certificate = new X509Certificate2(ms.ToArray(), exportpw, X509KeyStorageFlags.Exportable);
            }

            Certificate = certificate;
        }
    }
}
