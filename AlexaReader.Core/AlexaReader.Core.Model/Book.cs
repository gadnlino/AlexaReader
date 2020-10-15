using System;
using System.Collections.Generic;
using System.Text;

namespace AlexaReader.Core.Model
{
    public class Book
    {
        public string Uuid { get; set; }
        public string Name { get; set; }
        public string EpubFileName { get; set; }
        public List<Chapter> Chapters { get; set; }
        public Chapter CurrentChapter { get; set; }
    }
}
