using pFemmeExample.Shared.Models;
using System.Text.Json.Serialization;

namespace pFemmeExample.JsonContexts
{
    /// <summary>
    /// JSON source generation context for pFemme customer models.
    /// </summary>
    [JsonSourceGenerationOptions(
        WriteIndented = false,
        PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
        PropertyNameCaseInsensitive = true
    )]
    [JsonSerializable(typeof(CyclesModel))]
    [JsonSerializable(typeof(List<CyclesModel>))]
    [JsonSerializable(typeof(CyclesCompareModel))]
    [JsonSerializable(typeof(List<CyclesCompareModel>))]
    [JsonSerializable(typeof(CyclePhasesModel))]
    [JsonSerializable(typeof(List<CyclePhasesModel>))]
    [JsonSerializable(typeof(ChartsModel))]
    [JsonSerializable(typeof(List<ChartsModel>))]
    public partial class pFemmeJsonContext : JsonSerializerContext
    {
    }
}
