using MyMediaList_HttpServer.Server;
using MyMediaList_HttpServer.System.User;
using System.Net;
using System.Text.Json.Nodes;

namespace MyMediaList_HttpServer.Handlers
{
    public sealed class UserHandler : Handler, IHandler
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // private readonly members                                                                                         //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Repository for user data access.</summary>
        private readonly UserRepository _UserRepo = new();


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // [override] Handler                                                                                               //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Handles a request if possible.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        public override void Handle(HttpRestEventArgs e)
        {
            if (!e.Path.StartsWith("/users")) { return; }

            // --------------------------------------------------------------------------------
            // POST /users (Register) - No Auth Required
            // --------------------------------------------------------------------------------
            if (e.Path == "/users" && e.Method == HttpMethod.Post)
            {
                try
                {
                    string username = e.Content?["username"]?.GetValue<string>() ?? string.Empty;
                    string password = e.Content?["password"]?.GetValue<string>() ?? string.Empty;

                    User user = new() { UserName = username };
                    user.SetPassword(password);

                    _UserRepo.Add(user);

                    e.Respond(HttpStatusCode.OK,
                        new JsonObject { ["success"] = true, ["message"] = "User created." });
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"[{nameof(UserHandler)}] Handled {e.Method} {e.Path}.");
                }
                catch (Exception ex)
                {
                    e.Respond(HttpStatusCode.InternalServerError,
                        new JsonObject { ["success"] = false, ["reason"] = ex.Message });
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[{nameof(UserHandler)}] Exception creating user: {ex.Message}");
                }

                e.Responded = true;
                return;
            }

            // --------------------------------------------------------------------------------
            // AUTHENTICATION CHECK (For everything else)
            // --------------------------------------------------------------------------------
            if (e.Session is null)
            {
                e.Respond(HttpStatusCode.Unauthorized,
                    new JsonObject { ["success"] = false, ["reason"] = "Authentication required." });
                e.Responded = true;
                return;
            }

