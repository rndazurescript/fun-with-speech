# Media file to srt subtitles transcription

Simple application that transcribes a media file (tested with mkv files). The purpose of this demo app is to show how to parse files.

Usage:

```
media2srt -i c:\tmp\sample.mkv
```

Executable's parameters:
```
  -i, --input      Required. The input media file to process

  -o, --output     The output text file to write transcription.
                   If not specified, a .srt file with the same name as the input will be created.

  -v, --verbose    (Default: false) Set output to verbose messages.

  --help           Display this help screen.

  --version        Display version information.
```

## Requirements

You will need a [speech service](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/overview). 

In this sample we are using local docker container but you can use the speech service directly by using `SpeechConfig.FromSubscription` instead of the `SpeechConfig.FromHost` used in this sample.

To spin up the local container [follow official guidance](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/speech-container-howto). The basic idea is:

```
docker run --rm -it -p 5000:5000 --memory 4g --cpus 4 mcr.microsoft.com/azure-cognitive-services/speechservices/speech-to-text:latest Eula=accept Billing=https://{region}.api.cognitive.microsoft.com/sts/v1.0/issuetoken ApiKey={speech service key}
```

## Alternatives

Original idea was to extract audio from mkv using ffmpeg or a .net wrapper library:
```
ffmpeg -i video.mkv -acodec pcm_s16le -ac 2 audio.wav
```

Perhaps you can parallelize using https://markheath.net/post/trimming-wav-file-using-naudio.