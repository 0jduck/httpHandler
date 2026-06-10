// Prgoram.cs

// ---- setup ----
// using
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Collections.Generic;

// namespace
namespace App;

// ---- Program ----
public class Program
{
  // variables
  public static string port = "0";
  public static string directory = "";
  public static string badPathFile = "";
  public static List<List<string>> redirects = new List<List<string>>();

  // Main
  public static async Task Main(string[] args)
  {
    string directoryPath = "";
    if (args.Length > 0) directoryPath = args[0];
    directory = Path.Combine(Program.directory.StartsWith("/") ? AppContext.BaseDirectory : "", directoryPath);
    Log("BOOT", $"Serving from: {directory}");

    Config();

    if (args.Length > 1) port = args[1];

    HttpServer httpServer = new HttpServer(port);

    await httpServer.Run();
  }
  // config file
  public static void Config()
  {
    string configFile = Path.Combine(directory, "serve");
    if (File.Exists(configFile))
    {
      Log("BOOT", $"Reading config file {configFile}");
      foreach (string lineRaw in File.ReadLines(configFile))
      {
        try
        {
          string line = lineRaw.Split('#')[0];
          if (line=="") continue;
          string[] lineParts = line.Split(' ', 2);

          if (lineParts[0]=="port") port = lineParts[1];
          else if (lineParts[0]=="badPath") badPathFile = lineParts[1];
          else if (lineParts[0]=="redirects")
          {
            string[] values = lineParts[1].Split('"');
            string target = values[1];
            string value = values[3];
            redirects.Add(new List<string> { target, value });
          }
        }
        catch
        {
          Log($"ERROR", "Issue with config file, line: {lineRaw}");
          continue;
        }
      }
    }
    else Log("BOOT", $"No config file found at {configFile}, using defualts");
  }
  // Log
  static readonly Dictionary<string, string> logPrefix = new Dictionary<string, string> {
    ["ERROR"] = "31",
    ["BOOT"] = "32",
    ["GET"] = "34",
    ["POST"] = "33"
  };
  public static void Log(string prefix, string line)
  {
    string prefixFull = $"\x1B[{logPrefix[prefix]}m[{prefix}]\x1B[0m";
    string time = DateTime.Now.ToString("HH:mm:ss");
    string timeFull = $"\x1B[90m[{time}]\x1B[0m";
    Console.WriteLine($"{timeFull} {prefixFull} {line}"); 
  }
}


