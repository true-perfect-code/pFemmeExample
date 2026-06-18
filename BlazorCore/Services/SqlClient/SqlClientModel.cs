using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlazorCore.Services.SqlClient
{
    /// <summary>
    /// Defines the execution mode for stored procedure calls.
    /// </summary>
    public enum ExecuteMode
    {
        /// <summary>Executes a non-query command (INSERT, UPDATE, DELETE).</summary>
        ExecuteNonQuery,
        /// <summary>Executes a reader and returns raw data.</summary>
        ExecuteReader,
        /// <summary>Executes a reader and returns data as JSON.</summary>
        ExecuteReaderJson,
        /// <summary>Executes a scalar query and returns a single value.</summary>
        ExecuteScalar,
        /// <summary>Executes a query that returns binary data.</summary>
        ExecuteByte,
        /// <summary>Unknown or unsupported execution mode.</summary>
        Unknown
    }

    /// <summary>
    /// Placeholder for SQL client extensions. Reserved for future use.
    /// </summary>
    public class SqlClientModel
    {
    }

    /// <summary>
    /// Defines MSSQL database column type mappings.
    /// Maps SQL data types to .NET types and ADO.NET SqlDbType values.
    /// </summary>
    public class TableDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TableDefinition"/> class.
        /// </summary>
        public TableDefinition()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TableDefinition"/> class with specified properties.
        /// </summary>
        /// <param name="column_name">The name of the database column.</param>
        /// <param name="sp_parameter_name">The name of the stored procedure parameter.</param>
        /// <param name="data_type">The SQL data type.</param>
        /// <param name="col_size">The column size.</param>
        public TableDefinition(string column_name, string sp_parameter_name, string data_type, int col_size)
        {
            COLUMN_NAME = column_name;
            SP_PARAMETER_NAME = sp_parameter_name;
            DATA_TYPE = data_type;
            COL_SIZE = col_size;
        }

        /// <summary>Gets or sets the column name in the database.</summary>
        public string COLUMN_NAME { get; set; } = "";

        /// <summary>Gets or sets the stored procedure parameter name.</summary>
        public string SP_PARAMETER_NAME { get; set; } = "";

        /// <summary>Gets or sets the column size.</summary>
        public int COL_SIZE { get; set; } = 0;

        /// <summary>Gets or sets the ADO.NET SqlDbType integer value.</summary>
        public int DATA_ADOTYPE { get; set; } = 0;

        /// <summary>Gets or sets the corresponding .NET type as a string.</summary>
        public string NET_TYPE { get; set; } = "";

        private string _DATA_TYPE = "";

        /// <summary>Gets or sets the SQL data type. Setting this automatically populates DATA_ADOTYPE and NET_TYPE.</summary>
        public string DATA_TYPE
        {
            get => _DATA_TYPE;
            set
            {
                _DATA_TYPE = value;
                MapSqlTypeToNetType(value);
            }
        }

        /// <summary>
        /// Maps a SQL data type to ADO.NET SqlDbType and .NET type.
        /// </summary>
        private void MapSqlTypeToNetType(string sqlType)
        {
            switch (sqlType.Trim().ToLower())
            {
                case "bigint":
                    DATA_ADOTYPE = 0;
                    NET_TYPE = "long";
                    break;

                case "binary":
                    DATA_ADOTYPE = 1;
                    NET_TYPE = "byte[]";
                    break;

                case "bit":
                    DATA_ADOTYPE = 2;
                    NET_TYPE = "bool";
                    break;

                case "char":
                    DATA_ADOTYPE = 3;
                    NET_TYPE = "string";
                    break;

                case "date":
                    DATA_ADOTYPE = 31;
                    NET_TYPE = "DateTime";
                    break;

                case "datetime":
                    DATA_ADOTYPE = 4;
                    NET_TYPE = "DateTime";
                    break;

                case "datetime2":
                    DATA_ADOTYPE = 33;
                    NET_TYPE = "DateTime";
                    break;

                case "datetimeoffset":
                    DATA_ADOTYPE = (int)System.Data.SqlDbType.DateTimeOffset; // 34
                    NET_TYPE = "DateTimeOffset";
                    break;

                case "decimal":
                    DATA_ADOTYPE = 5;
                    NET_TYPE = "double";
                    break;

                case "float":
                    DATA_ADOTYPE = 6;
                    NET_TYPE = "double";
                    break;

                case "image":
                    DATA_ADOTYPE = 7;
                    NET_TYPE = "byte[]";
                    break;

                case "int":
                    DATA_ADOTYPE = 8;
                    NET_TYPE = "int";
                    break;

                case "money":
                    DATA_ADOTYPE = 9;
                    NET_TYPE = "double";
                    break;

                case "nchar":
                    DATA_ADOTYPE = 10;
                    NET_TYPE = "string";
                    break;

                case "ntext":
                    DATA_ADOTYPE = 11;
                    NET_TYPE = "string";
                    break;

                case "nvarchar":
                    DATA_ADOTYPE = 12;
                    NET_TYPE = "string";
                    break;

                case "real":
                    DATA_ADOTYPE = 13;
                    NET_TYPE = "double";
                    break;

                case "smalldatetime":
                    DATA_ADOTYPE = 15;
                    NET_TYPE = "DateTime";
                    break;

                case "smallint":
                    DATA_ADOTYPE = 16;
                    NET_TYPE = "int";
                    break;

                case "smallmoney":
                    DATA_ADOTYPE = 17;
                    NET_TYPE = "double";
                    break;

                case "structured":
                    DATA_ADOTYPE = 30;
                    NET_TYPE = "byte[]";
                    break;

                case "text":
                    DATA_ADOTYPE = 18;
                    NET_TYPE = "string";
                    break;

                case "time":
                    DATA_ADOTYPE = 32;
                    NET_TYPE = "DateTime";
                    break;

                case "timestamp":
                    DATA_ADOTYPE = 19;
                    NET_TYPE = "int";
                    break;

                case "tinyint":
                    DATA_ADOTYPE = 20;
                    NET_TYPE = "int";
                    break;

                case "udt":
                    DATA_ADOTYPE = 29;
                    NET_TYPE = "int";
                    break;

                case "uniqueidentifier":
                    DATA_ADOTYPE = 14;
                    NET_TYPE = "int";
                    break;

                case "varbinary":
                    DATA_ADOTYPE = 21;
                    NET_TYPE = "byte[]";
                    break;

                case "varchar":
                    DATA_ADOTYPE = 22;
                    NET_TYPE = "string";
                    break;

                case "variant":
                    DATA_ADOTYPE = 23;
                    NET_TYPE = "byte[]";
                    break;

                case "xml":
                    DATA_ADOTYPE = 25;
                    NET_TYPE = "string";
                    break;

                default:
                    DATA_ADOTYPE = 18;
                    NET_TYPE = "string";
                    break;
            }
        }
    }

    /// <summary>
    /// Dummy model used when spExecute is called with ExecuteScalar mode.
    /// </summary>
    public class DummyModel
    {
        /// <summary>Gets or sets the dummy boolean value.</summary>
        public bool dummy { get; set; } = false;
    }

    /// <summary>
    /// Model for reading dynamic data from the WebAPI.
    /// Contains JSON result or error information.
    /// </summary>
    public class ReaderDynamicModel
    {
        /// <summary>Gets or sets the error message (if any).</summary>
        public string? out_err { get; set; } = "";

        /// <summary>Gets or sets the JSON result string.</summary>
        public string? out_json { get; set; } = "";
    }

    /// <summary>
    /// Generic key-value pair model for dictionary operations.
    /// </summary>
    public class DictionaryModel
    {
        /// <summary>Gets or sets the key.</summary>
        public string? key { get; set; }

        /// <summary>Gets or sets the value.</summary>
        public string? value { get; set; }
    }

    /// <summary>
    /// Generic read model that separates cloud and local results.
    /// </summary>
    /// <typeparam name="T">The type of items in the lists.</typeparam>
    public class ReadModel<T>
    {
        /// <summary>Gets or sets the error message (if any).</summary>
        public string? out_err { get; set; } = "";

        /// <summary>Gets or sets the list from the primary source.</summary>
        public List<T?>? out_list { get; set; }

        /// <summary>Gets or sets the list from the cloud source.</summary>
        public List<T?>? out_list_cloud { get; set; }

        /// <summary>Gets or sets the list from the local source.</summary>
        public List<T?>? out_list_local { get; set; }
    }

    /// <summary>
    /// Generic model for scalar return values with multiple typed properties.
    /// Used for single-value database results and API responses.
    /// </summary>
    public class ScalarModel : ICloneable
    {
        /// <summary>The generated SQL command string (for debugging).</summary>
        [JsonPropertyName("in_sql")]
        public string? in_sql { get; set; } = "";

        /// <summary>Error message (if any). Empty string or null indicates success.</summary>
        [JsonPropertyName("out_err")]
        public string? out_err { get; set; } = "";

        /// <summary>String result value.</summary>
        [JsonPropertyName("out_value_str")]
        public string? out_value_str { get; set; } = "";

        /// <summary>Integer result value.</summary>
        [JsonPropertyName("out_value_int")]
        public int out_value_int { get; set; } = 0;

        /// <summary>Long result value.</summary>
        [JsonPropertyName("out_value_long")]
        public long out_value_long { get; set; } = 0;

        /// <summary>Double result value.</summary>
        [JsonPropertyName("out_value_dbl")]
        public double out_value_dbl { get; set; } = 0.0;

        /// <summary>Binary result data (e.g., images, files).</summary>
        [JsonPropertyName("out_bytes")]
        public byte[]? out_bytes { get; set; }

        /// <summary>Boolean result value.</summary>
        [JsonPropertyName("out_value_bool")]
        public bool out_value_bool { get; set; } = false;

        /// <summary>Result from cloud source (for merged operations).</summary>
        [JsonPropertyName("out_cloud")]
        public string? out_cloud { get; set; } = "";

        /// <summary>Result from local source (for merged operations).</summary>
        [JsonPropertyName("out_local")]
        public string? out_local { get; set; } = "";

        /// <inheritdoc/>
        public object Clone()
        {
            return new ScalarModel
            {
                in_sql = this.in_sql,
                out_err = this.out_err,
                out_value_str = this.out_value_str,
                out_value_int = this.out_value_int,
                out_value_long = this.out_value_long,
                out_value_dbl = this.out_value_dbl,
                out_bytes = this.out_bytes?.Clone() as byte[],
                out_value_bool = this.out_value_bool,
                out_cloud = this.out_cloud,
                out_local = this.out_local
            };
        }
    }

    /// <summary>
    /// Generic reader model that extends ScalarModel with typed data.
    /// Used for queries returning multiple rows.
    /// </summary>
    /// <typeparam name="T">The type of items in the result list.</typeparam>
    public class ReaderModel<T> : ScalarModel
    {
        /// <summary>Single item result (for queries expecting one row).</summary>
        public T? out_data { get; set; }

        /// <summary>List of items (for queries expecting multiple rows).</summary>
        public List<T?>? out_list { get; set; }

        /// <summary>JSON representation of the result.</summary>
        public string? out_json { get; set; } = "";
    }

    /// <summary>
    /// Connection string configuration model.
    /// Maps parameters from the connection JSON file.
    /// </summary>
    public class ConnectionStringModel
    {
        /// <summary>Database server address.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Server { get; set; }

        /// <summary>Database name.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Database { get; set; }

        /// <summary>Database username (for SQL Server authentication).</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? User_ID { get; set; }

        /// <summary>Database password (for SQL Server authentication).</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Password { get; set; }

        /// <summary>Whether to encrypt the connection.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Encrypt { get; set; } = false;

        /// <summary>Whether to use integrated Windows authentication.</summary>
        public bool Integrated_Security { get; set; } = false;

        /// <summary>Connection timeout in seconds.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Connection_Timeout { get; set; } = 0;

        /// <summary>Whether to enable connection pooling.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Pooling { get; set; } = true;

        /// <summary>Minimum pool size.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Min_Pool_Size { get; set; } = 0;

        /// <summary>Maximum pool size.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Max_Pool_Size { get; set; } = 0;

        /// <summary>Whether to enable multiple active result sets (MARS).</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool MultipleActiveResultSets { get; set; } = false;

        /// <summary>Application name for the connection.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Application_Name { get; set; }

        /// <summary>Whether to trust the server certificate (for encrypted connections).</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool TrustServerCertificate { get; set; } = false;

        /// <summary>Current language setting for the connection.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Current_Language { get; set; }

        /// <summary>Network packet size in bytes.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Packet_Size { get; set; } = 0;

        /// <summary>Workstation identifier.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Workstation_ID { get; set; }

        /// <summary>Internal flag to show the connection form.</summary>
        [JsonIgnore]
        public bool Int__ShowForm { get; set; } = false;

        /// <summary>Internal connection file name.</summary>
        [JsonIgnore]
        public string Int__ConnectionFileName { get; set; } = "";

        /// <summary>Internal path type indicator.</summary>
        [JsonIgnore]
        public string Int__Pathtype { get; set; } = "";

        /// <summary>Internal connection string name.</summary>
        [JsonIgnore]
        public string Int__ConnectionStringName { get; set; } = "";

        /// <summary>Internal decrypted password (for in-memory use only).</summary>
        [JsonIgnore]
        public string Int__PasswordDecrypted { get; set; } = "";

        /// <summary>
        /// Resets all connection parameters to their default values.
        /// </summary>
        public void Clear()
        {
            Server = "";
            Database = "";
            User_ID = "";
            Password = "";
            Encrypt = false;
            Integrated_Security = false;
            Connection_Timeout = 0;
            Pooling = true;
            Min_Pool_Size = 0;
            Max_Pool_Size = 0;
            MultipleActiveResultSets = false;
            Application_Name = "";
            TrustServerCertificate = false;
            Current_Language = "";
            Packet_Size = 0;
            Workstation_ID = "";

            Int__ConnectionFileName = "";
            Int__ConnectionStringName = "";
            Int__PasswordDecrypted = "";
        }

        /// <inheritdoc/>
        public object Clone()
        {
            return MemberwiseClone();
        }
    }

    /// <summary>
    /// Custom JSON converter for nullable DateTime values.
    /// Handles null values gracefully during serialization/deserialization.
    /// </summary>
    public class CustomNullConverter : JsonConverter<DateTime?>
    {
        /// <inheritdoc/>
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }
            return JsonSerializer.Deserialize<DateTime?>(ref reader, options);
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                JsonSerializer.Serialize(writer, value, options);
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }

    /// <summary>
    /// Simple container for returning a single result with optional error information.
    /// Used for direct scalar operations without cloud/local merging logic.
    /// </summary>
    public class Scalar : ICloneable
    {
        /// <summary>Indicates whether the operation was successful (no error).</summary>
        public bool Success => string.IsNullOrEmpty(Error);

        /// <summary>Gets or sets the error message (if any).</summary>
        public string? Error { get; set; } = "";

        /// <summary>String result value.</summary>
        public string? ValString { get; set; } = "";

        /// <summary>Integer result value.</summary>
        public int? ValInt { get; set; } = null;

        /// <summary>Long result value.</summary>
        public long? ValLong { get; set; } = null;

        /// <summary>Double result value.</summary>
        public double? ValDouble { get; set; } = null;

        /// <summary>Boolean result value.</summary>
        public bool? ValBool { get; set; } = null;

        /// <summary>Binary result data.</summary>
        public byte[]? ValByte { get; set; } = null;

        /// <inheritdoc/>
        public object Clone()
        {
            return new Scalar
            {
                ValString = this.ValString,
                ValInt = this.ValInt,
                ValLong = this.ValLong,
                ValDouble = this.ValDouble,
                ValBool = this.ValBool,
                ValByte = this.ValByte?.Clone() as byte[],
                Error = this.Error
            };
        }
    }
}

