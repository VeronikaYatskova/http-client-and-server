using System.Net;
using System.Text;
using Serilog;
using Serilog.Events;

namespace http_server
{
    class Program 
    {
        public static HttpListener listener;
        public static string url = "http://localhost:8000/";
        public static int pageViews = 0;
        public static int requestCount = 0;
        public static string currentPage = string.Empty;
        public static string filesPage = "<!DOCTYPE>" +
            "<html>" +
            "  <head>" +
            "    <title>HttpListener Example</title>" +
            "  </head>" +
            "  <body>" +
            "  <form method=\"post\" enctype=\"multipart/form-data\"><input id=\"fileUp\" name=\"fileUpload\" type=\"file\" /><input type=\"submit\" /></form>" +
            "     <table>" +
            "        <tr>" +
            "            <th>Index</th>" +
            "            <th>Name</th>" +
            "        </tr>" +
            "        {0}" +
            "    </table>" +
            "  </body>" +
            "</html>";

        public static string noFilesPage = "<!DOCTYPE>" +
            "<html>" +
            "  <head>" +
            "    <title>HttpListener Example</title>" +
            "  </head>" +
            "  <body>" +
            "   <form method=\"post\" enctype=\"multipart/form-data\"><input id=\"fileUp\" name=\"fileUpload\" type=\"file\" /><input type=\"submit\" /></form>" +
            "   <h2>No files</h2>" +  
            "  </body>" +
            "</html>";

        public static string createdPage = "<!DOCTYPE>" +
            "<html>" +
            "  <head>" +
            "    <title>File is uploaded</title>" +
            "  </head>" +
            "  <body>" +
            "   <h2>File is uploaded</h2>" +
            "  </body>" +
            "</html>";

        public static string files = "";

        public static async Task HandleIncomingConnections(ILogger Log)
        {
            bool runServer = true;
                  
            while (runServer)
            {
                HttpListenerContext ctx = await listener.GetContextAsync();
                
                HttpListenerRequest req = ctx.Request;
                req.Headers.Add("Access-Control-Allow-Origin", "http://localhost:8000");
                req.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");

                HttpListenerResponse resp = ctx.Response;

                Log.Information("Request #: {0}", ++requestCount);
                Log.Information("Request URL: " + req.Url.ToString());
                Log.Information("Request Method: " + req.HttpMethod);
                Log.Information("User Hostname: " + req.UserHostName);

                Console.WriteLine();

                byte[]? data;
                int statusCode = 0;
                
                if (req.HttpMethod == "OPTIONS")
                {
                    var info = $"Access-Control-Allow-Origin: {req.Headers["Access-Control-Allow-Origin"]}\n" +
                               $"Content-Encoding: {req.ContentEncoding}\n" + 
                               $"Access-Control-Allow-Methods: {req.Headers["Access-Control-Allow-Methods"]}\n"; 

                    data = Encoding.Default.GetBytes(info);
                    statusCode = 200;
                }
                else if (req.HttpMethod == "POST")
                {
                    string[] result = new string[3];
                    List<string> fileData = new ();
                    
                    using var body = req.InputStream;
                    using var reader = new System.IO.StreamReader(body, req.ContentEncoding);
                    
                    int i = 0;
                    string? line = string.Empty;

                    while((line = reader.ReadLine()) is not null)
                    {
                        if (i < result.Length)
                            result[i++] = line;    
                        else 
                            fileData.Add(line);
                    }

                    fileData.Remove(fileData[^1]);

                    var splitedRes = result[1].Split(new char[] { ' ', ';', '=', '"' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    var fileNameIndex = splitedRes.FindIndex(el => el == "filename" || el == "name") + 1;
                    var fileName = splitedRes[fileNameIndex];
                    var filePath = "files/" + Path.GetFileName(fileName);
                    
                    Console.WriteLine(filePath);

                    if (!File.Exists(fileName))
                    {
                        statusCode = 400;
                        Console.WriteLine("Path doesn't exist.");
                        data = null;
                    }

                    using FileStream fstream = new FileStream(filePath, FileMode.Create);

                    byte[] buffer = Encoding.Default.GetBytes(fileData.Aggregate((prev, current) => prev + current));
                    
                    await fstream.WriteAsync(buffer, 0, buffer.Length);

                    statusCode = 201;
                    data = Encoding.Default.GetBytes(createdPage);
                }
                else
                {
                    string[] filePaths = Directory.GetFiles(@"files");
                    if (filePaths.Length == 0)
                    {
                        statusCode = 404;
                        data = Encoding.Default.GetBytes(noFilesPage);
                    }
                    else
                    {
                        var fileNames = filePaths.Select(path => Path.GetFileName(path)).OrderBy(name => name).ToArray();

                        for (int i = 0; i < fileNames.Length; i++)
                        {
                            files += "<tr>";
                            files += $"<td>{i + 1}</td>";
                            files += $"<td>{fileNames[i]}</td>";
                            files += "</tr>";
                        }

                        statusCode = 200;
                        data = Encoding.UTF8.GetBytes(String.Format(filesPage, files));
                    }
                }

                resp.ContentType = "text/html";
                resp.ContentEncoding = Encoding.UTF8;
                resp.StatusCode = statusCode;
                resp.ContentLength64 = data.LongLength;
                await resp.OutputStream.WriteAsync(data, 0, data.Length);
                
                Log.Information("Response #" + requestCount);
                Log.Information("Status Code: " + resp.StatusCode);
                Console.WriteLine();

                resp.Close();
            }
        }

        public static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                  .MinimumLevel.Debug()
                  .WriteTo.File("logs/logs.txt")
                  .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information)
                  .CreateLogger(); 

            listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();
            
            Console.WriteLine("Listening for connections on {0}", url);

            await HandleIncomingConnections(Log.Logger);
            listener.Close();
        }
    }
}
