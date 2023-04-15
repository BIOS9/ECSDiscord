using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace UserDataViewer
{
    public class CertificateSharingService
    {
        private RNGCryptoServiceProvider _rng = new RNGCryptoServiceProvider();
        private ECDiffieHellman _ec = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        private byte[] _sharedKey;
        private bool _used = false;

        public string PublicString { get; private set; }

        public CertificateSharingService()
        {
            string text = Convert.ToBase64String(_ec.PublicKey.ToByteArray());

            const int chunkSize = 64;
            string wrappedText = Enumerable.Range(0, text.Length / chunkSize)
                .Select(i => text.Substring(i * chunkSize, chunkSize)).Aggregate((a, b) => $"{a}\n{b}");
            wrappedText += "\n" + text.Substring((text.Length / chunkSize) * chunkSize);
            PublicString = $"-----BEGIN PUBLIC KEY-----\n{wrappedText}\n-----END PUBLIC KEY-----";
        }

        public void GenerateKey(string partnerPublicString)
        {
            byte[] partnerPublicKey = Convert.FromBase64String(partnerPublicString.Replace("-----BEGIN PUBLIC KEY-----", "").Replace("-----END PUBLIC KEY-----", "").Replace("\n", "").Trim());
            _sharedKey = _ec.DeriveKeyMaterial(ECDiffieHellmanCngPublicKey.FromByteArray(partnerPublicKey, CngKeyBlobFormat.EccPublicBlob));
        }

        public string SendCertificate(X509Certificate2 certificate)
        {
            if (_sharedKey == null)
                throw new InvalidOperationException("Key exchange must be completed before data can be exchanged.");

            using (Aes aes = Aes.Create())
            {
                aes.Key = _sharedKey;
                aes.GenerateIV();

                byte[] data = certificate.Export(X509ContentType.Pkcs12, "");

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                byte[] encryptedData = performCryptography(encryptor, data);
                string text = $"{Convert.ToBase64String(aes.IV)}|{Convert.ToBase64String(encryptedData)}";

                const int chunkSize = 64;
                string wrappedText = Enumerable.Range(0, text.Length / chunkSize)
                    .Select(i => text.Substring(i * chunkSize, chunkSize)).Aggregate((a, b) => $"{a}\n{b}");
                wrappedText += "\n" + text.Substring((text.Length / chunkSize) * chunkSize);
                return $"-----BEGIN ENCRYPTED DATA-----\n{wrappedText}\n-----END ENCRYPTED DATA-----";

            }
        }

        public X509Certificate2 ReceiveCertificate(string certificateData)
        {
            if (_sharedKey == null)
                throw new InvalidOperationException("Key exchange must be completed before data can be exchanged.");

            certificateData = certificateData.Replace("-----BEGIN ENCRYPTED DATA-----", "").Replace("-----END ENCRYPTED DATA-----", "").Replace("\n", "").Trim();
            
            string[] parts = certificateData.Split("|");
            byte[] iv = Convert.FromBase64String(parts[0]);
            byte[] encryptedData = Convert.FromBase64String(parts[1]);

            using (Aes aes = Aes.Create())
            {
                aes.Key = _sharedKey;
                aes.IV = iv;

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                byte[] decryptedData = performCryptography(decryptor, encryptedData);
                return new X509Certificate2(decryptedData, "", X509KeyStorageFlags.Exportable);
            }
        }

        private byte[] performCryptography(ICryptoTransform cryptoTransform, byte[] data)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var cryptoStream = new CryptoStream(memoryStream, cryptoTransform, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(data, 0, data.Length);
                    cryptoStream.FlushFinalBlock();
                    return memoryStream.ToArray();
                }
            }
        }
    }
}
