using System.Data;

namespace BlazorCore.Services.SqlClient
{
    public interface ISqlClientBase
    {
        string connectionString { get; }
        bool isConnected { get; }

        //Task<string> MapSaveExecTbl(string _exec);
        //Task<string> MapTbls(string _tables);
        Task MapApplTbls();

        Task<ScalarModel> Scalar(Dictionary<string, string> _db_para);
        Task<ReaderDynamicModel> Reader(Dictionary<string, string> _db_para);

        // KORREKTUR: Constraint hinzugefügt 
        Task<ReaderModel<T>> Reader<T>(Dictionary<string, string> _db_para) where T : new();

        Task<ScalarModel> Bytes(Dictionary<string, string> _db_para);
        Task<ScalarModel> NonQuery(Dictionary<string, string> _db_para);

        // Dieser Constraint war schon korrekt
        Task<ReaderModel<T>> spExecute<T>(Dictionary<string, string> _db_para, ExecuteMode _mode, string _sp = ".Crud") where T : new();

        string DataTableToJson(DataTable dataTable);
        string GetConnectionStringForError(string _err);
        //object? GetItem(DataRow _dr, string _namespace_classname);
        //T? GetItem<T>(DataRow dr) where T : new(); // Hinzugefügt, falls es die alte Reflection-Methode ist

        // KORREKTUR: Constraint hinzugefügt (Lösung für CS0425/CS0310)
        List<T> ConvertDataTable<T>(DataTable? dt) where T : new();

        string CreateExec(Dictionary<string, string> _db_para, string _sp_name = "Crud");

    }
}