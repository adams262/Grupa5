using System;
using System.Collections.Generic;
using System.Text;

namespace PZPP_Grupa5.Models
{
    public class ChatHistoryItem
    {
        public string TytulWideo { get; set; }
        public DateTime DataUtworzenia { get; set; }
        public string TekstWynikowy { get; set; }
        public string VideoThumbnailUrl { get; set; }
        public string VideoUrl { get; set; }
    }
}
