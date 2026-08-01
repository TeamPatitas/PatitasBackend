using DotNetEnv;

namespace PatitasAPI.Infraestructure;
public static class PatitasEnv
{
    public static void ValidateEnv()
    {
        // En caso de doppler no esté configurado, se usará el .env local xd
        Env.Load();

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

    public static string GetEnvVariable(string variableName)
    {
        var value = Environment.GetEnvironmentVariable(variableName);
        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException($"La variable: \"'{variableName}'\" no existe en el entorno (.env)");
        }
        return value;
    }

    public static string GetDbConnection()
    {
        var dbName = GetEnvVariable("POSTGRES_DB");
        var dbUser = GetEnvVariable("POSTGRES_USER");
        var dbPassword = GetEnvVariable("POSTGRES_PASSWORD");
        var dbPort = GetEnvVariable("POSTGRES_PORT");
        var dbHost = GetEnvVariable("SERVICE_IP"); 

        return $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword}";
    }

    public static bool IsDev()
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        return env == "Development";
    }
}