using System;
using CommandLine;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace BatchAudioTranscription
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            return await Parser.Default.ParseArguments<CliOptions>(args)
               .MapResult(
                async (CliOptions opts) =>
                {
                    try
                    {
                        if (!Directory.Exists(opts.Input))
                        {
                            Console.WriteLine($"Input folder '{opts.Input}' doesn't exist");
                            return -2;
                        }
                        // We have the parsed arguments, so let's just pass them down
                        return await ProcessMediaFiles(opts.Input, opts.Locale, opts.Output, opts.Key, opts.Region, opts.Verbose);
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

        private static async Task<int> ProcessMediaFiles(string inputFolder, string locale, string output, string speechServiceKey, string speechServiceRegion, bool verbose)
        {
            // Create the speeach config we are going to use for the files
            var config = SpeechConfig.FromSubscription(speechServiceKey, speechServiceRegion);
            config.SpeechRecognitionLanguage = locale;
            config.OutputFormat = OutputFormat.Detailed;

            var results = new List<FileResult>();
            var allGood = true;
            // The for each file loop
            var fileNames = Directory.GetFiles(inputFolder, "*.*", SearchOption.AllDirectories);
            foreach (var fileName in fileNames)
            {
                // Call the ProcessMediaFile
                try
                {
                    var fileResult = new FileResult(fileName);
                    results.Add(fileResult);
                    fileResult.FileLoaded = await ProcessMediaFile(config, fileResult, verbose);
                    if (!fileResult.FileLoaded)
                    {
                        Console.WriteLine($"Could not load file '{fileName}' as an audio file.");
                        Console.WriteLine("Try installing missing codecs, like gstreamer.");
                        allGood = false;
                    }
                }
                catch (Exception ex)
                {
                    // Catch exception in case it's not an audio file or it's corrupted.
                    Console.WriteLine($"Error processing file {fileName}!");
                    Console.WriteLine($"Error: {ex.Message}");
                    allGood = false;
                }
            }
            // TODO: Create an excel file and save the results


            return allGood ? 0 : -4;
        }

        static async Task<bool> ProcessMediaFile(SpeechConfig config, FileResult inputFile, bool verbose)
        {
            // Task to complete the recognision. Set in code when 
            // the inputFolder file finishes.
            var stopRecognition = new TaskCompletionSource<int>();

            Console.Write($"Processing file {inputFile.File.FullName} : ");
            // Create a mono audio inputFolder stream from the media file we want to process
            //https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/how-to-use-audio-inputFolder-streams
            var mediaFile = new MediaFileAudioStream(inputFile.File.FullName);
            var audioInput = AudioConfig.FromStreamInput(mediaFile);
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
                    var best = SpeechRecognitionResultExtensions.Best(e.Result).First();
                    var text = best.Text;
                    var confidence = best.Confidence;
                    inputFile.Results.Add(new TranscriptionResult()
                    {
                        Text = text,
                        Confidence = confidence,
                        StartTime = startOffset,
                        Duration = e.Result.Duration
                    });
                    if (verbose) Console.WriteLine($"Recognized: '{text}' with confidence {confidence}");
                }
                else if (e.Result.Reason == ResultReason.NoMatch)
                {
                    if (verbose) Console.WriteLine($"NOMATCH: Speech could not be recognized at {startOffset}.");
                }
            };

            recognizer.Canceled += (s, e) =>
            {
                if (verbose) Console.WriteLine($"CANCELED: Reason={e.Reason}");
                if (e.Reason == CancellationReason.EndOfStream)
                {
                    if (verbose) Console.WriteLine("File processing completed.");
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
                if (verbose) Console.WriteLine("\n    Session stopped event.");
                stopRecognition.TrySetResult(0);
            };

            var rec = recognizer.StartContinuousRecognitionAsync();

            // Waits for completion.The recognition should have already stopped
            // with Reason=EndOfStream
            var result = await stopRecognition.Task;
            
            if (mediaFile.AudioStreamLoaded)
            {
                Console.WriteLine("Done");
            }
            else
            {
                Console.WriteLine("Failed");
            }
            // Return whether we managed to load the file or not
            return mediaFile.AudioStreamLoaded;
        }
    }



}