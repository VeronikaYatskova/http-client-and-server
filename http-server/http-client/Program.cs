using System.Net;
using CommandLine;
using http_server.options;

class Program 
{
    static HttpClient client = new HttpClient();
    static string[] serverUrls = new string [] { "http://localhost:8000/" };
    static string cliPath = "";
    static string cliUrl = "";

    static async Task Main(string[] args)
    {
        await Parser.Default.ParseArguments<CommandLineOptions>(args)
            .MapResult(async (CommandLineOptions opts) =>
            {
                try
                {
                    await HandleRequest(opts);
                    return 1;
                }
                catch
                {
                    return -3;
                }
            },
            errs => Task.FromResult(-1));
    }

    private static async Task HandleRequest(CommandLineOptions options)
    {
        cliUrl = options.URL!;
        
        if (options.Method == "GET")
        {
            await SendGetRequest();
        }
        else if (options.Method == "POST")
        {
            if (options.FilePath is null)
                Console.WriteLine("File Path is required for post request.");
            else
            {
                cliPath = options.FilePath;
                await SendPostRequest(cliPath);
            }
        }   
        else if (options.Method == "OPTIONS")
        {
            await SendOptionsRequest();
        }     
    }

    private static async Task SendGetRequest()
    {
        try
        {
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, cliUrl);
            request.Headers.Add("Access-Control-Allow-Origin", "http://localhost:8000");
            request.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");

            using HttpResponseMessage response = await client.SendAsync(request);

            Console.WriteLine("Response Status Code: " + response.StatusCode);
            if (response.StatusCode == HttpStatusCode.NotFound)
                Console.WriteLine("No files");
            else
                Console.WriteLine(await response.Content.ReadAsStringAsync());
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private static async Task SendPostRequest(string filePath)
    {
        try
        {
            var fileName = Path.GetFileName(filePath);
            var name = Path.GetFileNameWithoutExtension(filePath);

            using var multipartFormContent = new MultipartFormDataContent();
            var fileStreamContent = new StreamContent(File.OpenRead(filePath));
            multipartFormContent.Add(fileStreamContent, filePath);
            
            using var response = await client.PostAsync(cliUrl, multipartFormContent);
            var responseText = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseText);
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private static async Task SendOptionsRequest()
    {
        try
        {
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Options, cliUrl);
            using HttpResponseMessage response = await client.SendAsync(request);
            request.Headers.Add("Access-Control-Allow-Origin", "http://localhost:8000");
            request.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");

            Console.WriteLine(await response.Content.ReadAsStringAsync());
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}
