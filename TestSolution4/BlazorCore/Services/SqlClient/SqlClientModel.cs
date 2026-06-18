using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlazorCore.Services.SqlClient
{
    public enum ExecuteMode
    {
        ExecuteNonQuery,
        ExecuteReader,
        ExecuteReaderJson,
        ExecuteScalar,
        ExecuteByte,
        Unknown
    }

    public class SqlClientModel
    {
    }

    /// <summary>
    /// Datentypdefinitionen aus MSSQL Datenbank
    /// </summary>
    public class TableDefinition
    {
        public TableDefinition()
        {
        }

        public TableDefinition(string column_name
                                        , string sp_parameter_name
                                        , string data_type
                                        , int col_size)
        {
            COLUMN_NAME = column_name;
            SP_PARAMETER_NAME = sp_parameter_name;
            DATA_TYPE = data_type;
            COL_SIZE = col_size;
        }
        public string COLUMN_NAME { get; set; } = "";
        public string SP_PARAMETER_NAME { get; set; } = "";
        public int COL_SIZE { get; set; } = 0;
        public int DATA_ADOTYPE { get; set; } = 0;

        public string NET_TYPE { get; set; } = "";

        private string _DATA_TYPE = "";
        public string DATA_TYPE
        {
            get
            {
                return _DATA_TYPE;
            }
            set
            {
                _DATA_TYPE = value;

                // Stand 27.11.2023 / Dsi: SqlDbType Enum => https://docs.microsoft.com/en-us/dotnet/api/system.data.sqldbtype?view=dotnet-plat-ext-5.0
                switch (value.Trim().ToLower())
                {
                    case "bigint":
                        {
                            DATA_ADOTYPE = 0;
                            NET_TYPE = "long";
                            break;
                        }

                    case "binary":
                        {
                            DATA_ADOTYPE = 1;
                            NET_TYPE = "byte[]";
                            break;
                        }

                    case "bit":
                        {
                            DATA_ADOTYPE = 2;
                            NET_TYPE = "bool";
                            break;
                        }

                    case "char":
                        {
                            DATA_ADOTYPE = 3;
                            NET_TYPE = "string";
                            break;
                        }

                    case "date":
                        {
                            DATA_ADOTYPE = 31;
                            NET_TYPE = "DateTime";
                            break;
                        }

                    case "datetime":
                        {
                            DATA_ADOTYPE = 4;
                            NET_TYPE = "DateTime";
                            break;
                        }

                    case "datetime2":
                        {
                            DATA_ADOTYPE = 33;
                            NET_TYPE = "DateTime";
                            break;
                        }

                    case "datedimeoffset":
                        {
                            //DATA_ADOTYPE = 34;
                            //NET_TYPE = "DateTime";
                            //break;
                            DATA_ADOTYPE = (int)System.Data.SqlDbType.DateTimeOffset; // 34
                            NET_TYPE = "DateTimeOffset";
                            break;
                        }

                    case "decimal":
                        {
                            DATA_ADOTYPE = 5;
                            NET_TYPE = "double";
                            break;
                        }

                    case "float":
                        {
                            DATA_ADOTYPE = 6;
                            NET_TYPE = "double";
                            break;
                        }

                    case "image":
                        {
                            DATA_ADOTYPE = 7;
                            NET_TYPE = "byte[]";
                            break;
                        }

                    case "int":
                        {
                            DATA_ADOTYPE = 8;
                            NET_TYPE = "int";
                            break;
                        }

                    case "money":
                        {
                            DATA_ADOTYPE = 9;
                            NET_TYPE = "double";
                            break;
                        }

                    case "nchar":
                        {
                            DATA_ADOTYPE = 10;
                            NET_TYPE = "string";
                            break;
                        }

                    case "ntext":
                        {
                            DATA_ADOTYPE = 11;
                            NET_TYPE = "string";
                            break;
                        }

                    case "nvarchar":
                        {
                            DATA_ADOTYPE = 12;
                            NET_TYPE = "string";
                            break;
                        }

                    case "real":
                        {
                            DATA_ADOTYPE = 13;
                            NET_TYPE = "double";
                            break;
                        }

                    case "smalldatetime":
                        {
                            DATA_ADOTYPE = 15;
                            NET_TYPE = "DateTime";
                            break;
                        }

                    case "smallint":
                        {
                            DATA_ADOTYPE = 16;
                            NET_TYPE = "int";
                            break;
                        }

                    case "smallmoney":
                        {
                            DATA_ADOTYPE = 17;
                            NET_TYPE = "double";
                            break;
                        }

                    case "structured":
                        {
                            DATA_ADOTYPE = 30;
                            NET_TYPE = "byte[]";
                            break;
                        }

                    case "text":
                        {
                            DATA_ADOTYPE = 18;
                            NET_TYPE = "string";
                            break;
                        }

                    case "time":
                        {
                            DATA_ADOTYPE = 32;
                            NET_TYPE = "DateTime";
                            break;
                        }

                    case "timestamp":
                        {
                            DATA_ADOTYPE = 19;
                            NET_TYPE = "int";
                            break;
                        }

                    case "tinyint":
                        {
                            DATA_ADOTYPE = 20;
                            NET_TYPE = "int";
                            break;
                        }

                    case "udt":
                        {
                            DATA_ADOTYPE = 29;
                            NET_TYPE = "int";
                            break;
                        }

                    case "uniqueIdentifier":
                        {
                            DATA_ADOTYPE = 14;
                            NET_TYPE = "int";
                            break;
                        }

                    case "varbinary":
                        {
                            DATA_ADOTYPE = 21;
                            NET_TYPE = "byte[]";
                            break;
                        }

                    case "varChar":
                        {
                            DATA_ADOTYPE = 22;
                            NET_TYPE = "string";
                            break;
                        }

                    case "variant":
                        {
                            DATA_ADOTYPE = 23;
                            NET_TYPE = "byte[]";
                            break;
                        }

                    case "xml":
                        {
                            DATA_ADOTYPE = 25;
                            NET_TYPE = "string";
                            break;
                        }

                    default:
                        {
                            DATA_ADOTYPE = 18;
                            NET_TYPE = "string";
                            break;
                        }
                }
            }
        }
    }

    /// <summary>
    /// Ein Dummy Model wenn die Methode spExecute über Scalar ausgeführt wird
    /// </summary>
    public class DummyModel
    {
        public bool dummy { get; set; } = false;
    }

    /// <summary>
    /// Model zum Lesen von Daten bei WebApi
    /// </summary>
    public class ReaderDynamicModel
    {
        public string? out_err { get; set; } = "";
        public string? out_json { get; set; } = "";
    }

    /// <summary>
    /// Model für Dictionary (Schlüssel und Wert)
    /// </summary>
    public class DictionaryModel
    {
        public string? key { get; set; }
        public string? value { get; set; }
    }




    /// <summary>
    /// Generisches Model zum Lesen von Daten
    /// </summary>
    public class ReadModel<T>
    {
        public string? out_err { get; set; } = "";
        public List<T?>? out_list { get; set; }
        //public List<T?>? out_list_mssql { get; set; }
        public List<T?>? out_list_cloud { get; set; }
        //public List<T?>? out_list_sqlite { get; set; }
        public List<T?>? out_list_local { get; set; }
    }

    /// <summary>
    /// Generisches Model zum Lesen von Skalarwerten
    /// </summary>
    public class ScalarModel : ICloneable
    {
        [JsonPropertyName("in_sql")]
        public string? in_sql { get; set; } = "";

        [JsonPropertyName("out_err")]
        public string? out_err { get; set; } = "";

        [JsonPropertyName("out_value_str")]
        public string? out_value_str { get; set; } = "";

        [JsonPropertyName("out_value_int")]
        public int out_value_int { get; set; } = 0;

        [JsonPropertyName("out_value_long")]
        public long out_value_long { get; set; } = 0;

        [JsonPropertyName("out_value_dbl")]
        public double out_value_dbl { get; set; } = 0.0;

        [JsonPropertyName("out_bytes")]
        public byte[]? out_bytes { get; set; }

        [JsonPropertyName("out_value_bool")]
        public bool out_value_bool { get; set; } = false;

        //[JsonPropertyName("out_mssql")]
        //public string? out_mssql { get; set; } = "";
        [JsonPropertyName("out_cloud")]
        public string? out_cloud { get; set; } = "";

        //[JsonPropertyName("out_sqlite")]
        //public string? out_sqlite { get; set; } = "";
        [JsonPropertyName("out_local")]
        public string? out_local { get; set; } = "";

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
                out_bytes = this.out_bytes?.Clone() as byte[], // Tiefe Kopie des Byte-Arrays
                out_value_bool = this.out_value_bool,
                //out_mssql = this.out_mssql,
                out_cloud = this.out_cloud,
                //out_sqlite = this.out_sqlite
                out_local = this.out_local
            };
        }
    }

    /// <summary>
    /// Generisches Model zum Lesen von Daten
    /// </summary>
    public class ReaderModel<T> : ScalarModel
    {
        //public string out_err { get; set; } = "";
        public T? out_data { get; set; }
        public List<T?>? out_list { get; set; }
        public string? out_json { get; set; } = "";
        //public byte[]? out_bytes { get; set; }
    }



    /// <summary>
    /// Datentypdefinitionen der ConnectionString Parameter im der Connection-Datei
    /// </summary>
    public class ConnectionStringModel
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Server { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Database { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? User_ID { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Password { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Encrypt { get; set; } = false;

        public bool Integrated_Security { get; set; } = false;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Connection_Timeout { get; set; } = 0;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Pooling { get; set; } = true;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Min_Pool_Size { get; set; } = 0;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Max_Pool_Size { get; set; } = 0;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool MultipleActiveResultSets { get; set; } = false;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Application_Name { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool TrustServerCertificate { get; set; } = false;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Current_Language { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Packet_Size { get; set; } = 0;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Workstation_ID { get; set; }

        [JsonIgnore]
        public bool Int__ShowForm { get; set; } = false;

        [JsonIgnore]
        public string Int__ConnectionFileName { get; set; } = "";

        [JsonIgnore]
        public string Int__Pathtype { get; set; } = "";

        [JsonIgnore]
        public string Int__ConnectionStringName { get; set; } = "";

        [JsonIgnore]
        public string Int__PasswordDecrypted { get; set; } = "";

        //[JsonIgnore]
        //public List<SpracheModel> Int_list_sprache { get; set; } = new();


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

        public object Clone()
        {
            return MemberwiseClone();
        }
    }


    public class CustomNullConverter : System.Text.Json.Serialization.JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            return JsonSerializer.Deserialize<DateTime?>(ref reader, options);
        }

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
    ///  Container for returning a single result with optional error information.
    /// </summary>
    public class Scalar : ICloneable
    {
        public bool Success => string.IsNullOrEmpty(Error);

        public string? Error { get; set; } = "";

        public string? ValString { get; set; } = "";
        public int? ValInt { get; set; } = null;
        public long? ValLong { get; set; } = null;
        public double? ValDouble { get; set; } = null;
        public bool? ValBool { get; set; } = null;
        public byte[]? ValByte { get; set; } = null;

        public object Clone()
        {
            return new Scalar
            {
                ValString = this.ValString,
                ValInt = this.ValInt,
                ValLong = this.ValLong,
                ValDouble = this.ValDouble,
                ValBool = this.ValBool,
                ValByte = this.ValByte?.Clone() as byte[], // Tiefe Kopie des Byte-Arrays
                Error = this.Error
            };
        }
    }
}
