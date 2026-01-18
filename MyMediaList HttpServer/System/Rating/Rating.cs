namespace MyMediaList_HttpServer.System.Rating
{
    public sealed class Rating
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // public properties                                                                                                //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Gets or sets the unique rating ID.</summary>
        public int Id { get; set; }

        /// <summary>Gets or sets the user ID who created the rating.</summary>
        public int UserId { get; set; }

        /// <summary>Gets or sets the media ID being rated.</summary>
        public int MediaId { get; set; }

        /// <summary>Gets or sets the score (1-5).</summary>
        public int Score { get; set; }

        /// <summary>Gets or sets the comment/review.</summary>
        public string Comment { get; set; } = string.Empty;

        /// <summary>Gets or sets if the comment is confirmed (visible).</summary>
        public bool IsConfirmed { get; set; }

        /// <summary>Time of entry creation.</summary>
        public DateTime CreatedAt { get; set; }
    }
}
