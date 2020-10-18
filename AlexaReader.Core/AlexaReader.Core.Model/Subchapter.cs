using System;
using System.Collections.Generic;
using System.Text;

namespace AlexaReader.Core.Model
{
    public class Subchapter
    {
        public string Uuid { get; set; }
        public string AudioFilePath { get; set; }
        public int CurrentTimePosition { get; set; }
    }
}
