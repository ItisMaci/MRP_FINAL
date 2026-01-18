using Npgsql;

namespace MyMediaList_HttpServer.System.Genre
{
    public sealed class GenreRepository : IGenreRepository
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // public methods                                                                                                   //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public Genre? GetByName(string name)
        {
            try
            {
                using var connection = Database.DatabaseConnection.GetConnection();
                connection.Open();

                string sql = "SELECT genre_id, name FROM genres WHERE name = @name";
                using var cmd = new NpgsqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@name", name);

                using var reader = cmd.ExecuteReader();

                if (!reader.Read()) { return null; }

                return new Genre
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1)
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Database error loading genre: {ex.Message}");
            }
        }

        public void Add(Genre genre)
        {
            if (string.IsNullOrWhiteSpace(genre.Name)) { throw new InvalidOperationException("Name cannot be empty."); }

            try
            {
                using var connection = Database.DatabaseConnection.GetConnection();
                connection.Open();

                string sql = "INSERT INTO genres (name) VALUES (@name) RETURNING genre_id";
                using var cmd = new NpgsqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@name", genre.Name);

                genre.Id = (int)cmd.ExecuteScalar()!;
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                throw new InvalidOperationException($"Genre '{genre.Name}' already exists.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to save genre.", ex);
            }
        }

        public void Update(Genre genre)
        {
            if (string.IsNullOrWhiteSpace(genre.Name)) { throw new InvalidOperationException("Name cannot be empty."); }

            try
            {
                using var connection = Database.DatabaseConnection.GetConnection();
                connection.Open();

                string sql = "UPDATE genres SET name = @name WHERE genre_id = @id";
                using var cmd = new NpgsqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@name", genre.Name);
                cmd.Parameters.AddWithValue("@id", genre.Id);

                if (cmd.ExecuteNonQuery() == 0) { throw new InvalidOperationException("Genre no longer exists."); }
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                throw new InvalidOperationException($"Genre '{genre.Name}' already exists.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to update genre.", ex);
            }
        }

        public void Delete(int id)
        {
            try
            {
                using var connection = Database.DatabaseConnection.GetConnection();
                connection.Open();

                string sql = "DELETE FROM genres WHERE genre_id = @id";
                using var cmd = new NpgsqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to delete genre.", ex);
            }
        }
    }
}
