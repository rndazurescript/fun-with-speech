using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace media2srt
{
    public class TranscriptionResults: List<TranscriptionResult>, IEnumerable<string>
    {
       
        public TranscriptionResult AddTranscription(TimeSpan start, TimeSpan duration, string text)
        {
            var output = new TranscriptionResult()
            {
                Start = start,
                Duration = duration,
                Text = text
            };
            this.Add(output);
            return output;
        }

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            int lineNumber = 1;

            for (int i = 0; i < this.Count; i++)
            {
                yield return $"{lineNumber}{Environment.NewLine}{this[i]}";
                lineNumber++;
            }
        }
    }
}
