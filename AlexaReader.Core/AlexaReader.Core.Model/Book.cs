using System;
using System.Collections.Generic;
using System.Text;

namespace AlexaReader.Core.Model
{
    public class Book
    {
        public string Uuid { get; set; }
        public string Title { get; set; }
        public string EpubFilePath { get; set; }
        public List<Chapter> Chapters { get; set; }
        public string CurrentChapterId { get; set; }
        public User Owner { get; set; }
    }
}
