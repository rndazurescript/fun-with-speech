# Microphone to text transcription to speaker output 

Simple application that uses the `StartContinuousRecognitionAsync` to transcribe text from microphone.
The text is converted into audio using the `SpeakTextAsync` which is played on the default speaker.
The speech-to-text recognizer is stopped before playing the audio, to avoid recognizer *listening* in the synthesized voice, causing infinite loop.


Read more [about text-to-speech service](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/get-started-text-to-speech).

## Requirements

You will need a [speech service](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/overview). 

In this sample we are using local docker container but you can use the speech service directly by using `SpeechConfig.FromSubscription` instead of the `SpeechConfig.FromHost` used in this sample.

To spin up the local containers [follow official guidance](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/speech-container-howto). 

You will need a local speech-to-text container listening to local port 5000:
```
docker run --rm -it -p 5000:5000 --memory 4g --cpus 4 mcr.microsoft.com/azure-cognitive-services/speechservices/speech-to-text:latest Eula=accept Billing=https://{region}.api.cognitive.microsoft.com/sts/v1.0/issuetoken ApiKey={speech service key}
```
and a local [neural-text-to-speech](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/text-to-speech) container listening to local port 5001:
```
docker run --rm -it -p 5001:5000 --memory 12g --cpus 6 mcr.microsoft.com/azure-cognitive-services/speechservices/neural-text-to-speech:latest Eula=accept Billing=https://{region}.api.cognitive.microsoft.com/sts/v1.0/issuetoken ApiKey={speech service key}
```
