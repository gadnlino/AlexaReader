using System;
using System.Collections.Generic;
using System.Text;

namespace AlexaReader.Core.Model
{
    public class EpubProcessContract
    {
        public string FileName { get; set; }
        public string FolderName { get; set; }
        public string FromId { get; set; }
        public string ChatId { get; set; }
        public List<int> ChaptersToProcess { get; set; }
    }
}
