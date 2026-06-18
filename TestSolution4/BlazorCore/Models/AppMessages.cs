using BlazorCore.Services.Dam;
using System.Text;
using System.Text.Json;

namespace BlazorCore.Models
{
    // Existiert nur auf dem Cloud
    public class AppMessagesModel : IBasisModel, ICloneable
    {
        public int ID { get; set; }
        //public string Title { get; set; } = "";
        //public string Beschreibung { get; set; } = "";
        public string DisplayName { get; set; } = string.Empty;

        private string _title = string.Empty;
        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;
                SetLanguageOfTitle(value);
            }
        }

        private string _body = string.Empty;
        public string Body
        {
            get { return _body; }
            set
            {
                _body = value;
                SetLanguageOfBody(value);
            }
        }

        public string imgJpeg { get; set; } = string.Empty;
        public string imgJpegThumbnail { get; set; } = string.Empty;
        public int MsgType { get; set; } = 0;
        public DateTime? RecordDate { get; set; }
        public string LinkInfo { get; set; } = string.Empty;
        public long LastUpdateUnixTS { get; set; } = 0;

        // Interface Properties
        public int sorter { get; set; } = 0;
        public bool Int__MigrationToMSSQL { get; set; } = false;
        public bool Int__MigrationToSqLite { get; set; } = false;
        public int AuthUsers_ID { get; set; }
        public string UnixTS { get; set; } = string.Empty;
        public bool Int__Checked { get; set; }

        public bool Int__inactive { get; set; } = false;
        public bool Int__paid { get; set; } = false;

        public string Int__Title { get; set; } = string.Empty;
        public string Int__EN_title { get; set; } = string.Empty;
        public string Int__DE_title { get; set; } = string.Empty;
        public string Int__AR_title { get; set; } = string.Empty;
        public string Int__ZH_title { get; set; } = string.Empty;
        public string Int__FR_title { get; set; } = string.Empty;
        public string Int__ID_title { get; set; } = string.Empty;
        public string Int__BR_title { get; set; } = string.Empty;
        public string Int__SP_title { get; set; } = string.Empty;
        public string Int__BL_title { get; set; } = string.Empty;
        public string Int__HI_title { get; set; } = string.Empty;

        public string Int__EN { get; set; } = string.Empty;
        public string Int__DE { get; set; } = string.Empty;
        public string Int__AR { get; set; } = string.Empty;
        public string Int__ZH { get; set; } = string.Empty;
        public string Int__FR { get; set; } = string.Empty;
        public string Int__ID { get; set; } = string.Empty;
        public string Int__BR { get; set; } = string.Empty;
        public string Int__SP { get; set; } = string.Empty;
        public string Int__BL { get; set; } = string.Empty;
        public string Int__HI { get; set; } = string.Empty;
        public string Int__Body { get; set; } = string.Empty;

        public object Clone()
        {
            return MemberwiseClone();
        }

        public static AppMessagesModel CloneItem(AppMessagesModel original)
        {
            return (AppMessagesModel)original.Clone();
        }

        private void SetLanguageOfTitle(string jsonText)
        {
            try
            {
                // Versuchen Sie, den JSON-Text zu parsen
                using (JsonDocument document = JsonDocument.Parse(jsonText))
                {
                    JsonElement root = document.RootElement;

                    // Sprach-Eigenschaften basierend auf dem JSON-Text
                    Int__EN_title = root.GetProperty("EN").GetString() ?? jsonText;
                    Int__DE_title = root.GetProperty("DE").GetString() ?? (!string.IsNullOrEmpty(Int__EN_title) ? Int__EN_title : jsonText);
                    Int__AR_title = root.GetProperty("AR").GetString() ?? (!string.IsNullOrEmpty(Int__EN_title) ? Int__EN_title : jsonText);
                    Int__ZH_title = root.GetProperty("ZH").GetString() ?? (!string.IsNullOrEmpty(Int__EN_title) ? Int__EN_title : jsonText);
                    Int__FR_title = root.GetProperty("FR").GetString() ?? (!string.IsNullOrEmpty(Int__EN_title) ? Int__EN_title : jsonText);
                    Int__ID_title = root.GetProperty("ID").GetString() ?? (!string.IsNullOrEmpty(Int__EN_title) ? Int__EN_title : jsonText);
                    Int__BR_title = root.GetProperty("BR").GetString() ?? (!string.IsNullOrEmpty(Int__EN_title) ? Int__EN_title : jsonText);
                    Int__SP_title = root.GetProperty("SP").GetString() ?? (!string.IsNullOrEmpty(Int__EN_title) ? Int__EN_title : jsonText);
                    Int__BL_title = root.GetProperty("BL").GetString() ?? (!string.IsNullOrEmpty(Int__EN_title) ? Int__EN_title : jsonText);
                    Int__HI_title = root.GetProperty("HI").GetString() ?? (!string.IsNullOrEmpty(Int__EN_title) ? Int__EN_title : jsonText);
                }
            }
            catch (JsonException)
            {
                // Wenn der Text kein gültiges JSON ist, setzen Sie den Text direkt
                Int__EN_title = jsonText;
                Int__DE_title = jsonText;
                Int__AR_title = jsonText;
                Int__ZH_title = jsonText;
                Int__FR_title = jsonText;
                Int__ID_title = jsonText;
                Int__BR_title = jsonText;
                Int__SP_title = jsonText;
                Int__BL_title = jsonText;
                Int__HI_title = jsonText;
            }
        }

        private void SetLanguageOfBody(string jsonText)
        {
            try
            {
                // Versuchen Sie, den JSON-Text zu parsen
                using (JsonDocument document = JsonDocument.Parse(jsonText))
                {
                    JsonElement root = document.RootElement;

                    // Setzen Sie die Sprach-Eigenschaften basierend auf dem JSON-Text
                    Int__EN = root.GetProperty("EN").GetString() ?? jsonText;
                    Int__DE = root.GetProperty("DE").GetString() ?? (!string.IsNullOrEmpty(Int__EN) ? Int__EN : jsonText);
                    Int__AR = root.GetProperty("AR").GetString() ?? (!string.IsNullOrEmpty(Int__EN) ? Int__EN : jsonText);
                    Int__ZH = root.GetProperty("ZH").GetString() ?? (!string.IsNullOrEmpty(Int__EN) ? Int__EN : jsonText);
                    Int__FR = root.GetProperty("FR").GetString() ?? (!string.IsNullOrEmpty(Int__EN) ? Int__EN : jsonText);
                    Int__ID = root.GetProperty("ID").GetString() ?? (!string.IsNullOrEmpty(Int__EN) ? Int__EN : jsonText);
                    Int__BR = root.GetProperty("BR").GetString() ?? (!string.IsNullOrEmpty(Int__EN) ? Int__EN : jsonText);
                    Int__SP = root.GetProperty("SP").GetString() ?? (!string.IsNullOrEmpty(Int__EN) ? Int__EN : jsonText);
                    Int__BL = root.GetProperty("BL").GetString() ?? (!string.IsNullOrEmpty(Int__EN) ? Int__EN : jsonText);
                    Int__HI = root.GetProperty("HI").GetString() ?? (!string.IsNullOrEmpty(Int__EN) ? Int__EN : jsonText);
                }
            }
            catch (JsonException)
            {
                // Wenn der Text kein gültiges JSON ist, setzen Sie den Text direkt
                Int__EN = jsonText;
                Int__DE = jsonText;
                Int__AR = jsonText;
                Int__ZH = jsonText;
                Int__FR = jsonText;
                Int__ID = jsonText;
                Int__BR = jsonText;
                Int__SP = jsonText;
                Int__BL = jsonText;
                Int__HI = jsonText;
            }
        }
    }
}
