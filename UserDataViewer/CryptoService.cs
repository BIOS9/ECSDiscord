using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace UserDataViewer
{
    public class CryptoService
    {
        private X509Certificate2 _certificate;

        public CryptoService(X509Certificate2 certificate)
        {
            _certificate = certificate;
        }

        public byte[] Decrypt(byte[] encrypted)
        {
            using (RSA rsa = _certificate.GetRSAPrivateKey())
                return rsa.Decrypt(encrypted, RSAEncryptionPadding.OaepSHA256);
        }

        public string DecryptUsername(byte[] encryptedUsername)
        {
            return Encoding.UTF8.GetString(Decrypt(encryptedUsername));
        }
    }
}
