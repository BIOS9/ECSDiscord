using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace UserDataViewer
{
    class UserDataViewer
    {
        private const string ServerHost = "ecsdiscord.nightfish.co";
        private const int ServerPort = 12036;

        private CertificateService _certificateService = new CertificateService();
        private CryptoService _cryptoService;

        private UserDataViewer()
        {
            Console.Title = "ECS Discord User Data Viewer";
            Console.ForegroundColor = ConsoleColor.White;
            writeColor("Please load credentials.\n", ConsoleColor.Yellow);

            bool validSelection = false;
            while (!validSelection)
            {
                writeColor("Please select an option:", ConsoleColor.Yellow);
                Console.WriteLine("1. Import from PKCS12 file.");
                Console.WriteLine("2. Import from another user.");
                Console.WriteLine("3. Generate new certificate and private key.");
                Console.WriteLine("4. Exit.");

                char selection = Console.ReadKey().KeyChar;
                Console.WriteLine();
                switch (selection)
                {
                    case '1':
                        if (importPkcs12()) validSelection = true;
                        break;
                    case '2':
                        if (importFromUser()) validSelection = true;
                        break;
                    case '3':
                        if (generateCert()) validSelection = true;
                        break;
                    case '4':
                        Environment.Exit(0);
                        break;
                    default:
                        writeColor("Invalid selection.", ConsoleColor.Red);
                        break;
                }
            }
            _cryptoService = new CryptoService(_certificateService.Certificate);
            writeColor("Credentials loaded.\n", ConsoleColor.Green);

            while (true)
            {
                writeColor("Please select an option:", ConsoleColor.Yellow);
                Console.WriteLine("1. View ECS Discord user data.");
                Console.WriteLine("2. Save credentials to file.");
                Console.WriteLine("3. Send credentials to another user.");
                Console.WriteLine("4. Exit.");

                char selection = Console.ReadKey().KeyChar;
                Console.WriteLine();
                switch (selection)
                {
                    case '1':
                        accessData();
                        break;
                    case '2':
                        exportPkcs12();
                        break;
                    case '3':
                        exportToUser();
                        break;
                    case '4':
                        Environment.Exit(0);
                        break;
                    default:
                        writeColor("Invalid selection.", ConsoleColor.Red);
                        break;
                }
            }
        }

        private bool importPkcs12()
        {
            while (true)
            {
                if (File.Exists("credentials.pfx"))
                {
                    Console.WriteLine("Starting auto import of credentials.pfx from working directory...");
                    while (true)
                    {
                        writeColor("Please enter the encryption password for the PKCS12 file:", ConsoleColor.Yellow);
                        SecureString password = readPassword();
                        try
                        {
                            _certificateService.LoadCertificateFromFile("credentials.pfx", password);
                            writeColor("Automatic import succeeded!", ConsoleColor.Green);
                            return true;
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message.Equals("Cannot find the requested object."))
                            {
                                writeColor("Auto import of credentials.pfx failed: Invalid PKCS12 file.", ConsoleColor.Red);
                                continue;
                            }
                            writeColor("Auto import of credentials.pfx failed: " + ex.Message, ConsoleColor.Red);
                        }
                    }
                }

                try
                {
                    writeColor("Enter file path of PKCS12 file to import. (Usually .pfx extension)", ConsoleColor.Yellow);
                    string path = Console.ReadLine();
                    if (!File.Exists(path))
                    {
                        writeColor("File does not exist.", ConsoleColor.Red);
                        continue;
                    }

                    writeColor("Please enter the encryption password for the PKCS12 file:", ConsoleColor.Yellow);
                    SecureString password = readPassword();

                    _certificateService.LoadCertificateFromFile(path, password);
                    writeColor("Import succeeded!", ConsoleColor.Green);
                    return true;
                }
                catch(Exception ex)
                {
                    if(ex.Message.Equals("Cannot find the requested object."))
                    {
                        writeColor("Import of PKCS12 file failed: Invalid PKCS12 file.", ConsoleColor.Red);
                        continue;
                    }
                    writeColor("Import of PKCS12 file failed: " + ex.Message, ConsoleColor.Red);
                }
            }
        }

        private bool importFromUser()
        {
            Console.WriteLine("Starting credential import...");
            CertificateSharingService sharingService = new CertificateSharingService();
            writeColor("Please share this public key with the other user:", ConsoleColor.Yellow);
            Console.WriteLine(sharingService.PublicString + "\n");

            while (true)
            {
                try
                {
                    writeColor("Please enter the other user's public key:", ConsoleColor.Yellow);
                    StringBuilder sb = new StringBuilder();
                    while (true)
                    {
                        string line = Console.ReadLine();
                        sb.Append(line);
                        if (line.Contains("-----END PUBLIC KEY-----"))
                            break;
                    }
                    sharingService.GenerateKey(sb.ToString());
                    writeColor("Ephemeral key exchange complete.", ConsoleColor.Green);
                    break;
                }
                catch
                {
                    writeColor("Invalid public key.", ConsoleColor.Red);
                }
            }

            while (true)
            {
                try
                {
                    writeColor("Enter the encrypted credential data shared with you by the other user:", ConsoleColor.Yellow);
                    StringBuilder sb = new StringBuilder();
                    while (true)
                    {
                        string line = Console.ReadLine();
                        sb.Append(line);
                        if (line.Contains("-----END ENCRYPTED DATA-----"))
                            break;
                    }
                    X509Certificate2 certificate = sharingService.ReceiveCertificate(sb.ToString());
                    _certificateService.Certificate = certificate;
                    writeColor("Credentials import successfully!", ConsoleColor.Green);
                    break;
                }
                catch
                {
                    writeColor("Invalid encrypted credential data.", ConsoleColor.Red);
                }
            }
            return true;
        }

        private bool generateCert()
        {
            Console.WriteLine("Generating keypair...");
            _certificateService.GenerateCertificate();
            Console.WriteLine("keypair generated successfully!");
            return true;
        }

        private void exportPkcs12()
        {
            writeColor("Exporting to credentials.pfx", ConsoleColor.Yellow);
            Console.WriteLine("(The program will automatically import any file named credentials.pfx from the working directory when import PKCS12 is selected)");
            if(File.Exists("credentials.pfx"))
            {
                writeColor("credentials.pfx already exists in the working directory.", ConsoleColor.Yellow);
                writeColor("Do you want to overwrite credentials.pfx? (WARNING THIS OPERATION MAY CAUSE YOU TO LOSE YOUR CREDENTIALS!)", ConsoleColor.Yellow);
                Console.Write("(Y/N) ");
                char c = Console.ReadKey().KeyChar;
                Console.WriteLine();
                if(c != 'y' && c != 'Y')
                {
                    writeColor("PKCS12 export cancelled.", ConsoleColor.Red);
                    return;
                }
            }

            while (true)
            {
                writeColor("Please enter a password to protect the PKCS12 bundle with:", ConsoleColor.Yellow);
                SecureString password = readPassword();
                if(password.Length == 0)
                {
                    writeColor("Password cannot be empty.", ConsoleColor.Red);
                    continue;
                }
                writeColor("Confirm password:", ConsoleColor.Yellow);
                SecureString passwordConf = readPassword();
                if (!compareSecureStrings(password, passwordConf))
                    writeColor("Passwords do not match!", ConsoleColor.Red);
                else
                {
                    _certificateService.SaveToPkcs12File("credentials.pfx", password);
                    writeColor("Credentials saved to credentials.pfx in working directory!", ConsoleColor.Green);
                    break;
                }
            }
        }

        private void exportToUser()
        {
            Console.WriteLine("Starting credential export...");
            CertificateSharingService sharingService = new CertificateSharingService();
            writeColor("Please share this public key with the other user:", ConsoleColor.Yellow);
            Console.WriteLine(sharingService.PublicString + "\n");

            while (true)
            {
                try
                {
                    writeColor("Please enter the other user's public key:", ConsoleColor.Yellow);
                    StringBuilder sb = new StringBuilder();
                    while (true)
                    {
                        string line = Console.ReadLine();
                        sb.Append(line);
                        if (line.Contains("-----END PUBLIC KEY-----"))
                            break;
                    }
                    sharingService.GenerateKey(sb.ToString());
                    writeColor("Ephemeral key exchange complete.", ConsoleColor.Green);
                    break;
                }
                catch
                {
                    writeColor("Invalid public key.", ConsoleColor.Red);
                }
            }

            writeColor("Share this encrypted credential data with the other user.", ConsoleColor.Yellow);
            Console.WriteLine(sharingService.SendCertificate(_certificateService.Certificate));
            writeColor("Credentials exported successfully!", ConsoleColor.Green);
        }

        private void accessData()
        {
            Console.WriteLine("Beginning user data access...");
            bool validSelection = false;
            while (!validSelection)
            {
                writeColor("Please select an option:", ConsoleColor.Yellow);
                Console.WriteLine("1. Read online data. (Recommended)");
                Console.WriteLine("2. Decrypt offline encrypted data.");
                Console.WriteLine("3. Exit.");

                char selection = Console.ReadKey().KeyChar;
                Console.WriteLine();
                switch (selection)
                {
                    case '1':
                        readOnlineData();
                        break;
                    case '2':
                        readOfflineData();
                        break;
                    case '3':
                        Environment.Exit(0);
                        break;
                    default:
                        writeColor("Invalid selection.", ConsoleColor.Red);
                        break;
                }
            }
        }

        private void readOnlineData()
        {
            try
            {
                ServerConnection connection = new ServerConnection(ServerHost, ServerPort, _certificateService.Certificate);
                connection.OpenConnection();
                connection.ReadStatus();
                while (true)
                {
                    writeColor("Please enter the Discord ID of a user to view their username: (Empty value to cancel)", ConsoleColor.White);
                    string discordIdStr = Console.ReadLine();
                    if(string.IsNullOrWhiteSpace(discordIdStr))
                    {
                        connection.CloseConnection();
                        return;
                    }
                    ulong discordId;
                    if(!ulong.TryParse(discordIdStr, out discordId))
                    {
                        writeColor("Invalid Discord ID. Please use the snowflake ID format.", ConsoleColor.Red);
                        continue;
                    }

                    try
                    {
                        byte[] encryptedUsername = connection.GetEncryptedUsername(discordId);
                        try
                        {
                            string username = _cryptoService.DecryptText(encryptedUsername);
                            writeColor("Decrypted username: " + username, ConsoleColor.Green);
                        }
                        catch
                        {
                            writeColor("Data decryption failed. The data is invalid or you have the wrong key.", ConsoleColor.Red);
                        }
                    }
                    catch(ServerConnection.UserNotFoundException)
                    {
                        writeColor("That Discord ID was not found. You might have entered the ID wrong or the user is unverified.", ConsoleColor.Red);
                    }
                    catch(ServerConnection.InvalidDiscordIdException)
                    {
                        writeColor("The server reported that the specified Discord ID is invalid.", ConsoleColor.Red);
                    }
                    catch(ServerConnection.GeneralFailureException)
                    {
                        writeColor("A general failure occured while obtaining the data, please check the server logs for more information.", ConsoleColor.Red);
                    }
                }
            }
            catch (ServerConnection.ServerDisconnectedException)
            {
                writeColor("The server closed the connection. This may be because the key you provided does not have access to the user records.", ConsoleColor.Red);
                return;
            }
            catch (Exception ex)
            {
                writeColor("Online data connection failed: " + ex.Message, ConsoleColor.Red);
            }
        }

        private void readOfflineData()
        {
            while (true)
            {
                writeColor("Please enter Base64 encoded encrypted username: (Empty value to cancel)", ConsoleColor.Yellow);
                string b64 = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(b64))
                    break;

                byte[] encryptedUsername;
                try
                {
                    encryptedUsername = Convert.FromBase64String(b64);
                }
                catch
                {
                    writeColor("Invalid Base64 string.", ConsoleColor.Red);
                    continue;
                }

                try
                {
                    string username = _cryptoService.DecryptText(encryptedUsername);
                    writeColor("Decrypted username: " + username, ConsoleColor.Green);
                }
                catch
                {
                    writeColor("Data decryption failed. The data is invalid or you have the wrong key.", ConsoleColor.Red);
                }
            }
        }

        private SecureString readPassword()
        {
            SecureString pass = new SecureString();
            ConsoleKey key;
            do
            {
                var keyInfo = Console.ReadKey(intercept: true);
                key = keyInfo.Key;

                if (key == ConsoleKey.Backspace && pass.Length > 0)
                {
                    Console.Write("\b \b");
                    pass.RemoveAt(pass.Length - 1);
                }
                else if (!char.IsControl(keyInfo.KeyChar))
                {
                    Console.Write("*");
                    pass.AppendChar(keyInfo.KeyChar);
                }
            } while (key != ConsoleKey.Enter);
            Console.WriteLine();
            return pass;
        }

        private bool compareSecureStrings(SecureString ss1, SecureString ss2)
        {
            IntPtr bstr1 = IntPtr.Zero;
            IntPtr bstr2 = IntPtr.Zero;
            try
            {
                bstr1 = Marshal.SecureStringToBSTR(ss1);
                bstr2 = Marshal.SecureStringToBSTR(ss2);
                int length1 = Marshal.ReadInt32(bstr1, -4);
                int length2 = Marshal.ReadInt32(bstr2, -4);
                if (length1 == length2)
                {
                    for (int x = 0; x < length1; ++x)
                    {
                        byte b1 = Marshal.ReadByte(bstr1, x);
                        byte b2 = Marshal.ReadByte(bstr2, x);
                        if (b1 != b2) return false;
                    }
                }
                else return false;
                return true;
            }
            finally
            {
                if (bstr2 != IntPtr.Zero) Marshal.ZeroFreeBSTR(bstr2);
                if (bstr1 != IntPtr.Zero) Marshal.ZeroFreeBSTR(bstr1);
            }
        }

        private void writeColor(string text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ForegroundColor = ConsoleColor.White;
        }

        static void Main(string[] args)
        {
            new UserDataViewer();
        }
    }
}
