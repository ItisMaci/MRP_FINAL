using System.Security.Cryptography;
using System.Text;

namespace MyMediaList_HttpServer.System.User
{
    public sealed class User
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // public properties                                                                                                //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Gets or sets the unique user ID.</summary>
        public int Id { get; set; }

        /// <summary>Gets or sets the username.</summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>Gets or sets the hashed password.</summary>
        public string PasswordHash { get; set; } = string.Empty;

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // public methods                                                                                                   //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sets the password for the user by hashing the input.
        /// </summary>
        /// <param name="password">The plain text password.</param>
        public void SetPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(UserName)) { throw new InvalidOperationException("Set UserName before setting password."); }

            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(UserName + password));
            PasswordHash = Convert.ToHexString(bytes).ToLower();
        }

        /// <summary>
        /// Verifies if a provided password matches the user's hash.
        /// </summary>
        public bool VerifyPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(UserName)) { return false; }

            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(UserName + password));
            string hash = Convert.ToHexString(bytes).ToLower();

            return hash == PasswordHash;
        }
    }
}
