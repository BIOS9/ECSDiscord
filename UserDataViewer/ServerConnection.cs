using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace UserDataViewer
{
    public class ServerConnection
    {
        public class UserNotFoundException : Exception { }
        public class InvalidDiscordIdException : Exception { }
        public class GeneralFailureException : Exception { }

        private class DataChunk
        {
            public enum Status
            {
                Success,
                UserNotFound,
                InvalidDiscordId,
                Failure
            }

            public Status TransferStatus;
            public string Data;
        }

        private readonly string _host;
        private readonly int _port;
        private readonly X509Certificate2 _certificate;
        private TcpClient _tcpClient;
        private SslStream _sslStream;

        public ServerConnection(string host, int port, X509Certificate2 clientCertificate)
        {
            _host = host;
            _port = port;
            _certificate = clientCertificate;
        }

        public void OpenConnection()
        {
            Console.WriteLine("Connecting...");
            _tcpClient = new TcpClient();
            _tcpClient.Connect(_host, _port);
            _sslStream = new SslStream(
                _tcpClient.GetStream(),
                false,
                new RemoteCertificateValidationCallback(validateServerCert),
                new LocalCertificateSelectionCallback(selectLocalCertificate),
                EncryptionPolicy.RequireEncryption);
            Console.WriteLine("Authenticating...");
            _sslStream.AuthenticateAsClient(_host);
        }

        public void CloseConnection()
        {
            Console.WriteLine("Closing connection...");
            _sslStream.Close();
            _sslStream.Dispose();
            _tcpClient.Dispose();
        }

        public byte[] GetEncryptedUsername(ulong discordId)
        {
            Console.WriteLine("Getting encrypted username...");
            sendData(new DataChunk { Data = discordId.ToString() });
            DataChunk chunk = readData();
            switch(chunk.TransferStatus)
            {
                case DataChunk.Status.Success:
                    return Convert.FromBase64String(chunk.Data);
                case DataChunk.Status.UserNotFound:
                    throw new UserNotFoundException();
                case DataChunk.Status.InvalidDiscordId:
                    throw new InvalidDiscordIdException();
                default:
                case DataChunk.Status.Failure:
                    throw new GeneralFailureException();
            }
        }

        private void sendData(DataChunk chunk)
        {
            string json = JsonConvert.SerializeObject(chunk);
            byte[] jsonData = Encoding.UTF8.GetBytes(json);
            if(jsonData.LongLength > int.MaxValue)
                throw new Exception("Encoded data length exceeds header capacity.");
            byte[] dataLength = BitConverter.GetBytes(jsonData.Length);

            _sslStream.Write(dataLength);
            _sslStream.Write(jsonData);
        }

        private DataChunk readData()
        {
            byte[] dataLengthBuffer = new byte[4]; // 4 byte integer
            _sslStream.Read(dataLengthBuffer, 0, 4);
            int dataLength = BitConverter.ToInt32(dataLengthBuffer, 0);
            byte[] data = new byte[dataLength];
            _sslStream.Read(data, 0, dataLength);
            string json = Encoding.UTF8.GetString(data);
            DataChunk chunk = JsonConvert.DeserializeObject<DataChunk>(json);
            return chunk;
        }

        private bool validateServerCert(
            object sender, 
            X509Certificate certificate, 
            X509Chain chain, 
            SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            return false;
        }

        public X509Certificate selectLocalCertificate(
            object sender,
            string targetHost,
            X509CertificateCollection localCertificates,
            X509Certificate remoteCertificate,
            string[] acceptableIssuers)
        {
            return _certificate;
        }
    }
}
