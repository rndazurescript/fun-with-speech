using Microsoft.CognitiveServices.Speech.Audio;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.IO;

namespace media2text
{
    internal class MediaFileAudioStream : PullAudioInputStreamCallback
    {
        private const int SAMPLE_RATE = 16000;
        private string filePath;
        private MemoryStream audioStream;

        internal MediaFileAudioStream(string filePath)
        {
            this.filePath = filePath;
        }

        private MemoryStream getStream()
        {
            if (audioStream == null)
            {
                using (var inputReader = new MediaFoundationReader(this.filePath))
                {
                    var sampleProvider = inputReader.ToSampleProvider();
                    // Convert to mono if needed
                    if (inputReader.WaveFormat.Channels > 1)
                    {
                        sampleProvider = new StereoToMonoSampleProvider(sampleProvider);
                    }
                    // Change sample rate to one supported by the Speech to text service.
                    sampleProvider = new WdlResamplingSampleProvider(sampleProvider, SAMPLE_RATE);

                    audioStream = new MemoryStream();
                    WaveFileWriter.WriteWavFileToStream(audioStream, sampleProvider.ToWaveProvider16());
                    audioStream.Position = 0;
                }
            }
            return audioStream;
        }

        public override int Read(byte[] dataBuffer, uint size)
        {
            return getStream().Read(dataBuffer,0, (int)size);
        }
        public override void Close()
        {
            // close and cleanup resources.
            if (audioStream != null)
            {
                audioStream.Close();
            }
            base.Close();
        }
    }

}
