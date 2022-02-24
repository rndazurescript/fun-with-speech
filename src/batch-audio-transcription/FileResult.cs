using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace BatchAudioTranscription
{
    //have a file name; includes the list of transcriptionResultp
    internal class FileResult
    {

        public FileResult(string fullFileName)
        {
            this.File = new FileInfo(fullFileName);
            this.Results = new List<TranscriptionResult>();
        }

        public FileInfo File { get; }

        public List<TranscriptionResult> Results { get; }

        public bool FileLoaded { get; set; }
    }

    
}
