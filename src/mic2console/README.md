# Microphone to console transcription

Simple application that uses the `StartContinuousRecognitionAsync` to continuously transcribe text from microphone.

Say a sentence that contains the `stop` word to cancel the transcription session.

You will need a [speech service](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/overview). 

In this sample we are using local docker container but you can use the speech service directly by using `SpeechConfig.FromSubscription` instead of the `SpeechConfig.FromHost` used in this sample.

To spin up the local container [follow official guidance](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/speech-container-howto). The basic idea is:

```
docker run --rm -it -p 5000:5000 --memory 4g --cpus 4 mcr.microsoft.com/azure-cognitive-services/speechservices/speech-to-text:latest Eula=accept Billing=https://{region}.api.cognitive.microsoft.com/sts/v1.0/issuetoken ApiKey={speech service key}
```
