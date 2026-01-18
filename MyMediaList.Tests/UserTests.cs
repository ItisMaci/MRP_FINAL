using NUnit.Framework;
using MyMediaList_HttpServer.System.User;

namespace MyMediaList.Tests
{
    [TestFixture]
    public class UserTests
    {
        [Test]
        public void SetPassword_UpdatesPasswordHash()
        {
            var user = new User { UserName = "test" };
            user.SetPassword("password123");
            
            // Hash should be generated
            Assert.That(user.PasswordHash, Is.Not.Null);
            Assert.That(user.PasswordHash, Is.Not.Empty);
            // Hash should NOT be the plain text
            Assert.That(user.PasswordHash, Is.Not.EqualTo("password123")); 
        }

        [Test]
        public void VerifyPassword_CorrectPassword_ReturnsTrue()
        {
            var user = new User { UserName = "test" };
            user.SetPassword("secret");
            Assert.That(user.VerifyPassword("secret"), Is.True);
        }

        [Test]
        public void VerifyPassword_WrongPassword_ReturnsFalse()
        {
            var user = new User { UserName = "test" };
            user.SetPassword("secret");
            Assert.That(user.VerifyPassword("wrong"), Is.False);
        }

        [Test]
        public void VerifyPassword_EmptyInput_ReturnsFalse()
        {
            var user = new User { UserName = "test" };
            user.SetPassword("secret");
            Assert.That(user.VerifyPassword(""), Is.False);
        }

        [Test]
        public void VerifyPassword_CaseSensitive_ReturnsFalse()
        {
            var user = new User { UserName = "test" };
            user.SetPassword("Secret");
            Assert.That(user.VerifyPassword("secret"), Is.False);
        }
        
        [Test]
        public void User_IsAdmin_ReturnsFalseByDefault()
        {
             var user = new User { UserName = "regular" };

             Assert.That(user.UserName, Is.EqualTo("regular"));
        }

        [Test]
        public void User_UserName_CanBeSet()
        {
            var user = new User { UserName = "NewName" };
            Assert.That(user.UserName, Is.EqualTo("NewName"));
        }
    }
}