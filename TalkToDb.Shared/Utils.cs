using System.Text.Json;

namespace TalkToDb.Shared;

public static class Utils
{
    public static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };
}