namespace TalkToDb.Shared;

public record GridDto(List<string> Columns, List<Dictionary<string, object?>> Rows);
