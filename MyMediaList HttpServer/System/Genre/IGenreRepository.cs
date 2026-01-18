namespace MyMediaList_HttpServer.System.Genre
{
    public interface IGenreRepository
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // methods                                                                                                          //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Retrieves a genre by its name.</summary>
        Genre? GetByName(string name);

        /// <summary>Creates a new genre.</summary>
        void Add(Genre genre);

        /// <summary>Updates an existing genre.</summary>
        void Update(Genre genre);

        /// <summary>Deletes a genre by its ID.</summary>
        void Delete(int id);
    }
}
