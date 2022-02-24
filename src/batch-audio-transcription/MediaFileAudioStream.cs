using Microsoft.CognitiveServices.Speech.Audio;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace BatchAudioTranscription
{
    internal class MediaFileAudioStream : PullAudioInputStreamCallback
    {
        // Supported sample rate by speech to text service
        // https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/how-to-use-audio-input-streams
        // 16000 samples per second as per documentation (can be 8000 as well).
        private const int SAMPLE_RATE = 16000;
        private string filePath;
        private MemoryStream audioStream = null;
        private bool audioStreamLoaded = false;

        internal MediaFileAudioStream(string filePath)
        {
            this.filePath = filePath;
            this.audioStreamLoaded = false;
        }

        public bool AudioStreamLoaded
        {
            get { return audioStreamLoaded; }
        }

        /// <summary>
        /// Returns a memory stream with the audio file.
        /// </summary>
        /// <returns></returns>
        private MemoryStream getStream()
        {
            if (audioStream == null)
            {
                try
                {
                    // If this is the first time we call this method, let's create the memory
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
                        // 16Bits
                        WaveFileWriter.WriteWavFileToStream(audioStream, sampleProvider.ToWaveProvider16());

                        // Reset position of the stream to the beginning of the audio track.
                        audioStream.Position = 0;

                        // Success loading the file
                        audioStreamLoaded = true;
                    }
                }
                catch
                {
                    // Failed to load the file
                    audioStreamLoaded = false;
                    return new MemoryStream();
                }
            }
            return audioStream;
        }

        /// <summary>
        /// Method to provide the raw audio data
        /// </summary>
        /// <param name="dataBuffer"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public override int Read(byte[] dataBuffer, uint size)
        {
            return getStream().Read(dataBuffer, 0, (int)size);
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