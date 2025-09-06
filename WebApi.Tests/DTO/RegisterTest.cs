using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Xunit;
using WebAPI.DTOs;

namespace WebApi.Tests.DTO;

public class RegisterDtoValidationTests
{
    private static IList<ValidationResult> Validate(object model)
    {
        var ctx = new ValidationContext(model);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, ctx, results, validateAllProperties: true);
        if (model is IValidatableObject v)
            foreach (var r in v.Validate(ctx)) results.Add(r);
        return results;
    }

    [Fact]
    public void Student_must_have_GroupId()
    {
        var dto = new RegisterDto
        {
            Email = "a@b.com",
            FirstName = "A",
            LastName = "B",
            Password = "Secret123!",
            Role = 0,
            GroupId = null
        };

        Validate(dto).Should().Contain(r => r.ErrorMessage!.Contains("Student"));
    }

    [Fact]
    public void Admin_must_not_have_GroupId()
    {
        var dto = new RegisterDto
        {
            Email = "a@b.com",
            FirstName = "A",
            LastName = "B",
            Password = "Secret123!",
            Role = 2,
            GroupId = 1
        };

        Validate(dto).Should().Contain(r => r.ErrorMessage!.Contains("Admin"));
    }
}
