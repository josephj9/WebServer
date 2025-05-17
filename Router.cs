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

public class ExtensionInfo
{
    public Func<string, string, ExtensionInfo, ResponsePacket> Loader { get; set; }
    public string ContentType { get; set; }
}

public class ResponsePacket
{
    public string Redirect { get; set; }
    public byte[] Data { get; set; }
    public string ContentType { get; set; }
    public Encoding Encoding { get; set; }
    public Server.ServerError Error { get; set; }

}

public class Router
{
    public string WebsitePath { get; set; }

    private Dictionary<string, ExtensionInfo> extFolderMap;

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

        if (!File.Exists(fullPath)){
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
        if (!File.Exists(fullPath)){
                return new ResponsePacket() { Error = Server.ServerError.FileNotFound };
            }

        if (fullPath == WebsitePath)
        {
            return Route("GET", "/index.html", null);
        }

        if (String.IsNullOrEmpty(ext))
        {
            fullPath += ".html";
        }

        fullPath = Path.Combine(WebsitePath, "Pages", fullPath.RightOf(WebsitePath).TrimStart('\\', '/'));
        return FileLoader(fullPath, ext, extInfo);
    }

    public ResponsePacket Route(string verb, string path, Dictionary<string, string> kvParams)
    {   
        if (path == "/"){
            path = "/index.html";
        }

        string ext = path.RightOf('.');
        if (extFolderMap.TryGetValue(ext, out ExtensionInfo extInfo)){
            string fullPath = Path.Combine(WebsitePath, path.TrimStart('/').Replace("/", "\\"));
            return extInfo.Loader(fullPath, ext, extInfo);
        }
        else{
            return new ResponsePacket() { Error = Server.ServerError.UnknownType };
        }
    }
}
