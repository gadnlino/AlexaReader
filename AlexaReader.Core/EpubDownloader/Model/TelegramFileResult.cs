using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace EpubFileDownloader.Model
{
    public class TelegramFileResult
    {
        [JsonProperty("ok")]
        public bool Ok { get; set; }
        [JsonProperty("result")]
        public TelegramFile Result { get; set; }
    }
}
