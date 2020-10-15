using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
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

            public static void SendMessage(string messageBody, string queueUrl)
            {
                var task = client.SendMessageAsync(new SendMessageRequest
                {
                    MessageBody = messageBody,
                    QueueUrl = queueUrl
                });
                task.Wait();
            }
        }
    }
}
