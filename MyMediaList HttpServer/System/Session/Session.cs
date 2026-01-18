namespace MyMediaList_HttpServer.System.Session
{
    public sealed class Session
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // public properties                                                                                                //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Gets or sets the session token.</summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>Gets or sets the user name of the session owner.</summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>Gets or sets the session timestamp (last activity).</summary>
        public DateTime Timestamp { get; set; }

        /// <summary>Gets or sets a value indicating if the session owner has administrative privileges.</summary>
        public bool IsAdmin { get; set; }

        /// <summary>Gets if the session is expired based on the provided timeout.</summary>
        public bool IsExpired(int timeoutMinutes)
        {
            return (DateTime.UtcNow - Timestamp).TotalMinutes > timeoutMinutes;
        }
    }
}
