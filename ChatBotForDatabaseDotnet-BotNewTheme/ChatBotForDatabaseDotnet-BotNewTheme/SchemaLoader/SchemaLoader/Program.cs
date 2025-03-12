
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Microsoft.Data.SqlClient; // For SQL Server Database
using Npgsql;   // For Postgres Database

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Extracting schema....");

List<KeyValuePair<string, string>> rows = new();
List<TableSchema> dbSchema = new();
//string strConn = "Data Source=d365byod-bit.database.windows.net; Initial Catalog=HRMS_Database; user id = Business_IT; PASSWORD =DB_Biz_it@onward2022;pooling = true;Min pool Size=10;Max pool Size=100;persist security info = false;Connect Timeout=60";

//string strConn = "Host=localhost;Port=5432;Username=postgres;Password=Onward@1234;Database=OTLDB;";
 string strConn = "Host=localhost;Port=5432;Username=otlchatbot;Password=ChAt80T@092024;Database=OTLDB;";

using (var connection = new NpgsqlConnection(strConn))
{
    connection.Open();

    // Get the schema
    string sql = @"SELECT table_schema || '.' || table_name AS TableName, column_name AS ColumnName
                   FROM information_schema.columns 
                   WHERE table_schema NOT IN ('information_schema', 'pg_catalog') 
                   ORDER BY table_name;";

    using (var command = new NpgsqlCommand(sql, connection))
    {
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                rows.Add(new KeyValuePair<string, string>(reader.GetValue(0).ToString(), reader.GetValue(1).ToString()));
            }
        }
    }
}

var groups = rows.GroupBy(x => x.Key);

foreach (var group in groups)
{
    dbSchema.Add(new TableSchema() { TableName = group.Key, Columns = group.Select(x => x.Value).ToList() });
    //use this list
}

Console.WriteLine("Copy the schema below into the Index.cshtml.cs file of the YourOwnData project:");
Console.WriteLine();
Console.WriteLine();

var textLines = new List<string>();

foreach (var table in dbSchema)
{
    var schemaLine = $"- {table.TableName} (";

    foreach (var column in table.Columns)
    {
        schemaLine += column + ", ";
    }

    schemaLine += ")";
    schemaLine = schemaLine.Replace(", )", " )");

    Console.WriteLine(schemaLine);
    textLines.Add(schemaLine);
}

File.WriteAllText(@"Schema.txt", JsonSerializer.Serialize(dbSchema));

public class TableSchema()
{
    public string TableName { get; set; }
    public List<string> Columns { get; set; }
}