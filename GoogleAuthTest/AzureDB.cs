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
                var text = "SELECT PlatformChannelID, YoutubeChannelID from dimYouTubeChannel";

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
                            channels.Add(channel);
                        }
                    }
                }

                return channels;
            }
        }

        public string InsertVideoMetrics(YouTubeVideoMetricsRecord videoMetricsRecord)
        {
            var rc = "insert success for " + videoMetricsRecord.PostContentId;
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    var cmd = new SqlCommand("insert into YoutubePostMetrics(YoutubeChannelID,PostContentID,Title,VideoURL,AuthorDisplayName,VideoDurationSeconds,Views,Likes,Dislikes,Shares,Replies,EngagementTotal,PostDatetime,CreateDatetime,UpdateDatetime) values (@youtubeChannelID,@postContentID,@title,@videoURL,@authorDisplayName,@videoDurationSeconds,@views,@likes,@dislikes,@shares,@replies,@engagementTotal,@postDatetime,@createDatetime,@updateDatetime)", conn);

                    cmd.Parameters.AddWithValue("@youtubeChannelID", videoMetricsRecord.YouTubeChannelId);
                    cmd.Parameters.AddWithValue("@postContentID", videoMetricsRecord.PostContentId);
                    cmd.Parameters.AddWithValue("@title", videoMetricsRecord.Title);
                    cmd.Parameters.AddWithValue("@videoURL", videoMetricsRecord.VideoUrl);
                    cmd.Parameters.AddWithValue("@authorDisplayName", videoMetricsRecord.AuthorDisplayName);
                    cmd.Parameters.AddWithValue("@videoDurationSeconds", videoMetricsRecord.VideoDurationSeconds);
                    cmd.Parameters.AddWithValue("@views", videoMetricsRecord.Views);
                    cmd.Parameters.AddWithValue("@likes", videoMetricsRecord.Likes);
                    cmd.Parameters.AddWithValue("@dislikes", videoMetricsRecord.Dislikes);
                    cmd.Parameters.AddWithValue("@shares", videoMetricsRecord.Shares);
                    cmd.Parameters.AddWithValue("@replies", videoMetricsRecord.Replies);
                    cmd.Parameters.AddWithValue("@engagementTotal", videoMetricsRecord.EngagementTotal);
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
