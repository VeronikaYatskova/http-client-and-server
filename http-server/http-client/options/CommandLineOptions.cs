using CommandLine;

namespace http_server.options
{
    public class CommandLineOptions
    {
        [Option(shortName: 'm', longName: "method", Required = true, HelpText = "Http Method")]
        public string? Method { get; set; }

        [Option(shortName: 'u', longName: "server-url", Required = true, HelpText = "Server Connection string")]
        public string? URL { get; set; }

        [Option(longName: "file-path", Required = false, HelpText = "File to upload to server")]
        public string? FilePath { get; set; }  
    }
}
