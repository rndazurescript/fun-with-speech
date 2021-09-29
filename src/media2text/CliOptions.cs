using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace media2text
{
    class CliOptions
    {
        [Option('i', "input", Required = true, HelpText = "The input media file to process")]
        public string Input { get; set; }

        [Option('o', "output", Required = false, HelpText = "The output text file to write transcription.\r\nIf not specified, a .txt file with the same name as the input will be created.")]
        public string Output { get; set; }

        [Option('v', "verbose", Required = false, Default = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }
    }
}
