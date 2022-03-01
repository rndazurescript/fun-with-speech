using System;
using CommandLine;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using ClosedXML.Excel;
using System.Data;

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
                    var audiofileresult = new FileResult(fileName);
                    results.Add(audiofileresult);
                    audiofileresult.FileLoaded = await ProcessMediaFile(config, audiofileresult, verbose);
                    if (!audiofileresult.FileLoaded)
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

            // Create an excel file and save the results
            SaveResultsInXL(output, results);

            return allGood ? 0 : -4;
        }

        private static void SaveResultsInXL(string output, List<FileResult> results)
        {
            // We are using the https://closedxml.github.io/ClosedXML/
            // for excel manipulation in .net 6
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("TranscriptionResults");
               
                DataTable table = new DataTable();

                table.Columns.Add("File name", typeof(string));
                table.Columns.Add("Folder", typeof(string));
                table.Columns.Add("Audio loaded", typeof(bool));
                table.Columns.Add("Timestamp", typeof(string));
                table.Columns.Add("Duration (ms)", typeof(int));
                table.Columns.Add("Transcription Text", typeof(string));
                table.Columns.Add("Confidence", typeof(double));


                foreach (var afr in results)
                {
                    if (!afr.FileLoaded)
                    {
                        // If the file is not an audio file that could be loaded by the MediaFileAudioStream
                        // Add it in the table and mark it as "Audio loaded" -> False 
                        table.Rows.Add(afr.File.Name, afr.File.DirectoryName, false, String.Empty, 0, String.Empty, 0);
                    }
                    else
                    {
                        // The audio file did load, so "Audio loaded" -> true
                        if (afr.Results.Count == 0)
                        {
                            // If we didn't transcribe any result, add a new line in excel
                            table.Rows.Add(afr.File.Name, afr.File.DirectoryName, true, String.Empty, 0, String.Empty, 0);
                        }
                        else
                        {
                            // Add one line in excel for each transcription we got
                            foreach (var tr in afr.Results)
                            {
                                table.Rows.Add(afr.File.Name, afr.File.DirectoryName, true, tr.StartTime.ToString(@"hh\:mm\:ss\,fff"), tr.Duration.TotalMilliseconds, tr.Text, tr.Confidence);
                            }
                        }
                    }
                }

                var tableWithData = worksheet.Cell(1, 1).InsertTable(table.AsEnumerable());

                workbook.SaveAs(output);
            }
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
                    // Document that something went wrong.
                    // For example you may be getting AuthenticationFailure
                    // which may be due to the pricing tier you are using.
                    inputFile.Results.Add(new TranscriptionResult()
                    {
                        Text = $"{e.ErrorCode}:{e.ErrorDetails}",
                        Confidence = 0,
                        StartTime = TimeSpan.FromMinutes(0),
                        Duration = TimeSpan.FromMinutes(0)
                    });
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