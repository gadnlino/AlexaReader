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

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
namespace EpubFileDownloader
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
            EpubDownloadContract epubDownloadContract =
                    JsonConvert.DeserializeObject<EpubDownloadContract>(message.Body);

            TelegramFileResult telegramFileResult = GetTelegramFile(epubDownloadContract.FileId);

            Stream fileStream = GetTelegramFileStream(telegramFileResult.Result.FilePath);

            string bucketName = Environment.GetEnvironmentVariable("ALEXA_READER_BUCKET");
            string contentType = "application/epub+zip";
            string uuid = Guid.NewGuid().ToString();
            string folderName = $"{epubDownloadContract.FromId}/{uuid}";
            string fileName = $"{uuid}.epub";

            AwsService.S3.PutObject(fileStream, $"{folderName}/{fileName}", bucketName, contentType);

            EpubProcessContract processContract = new EpubProcessContract
            {
                FromId = epubDownloadContract.FromId,
                ChatId = epubDownloadContract.ChatId,
                FileName = fileName,
                FolderName = folderName,
                ChaptersToProcess = new System.Collections.Generic.List<int> { 1, 2 }
            };

            AwsService.SQS.SendMessage(JsonConvert.SerializeObject(processContract));
        }

        public TelegramFileResult GetTelegramFile(string fileId)
        {
            string url = $"https://api.telegram.org/bot{Environment.GetEnvironmentVariable("BOT_TOKEN")}/getFile?file_id={fileId}";

            HttpClient http = new HttpClient();

            var getTask = http.GetAsync(url);
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