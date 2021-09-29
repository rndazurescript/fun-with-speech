using Microsoft.CognitiveServices.Speech;
using System;
using System.Threading.Tasks;

namespace mic2console
{
    class Program
    {
        public static async Task RecognizeSpeechAsync()
        {
            // Creates an instance of a speech config using local containers
            // https://docs.microsoft.com/azure/cognitive-services/speech-service/speech-container-howto
            var config = SpeechConfig.FromHost(new Uri("ws://localhost:5000"));
            // If you want to use an Azure subscription directly, use:
            // var config = SpeechConfig.FromSubscription("YourSubscriptionKey", "YourServiceRegion");

            // Optionally, change default language. Get list of supported languages
            // https://docs.microsoft.com/azure/cognitive-services/speech-service/language-support#speech-to-text
            // config.SpeechRecognitionLanguage = "en-US";

            // To enable logs
            // https://docs.microsoft.com/azure/cognitive-services/speech-service/how-to-use-logging
            // config.SetProperty(PropertyId.Speech_LogFilename, @"c:\tmp\mic2console.log");


            using (var recognizer = new SpeechRecognizer(config))
            {
                Console.WriteLine("Say something... Say a sentence that contains the word stop to stop transcribing.");
                var stopRecognition = new TaskCompletionSource<int>();
                recognizer.Recognizing += (s, e) =>
                {
                    Console.WriteLine($"RECOGNIZING: Text={e.Result.Text}");
                };

                recognizer.Recognized += async (s, e) =>
                {
                    if (e.Result.Reason == ResultReason.RecognizedSpeech)
                    {
                        TimeSpan startOffset = TimeSpan.FromTicks(e.Result.OffsetInTicks);
                        TimeSpan endOffset = startOffset.Add(TimeSpan.FromMilliseconds(e.Result.Duration.TotalMilliseconds));
                        Console.WriteLine($"{startOffset} - {endOffset}> {e.Result.Text}");
                        if (e.Result.Text.Contains("stop", StringComparison.OrdinalIgnoreCase))
                        {
                            await recognizer.StopContinuousRecognitionAsync();
                        }
                    }
                    else if (e.Result.Reason == ResultReason.NoMatch)
                    {
                        Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                    }
                };

                recognizer.Canceled += (s, e) =>
                {
                    Console.WriteLine($"CANCELED: Reason={e.Reason}");

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

                await recognizer.StartContinuousRecognitionAsync();
                // Waits for completion. Use Task.WaitAny to keep the task rooted.
                Task.WaitAny(new[] { stopRecognition.Task });
            }
        }

        static async Task Main()
        {
            await RecognizeSpeechAsync();
            Console.WriteLine("Please press <Return> to continue.");
            Console.ReadLine();
        }
    }
}
