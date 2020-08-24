using System;

namespace GoogleAuthTest.Models
{
    class YouTubeVideoMetricsRecord
    {
        public int YouTubeChannelId { get; set; }
        public string PostContentId { get; set; }
        public string Title { get; set; }
        public string VideoUrl { get; set; }
        public string AuthorDisplayName { get; set; }
        public int VideoDurationSeconds { get; set; }
        public int Views { get; set; }
        public int Likes { get; set; }
        public int Dislikes { get; set; }
        public int Shares { get; set; }
        public int Replies { get; set; }
        public int EngagementTotal { get; set; }
        public DateTime PostDateTime { get; set; }
        public DateTime CreateDateTime { get; set; }
        public DateTime UpdateDateTime { get; set; }
    }
}
