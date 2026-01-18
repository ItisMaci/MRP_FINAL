namespace MyMediaList_HttpServer.System.Media
{
    public sealed class Media
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // public properties                                                                                                //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Gets or sets the unique media ID.</summary>
        public int Id { get; set; }

        /// <summary>Gets or sets the title of the media.</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Gets or sets the description of the media.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Gets or sets the type of media (e.g., Movie, Game).</summary>
        public string Type { get; set; } = "Movie";

        /// <summary>Gets or sets the release year.</summary>
        public int ReleaseYear { get; set; }

        /// <summary>Gets or sets the age restriction.</summary>
        public int AgeRestriction { get; set; }

        /// <summary>Gets or sets the ID of the user who created this entry.</summary>
        public int CreatorId { get; set; }

        /// <summary>Gets or sets the average score (calculated field).</summary>
        public double AverageScore { get; set; }
    }
}
