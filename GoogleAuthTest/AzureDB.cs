using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Collections.Generic;
using GoogleAuthTest.Models;

namespace GoogleAuthTest
{
    class AzureDB
    {
        private string connectionString;

        public AzureDB()
        {
            var keyVault = new KeyVault();
            connectionString = keyVault.GetSecret("BRONZECONNECTIONSTRING");
        }

        public async Task<List<YouTubeChannelInfo>> GetChannelIds()
        {
            List<YouTubeChannelInfo> channels = new List<YouTubeChannelInfo>();
                using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var text = "SELECT PlatformChannelID, YoutubeChannelID, ClientID, ClientSecret, RefreshToken from dimYouTubeChannel where active = 1";

                using (SqlCommand cmd = new SqlCommand(text, conn))
                {
                    // Execute the command and log the # rows affected.
                    var reader = await cmd.ExecuteReaderAsync();

                    //build list of channels
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            var channel = new YouTubeChannelInfo();
                            channel.PlatformChannelID =  reader.GetString(0);
                            channel.YoutubeChannelID = reader.GetInt32(1);
                            channel.ClientID = reader.GetString(2);
                            channel.ClientSecret = reader.GetString(3);
                            channel.RefreshToken = reader.GetString(4);
                            channels.Add(channel);
                        }
                    }
                }

                return channels;
            }
        }

        public string InsertChannelMetrics(YouTubeChannelMetricsRecord channelMetricsRecord)
        {
            var rc = "channel metrics inserted successfully\n";
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    var cmd = new SqlCommand("insert into YoutubeChannelMetrics(YoutubeChannelID,SubscriberCount,VideoCount,ViewCount,CreateDatetime,UpdateDatetime) " +
                        "values (@youtubeChannelID,@subscriberCount,@videoCount,@viewCount,@createDatetime,@updateDatetime)", conn);

                    cmd.Parameters.AddWithValue("@youtubeChannelID", channelMetricsRecord.YouTubeChannelID);
                    cmd.Parameters.AddWithValue("@subscriberCount", channelMetricsRecord.SubscriberCount);
                    cmd.Parameters.AddWithValue("@videoCount", channelMetricsRecord.VideoCount);
                    cmd.Parameters.AddWithValue("@viewCount", channelMetricsRecord.ViewCount);
                    cmd.Parameters.AddWithValue("@createDatetime", channelMetricsRecord.CreateDateTime);
                    cmd.Parameters.AddWithValue("@updateDatetime", channelMetricsRecord.UpdateDateTime);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch(Exception ex)
            {
                rc = "FAILURE inserting channel metrics: " + ex.Message + "\n";
            }
            return rc;
        }

        public string InsertVideoMetrics(YouTubeVideoMetricsRecord videoMetricsRecord)
        {
            var rc = "insert success for " + videoMetricsRecord.PostContentId + "\n";
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    var cmd = new SqlCommand("insert into YoutubePostMetrics(YoutubeChannelID,PostContentID,Title,AuthorDisplayName,VideoDurationSeconds,Views,Likes,Dislikes,Shares,Comments,EstimatedMinutesWatched,AverageViewDurationSeconds,AverageViewPercentage,PostDatetime,CreateDatetime,UpdateDatetime) " +
                        "values (@youtubeChannelID,@postContentID,@title,@authorDisplayName,@videoDurationSeconds,@views,@likes,@dislikes,@shares,@comments,@estimatedMinutesWatched,@averageViewDurationSeconds,@averageViewPercentage,@postDatetime,@createDatetime,@updateDatetime)", conn);

                    cmd.Parameters.AddWithValue("@youtubeChannelID", videoMetricsRecord.YouTubeChannelId);
                    cmd.Parameters.AddWithValue("@postContentID", videoMetricsRecord.PostContentId);
                    cmd.Parameters.AddWithValue("@title", videoMetricsRecord.Title);
                    cmd.Parameters.AddWithValue("@authorDisplayName", videoMetricsRecord.AuthorDisplayName);
                    cmd.Parameters.AddWithValue("@videoDurationSeconds", videoMetricsRecord.VideoDurationSeconds);
                    cmd.Parameters.AddWithValue("@views", videoMetricsRecord.Views);
                    cmd.Parameters.AddWithValue("@likes", videoMetricsRecord.Likes);
                    cmd.Parameters.AddWithValue("@dislikes", videoMetricsRecord.Dislikes);
                    cmd.Parameters.AddWithValue("@shares", videoMetricsRecord.Shares);
                    cmd.Parameters.AddWithValue("@comments", videoMetricsRecord.Comments);
                    cmd.Parameters.AddWithValue("@estimatedMinutesWatched", videoMetricsRecord.EstimatedMinutesWatched);
                    cmd.Parameters.AddWithValue("@averageViewDurationSeconds", videoMetricsRecord.AverageViewDurationSeconds);
                    cmd.Parameters.AddWithValue("@averageViewPercentage", videoMetricsRecord.AverageViewPercentage);
                    cmd.Parameters.AddWithValue("@postDatetime", videoMetricsRecord.PostDateTime);
                    cmd.Parameters.AddWithValue("@createDatetime", videoMetricsRecord.CreateDateTime);
                    cmd.Parameters.AddWithValue("@updateDatetime", videoMetricsRecord.UpdateDateTime);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch(Exception ex)
            {
                rc = "error inserting " + videoMetricsRecord.PostContentId + " : " +ex.Message;
            }
            return rc;
        }
    }
}
