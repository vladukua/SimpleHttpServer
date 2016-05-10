using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Bend.Util.Abstract;

namespace Bend.Util
{
    public class MyHttpServer : HttpServer
    {

        public MyHttpServer(int port)
            : base(port)
        {
        }

        public override void HandleGetRequest(HttpProcessor p)
        {
            Console.WriteLine("request: {0}", p.HttpUrl);
            p.WriteSuccess();
            p.OutputStream.WriteLine("<html><body><h1>Test server</h1>");
            p.OutputStream.WriteLine("<h4>Current Time: " + DateTime.Now.ToString() + "</h4>");
            p.OutputStream.WriteLine("<h4>url: " + p.HttpUrl + "</h4>");

            p.OutputStream.WriteLine("<h3>Query Parameters:</h3>");
            foreach (var key in p.QueryString.Keys)
            {
                p.OutputStream.WriteLine("<p>{0} : <b>{1}</b></p>", key, p.QueryString[key]);
            }
        }

        public override void HandlePostRequest(HttpProcessor p, StreamReader inputData)
        {
            Console.WriteLine("POST request: {0}", p.HttpUrl);
            string data = inputData.ReadToEnd();

            p.WriteSuccess();
            p.OutputStream.WriteLine("<html><body><h1>test server</h1>");
            p.OutputStream.WriteLine("<a href=/test>return</a><p>");
            p.OutputStream.WriteLine("postbody: <pre>{0}</pre>", data);
        }
    }

}
