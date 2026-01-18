using Npgsql;
using System.Text;
using System.Text.Json.Nodes;

namespace MyMediaList_HttpServer.System.Media
{
    public sealed class MediaRepository : IMediaRepository
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // public methods                                                                                                   //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public Media? GetById(int id)
        {
            try
            {
                using var connection = Database.DatabaseConnection.GetConnection();
                connection.Open();

                string sql = "SELECT media_id, title, description, type, release_year, age_restriction, creator_id FROM media_entries WHERE media_id = @id";
                using var cmd = new NpgsqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@id", id);

                using var reader = cmd.ExecuteReader();

                if (!reader.Read()) { return null; }

                return new Media
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    Type = reader.GetString(3),
                    ReleaseYear = reader.GetInt32(4),
                    AgeRestriction = reader.GetInt32(5),
                    CreatorId = reader.GetInt32(6)
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error loading media: {ex.Message}");
            }
        }

        public void Add(Media media)
        {
            if (string.IsNullOrWhiteSpace(media.Title)) { throw new InvalidOperationException("Title cannot be empty."); }

            try
            {
                using var connection = Database.DatabaseConnection.GetConnection();
                connection.Open();

                // Check for duplicate title (Case Insensitive)
                using (var checkCmd = new NpgsqlCommand("SELECT COUNT(*) FROM media_entries WHERE LOWER(title) = LOWER(@title)", connection))
                {
                    checkCmd.Parameters.AddWithValue("@title", media.Title);
                    if ((long)checkCmd.ExecuteScalar()! > 0)
                    {
                        throw new InvalidOperationException($"A media entry with the title '{media.Title}' already exists.");
                    }
                }

                string sql = @"
                    INSERT INTO media_entries (title, description, type, release_year, age_restriction, creator_id) 
                    VALUES (@title, @desc, @type, @year, @age, @creator) 
                    RETURNING media_id";

                using var cmd = new NpgsqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@title", media.Title);
                cmd.Parameters.AddWithValue("@desc", (object?)media.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@type", media.Type);
                cmd.Parameters.AddWithValue("@year", media.ReleaseYear);
                cmd.Parameters.AddWithValue("@age", media.AgeRestriction);
                cmd.Parameters.AddWithValue("@creator", media.CreatorId);

                media.Id = (int)cmd.ExecuteScalar()!;
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                throw new InvalidOperationException($"A media entry with the title '{media.Title}' already exists.");
            }
        }

        public void Update(Media media)
        {
            if (string.IsNullOrWhiteSpace(media.Title)) { throw new InvalidOperationException("Title cannot be empty."); }

            try
            {
                using var connection = Database.DatabaseConnection.GetConnection();
                connection.Open();

                // Check for duplicate title on other items
                using (var checkCmd = new NpgsqlCommand("SELECT COUNT(*) FROM media_entries WHERE LOWER(title) = LOWER(@title) AND media_id != @id", connection))
                {
                    checkCmd.Parameters.AddWithValue("@title", media.Title);
                    checkCmd.Parameters.AddWithValue("@id", media.Id);
                    if ((long)checkCmd.ExecuteScalar()! > 0)
                    {
                        throw new InvalidOperationException($"A media entry with the title '{media.Title}' already exists.");
                    }
                }

                string sql = @"
                    UPDATE media_entries 
                    SET title=@title, description=@desc, type=@type, release_year=@year, age_restriction=@age 
                    WHERE media_id=@id";

                using var cmd = new NpgsqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@title", media.Title);
                cmd.Parameters.AddWithValue("@desc", (object?)media.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@type", media.Type);
                cmd.Parameters.AddWithValue("@year", media.ReleaseYear);
                cmd.Parameters.AddWithValue("@age", media.AgeRestriction);
                cmd.Parameters.AddWithValue("@id", media.Id);

                if (cmd.ExecuteNonQuery() == 0) { throw new InvalidOperationException("Media no longer exists."); }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("already exists")) { throw; }
                throw new InvalidOperationException("Failed to update media.", ex);
            }
        }

        public void Delete(int id)
        {
            try
            {
                using var connection = Database.DatabaseConnection.GetConnection();
                connection.Open();

                using var cmd = new NpgsqlCommand("DELETE FROM media_entries WHERE media_id=@id", connection);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to delete media.", ex);
            }
        }

        /// <summary>
        /// Retrieves a list of media entries based on filter criteria.
        /// </summary>
        /// <param name="search">Partial title search.</param>
        /// <param name="type">Exact media type match.</param>
        /// <param name="year">Exact release year match.</param>
        /// <param name="age">Maximum age restriction.</param>
        /// <param name="sort">Sort order (year, title, score).</param>
        /// <param name="genre">Genre name to filter by.</param>
        /// <returns>A JSON array of media entries.</returns>
        public JsonArray GetList(string? search = null, string? type = null, int? year = null, int? age = null, string? sort = null, string? genre = null)
        {
            try
            {
                using var connection = Database.DatabaseConnection.GetConnection();
                connection.Open();

                var sqlBuilder = new StringBuilder(@"
                    SELECT m.media_id, m.title, m.description, m.type, m.release_year, m.age_restriction, m.creator_id,
                           COALESCE(AVG(r.score), 0) as avg_score
                    FROM media_entries m
                    LEFT JOIN ratings r ON m.media_id = r.media_id
                ");

                if (!string.IsNullOrEmpty(genre))
                {
                    sqlBuilder.Append(" JOIN media_genres mg ON m.media_id = mg.media_id ");
                    sqlBuilder.Append(" JOIN genres g ON mg.genre_id = g.genre_id ");
                }

                sqlBuilder.Append(" WHERE 1=1 ");

                if (!string.IsNullOrEmpty(search)) { sqlBuilder.Append(" AND LOWER(m.title) LIKE LOWER(@search) "); }
                if (!string.IsNullOrEmpty(type)) { sqlBuilder.Append(" AND m.type = @type "); }
                if (year.HasValue) { sqlBuilder.Append(" AND m.release_year = @year "); }
                if (age.HasValue) { sqlBuilder.Append(" AND m.age_restriction <= @age "); }
                if (!string.IsNullOrEmpty(genre)) { sqlBuilder.Append(" AND LOWER(g.name) = LOWER(@genre) "); }

                sqlBuilder.Append(" GROUP BY m.media_id ");

                if (sort == "year") { sqlBuilder.Append(" ORDER BY m.release_year DESC "); }
                else if (sort == "title") { sqlBuilder.Append(" ORDER BY m.title ASC "); }
                else if (sort == "score") { sqlBuilder.Append(" ORDER BY avg_score DESC "); }
                else { sqlBuilder.Append(" ORDER BY m.media_id ASC "); }

                using var cmd = new NpgsqlCommand(sqlBuilder.ToString(), connection);
                if (!string.IsNullOrEmpty(search)) { cmd.Parameters.AddWithValue("@search", $"%{search}%"); }
                if (!string.IsNullOrEmpty(type)) { cmd.Parameters.AddWithValue("@type", type); }
                if (year.HasValue) { cmd.Parameters.AddWithValue("@year", year.Value); }
                if (age.HasValue) { cmd.Parameters.AddWithValue("@age", age.Value); }
                if (!string.IsNullOrEmpty(genre)) { cmd.Parameters.AddWithValue("@genre", genre); }

                using var reader = cmd.ExecuteReader();
                var list = new JsonArray();

                while (reader.Read())
                {
                    list.Add(new JsonObject
                    {
                        ["id"] = reader.GetInt32(0),
                        ["title"] = reader.GetString(1),
                        ["description"] = reader.GetString(2),
                        ["type"] = reader.GetString(3),
                        ["release_year"] = reader.GetInt32(4),
                        ["age_restriction"] = reader.GetInt32(5),
                        ["creator_id"] = reader.GetInt32(6),
                        ["avg_score"] = Math.Round(reader.GetDouble(7), 1)
                    });
                }

                return list;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error retrieving media list: {ex.Message}");
            }
        }

        public JsonArray GetRatings(int mediaId)
        {
            var list = new JsonArray();
            try
            {
                using var connection = Database.DatabaseConnection.GetConnection();
                connection.Open();

                string sql = @"
                SELECT r.rating_id, r.score, r.comment, r.is_confirmed, u.username,
                       (SELECT COUNT(*) FROM rating_likes rl WHERE rl.rating_id = r.rating_id) as likecount
                FROM ratings r
                JOIN users u ON r.user_id = u.user_id 
                WHERE r.media_id = @mid
                ORDER BY r.created_at DESC";

                using var cmd = new NpgsqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@mid", mediaId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    bool isConfirmed = reader.GetBoolean(3);
                    string rawComment = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);

                    // Only show the comment if it is confirmed.
                    string displayComment = isConfirmed ? rawComment : string.Empty;

                    list.Add(new JsonObject
                    {
                        ["id"] = reader.GetInt32(0),
                        ["user"] = reader.GetString(4),
                        ["score"] = reader.GetInt32(1),
                        ["comment"] = displayComment,
                        ["likes"] = reader.GetInt64(5)
                    });
                }
            }
            catch (Exception) { }
            return list;
        }