//using System.Text.Json;
//using System.Text.Json.Serialization;

//namespace BlazorCore.Services.SqlClient
//{
//    public enum ExecuteMode
//    {
//        ExecuteNonQuery,
//        ExecuteReader,
//        ExecuteReaderJson,
//        ExecuteScalar,
//        ExecuteByte,
//        Unknown
//    }

//    public class SqlClientModel
//    {
//    }

//    /// <summary>
//    /// Datentypdefinitionen aus MSSQL Datenbank
//    /// </summary>
//    public class TableDefinition
//    {
//        public TableDefinition()
//        {
//        }

//        public TableDefinition(string column_name
//                                        , string sp_parameter_name
//                                        , string data_type
//                                        , int col_size)
//        {
//            COLUMN_NAME = column_name;
//            SP_PARAMETER_NAME = sp_parameter_name;
//            DATA_TYPE = data_type;
//            COL_SIZE = col_size;
//        }
//        public string COLUMN_NAME { get; set; } = "";
//        public string SP_PARAMETER_NAME { get; set; } = "";
//        public int COL_SIZE { get; set; } = 0;
//        public int DATA_ADOTYPE { get; set; } = 0;

//        public string NET_TYPE { get; set; } = "";

//        private string _DATA_TYPE = "";
//        public string DATA_TYPE
//        {
//            get
//            {
//                return _DATA_TYPE;
//            }
//            set
//            {
//                _DATA_TYPE = value;

