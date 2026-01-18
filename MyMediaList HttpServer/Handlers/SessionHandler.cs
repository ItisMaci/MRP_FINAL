using MyMediaList_HttpServer.Server;
using MyMediaList_HttpServer.System.Session;
using System.Net;
using System.Text.Json.Nodes;

namespace MyMediaList_HttpServer.Handlers
{
    public sealed class SessionHandler : Handler, IHandler
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // [override] Handler                                                                                               //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Handles a request if possible.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        public override void Handle(HttpRestEventArgs e)
        {
            if (!e.Path.StartsWith("/api/login")) { return; }

            // --------------------------------------------------------------------------------
            // POST /api/login (Login)
            // --------------------------------------------------------------------------------
            if (e.Path == "/api/login" && e.Method == HttpMethod.Post)
            {
                try
                {
                    string username = e.Content?["username"]?.GetValue<string>() ?? string.Empty;
                    string password = e.Content?["password"]?.GetValue<string>() ?? string.Empty;

                    Session? session = SessionManager.Login(username, password);

                    if (session is null)
                    {
                        e.Respond(HttpStatusCode.Unauthorized,
                            new JsonObject { ["success"] = false, ["reason"] = "Invalid username or password." });

                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[{nameof(SessionHandler)}] Invalid login attempt for '{username}'.");
                    }
                    else
                    {
                        e.Respond(HttpStatusCode.OK,
                            new JsonObject { ["success"] = true, ["token"] = session.Token });

                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine($"[{nameof(SessionHandler)}] User '{username}' logged in.");
                    }
                }
                catch (Exception ex)
                {
                    e.Respond(HttpStatusCode.InternalServerError,
                        new JsonObject { ["success"] = false, ["reason"] = ex.Message });

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[{nameof(SessionHandler)}] Exception creating session: {ex.Message}");
                }
            }
            // --------------------------------------------------------------------------------
            // DELETE /api/login (Logout)
            // --------------------------------------------------------------------------------
            else if (e.Path == "/api/login" && e.Method == HttpMethod.Delete)
            {
                if (e.Session != null)
                {
                    SessionManager.Close(e.Session.Token);
                    e.Respond(HttpStatusCode.OK,
                        new JsonObject { ["success"] = true, ["message"] = "Logged out." });

                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"[{nameof(SessionHandler)}] User logged out.");
                }
                else
                {
                    e.Respond(HttpStatusCode.Unauthorized,
                        new JsonObject { ["success"] = false, ["reason"] = "No active session." });
                }
            }
            else
            {
                e.Respond(HttpStatusCode.BadRequest,
                    new JsonObject { ["success"] = false, ["reason"] = "Invalid session endpoint." });

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[{nameof(SessionHandler)}] Invalid session endpoint.");
            }

            e.Responded = true;
        }
    }
}
