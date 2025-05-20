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


public class Route{
  public string Verb { get; set; }
  public string Path { get; set; }
  public Func<Dictionary<string,string>, string> Action { get; set; }
}
public class ExtensionInfo
{
    public Func<string, string, ExtensionInfo, ResponsePacket> Loader { get; set; }
    public string ContentType { get; set; }
}

public class ResponsePacket{
    public string Redirect { get; set; }
    public byte[] Data { get; set; }
    public string ContentType { get; set; }
    public Encoding Encoding { get; set; }
    public Server.ServerError Error { get; set; }

}

public  class Router
{
    public const string GET = "GET";
    public const string POST = "POST";
    public const string PUT = "PUT";
    public const string DELETE = "DELETE";

    public string WebsitePath { get; set; }

    private Dictionary<string, ExtensionInfo> extFolderMap;
    public List<Route> routes = new List<Route>();


    public Router()
    {
        extFolderMap = new Dictionary<string, ExtensionInfo>()
        {
            {"ico", new ExtensionInfo() {Loader = ImageLoader, ContentType = "image/ico"}},
            {"png", new ExtensionInfo() {Loader = ImageLoader, ContentType = "image/png"}},
            {"jpg", new ExtensionInfo() {Loader = ImageLoader, ContentType = "image/jpg"}},
            {"gif", new ExtensionInfo() {Loader = ImageLoader, ContentType = "image/gif"}},
            {"bmp", new ExtensionInfo() {Loader = ImageLoader, ContentType = "image/bmp"}},
            {"html", new ExtensionInfo() {Loader = PageLoader, ContentType = "text/html"}},
            {"css", new ExtensionInfo() {Loader = FileLoader, ContentType = "text/css"}},
            {"js", new ExtensionInfo() {Loader = FileLoader, ContentType = "text/javascript"}},
            {"", new ExtensionInfo() {Loader = PageLoader, ContentType = "text/html"}},
        };
    }

    private ResponsePacket ImageLoader(string fullPath, string ext, ExtensionInfo extInfo)
    {

        if (!File.Exists(fullPath))
        {
            return new ResponsePacket() { Error = Server.ServerError.FileNotFound };
        }

        using (FileStream fStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
        using (BinaryReader br = new BinaryReader(fStream))
        {
            return new ResponsePacket()
            {
                Data = br.ReadBytes((int)fStream.Length),
                ContentType = extInfo.ContentType
            };
        }
    }


    private ResponsePacket FileLoader(string fullPath, string ext, ExtensionInfo extInfo)
    {
        string text = File.ReadAllText(fullPath);
        return new ResponsePacket()
        {
            Data = Encoding.UTF8.GetBytes(text),
            ContentType = extInfo.ContentType,
            Encoding = Encoding.UTF8
        };
    }

    private ResponsePacket PageLoader(string fullPath, string ext, ExtensionInfo extInfo)
    {
        if (fullPath == WebsitePath)
        {
            return Route("GET", "/index.html", null);
        }

        fullPath = Path.Combine(WebsitePath, "Pages", fullPath.RightOf(WebsitePath).TrimStart('\\', '/'));

        if (String.IsNullOrEmpty(ext))
        {
            fullPath += ".html";
        }

        if (!File.Exists(fullPath))
        {
            return new ResponsePacket() { Error = Server.ServerError.FileNotFound };
        }

        return FileLoader(fullPath, ext, extInfo);

    }

    public ResponsePacket Route(string verb, string path, Dictionary<string, string> kvParams)
    {
    if (path == "/")
    {
        path = "/index.html";
    }

    string ext = path.RightOf('.');
    ResponsePacket response = null;
    verb = verb.ToLower();

    // Look for a matching custom route
    Route matchedRoute = routes.SingleOrDefault(r => r.Verb.ToLower() == verb && r.Path == path);
    if (matchedRoute != null){
        string redirect = matchedRoute.Action(kvParams);

        if (!string.IsNullOrEmpty(redirect)){
            return new ResponsePacket() { Redirect = redirect };
        }
        // If no redirect, fall through to regular file load
    }

    // Fallback to static file loading
    if (extFolderMap.TryGetValue(ext, out ExtensionInfo extInfo)){
        string fullPath = Path.Combine(WebsitePath, path.TrimStart('/').Replace("/", "\\"));
        response = extInfo.Loader(fullPath, ext, extInfo);
    }
    else{
        response = new ResponsePacket() { Error = Server.ServerError.UnknownType };
    }
    return response;
    }
    
    public void AddRoute(Route route){
        routes.Add(route);
    }

}
