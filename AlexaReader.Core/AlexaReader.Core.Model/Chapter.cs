using System;
using System.Collections.Generic;
using System.Text;

namespace AlexaReader.Core.Model
{
    public class Chapter
    {
        public string Uuid { get; set; }
        public string Title { get; set; }
        public List<Subchapter> Subchapters { get; set; }
        public string CurrentSubchapterId { get; set; }
    }
}