            // --------------------------------------------------------------------------------
            // GET /users/leaderboard
            // --------------------------------------------------------------------------------
            if (e.Path == "/users/leaderboard" && e.Method == HttpMethod.Get)
            {
                try
                {
                    JsonArray leaderboard = _UserRepo.GetLeaderboard();
                    e.Respond(HttpStatusCode.OK, new JsonObject
                    {
                        ["success"] = true,
                        ["leaderboard"] = leaderboard
                    });
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"[{nameof(UserHandler)}] Handled {e.Method} {e.Path}.");
                }
                catch (Exception ex)
                {
                    e.Respond(HttpStatusCode.InternalServerError,
                        new JsonObject { ["success"] = false, ["reason"] = ex.Message });
                }
                e.Responded = true;
                return;
            }

            // Parse Username from URL: /users/{username}/...
            string[] segments = e.Path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 2)
            {
                e.Respond(HttpStatusCode.BadRequest,
                    new JsonObject { ["success"] = false, ["reason"] = "Invalid URL format." });
                e.Responded = true;
                return;
            }
            string targetUsername = segments[1];

            // --------------------------------------------------------------------------------
            // GET /users/{username}/profile
            // --------------------------------------------------------------------------------
            if (e.Path.EndsWith("/profile") && e.Method == HttpMethod.Get)
            {
                try
                {
                    User? user = _UserRepo.GetByUsername(targetUsername);
                    if (user == null) { throw new InvalidOperationException("User not found."); }

                    JsonObject stats = _UserRepo.GetStatistics(targetUsername);

                    e.Respond(HttpStatusCode.OK, new JsonObject
                    {
                        ["success"] = true,
                        ["username"] = user.UserName,
                        ["stats"] = stats
                    });
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"[{nameof(UserHandler)}] Handled {e.Method} {e.Path}.");
                }
                catch (Exception ex)
                {
                    e.Respond(HttpStatusCode.NotFound,
                        new JsonObject { ["success"] = false, ["reason"] = ex.Message });
                }
            }
            // --------------------------------------------------------------------------------
            // GET /users/{username}/favorites
            // --------------------------------------------------------------------------------
            else if (e.Path.EndsWith("/favorites") && e.Method == HttpMethod.Get)
            {
                try
                {
                    JsonArray favorites = _UserRepo.GetFavorites(targetUsername);
                    e.Respond(HttpStatusCode.OK, new JsonObject
                    {
                        ["success"] = true,
                        ["username"] = targetUsername,
                        ["favorites"] = favorites
                    });
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"[{nameof(UserHandler)}] Handled {e.Method} {e.Path}.");
                }
                catch (Exception ex)
                {
                    e.Respond(HttpStatusCode.InternalServerError,
                        new JsonObject { ["success"] = false, ["reason"] = ex.Message });
                }
            }
            // --------------------------------------------------------------------------------
            // GET /users/{username}/ratings
            // --------------------------------------------------------------------------------
            else if (segments.Length == 3 && segments[2] == "ratings" && e.Method == HttpMethod.Get)
            {
                JsonArray history = _UserRepo.GetRatingHistory(targetUsername);
                e.Respond(HttpStatusCode.OK, new JsonObject { ["success"] = true, ["history"] = history });
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"[{nameof(UserHandler)}] Handled {e.Method} {e.Path}.");
                e.Responded = true;
                return;
            }
            // --------------------------------------------------------------------------------
            // GET /users/{username} (Simple Info)
            // --------------------------------------------------------------------------------
            else if (segments.Length == 2 && e.Method == HttpMethod.Get)
            {
                try
                {
                    User? user = _UserRepo.GetByUsername(targetUsername);
                    if (user == null)
                    {
                        e.Respond(HttpStatusCode.NotFound,
                            new JsonObject { ["success"] = false, ["reason"] = "User not found." });
                    }
                    else
                    {
                        e.Respond(HttpStatusCode.OK,
                            new JsonObject { ["success"] = true, ["username"] = user.UserName });
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine($"[{nameof(UserHandler)}] Handled {e.Method} {e.Path}.");
                    }
                }
                catch (Exception ex)
                {
                    e.Respond(HttpStatusCode.InternalServerError,
                        new JsonObject { ["success"] = false, ["reason"] = ex.Message });
                }
            }
            // --------------------------------------------------------------------------------
            // PUT /users/{username} (Update Password) - REQUIRES OWNERSHIP
            // --------------------------------------------------------------------------------
            else if (segments.Length == 2 && e.Method == HttpMethod.Put)
            {
                if (e.Session.UserName != targetUsername && !e.Session.IsAdmin)
                {
                    e.Respond(HttpStatusCode.Forbidden,
                        new JsonObject { ["success"] = false, ["reason"] = "You can only edit your own account." });
                    e.Responded = true;
                    return;
                }

                try
                {
                    User? user = _UserRepo.GetByUsername(targetUsername);
                    if (user == null) { throw new InvalidOperationException("User not found."); }

                    string newPass = e.Content?["password"]?.GetValue<string>() ?? string.Empty;
                    user.SetPassword(newPass);

                    _UserRepo.Update(user);

                    e.Respond(HttpStatusCode.OK, new JsonObject
                    {
                        ["success"] = true,
                        ["message"] = $"User '{targetUsername}' updated."
                    });
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"[{nameof(UserHandler)}] Handled {e.Method} {e.Path}.");
                }
                catch (Exception ex)
                {
                    e.Respond(HttpStatusCode.InternalServerError,
                        new JsonObject { ["success"] = false, ["reason"] = ex.Message });
                }
            }
            // --------------------------------------------------------------------------------
            // DELETE /users/{username} - REQUIRES OWNERSHIP
            // --------------------------------------------------------------------------------
            else if (segments.Length == 2 && e.Method == HttpMethod.Delete)
            {
                if (e.Session.UserName != targetUsername && !e.Session.IsAdmin)
                {
                    e.Respond(HttpStatusCode.Forbidden,
                        new JsonObject { ["success"] = false, ["reason"] = "You can only delete your own account." });
                    e.Responded = true;
                    return;
                }

                try
                {
                    _UserRepo.Delete(targetUsername);
                    e.Respond(HttpStatusCode.OK, new JsonObject
                    {
                        ["success"] = true,
                        ["message"] = $"User '{targetUsername}' deleted."
                    });
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"[{nameof(UserHandler)}] Handled {e.Method} {e.Path}.");
                }
                catch (Exception ex)
                {
                    e.Respond(HttpStatusCode.InternalServerError,
                        new JsonObject { ["success"] = false, ["reason"] = ex.Message });
                }
            }
            else
            {
                e.Respond(HttpStatusCode.BadRequest,
                    new JsonObject { ["success"] = false, ["reason"] = "Invalid user endpoint." });
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[{nameof(UserHandler)}] Invalid user endpoint.");
            }

            e.Responded = true;
        }
    }
}