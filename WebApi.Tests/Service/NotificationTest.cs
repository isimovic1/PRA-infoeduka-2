using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WebApi.Tests.TestInfrastructure;
using WebAPI.Models;
using WebAPI.Services;

namespace WebApi.Tests.Services;

public class NotificationTests
{
    private static TestInfoeduka2Context NewDb()
    {
        var options = new DbContextOptionsBuilder<Infoeduka2Context>()
            .UseInMemoryDatabase($"testdb-{Guid.NewGuid()}")
            .Options;

        var db = new TestInfoeduka2Context(options);
        db.Database.EnsureCreated(); 
        return db;
    }


    [Fact]
    public async Task SendToUser_creates_one_notification()
    {
        using var db = NewDb();
        db.Users.Add(new User { Id = 1, FirstName = "A", LastName = "B", Email = "a@b.com", PasswordHash = "x", Role = 0 });
        db.SaveChanges();

        var svc = new NotificationService(db);

        await svc.SendToUserAsync(1, null, "Hello", "World", "/url");

        db.Notifications.Should().HaveCount(1);
        var n = db.Notifications.Single();
        n.ToUserId.Should().Be(1);
        n.FromUserId.Should().BeNull();
        n.Title.Should().Be("Hello");
        n.Body.Should().Be("World");
        n.Link.Should().Be("/url");
    }

    [Fact]
    public async Task SendToUsers_deduplicates_targets()
    {
        using var db = NewDb();
        db.Users.AddRange(
            new User { Id = 1, FirstName = "A", LastName = "B", Email = "a@b.com", PasswordHash = "x", Role = 0 },
            new User { Id = 2, FirstName = "C", LastName = "D", Email = "c@d.com", PasswordHash = "x", Role = 0 }
        );
        db.SaveChanges();

        var svc = new NotificationService(db);
        await svc.SendToUsersAsync(new[] { 1, 1, 2 }, 5, "Title");

        db.Notifications.Should().HaveCount(2);
        db.Notifications.Select(x => x.ToUserId).Should().BeEquivalentTo(new[] { 1, 2 });
        db.Notifications.All(n => n.FromUserId == 5 && n.Title == "Title").Should().BeTrue();
    }
}
