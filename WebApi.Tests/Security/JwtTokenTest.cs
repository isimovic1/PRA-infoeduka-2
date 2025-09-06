using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using FluentAssertions;
using Xunit;

using WebAPI.Security;

namespace WebApi.Tests.Security;

public class JwtTokenTest
{
    [Fact]
    public void CreateToken_sets_expected_claims_and_metadata()
    {
        var key = new string('x', 64); 
        var token = JwtTokenProvider.CreateToken(
            secureKey: key,
            expirationMinutes: 60,
            subject: "user@example.com",
            role: "Student",
            issuer: "test-issuer",
            audience: "test-audience");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Issuer.Should().Be("test-issuer");
        jwt.Audiences.Should().Contain("test-audience");
        jwt.Claims.First(c => c.Type.EndsWith("/claims/name")).Value.Should().Be("user@example.com");
        jwt.Claims.First(c => c.Type.EndsWith("/claims/role")).Value.Should().Be("Student");
    }
}