        public bool ToggleFavorite(int userId, int mediaId)
        {
            try
            {
                using var connection = Database.DatabaseConnection.GetConnection();
                connection.Open();

                string checkSql = "SELECT COUNT(*) FROM favorites WHERE user_id = @uid AND media_id = @mid";
                using (var checkCmd = new NpgsqlCommand(checkSql, connection))
                {
                    checkCmd.Parameters.AddWithValue("@uid", userId);
                    checkCmd.Parameters.AddWithValue("@mid", mediaId);
                    if ((long)checkCmd.ExecuteScalar()! > 0)
                    {
                        using var delCmd = new NpgsqlCommand("DELETE FROM favorites WHERE user_id = @uid AND media_id = @mid", connection);
                        delCmd.Parameters.AddWithValue("@uid", userId);
                        delCmd.Parameters.AddWithValue("@mid", mediaId);
                        delCmd.ExecuteNonQuery();
                        return false;
                    }
                    else
                    {
                        using var addCmd = new NpgsqlCommand("INSERT INTO favorites (user_id, media_id) VALUES (@uid, @mid)", connection);
                        addCmd.Parameters.AddWithValue("@uid", userId);
                        addCmd.Parameters.AddWithValue("@mid", mediaId);
                        addCmd.ExecuteNonQuery();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error toggling favorite: {ex.Message}");
            }
        }

        public JsonArray GetRecommendations(int userId)
        {
            JsonArray list = new();
            try
            {
                using var connection = Database.DatabaseConnection.GetConnection();
                connection.Open();

                string sql = @"
                    SELECT DISTINCT m.media_id, m.title, m.type, m.release_year, m.creator_id, m.age_restriction
                    FROM media_entries m
                    WHERE m.type IN (
                        SELECT DISTINCT m2.type
                        FROM ratings r
                        JOIN media_entries m2 ON r.media_id = m2.media_id
                        WHERE r.user_id = @uid AND r.score >= 4
                    )
                    AND m.media_id NOT IN (
                        SELECT r2.media_id FROM ratings r2 WHERE r2.user_id = @uid
                    )
                    ORDER BY m.release_year DESC
                    LIMIT 5";

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
                        ["release_year"] = reader.GetInt32(3),
                        ["creator_id"] = reader.GetInt32(4),
                        ["age_restriction"] = reader.GetInt32(5)
                    });
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error loading recommendations: {ex.Message}");
            }
            return list;
        }
        public void AddGenre(int mediaId, string genreName)
        {
            try
            {
                using var connection = Database.DatabaseConnection.GetConnection();
                connection.Open();

                string getGenreSql = "SELECT genre_id FROM genres WHERE LOWER(name) = LOWER(@name)";
                int genreId;
                using (var cmd = new NpgsqlCommand(getGenreSql, connection))
                {
                    cmd.Parameters.AddWithValue("@name", genreName);
                    object? result = cmd.ExecuteScalar();
                    if (result == null) throw new InvalidOperationException($"Genre '{genreName}' does not exist.");
                    genreId = (int)result;
                }

                string linkSql = "INSERT INTO media_genres (media_id, genre_id) VALUES (@mid, @gid) ON CONFLICT DO NOTHING";
                using (var cmd = new NpgsqlCommand(linkSql, connection))
                {
                    cmd.Parameters.AddWithValue("@mid", mediaId);
                    cmd.Parameters.AddWithValue("@gid", genreId);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to add genre: {ex.Message}");
            }
        }

    }
}
