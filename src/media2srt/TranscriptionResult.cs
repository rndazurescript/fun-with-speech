using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace media2srt
{
    public class TranscriptionResult
    {
        public TimeSpan Start { get; set; }
        public TimeSpan Duration { get; set; }
        public string Text { get; set; }

        public override string ToString()
        {
            return $"{this.Start.ToString(@"hh\:mm\:ss\,fff")} --> {this.Start.Add(this.Duration).ToString(@"hh\:mm\:ss\,fff")}{Environment.NewLine}{this.Text}{Environment.NewLine}";
        }
    }
}
