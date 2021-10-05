using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System;
using System.Threading.Tasks;

namespace mic2text2speech
{
    class Program
    {
        public static async Task RunProcess()
        {
            // Creates an instance of a speech config using local containers
            // https://docs.microsoft.com/azure/cognitive-services/speech-service/speech-container-howto
            var speech2textConfig = SpeechConfig.FromHost(new Uri("ws://localhost:5000"));
            // You can also use the http://localhost:5000 binding
            var text2speechConfig = SpeechConfig.FromHost(new Uri("http://localhost:5001"));
            // The following is the voice downloaded with the latest tag.
            text2speechConfig.SpeechSynthesisLanguage = "en-US";
            text2speechConfig.SpeechSynthesisVoiceName = "en-US-AriaNeural";
            // To debug issues:
            // text2speechConfig.SetProperty(PropertyId.Speech_LogFilename, @"c:\tmp\mic2text2speech.log");
            // If you want to use an Azure subscription directly, use:
            // SpeechConfig.FromSubscription("YourSubscriptionKey", "YourServiceRegion");

            using (var recognizer = new SpeechRecognizer(speech2textConfig, AudioConfig.FromDefaultMicrophoneInput()))
            {
                using (var synthesizer = new SpeechSynthesizer(text2speechConfig, AudioConfig.FromDefaultSpeakerOutput()))
                {
                    Console.WriteLine("Say something and pause. Computer will repeat and stop.");
                    var stopRecognition = new TaskCompletionSource<int>();
                    var stopSpeechService = new TaskCompletionSource<int>();

                    synthesizer.SynthesisCanceled += (s, e) =>
                    {
                        Console.WriteLine($"Synthesis Canceled: {e.Result.Reason}");
                        stopSpeechService.TrySetResult(-1);
                    };

                    synthesizer.SynthesisCompleted += (s, e) =>
                    {
                        Console.WriteLine($"Synthesis Completed: {e.Result.Reason}");
                        stopSpeechService.TrySetResult(0);
                    };

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
                            await recognizer.StopContinuousRecognitionAsync();
                            // Repeat what you heard
                            await synthesizer.StartSpeakingTextAsync(e.Result.Text);
                            // We could use SSML with the voice downloaded using the latest tag in the docker image.
                            // string SPEECH_TEMPLATE = "<speak version=\"1.0\" xml:lang=\"en-US\"><voice name=\"en-US-AriaNeural\">{0}</voice></speak>";
                            // await synthesizer.SpeakSsmlAsync(string.Format(SPEECH_TEMPLATE,e.Result.Text));
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
                    Task.WaitAll(new[] { stopRecognition.Task, stopSpeechService.Task });
                    // Empty the speech to text buffer
                    await synthesizer.StopSpeakingAsync();
                }
            }
        }

        static async Task Main()
        {
            await RunProcess();
            Console.WriteLine("Please press <Return> to continue.");
            Console.ReadLine();
        }
    }
}
