using System;
using System.Collections.Generic;
using System.Text;

namespace AlexaReader.Core.Model
{
    public class Chapter
    {
        public string Uuid { get; set; }
        public string Name { get; set; }
        public List<Subchapter> SubChapters { get; set; }
    }
}
