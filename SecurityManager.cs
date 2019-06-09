using System;
using System.Security.Cryptography.X509Certificates;

namespace TheWoosh.HTTPServer
{
    public class SecurityManager
    {
        public static bool Enabled = false;
        public static X509Certificate2 Certificate;

        public static void Enable()
        {
            Certificate = new X509Certificate2(Configuration.CertificateLocation,
                Configuration.CertificatePassword);
            Enabled = true;
            Console.WriteLine("[SecurityManager] Ready.");
        }
    }
}
