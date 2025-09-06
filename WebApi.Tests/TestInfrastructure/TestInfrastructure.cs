using Microsoft.EntityFrameworkCore;
using WebAPI.Models;

namespace WebApi.Tests.TestInfrastructure;

public class TestInfoeduka2Context : Infoeduka2Context
{
    public TestInfoeduka2Context(DbContextOptions<Infoeduka2Context> options)
        : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
    }
}
