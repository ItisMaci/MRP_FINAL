using MyMediaList_HttpServer.Server;
using MyMediaList_HttpServer.System.Genre;
using System.Net;
using System.Text.Json.Nodes;

namespace MyMediaList_HttpServer.Handlers
{
    public sealed class GenreHandler : Handler, IHandler
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // private readonly members                                                                                         //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Repository for genre data access.</summary>
        private readonly GenreRepository _GenreRepo = new();


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // [override] Handler                                                                                               //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Handles a request if possible.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        public override void Handle(HttpRestEventArgs e)
        {
            if (!e.Path.StartsWith("/genres")) { return; }

            // --------------------------------------------------------------------------------
            // POST /genres (Create) - Admin Only
            // --------------------------------------------------------------------------------
            if (e.Path == "/genres" && e.Method == HttpMethod.Post)
            {
                if (e.Session == null || !e.Session.IsAdmin)
                {
                    e.Respond(HttpStatusCode.Forbidden,
                        new JsonObject { ["success"] = false, ["reason"] = "Admin privileges required." });
                    e.Responded = true;
                    return;
                }

                try
                {
                    Genre genre = new()
                    {
                        Name = e.Content?["name"]?.GetValue<string>() ?? string.Empty
                    };

                    _GenreRepo.Add(genre);

                    e.Respond(HttpStatusCode.OK,
                        new JsonObject { ["success"] = true, ["message"] = "Genre created.", ["id"] = genre.Id });
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"[{nameof(GenreHandler)}] Created genre '{genre.Name}'.");
                }
                catch (Exception ex)
                {
                    e.Respond(HttpStatusCode.InternalServerError,
                        new JsonObject { ["success"] = false, ["reason"] = ex.Message });
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[{nameof(GenreHandler)}] Error: {ex.Message}");
                }
            }
            // --------------------------------------------------------------------------------
            // GET /genres/{name}
            // --------------------------------------------------------------------------------
            else if (e.Path.StartsWith("/genres/") && e.Method == HttpMethod.Get)
            {
                try
                {
                    string name = e.Path.Substring("/genres/".Length);
                    Genre? genre = _GenreRepo.GetByName(name);

                    if (genre == null)
                    {
                        e.Respond(HttpStatusCode.NotFound,
                            new JsonObject { ["success"] = false, ["reason"] = "Genre not found." });
                    }
                    else
                    {
                        e.Respond(HttpStatusCode.OK,
                            new JsonObject { ["success"] = true, ["name"] = genre.Name, ["id"] = genre.Id });
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine($"[{nameof(GenreHandler)}] Handled {e.Method} {e.Path}.");
                    }
                }
                catch (Exception ex)
                {
                    e.Respond(HttpStatusCode.InternalServerError,
                        new JsonObject { ["success"] = false, ["reason"] = ex.Message });
                }
            }
            // --------------------------------------------------------------------------------
            // DELETE /genres/{name} - Admin Only
            // --------------------------------------------------------------------------------
            else if (e.Path.StartsWith("/genres/") && e.Method == HttpMethod.Delete)
            {
                if (e.Session == null || !e.Session.IsAdmin)
                {
                    e.Respond(HttpStatusCode.Forbidden,
                        new JsonObject { ["success"] = false, ["reason"] = "Admin privileges required." });
                    e.Responded = true;
                    return;
                }

                try
                {
                    string name = e.Path.Substring("/genres/".Length);
                    Genre? genre = _GenreRepo.GetByName(name);

                    if (genre == null)
                    {
                        e.Respond(HttpStatusCode.NotFound,
                            new JsonObject { ["success"] = false, ["reason"] = "Genre not found." });
                    }
                    else
                    {
                        _GenreRepo.Delete(genre.Id);
                        e.Respond(HttpStatusCode.OK,
                            new JsonObject { ["success"] = true, ["message"] = "Genre deleted." });
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine($"[{nameof(GenreHandler)}] Deleted genre '{name}'.");
                    }
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
                    new JsonObject { ["success"] = false, ["reason"] = "Invalid endpoint." });
            }

            e.Responded = true;
        }
    }
}
