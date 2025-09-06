using AutoMapper;
using FluentAssertions;

using WebAPI.Mappings;

namespace WebApi.Tests.Mappings;

public class MappingProfileTests
{
    [Fact]
    public void Profiles_are_valid()
    {
        var cfg = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<EntityToDtoProfile>();
        });

        cfg.AssertConfigurationIsValid();
        cfg.CreateMapper().Should().NotBeNull();
    }
}
