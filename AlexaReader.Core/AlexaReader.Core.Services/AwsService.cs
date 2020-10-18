using Amazon;
using Amazon.Polly;
using Amazon.Polly.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.Comprehend.Model;
using Amazon.Comprehend;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EpubFileDownloader.Service
{
    public class AwsService
    {
        public static class S3
        {
            private static AmazonS3Client client;
            static S3()
            {
                client = new AmazonS3Client(RegionEndpoint
                        .GetBySystemName(Environment.GetEnvironmentVariable("S3_REGION")));
            }

            public static void PutObject(Stream inputStream, string fileName, string bucketName,
                    string contentType, bool autoCloseStream = true)
            {
                var task = client.PutObjectAsync(new PutObjectRequest
                {
                    Key = fileName,
                    BucketName = bucketName,
                    InputStream = inputStream,
                    ContentType = contentType,
                    AutoCloseStream = autoCloseStream
                });

                task.Wait();
            }

            public static Stream GetObject(string fileName, string bucketName)
            {
                var task = client.GetObjectAsync(new GetObjectRequest
                {
                    BucketName = bucketName,
                    Key = fileName
                });
                task.Wait();
                GetObjectResponse response = task.Result;

                return response.ResponseStream;
            }
        }

        public static class SQS
        {
            private static AmazonSQSClient client;
            static SQS()
            {
                client = new AmazonSQSClient(RegionEndpoint
                        .GetBySystemName(Environment.GetEnvironmentVariable("SQS_REGION")));
            }

            public static void SendMessage(string messageBody, string queueUrl,
                    string messageGroupId = null, string messageDeduplicationId = null)
            {
                var request = new SendMessageRequest
                {
                    MessageBody = messageBody,
                    QueueUrl = queueUrl
                };

                //Used to group messages in a fifo queue
                if (!string.IsNullOrWhiteSpace(messageGroupId))
                {
                    request.MessageGroupId = messageGroupId;
                }

                //Used to desambiguate messages in a fifo queue
                if (!string.IsNullOrWhiteSpace(messageDeduplicationId))
                {
                    request.MessageDeduplicationId = messageDeduplicationId;
                }

                var task = client.SendMessageAsync(request);
                task.Wait();
            }

            public static void SendMessageBatch(List<SendMessageBatchRequestEntry> messages, string queueUrl)
            {
                var request = new SendMessageBatchRequest
                {
                    Entries = messages,
                    QueueUrl = queueUrl
                };

                var task = client.SendMessageBatchAsync(request);
                task.Wait();
            }
        }

        public static class Polly
        {
            private static AmazonPollyClient client;

            static Polly()
            {
                client = new AmazonPollyClient(RegionEndpoint
                    .GetBySystemName(Environment.GetEnvironmentVariable("POLLY_REGION")));
            }

            public static SynthesizeSpeechResponse SynthesizeSpeech(string textContent,
                Amazon.Polly.LanguageCode languageCode, string outputFormat = "mp3")
            {
                var synteshisRequest = new SynthesizeSpeechRequest
                {
                    Engine = Engine.Neural,
                    OutputFormat = outputFormat,
                    //SampleRate = "8000",
                    Text = textContent,
                    TextType = "text",
                    VoiceId = VoiceId.Joanna,
                    LanguageCode = languageCode
                };

                var client = new AmazonPollyClient(RegionEndpoint.USEast1);

                var task = client.SynthesizeSpeechAsync(synteshisRequest);
                task.Wait();

                return task.Result;
            }
        }

        public static class Comprehend
        {
            private static AmazonComprehendClient client;
            static Comprehend()
            {
                client = new AmazonComprehendClient(RegionEndpoint
                    .GetBySystemName(Environment.GetEnvironmentVariable("COMPREHEND_REGION")));
            }

            public static DetectDominantLanguageResponse DetectDominantLanguage(string text)
            {
                var task = client.DetectDominantLanguageAsync(new DetectDominantLanguageRequest
                {
                    Text = text
                });
                task.Wait();

                return task.Result;
            }
        }
    }
}
