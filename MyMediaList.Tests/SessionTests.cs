using NUnit.Framework;
using MyMediaList_HttpServer.System.Session;
using System;

namespace MyMediaList.Tests
{
    [TestFixture]
    public class SessionTests
    {
        [Test]
        public void Session_Properties_AreSetCorrectly()
        {
            var session = new Session 
            { 
                UserName = "user1", 
                IsAdmin = false, 
                Timestamp = DateTime.UtcNow, 
                Token = "token123" 
            };

            Assert.That(session.Token, Is.EqualTo("token123"));
            Assert.That(session.UserName, Is.EqualTo("user1"));
        }

        [Test]
        public void Session_IsAdmin_ReflectsValue()
        {
            var adminSession = new Session { UserName = "admin", IsAdmin = true };
            var userSession = new Session { UserName = "user", IsAdmin = false };

            Assert.That(adminSession.IsAdmin, Is.True);
            Assert.That(userSession.IsAdmin, Is.False);
        }

        [Test]
        public void Session_WithExpiredTimestamp_LogicCheck()
        {
            var oldDate = DateTime.UtcNow.AddHours(-1);
            var session = new Session { Timestamp = oldDate };
            Assert.That(session.Timestamp, Is.EqualTo(oldDate));
        }

    }
}
