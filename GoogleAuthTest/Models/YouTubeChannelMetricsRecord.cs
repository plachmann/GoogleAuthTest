using System;
using System.Collections.Generic;
using System.Text;

namespace GoogleAuthTest.Models
{
    class YouTubeChannelMetricsRecord
    {
        public int YouTubeChannelID { get; set; }
        public string PlatformChannelID { get; set; }
        public int ViewCount { get; set; }
        public int SubscriberCount { get; set; }
        public int VideoCount { get; set; }
    }
}
