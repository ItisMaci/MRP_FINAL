using MyMediaList_HttpServer.Server;
using MyMediaList_HttpServer.System.Rating;
using MyMediaList_HttpServer.System.User;
using System.Net;
using System.Text.Json.Nodes;

namespace MyMediaList_HttpServer.Handlers
{
    public sealed class RatingHandler : Handler, IHandler
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // private readonly members                                                                                         //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Repository for rating data access.</summary>
        private readonly RatingRepository _RatingRepo = new();

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
            if (!e.Path.StartsWith("/ratings")) { return; }

            try
            {
                // --------------------------------------------------------------------------------
                // POST /ratings/{id}/confirm (Confirm Comment) - CHECK FIRST
                // --------------------------------------------------------------------------------
                if (e.Path.StartsWith("/ratings/") && e.Path.EndsWith("/confirm") && e.Method == HttpMethod.Post)
                {
                    if (e.Session == null)
                    {
                        e.Respond(HttpStatusCode.Unauthorized,
                            new JsonObject { ["success"] = false, ["reason"] = "Authentication required." });
                        e.Responded = true;
                        return;
                    }

                    // Parse ID: /ratings/123/confirm
                    string sub = e.Path.Substring("/ratings/".Length);
                    string idStr = sub.Substring(0, sub.Length - "/confirm".Length);

                    if (int.TryParse(idStr, out int id))
                    {
                        Rating? rating = _RatingRepo.GetById(id);
                        if (rating == null)
                        {
                            e.Respond(HttpStatusCode.NotFound,
                                new JsonObject { ["success"] = false, ["reason"] = "Rating not found." });
                            e.Responded = true;
                            return;
                        }

                        int currentUserId = _UserRepo.GetId(e.Session.UserName);
                        if (rating.UserId != currentUserId && !e.Session.IsAdmin)
                        {
                            e.Respond(HttpStatusCode.Forbidden,
                                new JsonObject { ["success"] = false, ["reason"] = "You can only confirm your own comments." });
                            e.Responded = true;
                            return;
                        }

                        rating.IsConfirmed = true;
                        _RatingRepo.Update(rating);

                        e.Respond(HttpStatusCode.OK,
                            new JsonObject { ["success"] = true, ["message"] = "Comment confirmed." });
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine($"[{nameof(RatingHandler)}] Confirmed comment for rating {id}.");
                    }
                    e.Responded = true;
                    return;
                }

                // --------------------------------------------------------------------------------
                // POST /ratings/{id}/like (Toggle Like) - CHECK SECOND
                // --------------------------------------------------------------------------------
                if (e.Path.StartsWith("/ratings/") && e.Path.EndsWith("/like") && e.Method == HttpMethod.Post)
                {
                    if (e.Session == null)
                    {
                        e.Respond(HttpStatusCode.Unauthorized,
                            new JsonObject { ["success"] = false, ["reason"] = "Authentication required." });
                        e.Responded = true;
                        return;
                    }

                    // Parse ID: /ratings/123/like
                    string sub = e.Path.Substring("/ratings/".Length);
                    string idStr = sub.Substring(0, sub.Length - "/like".Length);

                    if (int.TryParse(idStr, out int id))
                    {
                        int userId = _UserRepo.GetId(e.Session.UserName);
                        bool isLiked = _RatingRepo.ToggleLike(userId, id);

                        e.Respond(HttpStatusCode.OK,
                            new JsonObject { ["success"] = true, ["is_liked"] = isLiked });
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine($"[{nameof(RatingHandler)}] Toggled like for rating {id}.");
                    }
                    else
                    {
                        e.Respond(HttpStatusCode.BadRequest, new JsonObject { ["success"] = false, ["reason"] = "Invalid ID." });
                    }
                    e.Responded = true;
                    return;
                }

                // --------------------------------------------------------------------------------
                // DELETE /ratings/{id} - CHECK THIRD
                // --------------------------------------------------------------------------------
                if (e.Path.StartsWith("/ratings/") && e.Method == HttpMethod.Delete)
                {
                    if (e.Session == null)
                    {
                        e.Respond(HttpStatusCode.Unauthorized,
                            new JsonObject { ["success"] = false, ["reason"] = "Authentication required." });
                        e.Responded = true;
                        return;
                    }

                    // Parse ID: /ratings/123
                    string sub = e.Path.Substring("/ratings/".Length);
                    if (int.TryParse(sub, out int id))
                    {
                        Rating? rating = _RatingRepo.GetById(id);
                        if (rating == null)
                        {
                            e.Respond(HttpStatusCode.NotFound,
                                new JsonObject { ["success"] = false, ["reason"] = "Rating not found." });
                            e.Responded = true;
                            return;
                        }

                        int currentUserId = _UserRepo.GetId(e.Session.UserName);
                        if (rating.UserId != currentUserId && !e.Session.IsAdmin)
                        {
                            e.Respond(HttpStatusCode.Forbidden,
                                new JsonObject { ["success"] = false, ["reason"] = "You can only delete your own ratings." });
                            e.Responded = true;
                            return;
                        }

                        _RatingRepo.Delete(id);
                        e.Respond(HttpStatusCode.OK, new JsonObject { ["success"] = true, ["message"] = "Rating deleted." });
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine($"[{nameof(RatingHandler)}] Deleted rating {id}.");
                    }
                    else
                    {
                        e.Respond(HttpStatusCode.BadRequest, new JsonObject { ["success"] = false, ["reason"] = "Invalid ID." });
                    }
                    e.Responded = true;
                    return;
                }

                // --------------------------------------------------------------------------------
                // PUT /ratings/{id} (Update Rating) - CHECK FOURTH
                // --------------------------------------------------------------------------------
                if (e.Path.StartsWith("/ratings/") && e.Method == HttpMethod.Put)
                {
                    if (e.Session == null)
                    {
                        e.Respond(HttpStatusCode.Unauthorized,
                            new JsonObject { ["success"] = false, ["reason"] = "Authentication required." });
                        e.Responded = true;
                        return;
                    }

                    string sub = e.Path.Substring("/ratings/".Length);
                    if (int.TryParse(sub, out int id))
                    {
                        Rating? rating = _RatingRepo.GetById(id);
                        if (rating == null)
                        {
                            e.Respond(HttpStatusCode.NotFound,
                                new JsonObject { ["success"] = false, ["reason"] = "Rating not found." });
                            e.Responded = true;
                            return;
                        }

                        int currentUserId = _UserRepo.GetId(e.Session.UserName);
                        if (rating.UserId != currentUserId && !e.Session.IsAdmin)
                        {
                            e.Respond(HttpStatusCode.Forbidden,
                                new JsonObject { ["success"] = false, ["reason"] = "You can only edit your own ratings." });
                            e.Responded = true;
                            return;
                        }

                        if (e.Content?.ContainsKey("score") == true)
                        {
                            rating.Score = e.Content["score"]!.GetValue<int>();
                        }
                        if (e.Content?.ContainsKey("comment") == true)
                        {
                            rating.Comment = e.Content["comment"]!.GetValue<string>();
                        }

                        _RatingRepo.Update(rating);

                        e.Respond(HttpStatusCode.OK,
                            new JsonObject { ["success"] = true, ["message"] = "Rating updated." });
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine($"[{nameof(RatingHandler)}] Updated rating {id}.");
                    }
                    e.Responded = true;
                    return;
                }

                // --------------------------------------------------------------------------------
                // POST /ratings (Create Rating) - CHECK LAST (Generic Route)
                // --------------------------------------------------------------------------------
                if (e.Path == "/ratings" && e.Method == HttpMethod.Post)
                {
                    if (e.Session == null)
                    {
                        e.Respond(HttpStatusCode.Unauthorized,
                            new JsonObject { ["success"] = false, ["reason"] = "Authentication required." });
                        e.Responded = true;
                        return;
                    }

                    int uid = _UserRepo.GetId(e.Session.UserName);

                    // Support both JSON keys to fix Postman test ambiguity
                    int mid = 0;
                    if (e.Content?.ContainsKey("mediaid") == true) mid = e.Content["mediaid"]!.GetValue<int>();
                    else if (e.Content?.ContainsKey("media_id") == true) mid = e.Content["media_id"]!.GetValue<int>();

                    int score = e.Content?["score"]?.GetValue<int>() ?? 1;
                    string comment = e.Content?["comment"]?.GetValue<string>() ?? string.Empty;

                    Rating rating = new()
                    {
                        UserId = uid,
                        MediaId = mid,
                        Score = score,
                        Comment = comment
                    };

                    try
                    {
                        _RatingRepo.Add(rating);
                        e.Respond(HttpStatusCode.OK, new JsonObject { ["success"] = true, ["id"] = rating.Id });
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine($"[{nameof(RatingHandler)}] Created rating {rating.Id}.");
                    }
                    catch (InvalidOperationException ex)
                    {
                        // Handle Duplicate Rating or DB constraint errors gracefully
                        e.Respond(HttpStatusCode.Conflict, new JsonObject { ["success"] = false, ["reason"] = ex.Message });
                    }
                    e.Responded = true;
                    return;
                }

                // If nothing matched
                e.Respond(HttpStatusCode.BadRequest,
                    new JsonObject { ["success"] = false, ["reason"] = "Invalid rating endpoint." });
                e.Responded = true;
            }
            catch (Exception ex)
            {
                e.Respond(HttpStatusCode.InternalServerError,
                    new JsonObject { ["success"] = false, ["reason"] = ex.Message });
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[{nameof(RatingHandler)}] Error: {ex.Message}");
                e.Responded = true;
            }
        }
    }
}
