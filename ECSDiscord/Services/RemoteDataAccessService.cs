using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace ECSDiscord.Services
{
    public class RemoteDataAccessService
    {
        private readonly IConfigurationRoot _config;
        private bool _enable;
        private int _port;
        private X509Certificate2 _certificate;

        public RemoteDataAccessService(IConfigurationRoot config)
        {
            Log.Debug("Remote data access service loading.");
            _config = config;
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
                while (true)
                {
                    try
                    {
                        Log.Information("Remote data access server listening for connections on port {port}", _port);
                        TcpListener tcpListener = new TcpListener(new IPEndPoint(IPAddress.Any, _port));
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
            while(client.Connected)
            {

            }
            Log.Information("[Remote data access]: Client disconnected {client}", client.Client.RemoteEndPoint.ToString());
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
        }
    }
}
