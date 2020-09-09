using System;

namespace GoogleAuthTest.Models
{
    public class YouTubeChannelInfo
    {
        public int YoutubeChannelID { get; set; }
        public string PlatformChannelID { get; set; }
        public string ClientID { get; set; }
        public string ClientSecret { get; set; }
        public string RefreshToken { get; set; }
    }
}
