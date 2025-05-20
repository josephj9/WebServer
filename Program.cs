using System.Net.Sockets;
using System.Threading;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Clifton.Extensions;
using ConsoleWebServer;
using WebServer;

namespace ConsoleWebServer
{
    class Program
    {
        static void Main(string[] args)
        {
        string websitePath = Server.GetWebsitePath();
        Server.onError = ErrorHandler;
        Router server = new Router();


            // Register a custom POST route
            server.AddRoute(new Route
            {
                Verb = Router.POST,
                Path = "/Pages/demo/redirect",
                Action = RedirectMe
            });

            Server.Start(websitePath);
            Console.ReadLine();            
            
        }
        public static string RedirectMe(Dictionary<string, string> parms)
        {
            return "/Pages/demo/clicked.html"; 
        }

        public static string ErrorHandler(Server.ServerError error)
        {
            switch (error)
            {
                case Server.ServerError.ExpiredSession:
                    return "/ErrorPages/expiredSession.html";
                case Server.ServerError.FileNotFound:
                    return "/ErrorPages/fileNotFound.html";
                case Server.ServerError.NotAuthorized:
                    return "/ErrorPages/notAuthorized.html";
                case Server.ServerError.PageNotFound:
                    return "/ErrorPages/pageNotFound.html";
                case Server.ServerError.ServerError:
                    return "/ErrorPages/serverError.html";
                case Server.ServerError.UnknownType:
                    return "/ErrorPages/unknownType.html";
                default:
                    return null;
            }
        }
    }
}