//                // Stand 27.11.2023 / Dsi: SqlDbType Enum => https://docs.microsoft.com/en-us/dotnet/api/system.data.sqldbtype?view=dotnet-plat-ext-5.0
//                switch (value.Trim().ToLower())
//                {
//                    case "bigint":
//                        {
//                            DATA_ADOTYPE = 0;
//                            NET_TYPE = "long";
//                            break;
//                        }

//                    case "binary":
//                        {
//                            DATA_ADOTYPE = 1;
//                            NET_TYPE = "byte[]";
//                            break;
//                        }

//                    case "bit":
//                        {
//                            DATA_ADOTYPE = 2;
//                            NET_TYPE = "bool";
//                            break;
//                        }

//                    case "char":
//                        {
//                            DATA_ADOTYPE = 3;
//                            NET_TYPE = "string";
//                            break;
//                        }

//                    case "date":
//                        {
//                            DATA_ADOTYPE = 31;
//                            NET_TYPE = "DateTime";
//                            break;
//                        }

//                    case "datetime":
//                        {
//                            DATA_ADOTYPE = 4;
//                            NET_TYPE = "DateTime";
//                            break;
//                        }

//                    case "datetime2":
//                        {
//                            DATA_ADOTYPE = 33;
//                            NET_TYPE = "DateTime";
//                            break;
//                        }

