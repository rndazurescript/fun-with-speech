using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatchAudioTranscription
{
    internal class TranscriptionResult
    {
        public string Text { get; set; }
        public double Confidence { get; internal set; }
        public TimeSpan StartTime { get; internal set; }
        public TimeSpan Duration { get; internal set; }
    }
}
