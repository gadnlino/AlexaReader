using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VersOne.Epub;
using VersOne.Epub.Schema;
using static System.Net.Mime.MediaTypeNames;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.Data.SqlTypes;
using Amazon.Polly;
using Amazon.Polly.Model;
using Amazon;
using Amazon.Runtime;

namespace AlexaEbookReader
{
    class Program
    {
        static void Main(string[] args)
        {
            // Opens a book and reads all of its content into memory
            EpubBook epubBook = EpubReader.ReadBook("teste.epub");

            //// Enumerating the whole text content of the book in the order of reading
            //foreach (EpubTextContentFile textContentFile in epubBook.ReadingOrder)
            //{
            //    // HTML of current text content file
            //    string htmlContent = textContentFile.Content;
            //}

            List<string> chaptersToSkip = new List<string>
            {
                "index",
                "preface",
                "glossary",
                "quick glossary"
            };

            var validChapters = epubBook.Navigation
                        .Where(c => !chaptersToSkip.Contains(c.Title.ToLower()))
                        .ToList();

            string bookContent = string.Empty;

            foreach (EpubNavigationItem chapter in validChapters)
            {
                // Title of chapter
                string chapterTitle = chapter.Title;

                string chapterContent = string.Empty;

                // Nested chapters
                List<EpubNavigationItem> subChapters = chapter.NestedItems;

                foreach (var subChapter in subChapters)
                {
                    EpubTextContentFile content = subChapter.HtmlContentFile;
                    //XDocument html = XDocument.Parse(content.Content);

                    //foreach(var desc in html.Descendants())
                    //{
                    //    Console.WriteLine(desc.Value);
                    //}
                    string stripped = StripHTML(content.Content);
                    //Console.WriteLine(stripped);
                    bookContent += stripped;
                    chapterContent += stripped;
                }

                Console.WriteLine(chapterTitle);
                Console.WriteLine(chapterContent.Length);
            }

            Console.WriteLine(bookContent.Length);


            //Synthetizing the first subchapter

            var firstChapter = validChapters.First();

            var firstSubChapter = firstChapter.NestedItems.First();

            var txtContent = StripHTML(firstSubChapter.HtmlContentFile.Content);

            Console.WriteLine(txtContent);

            var synteshisRequest = new SynthesizeSpeechRequest
            {
                // LexiconNames = new List<string> {
                //     "example"
                //},
                Engine = Engine.Neural,
                OutputFormat = "mp3",
                SampleRate = "8000",
                Text = txtContent,
                TextType = "text",
                VoiceId = VoiceId.Joanna,
                LanguageCode = LanguageCode.EnUS
            };

            var client = new AmazonPollyClient(RegionEndpoint.USEast1);

            var task = client.SynthesizeSpeechAsync(synteshisRequest);
            task.Wait();
            var response = task.Result;

            Console.WriteLine($"Synthetized {response.RequestCharacters} caracthers");

            byte[] audioBytes = ReadToEnd(response.AudioStream);

            File.WriteAllBytes($"teste_{DateTime.Now.ToString("yyyyMMddhhmmss")}.mp3", audioBytes);

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

        public static string StripHTML(string input)
        {
            return Regex.Replace(input, "<.*?>", string.Empty);
        }
    }
}