//                    case "datedimeoffset":
//                        {
//                            //DATA_ADOTYPE = 34;
//                            //NET_TYPE = "DateTime";
//                            //break;
//                            DATA_ADOTYPE = (int)System.Data.SqlDbType.DateTimeOffset; // 34
//                            NET_TYPE = "DateTimeOffset";
//                            break;
//                        }

//                    case "decimal":
//                        {
//                            DATA_ADOTYPE = 5;
//                            NET_TYPE = "double";
//                            break;
//                        }

//                    case "float":
//                        {
//                            DATA_ADOTYPE = 6;
//                            NET_TYPE = "double";
//                            break;
//                        }

//                    case "image":
//                        {
//                            DATA_ADOTYPE = 7;
//                            NET_TYPE = "byte[]";
//                            break;
//                        }

//                    case "int":
//                        {
//                            DATA_ADOTYPE = 8;
//                            NET_TYPE = "int";
//                            break;
//                        }

//                    case "money":
//                        {
//                            DATA_ADOTYPE = 9;
//                            NET_TYPE = "double";
//                            break;
//                        }

//                    case "nchar":
//                        {
//                            DATA_ADOTYPE = 10;
//                            NET_TYPE = "string";
//                            break;
//                        }

//                    case "ntext":
//                        {
//                            DATA_ADOTYPE = 11;
//                            NET_TYPE = "string";
//                            break;
//                        }

