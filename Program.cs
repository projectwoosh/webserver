using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

/**
 */
namespace TheWoosh.HTTPServer
{
    public static class Start
    {
        public static HTTPServer PlainRedirectServer;
        public static HTTPServer TLSServer;

        public static void Main()
        {
            Configuration.Load();
            SecurityManager.Enable();

            PlainRedirectServer = new PlainServer(false, 80);
            TLSServer = new ContentServingServer(true, 443, "/var/www/html", "/var/www/html/hidden/404.html");

            ThreadPool.SetMaxThreads(25, 5);
            new Thread(new ThreadStart(PlainRedirectServer.Start)).Start();
            new Thread(new ThreadStart(TLSServer.Start)).Start();
        }
    }
}
