using System.Text.Json.Nodes;

namespace MyMediaList_HttpServer.System.Media
{
    public interface IMediaRepository
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // methods                                                                                                          //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Retrieves a media entry by its ID.</summary>
        Media? GetById(int id);

        /// <summary>Creates a new media entry.</summary>
        void Add(Media media);

        /// <summary>Updates an existing media entry.</summary>
        void Update(Media media);

        /// <summary>Deletes a media entry by its ID.</summary>
        void Delete(int id);

        /// <summary>Gets a filtered list of media entries.</summary>
        JsonArray GetList(string? search = null, string? type = null, int? year = null, int? age = null, string? sort = null, string? genre = null);

        /// <summary>Gets the list of ratings for a specific media entry.</summary>
        JsonArray GetRatings(int mediaId);

        /// <summary>Toggles the favorite status for a user/media pair.</summary>
        bool ToggleFavorite(int userId, int mediaId);

        /// <summary>Gets recommendations for a user.</summary>
        JsonArray GetRecommendations(int userId);

        /// <summary>Assigns a genre to a specific media entry.</summary>
        void AddGenre(int mediaId, string genreName);
    }
}
