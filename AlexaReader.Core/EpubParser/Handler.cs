using AlexaReader.Core.Model;
using Amazon;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.SQS.Model;
using EpubFileDownloader.Service;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VersOne.Epub;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
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
            ParseEpubContract parseEpubContract =
                    JsonConvert.DeserializeObject<ParseEpubContract>(message.Body);

            string bucketName = Environment.GetEnvironmentVariable("ALEXA_READER_BUCKET");
            string filePath = $"{parseEpubContract.FolderName}/{parseEpubContract.FileName}";

            Stream fileStream = AwsService.S3.GetObject(filePath, bucketName);

            // Opens a book and reads all of its content into memory
            EpubBook epubBook = EpubReader.ReadBook(fileStream);

            Book book = new Book();
            book.Uuid = Guid.NewGuid().ToString();
            book.Title = epubBook.Title;
            book.Chapters = new List<Chapter>();
            book.Owner = parseEpubContract.User;
            book.EpubFilePath = filePath;

            List<string> chaptersToSkip = new List<string>
            {
                "index",
                "preface",
                "glossary",
                "quick glossary"
            };

            List<EpubNavigationItem> validChapters = epubBook.Navigation
                        .Where(c => !chaptersToSkip.Contains(c.Title.ToLower()))
                        .ToList();

            List<ConvertTextToSpeechContract> convertTextToSpeechContracts =
                    new List<ConvertTextToSpeechContract>();

            foreach (EpubNavigationItem epubChapter in validChapters)
            {
                Chapter chapter = new Chapter();
                chapter.Uuid = Guid.NewGuid().ToString();
                chapter.Title = epubChapter.Title;
                chapter.Subchapters = new List<Subchapter>();

                // Nested chapters
                List<EpubNavigationItem> subChapters = epubChapter.NestedItems;

                foreach (var subChapter in subChapters)
                {
                    EpubTextContentFile content = subChapter.HtmlContentFile;

                    string stripped = StripHTML(content.Content);

                    string id = Guid.NewGuid().ToString();
                    string audioFilePath = $"{parseEpubContract.FolderName}/{id}.mp3";

                    chapter.Subchapters.Add(new Subchapter
                    {
                        AudioFilePath = audioFilePath,
                        Uuid = id,
                        CurrentTimePosition = 0
                    });

                    convertTextToSpeechContracts.Add(new ConvertTextToSpeechContract
                    {
                        TextContent = stripped,
                        AudioFilePathToSave = audioFilePath,
                        Owner = parseEpubContract.User
                    });
                }

                chapter.CurrentSubchapterId = chapter.Subchapters.FirstOrDefault()?.Uuid;
                book.Chapters.Add(chapter);
            }

            book.CurrentChapterId = book.Chapters.FirstOrDefault()?.Uuid;

            string queueUrl = Environment.GetEnvironmentVariable("CONVERSION_QUEUE_URL");
            string messageGroupId = Guid.NewGuid().ToString();

            List<SendMessageBatchRequestEntry> messages = new List<SendMessageBatchRequestEntry>();

            Action<ConvertTextToSpeechContract> addMessageToSend = (contract) =>
            {
                string messageBody = JsonConvert.SerializeObject(contract);
                string messageDeduplicationId = Guid.NewGuid().ToString();

                messages.Add(new SendMessageBatchRequestEntry
                {
                    Id = messageDeduplicationId,
                    MessageBody = messageBody,
                    MessageGroupId = messageGroupId,
                    MessageDeduplicationId = messageDeduplicationId
                });
            };

            convertTextToSpeechContracts
                .Take(convertTextToSpeechContracts.Count - 1)
                .ToList()
                .ForEach(contract => addMessageToSend(contract));

            ConvertTextToSpeechContract last = convertTextToSpeechContracts.Last();
            last.NotifyOwner = true;
            addMessageToSend(last);

            List<List<SendMessageBatchRequestEntry>> messageGroups = SplitList(messages, 10).ToList();

            messageGroups.ForEach(messageGroup =>
            {
                AwsService.SQS.SendMessageBatch(messageGroup, queueUrl);
            });

            var synteshisRequest = new SynthesizeSpeechRequest
            {
                Engine = Engine.Neural,
                OutputFormat = "mp3",
                //SampleRate = "8000",
                Text = txtContent,
                TextType = "text",
                VoiceId = VoiceId.Joanna,
                LanguageCode = LanguageCode.EnUS
            };

            var client = new AmazonPollyClient(RegionEndpoint.USEast1);

            var task = client.SynthesizeSpeechAsync(synteshisRequest);
            task.Wait();
            var response = task.Result;

            //Console.WriteLine($"Synthetized {response.RequestCharacters} caracthers");

            //// COMMON PROPERTIES

            //// Book's title
            //string title = epubBook.Title;

            //// Book's authors (comma separated list)
            //string author = epubBook.Author;

            //// Book's authors (list of authors names)
            //List<string> authors = epubBook.AuthorList;

            //// Book's cover image (null if there is no cover)
            //byte[] coverImageContent = epubBook.CoverImage;
            //if (coverImageContent != null)
            //{
            //    using (MemoryStream coverImageStream = new MemoryStream(coverImageContent))
            //    {
            //        Image coverImage = Image.FromStream(coverImageStream);
            //    }
            //}

            //// TABLE OF CONTENTS

            //// Enumerating chapters
            //foreach (EpubNavigationItem chapter in epubBook.Navigation)
            //{
            //    // Title of chapter
            //    string chapterTitle = chapter.Title;

            //    // Nested chapters
            //    List<EpubNavigationItem> subChapters = chapter.NestedItems;
            //}

            //// READING ORDER

            //// Enumerating the whole text content of the book in the order of reading
            //foreach (EpubTextContentFile textContentFile in book.ReadingOrder)
            //{
            //    // HTML of current text content file
            //    string htmlContent = textContentFile.Content;
            //}


            //// CONTENT

            //// Book's content (HTML files, stlylesheets, images, fonts, etc.)
            //EpubContent bookContent = epubBook.Content;


            //// IMAGES

            //// All images in the book (file name is the key)
            //Dictionary<string, EpubByteContentFile> images = bookContent.Images;

            //EpubByteContentFile firstImage = images.Values.First();

            //// Content type (e.g. EpubContentType.IMAGE_JPEG, EpubContentType.IMAGE_PNG)
            //EpubContentType contentType = firstImage.ContentType;

            //// MIME type (e.g. "image/jpeg", "image/png")
            //string mimeType = firstImage.ContentMimeType;

            //// Creating Image class instance from the content
            //using (MemoryStream imageStream = new MemoryStream(firstImage.Content))
            //{
            //    Image image = Image.FromStream(imageStream);
            //}

            //// Cover metadata
            //if (bookContent.Cover != null)
            //{
            //    string coverFileName = bookContent.Cover.FileName;
            //    EpubContentType coverContentType = bookContent.Cover.ContentType;
            //    string coverMimeType = bookContent.Cover.ContentMimeType;
            //}

            //// HTML & CSS

            //// All XHTML files in the book (file name is the key)
            //Dictionary<string, EpubTextContentFile> htmlFiles = bookContent.Html;

            //// All CSS files in the book (file name is the key)
            //Dictionary<string, EpubTextContentFile> cssFiles = bookContent.Css;

            //// Entire HTML content of the book
            //foreach (EpubTextContentFile htmlFile in htmlFiles.Values)
            //{
            //    string htmlContent = htmlFile.Content;
            //}

            //// All CSS content in the book
            //foreach (EpubTextContentFile cssFile in cssFiles.Values)
            //{
            //    string cssContent = cssFile.Content;
            //}


            //// OTHER CONTENT

            //// All fonts in the book (file name is the key)
            //Dictionary<string, EpubByteContentFile> fonts = bookContent.Fonts;

            //// All files in the book (including HTML, CSS, images, fonts, and other types of files)
            //Dictionary<string, EpubContentFile> allFiles = bookContent.AllFiles;


            //// ACCESSING RAW SCHEMA INFORMATION

            //// EPUB OPF data
            //EpubPackage package = epubBook.Schema.Package;

            //// Enumerating book's contributors
            //foreach (EpubMetadataContributor contributor in package.Metadata.Contributors)
            //{
            //    string contributorName = contributor.Contributor;
            //    string contributorRole = contributor.Role;
            //}

            //// EPUB 2 NCX data
            //Epub2Ncx epub2Ncx = epubBook.Schema.Epub2Ncx;

            //// Enumerating EPUB 2 NCX metadata
            //foreach (Epub2NcxHeadMeta meta in epub2Ncx.Head)
            //{
            //    string metadataItemName = meta.Name;
            //    string metadataItemContent = meta.Content;
            //}

            //// EPUB 3 navigation
            //Epub3NavDocument epub3NavDocument = epubBook.Schema.Epub3NavDocument;

            //// Accessing structural semantics data of the head item
            //StructuralSemanticsProperty? ssp = epub3NavDocument.Navs.First().Type;
        }

        public static IEnumerable<List<T>> SplitList<T>(List<T> locations, int nSize = 30)
        {
            for (int i = 0; i < locations.Count; i += nSize)
            {
                yield return locations.GetRange(i, Math.Min(nSize, locations.Count - i));
            }
        }

        public static string StripHTML(string input)
        {
            return Regex.Replace(input, "<.*?>", string.Empty);
        }
    }
}