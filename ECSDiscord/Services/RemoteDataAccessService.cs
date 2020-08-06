using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace ECSDiscord.Services
{
    public class RemoteDataAccessService
    {
        private readonly IConfigurationRoot _config;
        private readonly StorageService _storageService;
        private bool _enable;
        private int _port;
        private X509Certificate2 _certificate;
        private string _allowedCertificateHash;

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

        public class ClientDisconnectedException : Exception { }

        public RemoteDataAccessService(IConfigurationRoot config, StorageService storageService)
        {
            Log.Debug("Remote data access service loading.");
            _config = config;
            _storageService = storageService;
            loadConfig();
            if (_enable)
            {
                startServer();
                Log.Debug("Remote data access service loaded.");
            }
            else
            {
                Log.Debug("Remote data access service unloaded (Service is disabled).");
            }
        }

        private async void startServer()
        {
            Log.Debug("Starting remote data access server.");
            try
            {
                Log.Information("Remote data access server listening for connections on port {port}", _port);
                TcpListener tcpListener = new TcpListener(new IPEndPoint(IPAddress.Any, _port));
                tcpListener.Start();
                while (true)
                {
                    try
                    {
                        handleClient(await tcpListener.AcceptTcpClientAsync());
                    }
                    catch(Exception ex)
                    {
                        Log.Warning("[Remote data access]: Network error {message}", ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to start remote data access server {message}", ex.Message);
            }
        }

        private async void handleClient(TcpClient client)
        {
            Log.Information("[Remote data access]: Client connected {client}", client.Client.RemoteEndPoint.ToString());
            try
            {
                SslStream sslStream = new SslStream(client.GetStream(), false, new RemoteCertificateValidationCallback(validateClientCert), new LocalCertificateSelectionCallback(selectServerCert));
                Log.Debug("[Remote data access]: Authenticating client using TLS client certificate...");
                await sslStream.AuthenticateAsServerAsync(_certificate, true, SslProtocols.Tls12 | SslProtocols.Tls12, false);
                await sslStream.WriteAsync(new byte[] { 255 });
                Log.Debug("[Remote data access]: Sent status byte.");
                while (client.Connected)
                {
                    DataChunk chunk = await readData(sslStream);
                    ulong discordId;
                    if (!ulong.TryParse(chunk.Data, out discordId))
                    {
                        Log.Warning("[Remote data access]: Client {client} provided invalid Discord ID {id}", client.Client.RemoteEndPoint.ToString(), chunk.Data);
                        await sendData(sslStream, new DataChunk { TransferStatus = DataChunk.Status.InvalidDiscordId });
                        continue;
                    }
                    try
                    {
                        Log.Information("[Remote data access]: Client {client} requested encrypted username for Discord ID {id}", client.Client.RemoteEndPoint.ToString(), discordId);
                        byte[] encryptedUsername = await _storageService.Users.GetEncryptedUsernameAsync(discordId);
                        string b64 = Convert.ToBase64String(encryptedUsername);
                        await sendData(sslStream, new DataChunk { Data = b64, TransferStatus = DataChunk.Status.Success });
                    }
                    catch (StorageService.RecordNotFoundException)
                    {
                        Log.Information("[Remote data access]: Client {client} attempted to get encrypted username for Discord ID {id} but the user was not found", client.Client.RemoteEndPoint.ToString(), discordId);
                        await sendData(sslStream, new DataChunk { TransferStatus = DataChunk.Status.UserNotFound });
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "[Remote data access]: Client {client} attempted to get encrypted username for Discord ID {id} there was an error: {error}", client.Client.RemoteEndPoint.ToString(), discordId, ex.Message);
                        await sendData(sslStream, new DataChunk { TransferStatus = DataChunk.Status.Failure });
                    }
                }
            }
            catch (AuthenticationException)
            {
                Log.Information("[Remote data access]: Client {client} access denied. Invalid client certificate.", client.Client.RemoteEndPoint.ToString());
                return;
            }
            catch (ClientDisconnectedException)
            {
                Log.Information("[Remote data access]: Client {client} disconnected.", client.Client.RemoteEndPoint.ToString());
                return;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Remote data access]: Error occured communicating with a client {client} {message}", client.Client.RemoteEndPoint.ToString(), ex.Message);
            }
            Log.Information("[Remote data access]: Client disconnected {client}", client.Client.RemoteEndPoint.ToString());
        }

        private async Task sendData(SslStream stream, DataChunk chunk)
        {
            string json = JsonConvert.SerializeObject(chunk);
            byte[] jsonData = Encoding.UTF8.GetBytes(json);
            if (jsonData.LongLength > int.MaxValue)
                throw new Exception("Encoded data length exceeds header capacity.");
            byte[] dataLength = BitConverter.GetBytes(jsonData.Length);

            await stream.WriteAsync(dataLength);
            await stream.WriteAsync(jsonData);
        }

        private async Task<DataChunk> readData(SslStream stream)
        {
            byte[] dataLengthBuffer = new byte[4]; // 4 byte integer
            int readBytes = await stream.ReadAsync(dataLengthBuffer, 0, 4);
            if (readBytes == 0)
                throw new ClientDisconnectedException();
            int dataLength = BitConverter.ToInt32(dataLengthBuffer, 0);
            byte[] data = new byte[dataLength];
            readBytes = await stream.ReadAsync(data, 0, dataLength);
            if (readBytes == 0)
                throw new ClientDisconnectedException();
            string json = Encoding.UTF8.GetString(data);
            DataChunk chunk = JsonConvert.DeserializeObject<DataChunk>(json);
            return chunk;
        }

        private bool validateClientCert(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            string hash = certificate.GetCertHashString(HashAlgorithmName.SHA256);
            if (!hash.Equals(_allowedCertificateHash))
            {
                Log.Information("[Remote data access]: Access denied for client due to unauthorised client certificate. Hash: {hash}", hash);
                return false;
            }

            Log.Debug("[Remote data access]: Access granted for certificate hash {hash}", hash);
            return true;
        }

        public X509Certificate selectServerCert(
            object sender,
            string targetHost,
            X509CertificateCollection localCertificates,
            X509Certificate remoteCertificate,
            string[] acceptableIssuers)
        {
            return _certificate;
        }

        private void loadConfig()
        {
            if (!bool.TryParse(_config["remoteDataAccess:enable"], out _enable))
            {
                Log.Error("Invalid enable boolean configured in remote data access settings.");
                throw new ArgumentException("Invalid enable boolean configured in remote data access settings.");
            }
            if (!_enable)
                return;

            if (!int.TryParse(_config["remoteDataAccess:port"], out _port))
            {
                Log.Error("Invalid port configured in remote data access settings.");
                throw new ArgumentException("Invalid port configured in remote data access settings.");
            }

            string pkcs12Pass = _config["remoteDataAccess:pkcs12Password"];
            string pkcs12Path = _config["remoteDataAccess:pkcs12Bundle"];
            if (!File.Exists(pkcs12Path))
            {
                Log.Error("Invalid pkcs12 bundle path configured in remote data access settings. File not found.");
                throw new ArgumentException("Invalid pkcs12 bundle path configured in remote data access settings. File not found.");
            }

            try
            {
                _certificate = new X509Certificate2(pkcs12Path, pkcs12Pass);
                if (!_certificate.HasPrivateKey)
                {
                    Log.Error("Failed to import remote data access PKCS12 bundle. Bundle does not contain a private key.");
                    throw new ArgumentException("Failed to import remote data access PKCS12 bundle. Bundle does not contain a private key.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to import remote data access PKCS12 bundle. Invalid bundle or incorrect password. {message}", ex.Message);
                throw new ArgumentException("Failed to import remote data access PKCS12 bundle. Invalid bundle or incorrect password.", ex);
            }

            try
            {
                X509Certificate2 verificationCert = new X509Certificate2(_config["verification:publicKeyCertPath"]);
                _allowedCertificateHash = verificationCert.GetCertHashString(HashAlgorithmName.SHA256);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to import verification public key certificate certificate.", ex.Message);
                throw new ArgumentException("Failed to import verification public key certificate certificate.", ex);
            }
        }
    }
}
