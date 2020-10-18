using System;
using System.Collections.Generic;
using System.Text;

namespace AlexaReader.Core.Model
{
    public class ConvertTextToSpeechContract
    {
        public string TextContent { get; set; }
        public string AudioFilePathToSave { get; set; }
        public User Owner { get; set; }
        public bool NotifyOwner { get; set; }
    }
}
