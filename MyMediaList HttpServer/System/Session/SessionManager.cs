using MyMediaList_HttpServer.System.User;
using System.Text;

namespace MyMediaList_HttpServer.System.Session
{
    public sealed class SessionManager
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // private constants                                                                                                //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Alphabet used for token generation.</summary>
        private const string _ALPHABET = "1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

        /// <summary>Session timeout in minutes.</summary>
        private const int _TIMEOUT_MINUTES = 30;


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // private static members                                                                                           //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Dictionary of active sessions indexed by token.</summary>
        private static readonly Dictionary<string, Session> _Sessions = new();

        /// <summary>Repository for verifying user credentials.</summary>
        private static readonly UserRepository _UserRepository = new();


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // public static methods                                                                                            //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Attempts to log in a user and create a new session.
        /// </summary>
        /// <param name="userName">The username.</param>
        /// <param name="password">The plain text password.</param>
        /// <returns>A new Session object if successful, otherwise null.</returns>
        public static Session? Login(string userName, string password)
        {
            // 1. Verify User Credentials using UserRepository
            // Note: This relies on the User model and repository we refactored earlier
            User.User? user = _UserRepository.GetByUsername(userName);

            if (user == null || !user.VerifyPassword(password))
            {
                return null;
            }

            // 2. Create Session Object
            Session session = new Session
            {
                UserName = user.UserName,
                IsAdmin = (user.UserName == "admin"),
                Timestamp = DateTime.UtcNow,
                Token = _GenerateToken()
            };

            // 3. Store in Memory
            lock (_Sessions)
            {
                _Sessions[session.Token] = session;
            }

            return session;
        }

        /// <summary>
        /// Retrieves a valid session by its token and refreshes its timestamp.
        /// </summary>
        /// <param name="token">The session token.</param>
        /// <returns>The Session object if found and valid, otherwise null.</returns>
        public static Session? GetSession(string token)
        {
            _Cleanup(); // Remove expired sessions first

            Session? session = null;

            lock (_Sessions)
            {
                if (_Sessions.TryGetValue(token, out session))
                {
                    session.Timestamp = DateTime.UtcNow; // Refresh activity
                }
            }

            return session;
        }

        /// <summary>
        /// Closes (removes) a specific session.
        /// </summary>
        /// <param name="token">The token of the session to close.</param>
        public static void Close(string token)
        {
            lock (_Sessions)
            {
                if (_Sessions.ContainsKey(token))
                {
                    _Sessions.Remove(token);
                }
            }
        }


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // private static methods                                                                                           //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Generates a random 24-character token.
        /// </summary>
        private static string _GenerateToken()
        {
            StringBuilder sb = new StringBuilder();
            Random rnd = new Random();
            for (int i = 0; i < 24; i++)
            {
                sb.Append(_ALPHABET[rnd.Next(0, _ALPHABET.Length)]);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Removes all sessions that have exceeded the timeout duration.
        /// </summary>
        private static void _Cleanup()
        {
            List<string> toRemove = new List<string>();

            lock (_Sessions)
            {
                foreach (KeyValuePair<string, Session> pair in _Sessions)
                {
                    if (pair.Value.IsExpired(_TIMEOUT_MINUTES))
                    {
                        toRemove.Add(pair.Key);
                    }
                }

                foreach (string key in toRemove)
                {
                    _Sessions.Remove(key);
                }
            }
        }
    }
}
