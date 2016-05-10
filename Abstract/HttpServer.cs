using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Bend.Util.Abstract
{
    public abstract class HttpServer
    {

        protected int Port;
        private TcpListener _listener;
        private bool _isActive;

        protected HttpServer(int port)
        {
            this.Port = port;
        }

        public void Listen()
        {
            Console.WriteLine("------------------------------------------------Simple Http Server Started--------------------------------------------------");
            _isActive = true;
            _listener = new TcpListener(Port);
            _listener.Start();
            while (_isActive)
            {
                TcpClient s = _listener.AcceptTcpClient();
                HttpProcessor processor = new HttpProcessor(s, this);
                Thread thread = new Thread(processor.Process);
                thread.Start();
                Thread.Sleep(1);
            }
        }

        public abstract void HandleGetRequest(HttpProcessor p);

        public abstract void HandlePostRequest(HttpProcessor p, StreamReader inputData);

        public virtual void Stop()
        {
            _isActive = false;
        }
    }

}
