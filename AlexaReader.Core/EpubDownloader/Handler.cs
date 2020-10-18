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
using EpubFileDownloader.Service;
using AlexaReader.Core.Model;
using Amazon.Runtime.Internal.Util;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
namespace EpubFileDownloader
{
    public class Handler
    {
        ILambdaLogger _logger;

        public async Task FunctionHandler(SQSEvent sqsEvent, ILambdaContext context)
        {
            _logger = context.Logger;

            foreach (var record in sqsEvent.Records)
            {
                await ProcessMessageAsync(record);
            }
        }

        public async Task ProcessMessageAsync(SQSEvent.SQSMessage message)
        {
            _logger.LogLine("Processing message \n" + message.Body);

            DownloadEpubContract epubDownloadContract =
                    JsonConvert.DeserializeObject<DownloadEpubContract>(message.Body);

            User user = epubDownloadContract.User;
            
            TelegramFileResult telegramFileResult = GetTelegramFile(epubDownloadContract.FileId);

            Stream fileStream = GetTelegramFileStream(telegramFileResult.Result.FilePath);

            string bucketName = Environment.GetEnvironmentVariable("ALEXA_READER_BUCKET");
            string contentType = "application/epub+zip";
            string uuid = Guid.NewGuid().ToString();
            string folderName = $"{user.FromId}/{uuid}";
            string fileName = $"{uuid}.epub";

            AwsService.S3.PutObject(fileStream, $"{folderName}/{fileName}", bucketName, contentType);

            ParseEpubContract processContract = new ParseEpubContract
            {
                User = user,
                FileName = fileName,
                FolderName = folderName,
            };

            string messageBody = JsonConvert.SerializeObject(processContract);
            string queueUrl = Environment.GetEnvironmentVariable("PARSE_EPUB_QUEUE_URL");
            AwsService.SQS.SendMessage(messageBody, queueUrl);

            _logger.LogLine($"Message sent to {queueUrl} queue\n" + messageBody);
        }

        public TelegramFileResult GetTelegramFile(string fileId)
        {
            string url = $"https://api.telegram.org/bot{Environment.GetEnvironmentVariable("BOT_TOKEN")}/getFile?file_id={fileId}";
            
            Uri uri = new Uri(url);
            HttpClient http = new HttpClient();

            var getTask = http.GetAsync(uri);
            getTask.Wait();
            HttpResponseMessage response = getTask.Result;

            response.EnsureSuccessStatusCode();

            var readStringTask = response.Content.ReadAsStringAsync();
            readStringTask.Wait();

            return JsonConvert.DeserializeObject<TelegramFileResult>(readStringTask.Result);
        }

        public Stream GetTelegramFileStream(string filePath)
        {
            string url = $"https://api.telegram.org/file/bot{Environment.GetEnvironmentVariable("BOT_TOKEN")}/{filePath}";

            HttpClient http = new HttpClient();

            var getTask = http.GetAsync(url);
            getTask.Wait();
            HttpResponseMessage response = getTask.Result;

            response.EnsureSuccessStatusCode();

            var readTask = response.Content.ReadAsStreamAsync();
            readTask.Wait();

            return readTask.Result;
        }
    }
}