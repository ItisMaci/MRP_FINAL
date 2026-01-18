using MyMediaList_HttpServer.Handlers;
using MyMediaList_HttpServer.Server;
using MyMediaList_HttpServer.System; // for Media, MediaRepository, UserRepository
using MyMediaList_HttpServer.System.Media;
using MyMediaList_HttpServer.System.User;
using System;
using System.Net;
using System.Text.Json.Nodes;

namespace MyMediaList.System
{
    public sealed class MediaHandler : Handler, IHandler
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // private readonly members                                                                                         //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Repository for media data access.</summary>
        private readonly MediaRepository _MediaRepo = new();

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
            if (!e.Path.StartsWith("/media", StringComparison.OrdinalIgnoreCase)) { return; }

            try
            {
                string subPath = e.Path.Substring("/media".Length);
                string[] segments = subPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

                // --------------------------------------------------------------------------------
                // /media (List or Create)
                // --------------------------------------------------------------------------------
                if (segments.Length == 0)
                {
                    if (e.Method == HttpMethod.Get)
                    {
                        var query = e.Context.Request.QueryString;
                        string? search = query["search"];
                        string? type = query["type"];
                        string? sort = query["sort"];
                        string? genre = query["genre"];
                        int? year = int.TryParse(query["year"], out int y) ? y : null;
                        int? age = int.TryParse(query["age"], out int a) ? a : null;

                        JsonArray list = _MediaRepo.GetList(search, type, year, age, sort, genre);

                        e.Respond(HttpStatusCode.OK, new JsonObject { ["success"] = true, ["data"] = list });
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine($"[{nameof(MediaHandler)}] Handled {e.Method} {e.Path}.");
                    }
                    else if (e.Method == HttpMethod.Post)
                    {
                        if (e.Session == null)
                        {
                            e.Respond(HttpStatusCode.Unauthorized,
                                new JsonObject { ["success"] = false, ["reason"] = "Authentication required." });
                            e.Responded = true;
                            return;
                        }

                        Media media = new()
                        {
                            Title = e.Content?["title"]?.GetValue<string>() ?? string.Empty,
                            Description = e.Content?["description"]?.GetValue<string>() ?? string.Empty,
                            Type = e.Content?["type"]?.GetValue<string>() ?? "Movie",
                            ReleaseYear = e.Content?["release_year"]?.GetValue<int>() ?? 0,
                            AgeRestriction = e.Content?["age_restriction"]?.GetValue<int>() ?? 0
                        };

                        int userId = _UserRepo.GetId(e.Session.UserName);
                        media.CreatorId = userId;

                        _MediaRepo.Add(media);

                        e.Respond(HttpStatusCode.Created,
                            new JsonObject
                            {
                                ["success"] = true,
                                ["id"] = media.Id,
                                ["message"] = "Media entry created."
                            });
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine($"[{nameof(MediaHandler)}] Created media '{media.Title}'.");
                    }
                    else
                    {
                        e.Respond(HttpStatusCode.MethodNotAllowed,
                            new JsonObject { ["success"] = false, ["reason"] = "Method not allowed." });
                    }
                }
                // --------------------------------------------------------------------------------
                // /media/recommendations
                // --------------------------------------------------------------------------------
                else if (segments.Length == 1 && segments[0] == "recommendations")
                {
                    if (e.Method == HttpMethod.Get)
                    {
                        if (e.Session == null)
                        {
                            e.Respond(HttpStatusCode.Unauthorized,
                                new JsonObject { ["success"] = false, ["reason"] = "Authentication required." });
                            e.Responded = true;
                            return;
                        }

                        int userId = _UserRepo.GetId(e.Session.UserName);
                        JsonArray recs = _MediaRepo.GetRecommendations(userId);

                        e.Respond(HttpStatusCode.OK, new JsonObject
                        {
                            ["success"] = true,
                            ["recommendations"] = recs
                        });
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine($"[{nameof(MediaHandler)}] Handled {e.Method} {e.Path}.");
                    }
                    else
                    {
                        e.Respond(HttpStatusCode.MethodNotAllowed,
                            new JsonObject { ["success"] = false, ["reason"] = "Method not allowed." });
                    }
                }
                // --------------------------------------------------------------------------------
                // /media/{id} ...
                // --------------------------------------------------------------------------------
                else if (int.TryParse(segments[0], out int id))
                {
                    // /media/{id} (Get, Update, Delete)
                    if (segments.Length == 1)
                    {
                        if (e.Method == HttpMethod.Get)
                        {
                            Media? media = _MediaRepo.GetById(id);
                            if (media == null) { throw new InvalidOperationException($"Media {id} not found."); }

                            JsonArray ratings = _MediaRepo.GetRatings(id);

                            double avgScore = 0.0;
                            if (ratings.Count > 0)
                            {
                                double sum = 0;
                                foreach (var node in ratings) { sum += node!["score"]!.GetValue<int>(); }
                                avgScore = Math.Round(sum / ratings.Count, 1);
                            }

                            e.Respond(HttpStatusCode.OK, new JsonObject
                            {
                                ["success"] = true,
                                ["id"] = media.Id,
                                ["title"] = media.Title,
                                ["description"] = media.Description,
                                ["type"] = media.Type,
                                ["release_year"] = media.ReleaseYear,
                                ["age_restriction"] = media.AgeRestriction,
                                ["creator_id"] = media.CreatorId,
                                ["avg_score"] = avgScore,
                                ["ratings"] = ratings
                            });
                            Console.ForegroundColor = ConsoleColor.Blue;
                            Console.WriteLine($"[{nameof(MediaHandler)}] Handled {e.Method} {e.Path}.");
                        }
                        else if (e.Method == HttpMethod.Put)
                        {
                            if (e.Session == null)
                            {
                                e.Respond(HttpStatusCode.Unauthorized,
                                    new JsonObject { ["success"] = false, ["reason"] = "Authentication required." });
                                e.Responded = true;
                                return;
                            }

                            Media? media = _MediaRepo.GetById(id);
                            if (media == null) { throw new InvalidOperationException($"Media {id} not found."); }

                            int currentUserId = _UserRepo.GetId(e.Session.UserName);

                            if (media.CreatorId != currentUserId && !e.Session.IsAdmin)
                            {
                                e.Respond(HttpStatusCode.Forbidden,
                                    new JsonObject { ["success"] = false, ["reason"] = "You can only edit your own entries." });
                                e.Responded = true;
                                return;
                            }

                            if (e.Content?.ContainsKey("title") == true) { media.Title = e.Content["title"]!.GetValue<string>(); }
                            if (e.Content?.ContainsKey("description") == true) { media.Description = e.Content["description"]!.GetValue<string>(); }
                            if (e.Content?.ContainsKey("type") == true) { media.Type = e.Content["type"]!.GetValue<string>(); }
                            if (e.Content?.ContainsKey("release_year") == true) { media.ReleaseYear = e.Content["release_year"]!.GetValue<int>(); }
                            if (e.Content?.ContainsKey("age_restriction") == true) { media.AgeRestriction = e.Content["age_restriction"]!.GetValue<int>(); }

                            _MediaRepo.Update(media);

                            e.Respond(HttpStatusCode.OK, new JsonObject { ["success"] = true, ["message"] = "Media updated." });
                            Console.ForegroundColor = ConsoleColor.Blue;
                            Console.WriteLine($"[{nameof(MediaHandler)}] Updated media {id}.");
                        }
                        else if (e.Method == HttpMethod.Delete)
                        {
                            if (e.Session == null)
                            {
                                e.Respond(HttpStatusCode.Unauthorized,
                                    new JsonObject { ["success"] = false, ["reason"] = "Authentication required." });
                                e.Responded = true;
                                return;
                            }

                            Media? media = _MediaRepo.GetById(id);
                            if (media == null)
                            {
                                e.Respond(HttpStatusCode.NotFound,
                                    new JsonObject { ["success"] = false, ["reason"] = "Media not found." });
                                return;
                            }

                            int currentUserId = _UserRepo.GetId(e.Session.UserName);

                            if (media.CreatorId != currentUserId && !e.Session.IsAdmin)
                            {
                                e.Respond(HttpStatusCode.Forbidden,
                                    new JsonObject { ["success"] = false, ["reason"] = "You can only delete your own entries." });
                                e.Responded = true;
                                return;
                            }

                            _MediaRepo.Delete(id);
                            e.Respond(HttpStatusCode.OK, new JsonObject { ["success"] = true, ["message"] = "Media deleted." });
                            Console.ForegroundColor = ConsoleColor.Blue;
                            Console.WriteLine($"[{nameof(MediaHandler)}] Deleted media {id}.");
                        }
                        else
                        {
                            e.Respond(HttpStatusCode.MethodNotAllowed,
                                new JsonObject { ["success"] = false, ["reason"] = "Method not allowed." });
                        }
                    }
                    // Sub-case: /media/{id}/favorite
                    else if (segments.Length == 2 && segments[1] == "favorite")
                    {
                        if (e.Method == HttpMethod.Post)
                        {
                            if (e.Session == null)
                            {
                                e.Respond(HttpStatusCode.Unauthorized,
                                    new JsonObject { ["success"] = false, ["reason"] = "Authentication required." });
                                e.Responded = true;
                                return;
                            }

                            int userId = _UserRepo.GetId(e.Session.UserName);
                            bool isFavorite = _MediaRepo.ToggleFavorite(userId, id);

                            e.Respond(HttpStatusCode.OK,
                                new JsonObject { ["success"] = true, ["is_favorite"] = isFavorite });
                            Console.ForegroundColor = ConsoleColor.Blue;
                            Console.WriteLine($"[{nameof(MediaHandler)}] Toggled favorite for media {id}.");
                        }
                        else
                        {
                            e.Respond(HttpStatusCode.MethodNotAllowed,
                                new JsonObject { ["success"] = false, ["reason"] = "Method not allowed." });
                        }
                    }
                    // Sub-case: /media/{id}/genres
                    else if (segments.Length == 2 && segments[1] == "genres")
                    {
                        if (e.Method == HttpMethod.Post)
                        {
                            if (e.Session == null)
                            {
                                e.Respond(HttpStatusCode.Unauthorized, new JsonObject { ["success"] = false, ["reason"] = "Authentication required." });
                                e.Responded = true;
                                return;
                            }

                            Media? media = _MediaRepo.GetById(id);
                            int currentUserId = _UserRepo.GetId(e.Session.UserName);

                            if (media == null || (media.CreatorId != currentUserId && !e.Session.IsAdmin))
                            {
                                e.Respond(HttpStatusCode.Forbidden, new JsonObject { ["success"] = false, ["reason"] = "Access denied." });
                                e.Responded = true;
                                return;
                            }

                            string genreName = e.Content?["name"]?.GetValue<string>() ?? string.Empty;

                            _MediaRepo.AddGenre(id, genreName);

                            e.Respond(HttpStatusCode.OK, new JsonObject { ["success"] = true, ["message"] = "Genre added." });
                            Console.ForegroundColor = ConsoleColor.Blue;
                            Console.WriteLine($"[{nameof(MediaHandler)}] Added genre '{genreName}' to media {id}.");
                        }
                        else
                        {
                            e.Respond(HttpStatusCode.MethodNotAllowed,
                                new JsonObject { ["success"] = false, ["reason"] = "Method not allowed." });
                        }
                    }
                    else
                    {
                        e.Respond(HttpStatusCode.NotFound,
                            new JsonObject { ["success"] = false, ["reason"] = "Endpoint not found." });
                    }
                }
                else
                {
                    e.Respond(HttpStatusCode.NotFound,
                        new JsonObject { ["success"] = false, ["reason"] = "Endpoint not found." });
                }

                e.Responded = true;
            }
            catch (InvalidOperationException ex)
            {
                e.Respond(HttpStatusCode.NotFound,
                    new JsonObject { ["success"] = false, ["reason"] = ex.Message });
                e.Responded = true;
            }
            catch (Exception ex)
            {
                e.Respond(HttpStatusCode.InternalServerError,
                    new JsonObject { ["success"] = false, ["reason"] = ex.Message });
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[{nameof(MediaHandler)}] Error: {ex.Message}");
                e.Responded = true;
            }
        }
    }
}
