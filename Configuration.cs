using System;
using System.IO.Compression;
using System.Security.Authentication;

namespace TheWoosh.HTTPServer
{
    public class Configuration
    {
        public static int ListenerTimeout = 100;

        // Should we reuse the connection, if possible?
        public static bool KeepAlive = true;

        // After how many requests should we close the connection?
        public static int KeepAliveLevel = 2;

        // Simple & Extendable Server for HTTP 1.1
        public static string ServerName = "TheWoosh SESH 1.1";
        public const CompressionLevel CompressionLevelValue = CompressionLevel.Fastest;

        // Mono doesn't support ALPN yet,
        // and isn't implemented yet.
        public static bool ALPNSupport = false;

        // Use TLS 1.3 if it gets released to .NET:
        // public const SslProtocols SecurityProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
        public const SslProtocols SecurityProtocols = SslProtocols.Tls12;

        public static string HostName;
        public static string CertificateLocation;
        public static string CertificatePassword;

        public static void Load()
        {
            string[] lines = System.IO.File.ReadAllLines("configuration.ini");
            foreach (string line in lines)
            {
                string[] parts = line.Split(new char[] { '=' }, 2);
                switch (parts[0].ToLower())
                {
                    case "certlocation":
                        CertificateLocation = parts[1];
                        break;
                    case "certpassword":
                        CertificatePassword = parts[1];
                        break;
                    case "hostname":
                        HostName = parts[1];
                        break;
                    case "keepalive":
                        KeepAlive = Convert.ToBoolean(parts[1]);
                        break;
                    case "keepalivelevel":
                        KeepAliveLevel = Convert.ToInt32(parts[1]);
                        break;
                    case "listenertimeout":
                        ListenerTimeout = Convert.ToInt32(parts[1]);
                        break;
                    case "servername":
                        ServerName = parts[1];
                        break;
                }
            }
            Console.WriteLine("[Configuration] Loaded.");
        }
    }
}