//                    case "nvarchar":
//                        {
//                            DATA_ADOTYPE = 12;
//                            NET_TYPE = "string";
//                            break;
//                        }

//                    case "real":
//                        {
//                            DATA_ADOTYPE = 13;
//                            NET_TYPE = "double";
//                            break;
//                        }

//                    case "smalldatetime":
//                        {
//                            DATA_ADOTYPE = 15;
//                            NET_TYPE = "DateTime";
//                            break;
//                        }

//                    case "smallint":
//                        {
//                            DATA_ADOTYPE = 16;
//                            NET_TYPE = "int";
//                            break;
//                        }

//                    case "smallmoney":
//                        {
//                            DATA_ADOTYPE = 17;
//                            NET_TYPE = "double";
//                            break;
//                        }

//                    case "structured":
//                        {
//                            DATA_ADOTYPE = 30;
//                            NET_TYPE = "byte[]";
//                            break;
//                        }

//                    case "text":
//                        {
//                            DATA_ADOTYPE = 18;
//                            NET_TYPE = "string";
//                            break;
//                        }

//                    case "time":
//                        {
//                            DATA_ADOTYPE = 32;
//                            NET_TYPE = "DateTime";
//                            break;
//                        }

//                    case "timestamp":
//                        {
//                            DATA_ADOTYPE = 19;
//                            NET_TYPE = "int";
//                            break;
//                        }

//                    case "tinyint":
//                        {
//                            DATA_ADOTYPE = 20;
//                            NET_TYPE = "int";
//                            break;
//                        }

