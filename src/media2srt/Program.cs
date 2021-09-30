using CommandLine;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace media2srt
{
    class Program
    {

        static async Task<int> Main(string[] args)
        {
            return await Parser.Default.ParseArguments<CliOptions>(args)
               .MapResult(
                async (CliOptions opts) =>
                {
                    try
                    {
                        if (string.IsNullOrEmpty(opts.Output))
                        {
                            var inputFile = new FileInfo(opts.Input);
                            opts.Output = $"{inputFile.FullName.Substring(0, inputFile.FullName.Length - inputFile.Extension.Length)}.srt";
                        }
                        // We have the parsed arguments, so let's just pass them down
                        return await ProcessMediaFile(opts.Input, opts.Output, opts.Verbose);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error! {ex.Message}");
                        return -3; // Not handled error
                    }
                },
               errs => Task.FromResult(-1) // Invalid arguments
              );
        }

        static async Task<int> ProcessMediaFile(string inputFile, string outputFile, bool verbose)
        {
            var config = SpeechConfig.FromHost(new Uri("ws://localhost:5000"));
            var output = new TranscriptionResults();

            var stopRecognition = new TaskCompletionSource<int>();
            // Create a mono audio input stream from the media file we want to process
            //https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/how-to-use-audio-input-streams
            var audioInput = AudioConfig.FromStreamInput(new MediaFileAudioStream(inputFile));
            var recognizer = new SpeechRecognizer(config, audioInput);

            recognizer.Recognizing += (s, e) =>
            {
                if (verbose) Console.WriteLine($"RECOGNIZING: Text={e.Result.Text}");
            };

            recognizer.Recognized += (s, e) =>
            {
                TimeSpan startOffset = TimeSpan.FromTicks(e.Result.OffsetInTicks);
                if (e.Result.Reason == ResultReason.RecognizedSpeech)
                {
                    var newline = output.AddTranscription(startOffset, e.Result.Duration, e.Result.Text);
                    Console.WriteLine(newline);
                }
                else if (e.Result.Reason == ResultReason.NoMatch)
                {
                    Console.WriteLine($"NOMATCH: Speech could not be recognized at {startOffset}.");
                }
            };

            recognizer.Canceled += (s, e) =>
            {
                if (verbose) Console.WriteLine($"CANCELED: Reason={e.Reason}");
                if (e.Reason == CancellationReason.EndOfStream)
                {
                    Console.WriteLine("File processing completed.");
                }

                if (e.Reason == CancellationReason.Error)
                {
                    Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                    Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                    Console.WriteLine($"CANCELED: Did you update the speech key and location/region info?");
                }
                stopRecognition.TrySetResult(0);
            };

            recognizer.SessionStopped += (s, e) =>
            {
                Console.WriteLine("\n    Session stopped event.");
                stopRecognition.TrySetResult(0);
            };

            var rec = recognizer.StartContinuousRecognitionAsync();

            // Waits for completion.The recognition should have already stopped
            // with Reason=EndOfStream
            var result = await stopRecognition.Task;
            // Store all text in the output file.
            File.AppendAllLines(outputFile, output);
            return result;
        }

    }
}
