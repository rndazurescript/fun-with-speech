using CommandLine;

namespace BatchAudioTranscription
{
    class CliOptions
    {
        [Option('i', "input", Required = true, HelpText = "The folder that contains the media files to process.\r\nE.g. c:\\temp\\")]
        public string Input { get; set; }

        [Option('k', "key", Required = true, HelpText = "This is your Speech Service key found in the Keys and Endpoint blade in Azure portal")]
        public string Key { get; set; }

        [Option('g', "region", Required = true, HelpText = "This is your Speech Service Location/Region found in the Keys and Endpoint blade in Azure portal")]
        public string Region { get; set; }

        [Option('l', "locale", Required = false, Default = "en-US", HelpText = "The locale to use while processing the files.\r\nDefault locale en-US.")]
        public string Locale { get; set; }

        [Option('o', "output", Required = false, Default = "output.xlsx", HelpText = "The output Excel file to write transcriptions.\r\nIf not specified, an output.xlsx will be created next to the executable.")]
        public string Output { get; set; }

        [Option('v', "verbose", Required = false, Default = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }


    }
}