//                    case "udt":
//                        {
//                            DATA_ADOTYPE = 29;
//                            NET_TYPE = "int";
//                            break;
//                        }

//                    case "uniqueIdentifier":
//                        {
//                            DATA_ADOTYPE = 14;
//                            NET_TYPE = "int";
//                            break;
//                        }

//                    case "varbinary":
//                        {
//                            DATA_ADOTYPE = 21;
//                            NET_TYPE = "byte[]";
//                            break;
//                        }

//                    case "varChar":
//                        {
//                            DATA_ADOTYPE = 22;
//                            NET_TYPE = "string";
//                            break;
//                        }

//                    case "variant":
//                        {
//                            DATA_ADOTYPE = 23;
//                            NET_TYPE = "byte[]";
//                            break;
//                        }

//                    case "xml":
//                        {
//                            DATA_ADOTYPE = 25;
//                            NET_TYPE = "string";
//                            break;
//                        }

//                    default:
//                        {
//                            DATA_ADOTYPE = 18;
//                            NET_TYPE = "string";
//                            break;
//                        }
//                }
//            }
//        }
//    }

//    /// <summary>
//    /// Ein Dummy Model wenn die Methode spExecute über Scalar ausgeführt wird
//    /// </summary>
//    public class DummyModel
//    {
//        public bool dummy { get; set; } = false;
//    }

//    /// <summary>
//    /// Model zum Lesen von Daten bei WebApi
//    /// </summary>
//    public class ReaderDynamicModel
//    {
//        public string? out_err { get; set; } = "";
//        public string? out_json { get; set; } = "";
//    }

//    /// <summary>
//    /// Model für Dictionary (Schlüssel und Wert)
//    /// </summary>
//    public class DictionaryModel
//    {
//        public string? key { get; set; }
//        public string? value { get; set; }
//    }




//    /// <summary>
//    /// Generisches Model zum Lesen von Daten
//    /// </summary>
//    public class ReadModel<T>
//    {
//        public string? out_err { get; set; } = "";
//        public List<T?>? out_list { get; set; }
//        //public List<T?>? out_list_mssql { get; set; }
//        public List<T?>? out_list_cloud { get; set; }
//        //public List<T?>? out_list_sqlite { get; set; }
//        public List<T?>? out_list_local { get; set; }
//    }

//    /// <summary>
//    /// Generisches Model zum Lesen von Skalarwerten
//    /// </summary>
//    public class ScalarModel : ICloneable
//    {
//        [JsonPropertyName("in_sql")]
//        public string? in_sql { get; set; } = "";

//        [JsonPropertyName("out_err")]
//        public string? out_err { get; set; } = "";

//        [JsonPropertyName("out_value_str")]
//        public string? out_value_str { get; set; } = "";

//        [JsonPropertyName("out_value_int")]
//        public int out_value_int { get; set; } = 0;

//        [JsonPropertyName("out_value_long")]
//        public long out_value_long { get; set; } = 0;

//        [JsonPropertyName("out_value_dbl")]
//        public double out_value_dbl { get; set; } = 0.0;

//        [JsonPropertyName("out_bytes")]
//        public byte[]? out_bytes { get; set; }

//        [JsonPropertyName("out_value_bool")]
//        public bool out_value_bool { get; set; } = false;

//        //[JsonPropertyName("out_mssql")]
//        //public string? out_mssql { get; set; } = "";
//        [JsonPropertyName("out_cloud")]
//        public string? out_cloud { get; set; } = "";

//        //[JsonPropertyName("out_sqlite")]
//        //public string? out_sqlite { get; set; } = "";
//        [JsonPropertyName("out_local")]
//        public string? out_local { get; set; } = "";

//        public object Clone()
//        {
//            return new ScalarModel
//            {
//                in_sql = this.in_sql,
//                out_err = this.out_err,
//                out_value_str = this.out_value_str,
//                out_value_int = this.out_value_int,
//                out_value_long = this.out_value_long,
//                out_value_dbl = this.out_value_dbl,
//                out_bytes = this.out_bytes?.Clone() as byte[], // Tiefe Kopie des Byte-Arrays
//                out_value_bool = this.out_value_bool,
//                //out_mssql = this.out_mssql,
//                out_cloud = this.out_cloud,
//                //out_sqlite = this.out_sqlite
//                out_local = this.out_local
//            };
//        }
//    }

