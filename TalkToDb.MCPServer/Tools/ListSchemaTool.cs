using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Data;

namespace TalkToDb.MCPServer.Tools;

[McpServerToolType]
public static class ListSchemaTool
{
    [McpServerTool(Name = "List-Db-Table-Schema")]
    [Description("Lists all the db tables and columns schema")]
    public static async Task<IEnumerable<TableSchema>> ListDbTablesSchema(IConfiguration configuration)
    {
        // list_db_tables_schema
        var connectionString = configuration.GetConnectionString("Default");

        using var sqlConnection = new SqlConnection(connectionString);
        try
        {
            using var sqlCommand = sqlConnection.CreateCommand();
            sqlCommand.CommandText = @"
            SELECT t.TABLE_SCHEMA, t.TABLE_NAME, c.COLUMN_NAME, c.DATA_TYPE, c.CHARACTER_MAXIMUM_LENGTH, 
	            CASE c.IS_NULLABLE 
		            WHEN 'NO' THEN CAST(0 AS BIT)
		            ELSE  CAST(1 AS BIT) 
	            END AS IsNullable
            FROM INFORMATION_SCHEMA.TABLES t
            JOIN INFORMATION_SCHEMA.COLUMNS c ON t.TABLE_NAME = c.TABLE_NAME AND t.TABLE_SCHEMA = c.TABLE_SCHEMA
            WHERE t.TABLE_TYPE = 'BASE TABLE'
            ORDER BY t.TABLE_SCHEMA, t.TABLE_NAME
            ";

            var tableSchemaList = new List<TableSchema>();
            await sqlConnection.OpenAsync();
            using var dataReader = await sqlCommand.ExecuteReaderAsync();
            while (await dataReader.ReadAsync())
            {
                var tableSchema = new TableSchema
                {
                    Schema = dataReader.GetString(0),
                    TableName = dataReader.GetString(1),
                    ColumnName = dataReader.GetString(2),
                    DataType = dataReader.GetString(3),
                    CharacterLength = dataReader.IsDBNull(4) ? null : dataReader.GetInt32(4),
                    IsNullable = dataReader.GetBoolean(5),
                };
                tableSchemaList.Add(tableSchema);
            }

            return tableSchemaList;
        }
        catch (Exception)
        {
            // Log exception
            //throw;
            return [];
        }
        finally
        {
            if (sqlConnection.State == ConnectionState.Open)
                await sqlConnection.CloseAsync();
        }
    }
}
public class TableSchema
{
    public string Schema { get; set; } // dbo.
    public string TableName { get; set; }
    public string ColumnName { get; set; }
    public string DataType { get; set; }
    public int? CharacterLength { get; set; }
    public bool IsNullable { get; set; }
}