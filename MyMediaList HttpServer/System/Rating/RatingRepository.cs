using Npgsql;

namespace MyMediaList_HttpServer.System.Rating
{
    public sealed class RatingRepository : IRatingRepository
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // public methods                                                                                                   //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public Rating? GetById(int id)
        {
            try
            {
                using var connection = Database.DatabaseConnection.GetConnection();
                connection.Open();

                string sql = @"
                SELECT rating_id, user_id, media_id, score, comment, is_confirmed, created_at 
                FROM ratings 
                WHERE rating_id = @id";

                using var cmd = new NpgsqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@id", id);

                using var reader = cmd.ExecuteReader();

                if (!reader.Read()) { return null; }

                return new Rating
                {
                    Id = reader.GetInt32(0),
                    UserId = reader.GetInt32(1),
                    MediaId = reader.GetInt32(2),
                    Score = reader.GetInt32(3),
                    Comment = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    IsConfirmed = reader.GetBoolean(5),
                    CreatedAt = reader.GetDateTime(6)
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error loading rating: {ex.Message}");
            }
        }


        public void Add(Rating rating)
        {
            if (rating.Score < 1 || rating.Score > 5)
            {
                throw new ArgumentException("Score must be 1-5.");
            }

            try
            {
                using var connection = Database.DatabaseConnection.GetConnection();
                connection.Open();

                // One rating per user per media
                string checkSql = "SELECT COUNT(*) FROM ratings WHERE user_id = @uid AND media_id = @mid";
                using (var checkCmd = new NpgsqlCommand(checkSql, connection))
                {
                    checkCmd.Parameters.AddWithValue("@uid", rating.UserId);
                    checkCmd.Parameters.AddWithValue("@mid", rating.MediaId);

                    long count = (long)checkCmd.ExecuteScalar()!;
                    if (count > 0)
                    {
                        throw new InvalidOperationException("User has already rated this media entry.");
                    }
                }

                string sql = @"
                INSERT INTO ratings (user_id, media_id, score, comment, is_confirmed) 
                VALUES (@uid, @mid, @score, @comment, FALSE) 
                RETURNING rating_id";

                using var cmd = new NpgsqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@uid", rating.UserId);
                cmd.Parameters.AddWithValue("@mid", rating.MediaId);
                cmd.Parameters.AddWithValue("@score", rating.Score);
                cmd.Parameters.AddWithValue("@comment", (object?)rating.Comment ?? DBNull.Value);

                rating.Id = (int)cmd.ExecuteScalar()!;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB ERROR] {ex.Message} \n {ex.StackTrace}");

                throw new InvalidOperationException("Failed to save rating.", ex);
            }
        }


        public void Update(Rating rating)
        {
            if (rating.Score < 1 || rating.Score > 5) { throw new ArgumentException("Score must be 1-5."); }

            try
            {
                using var connection = Database.DatabaseConnection.GetConnection();
                connection.Open();

                string sql = @"
                    UPDATE ratings 
                    SET score=@score, comment=@comment, is_confirmed=@confirmed 
                    WHERE rating_id=@id";

                using var cmd = new NpgsqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@score", rating.Score);
                cmd.Parameters.AddWithValue("@comment", (object?)rating.Comment ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@id", rating.Id);
                cmd.Parameters.AddWithValue("@confirmed", rating.IsConfirmed);

                if (cmd.ExecuteNonQuery() == 0) { throw new InvalidOperationException("Rating no longer exists."); }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to update rating.", ex);
            }
        }

        public void Delete(int id)
        {
            try
            {
                using var connection = Database.DatabaseConnection.GetConnection();
                connection.Open();

                using var cmd = new NpgsqlCommand("DELETE FROM ratings WHERE rating_id=@id", connection);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to delete rating.", ex);
            }
        }

        public bool ToggleLike(int userId, int ratingId)
        {
            try
            {
                using var connection = Database.DatabaseConnection.GetConnection();
                connection.Open();

                string checkSql = "SELECT COUNT(*) FROM rating_likes WHERE user_id = @uid AND rating_id = @rid";
                using (var checkCmd = new NpgsqlCommand(checkSql, connection))
                {
                    checkCmd.Parameters.AddWithValue("@uid", userId);
                    checkCmd.Parameters.AddWithValue("@rid", ratingId);

                    if ((long)checkCmd.ExecuteScalar()! > 0)
                    {
                        using var delCmd = new NpgsqlCommand("DELETE FROM rating_likes WHERE user_id = @uid AND rating_id = @rid", connection);
                        delCmd.Parameters.AddWithValue("@uid", userId);
                        delCmd.Parameters.AddWithValue("@rid", ratingId);
                        delCmd.ExecuteNonQuery();
                        return false; // Unliked
                    }
                    else
                    {
                        using var addCmd = new NpgsqlCommand("INSERT INTO rating_likes (user_id, rating_id) VALUES (@uid, @rid)", connection);
                        addCmd.Parameters.AddWithValue("@uid", userId);
                        addCmd.Parameters.AddWithValue("@rid", ratingId);
                        addCmd.ExecuteNonQuery();
                        return true; // Liked
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error toggling like: {ex.Message}");
            }
        }
    }
}
