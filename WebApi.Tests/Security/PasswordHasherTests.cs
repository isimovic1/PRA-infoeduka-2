using FluentAssertions;
using WebAPI.Security;   // prilagodi svom namespaceu
using Xunit;

namespace WebApi.Tests.Security;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_and_Verify_roundtrip()
    {
        var password = "Secret123!";
        var hash = PasswordHasher.Hash(password);
        hash.Should().NotBeNullOrEmpty();

        PasswordHasher.Verify(password, hash).Should().BeTrue();
        PasswordHasher.Verify("wrong", hash).Should().BeFalse();
    }
}
