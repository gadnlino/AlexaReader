using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace AlexaReader.Core.Model
{
    public class EpubDownloadContract
    {
        [JsonProperty("file_id")]
        public string FileId { get; set; }
        [JsonProperty("from_id")]
        public string FromId { get; set; }
        [JsonProperty("chat_id")]
        public string ChatId { get; set; }
        [JsonProperty("first_name")]
        public string FirstName { get; set; }
        [JsonProperty("last_name")]
        public string LastName { get; set; }
    }
}
