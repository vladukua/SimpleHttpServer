using System;
using System.Collections;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using Bend.Util.Abstract;

// offered to the public domain for any use with no restriction
// and also with no warranty of any kind, please enjoy. - David Jeske. 

// simple HTTP explanation
// http://www.jmarshall.com/easy/http/

namespace Bend.Util {

    public class TestMain {

        public static int Main(string[] args) {
            HttpServer httpServer = args.GetLength(0) > 0 ? new MyHttpServer(Convert.ToInt16(args[0])) : new MyHttpServer(8080);
            var thread = new Thread(new ThreadStart(httpServer.Listen));
            thread.Start();
            return 0;
        }

    }

}



