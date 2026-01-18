namespace MyMediaList_HttpServer.System.Rating
{
    public interface IRatingRepository
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // methods                                                                                                          //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Retrieves a rating by its ID.</summary>
        Rating? GetById(int id);

        /// <summary>Creates a new rating.</summary>
        void Add(Rating rating);

        /// <summary>Updates an existing rating.</summary>
        void Update(Rating rating);

        /// <summary>Deletes a rating by its ID.</summary>
        void Delete(int id);

        /// <summary>Toggles a "like" on a rating.</summary>
        bool ToggleLike(int userId, int ratingId);
    }
}
