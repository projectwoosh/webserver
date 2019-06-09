using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Security;
using System.Net.Sockets;

namespace TheWoosh.HTTPServer
{
    public class ContentServingClient : HTTPClient
    {
        public string FilesDirectory, File404;

        public ContentServingClient(TcpClient client, bool useSecurity,
            KeepAliveCallback callback, string filesDirectory, string file404)
            : base(client, useSecurity, callback)
        {
            FilesDirectory = filesDirectory;
            File404 = file404;
        }

        private string[] RequestLine;
        private Dictionary<string, string> Headers;

        private void ParseHTTP1()
        {
            RequestLine = Reader.ReadLine().Split(' ');
            Path = RequestLine[1];
            Headers = new Dictionary<string, string>();
            string line;
            while ((line = Reader.ReadLine()) != "")
            {
                string[] parts = line.Split(new char[] { ':' }, 2);
                string key = parts[0], value = parts[1].Substring(1);
                Headers.Add(key.ToLower(), value);
            }
        }

        public override void Run()
        {
            List<string> flags = new List<string>();

            string status = "404 Not Found";

            if (base.Protocol == SslApplicationProtocol.Http11)
                ParseHTTP1();
            else
            {
                Log("[!] Unsupported ALPN: " + base.Protocol);
                return;
            }

            string file = FilesDirectory + Path;
            bool invalid = file.ToLower().StartsWith("/hidden");
            bool fileFound = !invalid && File.Exists(file);

            if (!fileFound && Directory.Exists(file))
            {
                if (!file.EndsWith("/")) file += "/";
                file += "index.html";
                fileFound = File.Exists(file);
            }

            if (!fileFound)
            {
                flags.Add("error=404");
                file = File404;
            }

            byte[] data = null;
            DateTime? lastModified = null;
            bool useClientCache = false;
            bool compressable = true;

            Phase = "ClientCache";
            if (fileFound)
            {
                status = "200 OK";
                lastModified = File.GetLastWriteTime(file);

                if (Headers.ContainsKey("if-modified-since"))
                {
                    DateTime value = Util.ParseHTTPDate(Headers["if-modified-since"]);
                    var fileMod = ((DateTime)lastModified).ToUniversalTime();

                    // There might be a better way to compare these dates, but because the HTTP Date format
                    // ignores milliseconds etc we can't just compare DateTime.Ticks or something like that
                    if (Util.FormatHTTPDate(value).Equals(Util.FormatHTTPDate(fileMod)))
                    {
                        useClientCache = true;
                        status = "304 Not Modified";
                        flags.Add("cache=ClientSide");
                    }
                }
            }

            if (!useClientCache && data == null)
                data = File.ReadAllBytes(file);

            // After this point, you must not change the content/information for the request,
            // since the finalization of the request is handled here and compression can be 
            // applied, to write to the stream.
            int dataLength = data == null ? 0 : data.Length;

            Phase = "Headers";
            Writer.WriteLine("HTTP/1.1 " + status);
            Writer.WriteLine("Server: " + Configuration.ServerName);
            Writer.WriteLine("Date: " + Util.GetHTTPDate());
            Writer.WriteLine("Content-Type: " + Util.GetType(Path));
            Writer.WriteLine("Tk: N");

            String kavalue = Headers.ContainsKey("connection") ? Headers["connection"].ToLower() : null;
            if (!Closed && RequestId + 1 != Configuration.KeepAliveLevel && kavalue == "keep-alive")
            {
                Writer.WriteLine("Connection: keep-alive");
            }
            else
            {
                Closed = true;
                Writer.WriteLine("Connection: close");
            }

            Writer.WriteLine("Strict-Transport-Security: max-age=31536000; includeSubDomains; preload");

            if (lastModified != null)
                Writer.WriteLine("Last-Modified: " + Util.FormatHTTPDate((DateTime)lastModified));

            Phase = "Compression-1";
            string compression = null;
            if (compressable && data != null && data.Length > 0)
            {
                Phase = "Compression-2";
                if (Headers.ContainsKey("accept-encoding"))
                {
                    Phase = "Compression-3";
                    string[] list = Headers["accept-encoding"].ToLower().Replace(" ", "").Split(',');
                    int i = 0;
                    Phase = "Compression-4";
                    try
                    {
                        if (list.Length > 0)
                        {
                            do
                            {
                                switch (list[i])
                                {
                                    case "gzip":
                                        using (var outStream = new MemoryStream())
                                        {
                                            using (var compStream = new GZipStream(outStream, CompressionMode.Compress))
                                            {
                                                using (var mStream = new MemoryStream(data))
                                                {
                                                    mStream.CopyTo(compStream);
                                                }
                                            }

                                            data = outStream.ToArray();
                                        }
                                        compression = "gzip";
                                        break;
                                    default:
                                        break;
                                }
                                i++;
                            }
                            while (compression == null || i < list.Length);
                        }
                    }
                    catch (Exception e)
                    {
                        Log(e.ToString());
                    }
                }

                if (compressable && compression != null)
                {
                    flags.Add("cmprssn=" + compression + "[" + dataLength + "=>" + data.Length + "]");
                    Writer.WriteLine("Content-Encoding: " + compression);
                }

                Phase = "Write";

                Writer.WriteLine("Content-Length: " + data.Length);
                Writer.WriteLine("");
                Writer.Flush();

                Stream.Write(data, 0, data.Length);
                Stream.Flush();
            }
            else
            {
                Phase = "Write";
                Writer.WriteLine("");
                Writer.Flush();
                flags.Add("noData");
            }
            Phase = "PostWrite";
            Log("Request: path=" + Path + " " + string.Join(" ", flags));
            Phase = "Dead?";
        }
    }

    public class ContentServingServer : HTTPServer
    {
        public string FilesDirectory, File404;

        public ContentServingServer(bool useSecurity, ushort port, string startDir, string file404)
            : base(useSecurity, port)
        {
            FilesDirectory = startDir;
            File404 = file404;
        }

        public void KACHandler(HTTPClient client)
        {
            if (client != null)
                try
                {
                    client.Run();
                }
                catch (Exception e)
                {
                    if (client.Phase != "KACHandler")
                        client.Log("(Phase=" + client.Phase + ") exception=" + e.Message);
                }
        }

        public override void HandleNewClient(TcpClient tclient)
        {
            ContentServingClient client = new ContentServingClient(tclient,
    true, new HTTPClient.KeepAliveCallback(KACHandler),
    FilesDirectory, File404);
            try
            {
                client.Start();
            }
            catch
            {
            }
        }
    }
}
