using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace TheWoosh.HTTPServer
{
    public abstract class HTTPServer : IDisposable
    {
        public TcpListener Listener { get; protected set; }

        public bool UseSecurity { get; protected set; }
        public ushort Port { get; protected set; }

        public bool ShutdownRequested { get; protected set; }

        public HTTPServer(bool useSecurity, ushort port)
        {
            UseSecurity = useSecurity;
            Port = port;
            ShutdownRequested = false;
        }

        public abstract void HandleNewClient(TcpClient client);

        public void Start()
        {
            Console.WriteLine("[HTTPServer] Server starting, port=" + Port);
            try
            {
                Listener = new TcpListener(IPAddress.Any, (int)Port);
                Listener.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine("[HTTPServer] Could start on port=" + Port);
                Console.WriteLine(e.Message);
                return;
            }
            try
            {
                while (!ShutdownRequested)
                {
                    if (!Listener.Pending())
                    {
                        Thread.Sleep(Configuration.ListenerTimeout);
                        continue;
                    }

                    TcpClient client = Listener.AcceptTcpClient();
                    new Thread(new ThreadStart(() => { HandleNewClient(client); })).Start();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[HTTPServer] " + e.Message);
            }
            finally
            {
                try
                {
                    Listener.Stop();
                    Listener = null;
                }
                catch
                {
                }
            }
        }

        public void Dispose()
        {
            ShutdownRequested = true;
        }
    }
}
