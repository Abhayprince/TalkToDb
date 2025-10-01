using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;

namespace TalkToDb.MCPServer.Tools;

[McpServerToolType]
public static class RunSqlTool
{
    [McpServerTool]
    [Description("Executes the given SQL Select Statement against the database and returns the result")]
    public static async Task<object?> ExecuteSqlQuery(string sqlQuery, IConfiguration configuration)
    {
        // execute_sql_query

        if (!sqlQuery.StartsWith("Select", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only SELECT queries can be executed");

        var connectionString = configuration.GetConnectionString("Default");

        using var sqlConnection = new SqlConnection(connectionString);

        try
        {
            using var sqlCommand = sqlConnection.CreateCommand();
            sqlCommand.CommandText = sqlQuery;

            await sqlConnection.OpenAsync();

            using var dataReader = await sqlCommand.ExecuteReaderAsync();

            //var columnNames = Enumerable.Range(0, dataReader.FieldCount).Select(i => dataReader.GetName(i));

            List<string> columnNames = [];
            for (int i = 0; i < dataReader.FieldCount; i++)
            {
                var columnName = dataReader.GetName(i);
                columnNames.Add(columnName);
            }

            var rows = new List<Dictionary<string, object?>> ();

            while(await dataReader.ReadAsync())
            {
                var row = new Dictionary<string, object?>();
                foreach (var columnName in columnNames)
                {
                    var value = dataReader[columnName];
                    row[columnName] = value == DBNull.Value ? null : value;
                }

                rows.Add(row);
            }

            return new { Columns = columnNames, Rows = rows };
        }
        catch (Exception)
        {
            // log ex
            return null;
        }
        finally
        {
            if (sqlConnection.State == ConnectionState.Open)
                await sqlConnection.CloseAsync();
        }
    }
}
