using MyMediaList_HttpServer.System.Rating;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace MyMediaList.Tests.Mocks
{
    public class MockRatingRepository : IRatingRepository
    {
        public List<Rating> Ratings { get; } = new List<Rating>();
        // Simple Set for likes: "RatingID-UserID" strings
        public HashSet<string> Likes { get; } = new HashSet<string>(); 

        public void Add(Rating rating)
        {
            if (rating.Score < 1 || rating.Score > 5) throw new ArgumentException("Score must be 1-5.");
            
            if (Ratings.Any(r => r.UserId == rating.UserId && r.MediaId == rating.MediaId))
                throw new InvalidOperationException("User has already rated this media entry.");
                    
            rating.Id = Ratings.Count + 1;
            Ratings.Add(rating);
        }

        public Rating? GetById(int id) => Ratings.FirstOrDefault(r => r.Id == id);
        
        public void Delete(int id) => Ratings.RemoveAll(r => r.Id == id);

        public void Update(Rating rating)
        {
            var existing = GetById(rating.Id);
            if (existing != null)
            {
                existing.Score = rating.Score;
                existing.Comment = rating.Comment;
                existing.IsConfirmed = rating.IsConfirmed;
            }
        }

        public JsonArray GetList(int mediaId) 
        { 
            return new JsonArray();
        }

        public bool ToggleLike(int userId, int ratingId)
        {
            string key = $"{ratingId}-{userId}";
            if (Likes.Contains(key))
            {
                Likes.Remove(key);
                return false; // unliked
            }
            else
            {
                Likes.Add(key);
                return true; // liked
            }
        }
    }
}
