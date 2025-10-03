using Refit;
using TalkToDb.Shared;

namespace TalkToDb.UI;
public interface IApi
{
    [Get("/ask")]
    Task<QueryResult> AskAsync(string q);
}
