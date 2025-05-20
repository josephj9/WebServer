using System.Net;
using System.Net.Sockets;
using System.Threading;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Clifton.Extensions;
using System.Reflection;

namespace WebServer
{
    public static class Server
    {
        public enum ServerError
		{
			OK,
			ExpiredSession,
			NotAuthorized,
			FileNotFound,
			PageNotFound,
			ServerError,
			UnknownType,
			ValidationError,
			AjaxError,
		}
        public static Func<ServerError, string> onError;
        private static HttpListener listener;

        private static List<IPAddress> GetLocalHostIPs()
        {
            IPHostEntry host;
            host = Dns.GetHostEntry(Dns.GetHostName());
            List<IPAddress> ret = host.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).ToList();
            return ret;
        }
        private static HttpListener InitializeListener(List<IPAddress> localhostIPs)
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://localhost/");
            localhostIPs.ForEach(ip =>
              {
                  Console.WriteLine("Listening on IP " + "http://" + ip.ToString() + "/");
                  listener.Prefixes.Add("http://" + ip.ToString() + "/");
              });

            return listener;
        }
        public static int maxSimultaneousConnections = 20;
        private static Semaphore sem = new Semaphore(maxSimultaneousConnections, maxSimultaneousConnections);
        private static void Start(HttpListener listener)
        {
            listener.Start();
            Task.Run(() => RunServer(listener));
        }
        private static void RunServer(HttpListener listener)
        {
            while (true)
            {
                sem.WaitOne();
                StartConnectionListener(listener);
            }
        }
        
private static async void StartConnectionListener(HttpListener listener)
        {
            HttpListenerContext context = null;

            try
            {
                context = await listener.GetContextAsync();
                sem.Release();

                HttpListenerRequest request = context.Request;
                string path = request.RawUrl.LeftOf("?");
                string verb = request.HttpMethod;
                string parms = request.RawUrl.RightOf("?");

                Dictionary<string, string> kvParams = GetKeyValues(parms);

                if (request.HasEntityBody && request.HttpMethod.ToUpper() == "POST")
                {
                    using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        string postData = await reader.ReadToEndAsync();
                        GetKeyValues(postData, kvParams); // Add POST parameters to the same dictionary
                    }
                }

                Log(kvParams); 

                ResponsePacket packet = router.Route(verb, path, kvParams);

                if (packet.Error != ServerError.OK && onError != null)
                {
                    string errorPath = onError(packet.Error);
                    if (!string.IsNullOrEmpty(errorPath) && !path.Equals(errorPath, StringComparison.OrdinalIgnoreCase))
                    {
                        packet.Redirect = errorPath;
                    }
                }

                Respond(request, context.Response, packet);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                if (context != null)
                {
                    var packet = new ResponsePacket
                    {
                        Redirect = onError?.Invoke(ServerError.ServerError)
                    };
                    Respond(context.Request, context.Response, packet);
                }
            }
        }


        private static Router router = new Router();


        private static Dictionary<string, string> GetKeyValues(string data, Dictionary<string, string> kv = null)
        {
        kv.IfNull(() => kv = new Dictionary<string, string>());
        data.If(d => d.Length > 0, (d) => d.Split('&').ForEach(keyValue => kv[keyValue.LeftOf('=')] = keyValue.RightOf('=')));

        return kv;
        }
        public static void Start()
        {
            List<IPAddress> localHostIPs = GetLocalHostIPs();
            HttpListener listener = InitializeListener(localHostIPs);
            Start(listener);
        }
        private static void Log(Dictionary<string, string> kv)
        {
        kv.ForEach(kvp=>Console.WriteLine(kvp.Key+" : "+kvp.Value));
        }
        public static string GetWebsitePath(){
                string exePath = Assembly.GetExecutingAssembly().Location;
                string projectRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(exePath), @"..\..\.."));
                string websitePath = Path.Combine(projectRoot, "Website");
                return websitePath;
        }

        public static void Start(string websitePath){
            router.WebsitePath = websitePath;
            List<IPAddress> localHostIPs = GetLocalHostIPs();
            HttpListener listener = InitializeListener(localHostIPs);
            Start(listener);
        }

        private static void Respond(HttpListenerRequest request, HttpListenerResponse response, ResponsePacket resp)
        {   
            
            if (String.IsNullOrEmpty(resp.Redirect))
            {
                response.ContentType = resp.ContentType;
                response.ContentLength64 = resp.Data.Length;
                response.OutputStream.Write(resp.Data, 0, resp.Data.Length);
                response.ContentEncoding = resp.Encoding;
                response.StatusCode = (int)HttpStatusCode.OK;
            }
            else
            {
                response.StatusCode = (int)HttpStatusCode.Redirect;
                response.Redirect("http://" + request.UserHostAddress + resp.Redirect);
            }

            response.OutputStream.Close();

        }

    }
}