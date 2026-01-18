using NUnit.Framework;
using MyMediaList_HttpServer.System.Media;
using System.Text.Json.Nodes;

namespace MyMediaList.Tests
{
    [TestFixture]
    public class MediaTests
    {
        [Test]
        public void Media_Defaults_AreSetCorrectly()
        {
            var media = new Media();
            Assert.That(media.AverageScore, Is.EqualTo(0));
            Assert.That(media.Type, Is.EqualTo("Movie"));
        }

        [Test]
        public void Media_SetProperties_StoresValues()
        {
            var media = new Media
            {
                Title = "Test Game",
                Description = "Fun",
                ReleaseYear = 2022,
                AgeRestriction = 18
            };
            
            Assert.That(media.Title, Is.EqualTo("Test Game"));
            Assert.That(media.ReleaseYear, Is.EqualTo(2022));
            Assert.That(media.AgeRestriction, Is.EqualTo(18));
        }

    }
}
