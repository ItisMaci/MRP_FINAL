using System.Text.Json.Nodes;

namespace MyMediaList_HttpServer.System.User
{
    public interface IUserRepository
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // methods                                                                                                          //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Retrieves a user by their username.</summary>
        User? GetByUsername(string username);

        /// <summary>Retrieves a user ID by their username.</summary>
        int GetId(string username);

        /// <summary>Creates a new user in the database.</summary>
        void Add(User user);

        /// <summary>Updates an existing user's password.</summary>
        void Update(User user);

        /// <summary>Deletes a user from the database.</summary>
        void Delete(string username);

        /// <summary>Checks if a user exists.</summary>
        bool Exists(string username);

        /// <summary>Gets usage statistics for the user.</summary>
        JsonObject GetStatistics(string username);

        /// <summary>Gets the list of favorite media entries for this user.</summary>
        JsonArray GetFavorites(string username);

        /// <summary>Gets the leaderboard of users sorted by rating count.</summary>
        JsonArray GetLeaderboard();

        /// <summary>Gets rating history of a specific user.</summary>
        public JsonArray GetRatingHistory(string username);
    }
}
