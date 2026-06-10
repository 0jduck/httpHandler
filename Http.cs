// Http.cs

// ---- setup ----
// using
using System;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Collections.Generic;

// namespace
namespace App;

// ---- HttpServer ----
public class HttpServer
{
  // Setup
  private readonly int _port;
  public HttpServer(string port)
  {
    int portSafe;
    if (int.TryParse(port, out portSafe)) _port = portSafe;
    else
    {
      Program.Log("BOOT", "Port issue, defualting to random");
      _port = 0;
    }
  }
  // Run
  public async Task Run()
  {
    Program.Log("BOOT", "Starting");

    TcpListener listener = new TcpListener(IPAddress.Any, _port);
    listener.Start();
    int port = ((IPEndPoint)listener.LocalEndpoint).Port;
    
    Program.Log("BOOT", $"Running on port {port}");
    Program.Log("BOOT", $"URL: http://localhost:{port}");
    
    while (true)
    {
      TcpClient client = await listener.AcceptTcpClientAsync();
      _ = Task.Run(() => Handler(client));
    }
  }
  // Handler
  private async Task Handler(TcpClient client)
  {
    using NetworkStream stream = client.GetStream();

    try {
      // read
      byte[] buffer = new byte[1024];
      int bufferRead = await stream.ReadAsync(buffer, 0, buffer.Length);
      if (bufferRead == 0) return;

      string data = Encoding.UTF8.GetString(buffer, 0, bufferRead);

      // split in to parts
      int ctrfIndex = data.IndexOf("\r\n", StringComparison.Ordinal);
      if (ctrfIndex < 0) return;

      string requestLine = data[..ctrfIndex];
      string rest = data[(ctrfIndex+2)..];
      
      string[] requestParts = requestLine.Split(' ');
      if (requestParts.Length < 3) return;

      string method = requestParts[0];
      string path = requestParts[1].Split('?')[0];
      string protocol = requestParts[2];

      string[] sections = rest.Split("\r\n\r\n", 2);
      string sectionHeader = sections[0];
      string sectionBody = sections.Length > 1 ? sections[1] : "";

      // format headers
      Dictionary<string, string> headers = new Dictionary<string, string>();
      if (!string.IsNullOrEmpty(sectionHeader))
      {
        foreach (string line in sectionHeader.Split("\r\n"))
        {
          int colon = line.IndexOf(':');
          if (colon < 0) continue;

          string key = line[..colon].Trim();
          string value = line[(colon+1)..].Trim();
          headers[key] = value;
        }
      }
      
      // log
      if (method=="GET") Program.Log(method, path);
      else Program.Log("ERROR", $"{method} request on {path}");

      // route
      byte[] response;
      if (method=="GET") response = HandlerGet(path);
      else response = HandlerInvalid("405 Method Not Allowed");
      
      foreach (List<string> redirect in Program.redirects)
      {
        if (redirect[0]==path) HandlerRedirect(redirect[1]);
      }
      // responde
      await stream.WriteAsync(response);
      await stream.FlushAsync();
    }
    catch (Exception ex) when (ex is IOException or InvalidOperationException)
    {
      // Probobaly client dissconect before response, ignore
    }
    finally
    {
      client.Close();
    }
  }
  // handler reply
  private static byte[] HandlerReply(string status, List<string> headers, byte[] body)
  {
    string headersRaw = "";
    foreach (string header in headers)
    {
      headersRaw += ($"{header}\r\n");
    }
    string responseLine = $"HTTP/1.1 {status}\r\n{headersRaw}\r\n";
    byte[] head = Encoding.UTF8.GetBytes(responseLine);
    return head.Concat(body).ToArray();
  }
  // handler get
  private static byte[] HandlerGet(string path)
  {
    byte[] body = HandlerRead(path);
    
    if (body.Length != 0)
    {
      List<string> headers = new List<string>();
      headers.Add($"Content-Length: {body.Length}");
      headers.Add($"Content-Type: {HandlerMimeType(path)}");
      return HandlerReply("200 OK", headers, body);
    }
    else return HandlerInvalid("404 Not Found");
  }
  // handler post
  private static byte[] HandlerRedirect(string path)
  {
    byte[] body = new byte[0];

    List<string> headers = new List<string>();
    headers.Add($"Location: {path}");

    return HandlerReply("302 Found", headers, body);
  }
  // handler invalid
  private static byte[] HandlerInvalid(string error)
  {
    byte[] body = new byte[0];
    Program.Log("ERROR", Program.badPathFile);
    if (error=="404 Not Found") body = HandlerRead(Program.badPathFile);
    
    List<string> headers = new List<string>();
    headers.Add($"Content-Length: {body.Length}");
    headers.Add($"Content-Type: text/html");

    return HandlerReply(error, headers, body);
  }
  // file work
  private static byte[] HandlerRead(string path)
  {
    string pathSafe = path.TrimStart('/');
    string file = Path.Combine(Program.directory, pathSafe);

    if (!Path.GetFullPath(file).StartsWith(Path.GetFullPath(Program.directory))) {
      Program.Log("ERROR", $"Path traversal attempt: {path}");
      return new byte[0];
    }

    if (File.Exists(file)) return File.ReadAllBytes(file);
    if (File.Exists(file + ".html")) return File.ReadAllBytes(file + ".html");
    if (File.Exists(Path.Combine(file, "index.html"))) return File.ReadAllBytes(Path.Combine(file, "index.html"));
    Program.Log("ERROR", $"Bad path requested {file}");
    return new byte[0];
  }
  private static Dictionary<string, string> mimeType = new Dictionary<string, string> {
    [".txt"] = "text/plain",
    [".html"] = "text/html",
    [".css"] = "text/css",
    [".js"] = "text/javascript",
    [".json"] = "application/json",
    [".png"] = "image/png",
    [".jpg"] = "image/jpeg",
    [".jpeg"] = "image/jpeg",
    [".ico"] = "image/x-icon",
    [".gif"] = "image/gif"
  };
  private static string HandlerMimeType(string path)
  {
    return mimeType.TryGetValue(Path.GetExtension(path), out string? mime) ? mime : "text/html";
  }
}



