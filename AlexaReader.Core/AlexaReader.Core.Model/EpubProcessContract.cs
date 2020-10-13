using System;
using System.Collections.Generic;
using System.Text;

namespace AlexaReader.Core.Model
{
    public class EpubProcessContract
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FileName { get; set; }
        public string FromId { get; set; }
        public string ChatId { get; set; }
    }
}
