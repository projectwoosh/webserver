using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Collections.Generic;
using System.Threading;

namespace TheWoosh.HTTPServer
{
    public abstract class HTTPClient
    {
        public delegate void KeepAliveCallback(HTTPClient client);

        public TcpClient Client;
        public Stream Stream;
        public StreamWriter Writer;
        public StreamReader Reader;

        public bool UseSecurity;
        public bool Closed = !Configuration.KeepAlive;
        public string Ip;
        public KeepAliveCallback KACallback;

        // This value gets incremented every time a request is fulfilled, 
        // which can be convinient for debugging purposes.
        public int RequestId = 0;
        public string Phase = "Initialized";
        public string Path = null;

        public SslApplicationProtocol Protocol = SslApplicationProtocol.Http11;

        public HTTPClient(TcpClient client, bool useSecurity, KeepAliveCallback callback)
        {
            Client = client;

            var endPoint = (IPEndPoint)client.Client.RemoteEndPoint;
            Ip = endPoint.Address.ToString() + ":" + endPoint.Port;

            UseSecurity = useSecurity;
            KACallback = callback;
        }

        public void Log(object message)
        {
            Console.WriteLine("[HTTPClient] (" + Ip + "/req#" + RequestId + ") " + message);
        }

        public async void Start()
        {
            Phase = "Getting Stream";
            try
            {
                if (UseSecurity)
                {
                    var stream = new SslStream(Client.GetStream());
                    // An attempt to implement HTTP/2, but .NET doesn't support ALPN or 
                    // 'Application-Layer Protocol Negotiation' yet. This is nessecary 
                    // for negotiating with the client about the availability of HTTP/2
                    // on the client side.
                    if (Configuration.ALPNSupport)
                    {
                        Phase = "TLS-ALPN";
                        var authOptions = new SslServerAuthenticationOptions
                        {
                            AllowRenegotiation = false, // test 
                            EncryptionPolicy = EncryptionPolicy.RequireEncryption,
                            ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http2, SslApplicationProtocol.Http11 }, // ALPN
                            EnabledSslProtocols = Configuration.SecurityProtocols,
                            ClientCertificateRequired = false,
                            ServerCertificate = SecurityManager.Certificate
                        };
                        await stream.AuthenticateAsServerAsync(
                            authOptions,
                            new CancellationToken(false))
                            .ConfigureAwait(false);
                        Protocol = stream.NegotiatedApplicationProtocol;
                    }
                    else
                    {
                        stream.AuthenticateAsServer(SecurityManager.Certificate, false, Configuration.SecurityProtocols, false);
                    }
                    Phase = "Setting stream";
                    Stream = stream;
                }
                else
                {
                    Stream = Client.GetStream();
                }
                Phase = "Writer/Reader Init";
                Writer = new StreamWriter(Stream);
                Reader = new StreamReader(Stream);
                Phase = "Running";
                Run();
            }
            catch (Exception e)
            {
                if (!Phase.Equals("KACHandler"))
                    Log("(Phase=" + Phase + ") exception=" + e.Message);
            }
            finally
            {
                Phase = "Start():Final";
                if (Closed || KACallback == null)
                    Clean();
                else
                {
                    Phase = "KACHandler";
                    RequestId++;
                    Path = null;
                    KACallback(this);
                }
            }
            Phase = "dead";
        }

        public abstract void Run();

        public void Clean()
        {
            Log("Cleaning. (Phase=" + Phase + ")");
            if (Writer != null) try { Writer.Close(); } catch { }
            if (Reader != null) try { Reader.Close(); } catch { }
            if (Stream != null) try { Stream.Close(); } catch { }
            if (Client != null) try { Client.Close(); } catch { }
            Writer = null;
            Reader = null;
            Stream = null;
            Client = null;
        }
    }
}
