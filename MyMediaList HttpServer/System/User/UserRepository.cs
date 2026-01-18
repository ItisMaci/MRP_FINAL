using Npgsql;
using System.Text.Json.Nodes;

namespace MyMediaList_HttpServer.System.User
{
    public sealed class UserRepository : IUserRepository
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // public methods                                                                                                   //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public User? GetByUsername(string username)
        {
            try
            {
                using var connection = Database.DatabaseConnection.GetConnection();
                connection.Open();

                string sql = "SELECT user_id, username, password_hash FROM users WHERE username = @username";

                using var cmd = new NpgsqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@username", username);

                using var reader = cmd.ExecuteReader();
                if (!reader.Read())
                {
                    return null;
                }

                return new User
                {
                    Id = reader.GetInt32(0),
                    UserName = reader.GetString(1),
                    PasswordHash = reader.GetString(2)
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error loading user: {ex.Message}");
                throw;
            }
        }

        public int GetId(string username)
        {
            using var connection = Database.DatabaseConnection.GetConnection();
            connection.Open();

            string sql = "SELECT user_id FROM users WHERE username = @username";
            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@username", username);

            object? result = cmd.ExecuteScalar();

            if (result == null)
            {
                throw new InvalidOperationException($"User '{username}' not found.");
            }

            return Convert.ToInt32(result);
        }

        public void Add(User user)
        {
            if (string.IsNullOrWhiteSpace(user.UserName)) { throw new InvalidOperationException("Username cannot be empty."); }
            if (string.IsNullOrWhiteSpace(user.PasswordHash)) { throw new InvalidOperationException("Password must be set before saving."); }

            try
            {
                using var connection = Database.DatabaseConnection.GetConnection();
                connection.Open();

                string sql = @"
                    INSERT INTO users (username, password_hash) 
                    VALUES (@username, @password_hash)
                    RETURNING user_id";

                using var cmd = new NpgsqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@username", user.UserName);
                cmd.Parameters.AddWithValue("@password_hash", user.PasswordHash);

                user.Id = (int)cmd.ExecuteScalar()!;
                Console.WriteLine($"User '{user.UserName}' saved to database.");
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                throw new InvalidOperationException($"Username '{user.UserName}' already exists.");
            }
        }

        public void Update(User user)
        {
            try
            {
                using var connection = Database.DatabaseConnection.GetConnection();
                connection.Open();

                string sql = @"
                    UPDATE users 
                    SET password_hash = @password_hash
                    WHERE username = @username";

                using var cmd = new NpgsqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@username", user.UserName);
                cmd.Parameters.AddWithValue("@password_hash", user.PasswordHash);

                int rows = cmd.ExecuteNonQuery();
                if (rows == 0) { throw new InvalidOperationException("User no longer exists in database."); }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
                throw;
            }
        }

        public void Delete(string username)
        {
            try
            {
                using var connection = Database.DatabaseConnection.GetConnection();
                connection.Open();

                string sql = "DELETE FROM users WHERE username = @username";

                using var cmd = new NpgsqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@username", username);

                int rows = cmd.ExecuteNonQuery();
                if (rows == 0)
                {
                    Console.WriteLine($"Warning: User '{username}' was not found during delete.");
                }
                else
                {
                    Console.WriteLine($"User '{username}' deleted from database.");
                }
            }
            catch (PostgresException ex) when (ex.SqlState == "23503")
            {
                throw new InvalidOperationException($"Cannot delete user '{username}' because they have related data.", ex);
            }
        }

        public bool Exists(string username)
        {
            using var connection = Database.DatabaseConnection.GetConnection();
            connection.Open();

            string sql = "SELECT 1 FROM users WHERE username = @username";
            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@username", username);

            return cmd.ExecuteScalar() != null;
        }

        public JsonObject GetStatistics(string username)
        {
            try
            {
                int userId = GetId(username);
                using var connection = Database.DatabaseConnection.GetConnection();
                connection.Open();

                string sql = @"
                    SELECT COUNT(*), COALESCE(AVG(score), 0) 
                    FROM ratings 
                    WHERE user_id = @uid";

                using var cmd = new NpgsqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@uid", userId);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return new JsonObject
                    {
                        ["total_ratings"] = reader.GetInt32(0),
                        ["average_score"] = Math.Round(reader.GetDouble(1), 2)
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating stats for {username}: {ex.Message}");
            }

            return new JsonObject { ["total_ratings"] = 0, ["average_score"] = 0 };
        }

        public JsonArray GetFavorites(string username)
        {
            JsonArray list = new();
            try
            {
                int userId = GetId(username);
                using var connection = Database.DatabaseConnection.GetConnection();
                connection.Open();

                string sql = @"
                    SELECT m.media_id, m.title, m.type, m.release_year 
                    FROM favorites f
                    JOIN media_entries m ON f.media_id = m.media_id
                    WHERE f.user_id = @uid
                    ORDER BY m.title ASC";

                using var cmd = new NpgsqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@uid", userId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new JsonObject
                    {
                        ["id"] = reader.GetInt32(0),
                        ["title"] = reader.GetString(1),
                        ["type"] = reader.GetString(2),
                        ["release_year"] = reader.GetInt32(3)
                    });
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error loading favorites: {ex.Message}");
            }
            return list;
        }

        public JsonArray GetLeaderboard()
        {
            JsonArray list = new();
            try
            {
                using var connection = Database.DatabaseConnection.GetConnection();
                connection.Open();

                string sql = @"
                    SELECT u.username, COUNT(r.rating_id) as rating_count
                    FROM users u
                    LEFT JOIN ratings r ON u.user_id = r.user_id
                    GROUP BY u.user_id, u.username
                    ORDER BY rating_count DESC
                    LIMIT 10";

                using var cmd = new NpgsqlCommand(sql, connection);
                using var reader = cmd.ExecuteReader();

                int rank = 1;
                while (reader.Read())
                {
                    list.Add(new JsonObject
                    {
                        ["rank"] = rank++,
                        ["username"] = reader.GetString(0),
                        ["rating_count"] = reader.GetInt64(1)
                    });
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error loading leaderboard: {ex.Message}");
            }
            return list;
        }

        public JsonArray GetRatingHistory(string username)
        {
            using var connection = Database.DatabaseConnection.GetConnection();
            connection.Open();

            string sql = @"
            SELECT r.rating_id, r.score, r.comment, r.created_at, m.title 
            FROM ratings r
            JOIN users u ON r.user_id = u.user_id
            JOIN media_entries m ON r.media_id = m.media_id
            WHERE u.username = @username
            ORDER BY r.created_at DESC";

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@username", username);

            using var reader = cmd.ExecuteReader();
            var history = new JsonArray();

            while (reader.Read())
            {
                history.Add(new JsonObject
                {
                    ["id"] = reader.GetInt32(0),
                    ["score"] = reader.GetInt32(1),
                    ["comment"] = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    ["date"] = reader.GetDateTime(3).ToString("yyyy-MM-dd"),
                    ["media_title"] = reader.GetString(4)
                });
            }
            return history;
        }

    }
}