//    /// <summary>
//    /// Generisches Model zum Lesen von Daten
//    /// </summary>
//    public class ReaderModel<T> : ScalarModel
//    {
//        //public string out_err { get; set; } = "";
//        public T? out_data { get; set; }
//        public List<T?>? out_list { get; set; }
//        public string? out_json { get; set; } = "";
//        //public byte[]? out_bytes { get; set; }
//    }



//    /// <summary>
//    /// Datentypdefinitionen der ConnectionString Parameter im der Connection-Datei
//    /// </summary>
//    public class ConnectionStringModel
//    {
//        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
//        public string? Server { get; set; }

//        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
//        public string? Database { get; set; }

//        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
//        public string? User_ID { get; set; }

//        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
//        public string? Password { get; set; }

//        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
//        public bool Encrypt { get; set; } = false;

//        public bool Integrated_Security { get; set; } = false;

//        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
//        public int Connection_Timeout { get; set; } = 0;

//        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
//        public bool Pooling { get; set; } = true;

//        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
//        public int Min_Pool_Size { get; set; } = 0;

//        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
//        public int Max_Pool_Size { get; set; } = 0;

//        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
//        public bool MultipleActiveResultSets { get; set; } = false;

//        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
//        public string? Application_Name { get; set; }

//        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
//        public bool TrustServerCertificate { get; set; } = false;

//        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
//        public string? Current_Language { get; set; }

//        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
//        public int Packet_Size { get; set; } = 0;

//        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
//        public string? Workstation_ID { get; set; }

//        [JsonIgnore]
//        public bool Int__ShowForm { get; set; } = false;

//        [JsonIgnore]
//        public string Int__ConnectionFileName { get; set; } = "";

//        [JsonIgnore]
//        public string Int__Pathtype { get; set; } = "";

//        [JsonIgnore]
//        public string Int__ConnectionStringName { get; set; } = "";

//        [JsonIgnore]
//        public string Int__PasswordDecrypted { get; set; } = "";

//        //[JsonIgnore]
//        //public List<SpracheModel> Int_list_sprache { get; set; } = new();


//        public void Clear()
//        {
//            Server = "";
//            Database = "";
//            User_ID = "";
//            Password = "";
//            Encrypt = false;
//            Integrated_Security = false;
//            Connection_Timeout = 0;
//            Pooling = true;
//            Min_Pool_Size = 0;
//            Max_Pool_Size = 0;
//            MultipleActiveResultSets = false;
//            Application_Name = "";
//            TrustServerCertificate = false;
//            Current_Language = "";
//            Packet_Size = 0;
//            Workstation_ID = "";

//            Int__ConnectionFileName = "";
//            Int__ConnectionStringName = "";
//            Int__PasswordDecrypted = "";
//        }

//        public object Clone()
//        {
//            return MemberwiseClone();
//        }
//    }


//    public class CustomNullConverter : System.Text.Json.Serialization.JsonConverter<DateTime?>
//    {
//        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
//        {
//            if (reader.TokenType == JsonTokenType.Null)
//            {
//                return null;
//            }

//            return JsonSerializer.Deserialize<DateTime?>(ref reader, options);
//        }

//        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
//        {
//            if (value.HasValue)
//            {
//                JsonSerializer.Serialize(writer, value, options);
//            }
//            else
//            {
//                writer.WriteNullValue();
//            }
//        }
//    }



//    /// <summary>
//    ///  Container for returning a single result with optional error information.
//    /// </summary>
//    public class Scalar : ICloneable
//    {
//        public bool Success => string.IsNullOrEmpty(Error);

//        public string? Error { get; set; } = "";

//        public string? ValString { get; set; } = "";
//        public int? ValInt { get; set; } = null;
//        public long? ValLong { get; set; } = null;
//        public double? ValDouble { get; set; } = null;
//        public bool? ValBool { get; set; } = null;
//        public byte[]? ValByte { get; set; } = null;

//        public object Clone()
//        {
//            return new Scalar
//            {
//                ValString = this.ValString,
//                ValInt = this.ValInt,
//                ValLong = this.ValLong,
//                ValDouble = this.ValDouble,
//                ValBool = this.ValBool,
//                ValByte = this.ValByte?.Clone() as byte[], // Tiefe Kopie des Byte-Arrays
//                Error = this.Error
//            };
//        }
//    }
//}
