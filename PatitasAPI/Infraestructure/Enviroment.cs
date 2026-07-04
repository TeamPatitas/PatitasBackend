
namespace PatitasAPI.Infraestructure;
public static class PatitasEnv
{
    public static void ValidateEnv()
    {
        var requiredVariables = new List<string>
        {
            "AUTH_DEFAULT_PASSWORD",
            "JWT_SECRET_KEY",
            "POSTGRES_DB",
            "POSTGRES_USER",
            "POSTGRES_PASSWORD",
            "POSTGRES_PORT",
            "SERVICE_IP"
        };

        foreach (var variable in requiredVariables)
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(variable)))
            {
                throw new InvalidOperationException($"La variable: \"'{variable}'\" no existe en el entorno (.env)");
            }
        }
    }

    public static string GetDbConnection()
    {
        var dbName = Environment.GetEnvironmentVariable("POSTGRES_DB");
        var dbUser = Environment.GetEnvironmentVariable("POSTGRES_USER");
        var dbPassword = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");
        var dbPort = Environment.GetEnvironmentVariable("POSTGRES_PORT");

        return $"Host=localhost;Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword}";
    }
}