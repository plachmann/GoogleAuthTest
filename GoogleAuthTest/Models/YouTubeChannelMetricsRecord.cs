using System;

namespace GoogleAuthTest.Models
{
    class YouTubeChannelMetricsRecord
    {
        public int YouTubeChannelID { get; set; }
        public string PlatformChannelID { get; set; }
        public int ViewCount { get; set; }
        public int SubscriberCount { get; set; }
        public int VideoCount { get; set; }
        public DateTime ChannelPublishDate { get; set; }
        public DateTime CreateDateTime { get; set; }
        public DateTime UpdateDateTime { get; set; }
    }
}
