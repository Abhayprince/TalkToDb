namespace TalkToDb.Shared;

public class QueryResult
{
    public string? SqlQuery { get; set; }
    public bool IsGridResult { get; set; }
    public string ResultType { get; set; } = nameof(QueryResultType.None);
    public object? Result { get; set; }
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
}
