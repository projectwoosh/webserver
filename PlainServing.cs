using System;
using System.Net.Sockets;

namespace TheWoosh.HTTPServer
{
    public class PlainServer : HTTPServer
    {
        public PlainServer(bool useSecurity, ushort port) : base(useSecurity, port) { }

        public override void HandleNewClient(TcpClient tclient)
        {
            PlainClient client = new PlainClient(tclient, false, null);
            try
            {
                client.Start();
            }
            catch { }
        }
    }

    public class PlainClient : HTTPClient
    {
        public PlainClient(TcpClient client, bool useSecurity,
                KeepAliveCallback callback) :
                base(client, useSecurity, callback)
        {
        }

        // For some reason, clients can't read the body of this response. This isn't a 
        // breaking issue, but for legacy support and following the rules of W3C.
        public override void Run()
        {
            try
            {
                string path = "https://" + Configuration.HostName + Reader.ReadLine().Split(' ')[1];
                string content = "<A HREF=\"" + path + "\">" + path + "</A>";

                Writer.WriteLine("HTTP/1.1 301 Moved Permanently");
                Writer.WriteLine("Date: " + Util.GetHTTPDate());
                Writer.WriteLine("Server: " + Configuration.ServerName);
                Writer.WriteLine("Location: " + path);
                Writer.WriteLine("Connection: close");
                Writer.WriteLine("Content-Length: " + content.Length);

                Writer.WriteLine("Tk: N");
                Writer.WriteLine("Content-Type: text/html; charset=UTF-8");
                Writer.WriteLine("");
                Writer.WriteLine(content);

                Writer.Flush();
                Stream.Flush();
            }
            catch
            {
            }
            finally
            {
                try { Clean(); } catch { }
            }
        }
    }

}
