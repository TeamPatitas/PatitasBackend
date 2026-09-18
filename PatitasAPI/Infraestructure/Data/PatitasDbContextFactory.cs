using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PatitasAPI.Infraestructure.Data;

//Ignorar, solo es para migraciones para el dotnet ef xdd
public class PatitasDbContextFactory : IDesignTimeDbContextFactory<PatitasDbContext>
{
    public PatitasDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PatitasDbContext>();
        optionsBuilder.UseNpgsql(PatitasEnv.GetDbConnection());

        return new PatitasDbContext(optionsBuilder.Options);
    }
}