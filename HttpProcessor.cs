using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Bend.Util.Abstract;

namespace Bend.Util
{
    public class HttpProcessor
    {
        public TcpClient Socket;
        public HttpServer Srv;

        private Stream _inputStream;
        public StreamWriter OutputStream;

        public string HttpMethod;
        public string HttpUrl;
        public string HttpProtocolVersionstring;

        public Hashtable Headers { get; set; }

        public Hashtable QueryString { get; set; }

        private const int BufSize = 16*1024;

        private const int MaxPostSize = 10 * 1024 * 1024; // 10MB

        public HttpProcessor(TcpClient s, HttpServer srv)
        {
            this.Socket = s;
            this.Srv = srv;

            QueryString = new Hashtable();
            Headers = new Hashtable();
        }


        private string StreamReadLine(Stream inputStream)
        {
            int nextChar;
            string data = "";
            while (true)
            {
                nextChar = inputStream.ReadByte();
                if (nextChar == '\n') { break; }
                if (nextChar == '\r') { continue; }
                if (nextChar == -1) { Thread.Sleep(1); continue; };
                data += Convert.ToChar(nextChar);
            }
            return data;
        }
        public void Process()
        {
            // we can't use a StreamReader for input, because it buffers up extra data on us inside it's
            // "processed" view of the world, and we want the data raw after the headers
            _inputStream = new BufferedStream(Socket.GetStream());

            // we probably shouldn't be using a streamwriter for all output from handlers either
            OutputStream = new StreamWriter(new BufferedStream(Socket.GetStream()));
            try
            {
                ParseRequest();
                ReadHeaders();
                ParseQueryString();
                if (HttpMethod.Equals("GET"))
                {
                    HandleGetRequest();
                }
                else if (HttpMethod.Equals("POST"))
                {
                    HandlePostRequest();
                }
                Console.WriteLine("---------------------------------------------------------------------------------------------------------------------------");
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.ToString());
                WriteFailure();
            }
            OutputStream.Flush();
            // bs.Flush(); // flush any remaining output
            _inputStream = null; OutputStream = null; // bs = null;            
            Socket.Close();
        }

        public void ParseRequest()
        {
            string request = StreamReadLine(_inputStream);
            string[] tokens = request.Split(' ');
            if (tokens.Length != 3)
            {
                throw new Exception("invalid http request line");
            }
            HttpMethod = tokens[0].ToUpper();
            HttpUrl = tokens[1];
            HttpProtocolVersionstring = tokens[2];

            Console.WriteLine("---------------------------------------------------------------------------------------------------------------------------");
            Console.WriteLine("starting: " + request);
        }

        private void ParseQueryString()
        {
            var index = HttpUrl.IndexOf("/?", StringComparison.InvariantCulture);
            if (index >= 0)
            {
                var queryString = HttpUrl.Substring(index + 2);
                var queryStringArr = queryString.Split('&');
                foreach (var query in queryStringArr)
                {
                    var queryArr = query.Split('=');
                    if (queryArr.Length == 2)
                    {
                        QueryString.Add(queryArr[0], queryArr[1]);
                    }
                }
            }
        }

        public void ReadHeaders()
        {
            Console.WriteLine("readHeaders()");
            String line;
            while ((line = StreamReadLine(_inputStream)) != null)
            {
                if (line.Equals(""))
                {
                    Console.WriteLine("got headers");
                    return;
                }

                int separator = line.IndexOf(':');
                if (separator == -1)
                {
                    throw new Exception("invalid http header line: " + line);
                }
                String name = line.Substring(0, separator);
                int pos = separator + 1;
                while ((pos < line.Length) && (line[pos] == ' '))
                {
                    pos++; // strip any spaces
                }

                string value = line.Substring(pos, line.Length - pos);
                Console.WriteLine("header: {0}:{1}", name, value);
                Headers[name] = value;
            }
        }

        public void HandleGetRequest()
        {
            Srv.HandleGetRequest(this);
        }

        public void HandlePostRequest()
        {
            // this post data processing just reads everything into a memory stream.
            // this is fine for smallish things, but for large stuff we should really
            // hand an input stream to the request processor. However, the input stream 
            // we hand him needs to let him see the "end of the stream" at this content 
            // length, because otherwise he won't know when he's seen it all! 

            Console.WriteLine("get post data start");
            int contentLen = 0;
            MemoryStream ms = new MemoryStream();
            if (this.Headers.ContainsKey("Content-Length"))
            {
                contentLen = Convert.ToInt32(this.Headers["Content-Length"]);
                if (contentLen > MaxPostSize)
                {
                    throw new Exception(
                        String.Format("POST Content-Length({0}) too big for this simple server",
                          contentLen));
                }
                byte[] buf = new byte[BufSize];
                int toRead = contentLen;
                while (toRead > 0)
                {
                    Console.WriteLine("starting Read, to_read={0}", toRead);

                    int numread = this._inputStream.Read(buf, 0, Math.Min(BufSize, toRead));
                    Console.WriteLine("read finished, numread={0}", numread);
                    if (numread == 0)
                    {
                        if (toRead == 0)
                        {
                            break;
                        }
                        else {
                            throw new Exception("client disconnected during post");
                        }
                    }
                    toRead -= numread;
                    ms.Write(buf, 0, numread);
                }
                ms.Seek(0, SeekOrigin.Begin);
            }
            Console.WriteLine("get post data end");
            Srv.HandlePostRequest(this, new StreamReader(ms));

        }

        public void WriteSuccess(string contentType = "text/html")
        {
            // this is the successful HTTP response line
            OutputStream.WriteLine("HTTP/1.0 200 OK");
            // these are the HTTP headers...          
            OutputStream.WriteLine("Content-Type: " + contentType);
            OutputStream.WriteLine("Connection: close");
            // ..add your own headers here if you like

            OutputStream.WriteLine(""); // this terminates the HTTP headers.. everything after this is HTTP body..
        }

        public void WriteFailure()
        {
            // this is an http 404 failure response
            OutputStream.WriteLine("HTTP/1.0 404 File not found");
            // these are the HTTP headers
            OutputStream.WriteLine("Connection: close");
            // ..add your own headers here

            OutputStream.WriteLine(""); // this terminates the HTTP headers.
        }
    }

}
