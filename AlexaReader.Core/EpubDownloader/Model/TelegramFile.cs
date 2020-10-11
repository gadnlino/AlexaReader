using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace EpubFileDownloader.Model
{
    public class TelegramFile
    {
        [JsonProperty("file_id")]
        public string FileId { get; set; }
        [JsonProperty("file_unique_id")]
        public string FileUniqueId { get; set; }
        [JsonProperty("file_size")]
        public int FileSize { get; set; }
        [JsonProperty("file_path")]
        public string FilePath { get; set; }
    }
}
