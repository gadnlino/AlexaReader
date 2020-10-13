using AlexaReader.Core.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using EpubFileDownloader.Service;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

[assembly:LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
namespace AwsDotnetCsharp
{
    public class Handler
    {
        public async Task FunctionHandler(SQSEvent sqsEvent, ILambdaContext context)
        {
            foreach (var record in sqsEvent.Records)
            {
                await ProcessMessageAsync(record);
            }
        }

        public async Task ProcessMessageAsync(SQSEvent.SQSMessage message)
        {
            EpubProcessContract epubProcessContract =
                    JsonConvert.DeserializeObject<EpubProcessContract>(message.Body);

            string bucketName = Environment.GetEnvironmentVariable("ALEXA_READER_BUCKET");

            AwsService.S3.GetObject(epubProcessContract.FileName, bucketName);
        }
    }
}
