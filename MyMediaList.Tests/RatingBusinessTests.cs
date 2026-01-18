using NUnit.Framework;
using MyMediaList_HttpServer.System.Rating;
using MyMediaList.Tests.Mocks;
using System;

namespace MyMediaList.Tests
{
    [TestFixture]
    public class RatingBusinessTests
    {
        private MockRatingRepository _mockRepo;

        [SetUp]
        public void Setup()
        {
            _mockRepo = new MockRatingRepository();
        }

        [Test]
        public void AddRating_ValidRating_IsSaved()
        {
            var rating = new Rating { UserId = 1, MediaId = 1, Score = 5 };
            _mockRepo.Add(rating);

            Assert.That(_mockRepo.Ratings.Count, Is.EqualTo(1));
            Assert.That(_mockRepo.Ratings[0].Id, Is.EqualTo(1));
        }

        [Test]
        public void AddRating_DuplicateUserRating_ThrowsException()
        {
            // Arrange
            _mockRepo.Add(new Rating { UserId = 1, MediaId = 1, Score = 5 });

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => 
                _mockRepo.Add(new Rating { UserId = 1, MediaId = 1, Score = 3 }));
            
            Assert.That(ex.Message, Does.Contain("User has already rated"));
        }

                [Test]
        public void AddRating_ScoreTooLow_ThrowsException()
        {
             // If your Repository doesn't check this, ensure the Rating object or Handler does.
             // Assuming your Mock or Code checks it:
             var rating = new Rating { UserId = 1, MediaId = 1, Score = 0 }; 
             Assert.Throws<ArgumentException>(() => _mockRepo.Add(rating)); // Ensure Mock/Repo throws this
        }

        [Test]
        public void AddRating_ScoreTooHigh_ThrowsException()
        {
             var rating = new Rating { UserId = 1, MediaId = 1, Score = 6 };
             Assert.Throws<ArgumentException>(() => _mockRepo.Add(rating));
        }

        [Test]
        public void UpdateRating_ChangesScore_PersistsChange()
        {
            var rating = new Rating { UserId = 1, MediaId = 1, Score = 3 };
            _mockRepo.Add(rating);
            
            rating.Score = 5;
            _mockRepo.Update(rating);

            var updated = _mockRepo.GetById(rating.Id);
            Assert.That(updated, Is.Not.Null);
            Assert.That(updated.Score, Is.EqualTo(5));
        }

        [Test]
        public void DeleteRating_RemovesItFromRepo()
        {
            var rating = new Rating { UserId = 1, MediaId = 1, Score = 3 };
            _mockRepo.Add(rating);
            int id = rating.Id;

            _mockRepo.Delete(id);
            
            Assert.That(_mockRepo.GetById(id), Is.Null);
        }

        [Test]
        public void ToggleLike_FirstTime_ReturnsTrue()
        {
             bool liked = _mockRepo.ToggleLike(userId: 100, ratingId: 50);
             Assert.That(liked, Is.True);
        }

        [Test]
        public void ToggleLike_SecondTime_ReturnsFalse()
        {
             _mockRepo.ToggleLike(userId: 100, ratingId: 50); // Like
             bool liked = _mockRepo.ToggleLike(userId: 100, ratingId: 50); // Unlike
             Assert.That(liked, Is.False);
        }
    }
}
