using Microsoft.Data.SqlClient;
using Npgsql;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace YourOwnData
{
    public static class DataService
    {
        public static List<List<string>> GetDataTable(string sqlQuery)
        {
            var rows = new List<List<string>>();
            //string strConn = "Host=localhost;Port=5432;Username=postgres;Password=Onward@1234;Database=OTLDB;";
            //string strConn = "Host=localhost;Port=5432;Username=otlchatbot;Password=ChAt80T@092024;Database=OTLDB;";
            string strConn = "Host=otlpgdb.postgres.database.azure.com;Port=5432;Username=otlchatbot;Password=ChAt80T@092024;Database=otldb;";

            using (var connection = new NpgsqlConnection(strConn))
            {
                connection.Open();

                using (var command = new NpgsqlCommand(sqlQuery, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        int count = 0;
                        bool headersAdded = false;
                        while (reader.Read())
                        {
                            count += 1;
                            var cols = new List<string>();
                            var headerCols = new List<string>();

                            if (!headersAdded)
                            {
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    headerCols.Add(reader.GetName(i));
                                }
                                headersAdded = true;
                                rows.Add(headerCols);
                            }

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                try
                                {
                                    cols.Add(reader.GetValue(i).ToString());
                                }
                                catch
                                {
                                    cols.Add("DataTypeConversionError");
                                }
                            }
                            rows.Add(cols);
                        }
                    }
                }
            }

            return rows;
        }
    }
    /*
        public class TableSchema()
        {
            public string TableName { get; set; }
            public List<string> Columns { get; set; }
        }*/
}
