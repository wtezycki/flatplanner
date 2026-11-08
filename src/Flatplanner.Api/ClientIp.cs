namespace Flatplanner.Api;

public static class ClientIp
{
    // Railway's edge overwrites X-Real-IP; RemoteIpAddress is the proxy there (design §8).
    public static string? Of(HttpContext http) =>
        http.Request.Headers["X-Real-IP"].FirstOrDefault() ?? http.Connection.RemoteIpAddress?.ToString();
}
