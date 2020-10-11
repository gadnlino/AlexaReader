using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
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
        }
    }
}
