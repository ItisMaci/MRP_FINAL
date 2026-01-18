namespace MyMediaList_HttpServer.System.Genre
{
    public sealed class Genre
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // public properties                                                                                                //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Gets or sets the unique genre ID.</summary>
        public int Id { get; set; }

        /// <summary>Gets or sets the name of the genre.</summary>
        public string Name { get; set; } = string.Empty;
    }
}
