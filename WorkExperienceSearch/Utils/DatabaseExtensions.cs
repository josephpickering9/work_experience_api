using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Work_Experience_Search.Utils;

public static class DatabaseExtensions
{
    public static bool SupportsILike(this DatabaseFacade database) =>
        database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;
}
