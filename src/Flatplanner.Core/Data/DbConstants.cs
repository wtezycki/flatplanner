using Microsoft.EntityFrameworkCore;

namespace Flatplanner.Core.Data;

public static class DbConstants
{
    public const string CharSet = "utf8mb4";
    public const string Collation = "utf8mb4_0900_ai_ci";

    public static readonly ServerVersion ServerVersion = ServerVersion.Parse("8.4.0-mysql");
}
