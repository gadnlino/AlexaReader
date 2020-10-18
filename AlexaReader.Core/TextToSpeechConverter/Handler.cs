using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.SQS.Internal;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using System.IO;
using System;
using System.Net.Http;
using System.Net;
using AlexaReader.Core.Model;
using Amazon.Runtime.Internal.Util;
using EpubFileDownloader.Service;
using System.Linq;
using Amazon.Comprehend.Model;
using Amazon.Polly.Model;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
namespace TextToSpeechConverter
{
    public class Handler
    {
        ILambdaLogger _logger;

        public async Task FunctionHandler(SQSEvent sqsEvent, ILambdaContext context)
        {
            //_logger = context.Logger;

            foreach (var record in sqsEvent.Records)
            {
                await ProcessMessageAsync(record);
            }
        }

        public async Task ProcessMessageAsync(SQSEvent.SQSMessage message)
        {
            //_logger.LogLine("Processing message \n" + message.Body);

            ConvertTextToSpeechContract contract =
                JsonConvert.DeserializeObject<ConvertTextToSpeechContract>(message.Body);

            DetectDominantLanguageResponse dominantLanguageResponse = AwsService.Comprehend
                .DetectDominantLanguage(contract.TextContent);

            string dominantLanguageCode = dominantLanguageResponse
                .Languages
                .OrderByDescending(l => l.Score)
                .First()
                .LanguageCode;

            Amazon.Polly.LanguageCode languageCode = null;

            switch (dominantLanguageCode)
            {
                case "en":
                    languageCode = Amazon.Polly.LanguageCode.EnUS;
                    break;
                //TODO: Handle other languages
                default:
                    throw new Exception("Could not find the language specified");
            }

            SynthesizeSpeechResponse systhesisResponse = AwsService
                .Polly.SynthesizeSpeech(contract.TextContent, languageCode);

            MemoryStream inputStream = new MemoryStream(ReadToEnd(systhesisResponse.AudioStream));

            string contentType = "audio/mpeg";
            string bucketName = Environment.GetEnvironmentVariable("ALEXA_READER_BUCKET");

            AwsService.S3.PutObject(inputStream, contract.AudioFilePathToSave,
                bucketName, contentType);

            //_logger.LogLine("Processed message \n" + message.Body);
        }

        public static byte[] ReadToEnd(Stream stream)
        {
            long originalPosition = 0;

            if (stream.CanSeek)
            {
                originalPosition = stream.Position;
                stream.Position = 0;
            }

            try
            {
                byte[] readBuffer = new byte[4096];

                int totalBytesRead = 0;
                int bytesRead;

                while ((bytesRead = stream.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
                {
                    totalBytesRead += bytesRead;

                    if (totalBytesRead == readBuffer.Length)
                    {
                        int nextByte = stream.ReadByte();
                        if (nextByte != -1)
                        {
                            byte[] temp = new byte[readBuffer.Length * 2];
                            Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                            Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
                            readBuffer = temp;
                            totalBytesRead++;
                        }
                    }
                }

                byte[] buffer = readBuffer;
                if (readBuffer.Length != totalBytesRead)
                {
                    buffer = new byte[totalBytesRead];
                    Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
                }
                return buffer;
            }
            finally
            {
                if (stream.CanSeek)
                {
                    stream.Position = originalPosition;
                }
            }
        }
    }
}