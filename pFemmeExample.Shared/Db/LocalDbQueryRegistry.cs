using BlazorCore;
using BlazorCore.Models;
using BlazorCore.Services.Apis;
using BlazorCore.Services.Dam;
using BlazorCore.Services.LocalJsonFile;
using BlazorCore.Services.LocalQueryExecutor;
using BlazorCore.Services.LocalStorage;
using BlazorCore.Services.AppState;
using BlazorCore.Services.SqlClient;
using pFemmeExample.Shared.Models;
using System.Text.Json;

namespace pFemmeExample.Shared.Db
{
    /// <summary>
    /// Neue Registry für die vereinheitlichte Storage-Architektur.
    /// Verwendet ILocalStorage (ehemals MemoryStorageBase) als RAM-Cache.
    /// Unterstützt MEMORY und JSON_HYBRID gleichermaßen.
    /// </summary>
    public static class LocalDbQueryRegistry
    {
        public static void Register(
            ILocalQueryExecutor executor,
            ILocalStorage localStorage,
            ILocalJsonFile localJsonFile,
            IAppStateBase appState) 
        {
            RegisterInitialization(executor, localStorage, localJsonFile);
            RegisterCycles(executor, localStorage, localJsonFile);
            RegisterAuthUsers(executor, localStorage, localJsonFile);
            RegisterAuthUsersExtend(executor, localStorage, localJsonFile);
            RegisterAppParameter(executor, localStorage, localJsonFile, appState);
            RegisterGeneral(executor, localStorage);
        }

        // =========================================================
        // JSON -> RAM INITIALIZATION
        // =========================================================
        private static void RegisterInitialization(
            ILocalQueryExecutor executor,
            ILocalStorage localStorage,
            ILocalJsonFile localJsonFile)
        {
            executor.RegisterSave("InitializeInternal", async ctx =>
            {
                if(ctx.Storage == null) return new ScalarModel { out_value_bool = false, out_err = "Storage not initialized." };

                if (string.IsNullOrEmpty(localStorage.UserAccountHashed))
                    return new ScalarModel { out_err = "UserAccount missing." };

                // Access to the global catalog for the table list
                var tables = pFemmeExample.Shared.Global.Catalog.Sections.TablesMSSQL;
                var context = pFemmeExample.JsonContexts.pFemmeJsonContext.Default;

                if(tables != null)
                {
                    foreach (var table in tables)
                    {
                        var jsonFiles = await localJsonFile.ReadTableFilesAsync(localStorage.UserAccountHashed, table);

                        foreach (var json in jsonFiles)
                        {
                            try
                            {
                                object? item = table switch
                                {
                                    "AuthUsers" => System.Text.Json.JsonSerializer.Deserialize(json, JsonContext.Default.AuthUsersModel),
                                    "AuthUsersExtend" => System.Text.Json.JsonSerializer.Deserialize(json, JsonContext.Default.AuthUsersExtendModel),
                                    "AppParameter" => System.Text.Json.JsonSerializer.Deserialize(json, JsonContext.Default.AppParameterModel),
                                    //"SharingUsers" => System.Text.Json.JsonSerializer.Deserialize(json, JsonContext.Default.SharingUsersModel),
                                    "Cycles" => System.Text.Json.JsonSerializer.Deserialize(json, pFemmeExample.JsonContexts.pFemmeJsonContext.Default.CyclesModel),
                                    _ => null
                                };

                                if (item != null)
                                {
                                    ctx.Storage.RamCache[table].Add(item);
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[Init] Error deserializing {table}: {ex.Message}");
                            }
                        }
                    }
                }

                return new ScalarModel { out_value_bool = true };
            });
        }



        // =========================================================
        // CYCLES
        // =========================================================
        private static void RegisterCycles(
            ILocalQueryExecutor executor,
            ILocalStorage localStorage,
            ILocalJsonFile localJsonFile)
        {
            // =========================================================
            // READ - Select>>Cycles
            // =========================================================
            executor.RegisterRead("Select>>Cycles", ctx =>
            {
                try
                {
                    var authUsers_UnixTS = ctx.Parameters.GetValueOrDefault("@AuthUsers_UnixTS", "");

                    if (string.IsNullOrEmpty(authUsers_UnixTS))
                    {
                        return new LocalQueryResult
                        {
                            success = false,
                            out_err = "AuthUsers_UnixTS is empty",
                            out_list = new List<object?>()
                        };
                    }

                    // Secure access to RAM cache using TryGetValue
                    if (!localStorage.RamCache.TryGetValue("Cycles", out var cyclesCache) || cyclesCache == null)
                    {
                        return new LocalQueryResult
                        {
                            success = false,
                            out_err = "Cycles cache not available",
                            out_list = new List<object?>()
                        };
                    }

                    var cyclesList = cyclesCache.Cast<CyclesModel>().ToList();

                    // Filter records matching the specific user context
                    var data = cyclesList
                        .Where(x => x.AuthUsers_UnixTS == authUsers_UnixTS)
                        .Select(x => new CyclesModel
                        {
                            ID = x.ID,
                            UnixTS = x.UnixTS,
                            AuthUsers_UnixTS = x.AuthUsers_UnixTS,
                            Details = x.Details,
                            RecordDate = x.RecordDate,
                            bleeding = x.bleeding,
                            intensity = x.intensity,
                            pain = x.pain,
                            headache = x.headache,
                            fatigue = x.fatigue,
                            nausea = x.nausea,
                            cramps = x.cramps,
                            created_at = x.created_at,
                            updated_at = x.updated_at,
                            LastUpdateUnixTS = x.LastUpdateUnixTS
                        })
                        .Cast<object?>()
                        .ToList();

                    return new LocalQueryResult
                    {
                        success = true,
                        out_list = data,
                        out_data = data.FirstOrDefault()
                    };
                }
                catch (Exception ex)
                {
                    return new LocalQueryResult
                    {
                        success = false,
                        out_err = $"Select>>Cycles Error: {ex.Message}",
                        out_list = new List<object?>()
                    };
                }
            });

            executor.RegisterRead("SelectDay>>Cycles", ctx =>
            {
                try
                {
                    var authUsers_UnixTS = ctx.Parameters.GetValueOrDefault("@AuthUsers_UnixTS", "");
                    var recordDate = ctx.Parameters.GetValueOrDefault("@RecordDate", "");

                    if (string.IsNullOrEmpty(authUsers_UnixTS))
                    {
                        return new LocalQueryResult
                        {
                            success = false,
                            out_err = "AuthUsers_UnixTS is empty",
                            out_list = new List<object?>()
                        };
                    }

                    if (string.IsNullOrEmpty(recordDate))
                    {
                        return new LocalQueryResult
                        {
                            success = false,
                            out_err = "RecordDate is empty",
                            out_list = new List<object?>()
                        };
                    }

                    // Parse target date safely into non-nullable struct to prevent static analysis warnings
                    DateTime targetDate;
                    try
                    {
                        targetDate = AotConverter.ConvertTo<DateTime>(recordDate);
                    }
                    catch
                    {
                        return new LocalQueryResult
                        {
                            success = false,
                            out_err = $"Invalid RecordDate format: {recordDate}",
                            out_list = new List<object?>()
                        };
                    }

                    // Secure access to RAM cache using TryGetValue
                    if (!localStorage.RamCache.TryGetValue("Cycles", out var cyclesCache) || cyclesCache == null)
                    {
                        return new LocalQueryResult
                        {
                            success = false,
                            out_err = "Cycles cache not available",
                            out_list = new List<object?>()
                        };
                    }

                    var cyclesList = cyclesCache.Cast<CyclesModel>().ToList();

                    // Filter records by AuthUsers_UnixTS and structural safe exact date component match in a single expression
                    var result = cyclesList
                        .Where(x => x.AuthUsers_UnixTS == authUsers_UnixTS &&
                                    x.RecordDate.HasValue &&
                                    x.RecordDate.Value.Year == targetDate.Year &&
                                    x.RecordDate.Value.Month == targetDate.Month &&
                                    x.RecordDate.Value.Day == targetDate.Day)
                        .Select(x => new CyclesModel
                        {
                            ID = x.ID,
                            UnixTS = x.UnixTS,
                            AuthUsers_UnixTS = x.AuthUsers_UnixTS,
                            Details = x.Details,
                            RecordDate = x.RecordDate,
                            bleeding = x.bleeding,
                            intensity = x.intensity,
                            pain = x.pain,
                            headache = x.headache,
                            fatigue = x.fatigue,
                            nausea = x.nausea,
                            cramps = x.cramps,
                            created_at = x.created_at,
                            updated_at = x.updated_at,
                            LastUpdateUnixTS = x.LastUpdateUnixTS
                        })
                        .Cast<object?>()
                        .ToList();

                    return new LocalQueryResult
                    {
                        success = true,
                        out_list = result,
                        out_data = result.FirstOrDefault()
                    };
                }
                catch (Exception ex)
                {
                    return new LocalQueryResult
                    {
                        success = false,
                        out_err = $"SelectDay>>Cycles Error: {ex.Message}",
                        out_list = new List<object?>()
                    };
                }
            });

            executor.RegisterRead("SelectTrendsBleeding>>Cycles", ctx =>
            {
                try
                {
                    var authUsers_UnixTS = ctx.Parameters.GetValueOrDefault("@AuthUsers_UnixTS", "");

                    if (string.IsNullOrEmpty(authUsers_UnixTS))
                    {
                        return new LocalQueryResult
                        {
                            success = false,
                            out_err = "AuthUsers_UnixTS is empty",
                            out_list = new List<object?>()
                        };
                    }

                    // Secure access to RAM cache using TryGetValue
                    if (!localStorage.RamCache.TryGetValue("Cycles", out var cyclesCache) || cyclesCache == null)
                    {
                        return new LocalQueryResult
                        {
                            success = false,
                            out_err = "Cycles cache not available",
                            out_list = new List<object?>()
                        };
                    }

                    var cyclesList = cyclesCache.Cast<CyclesModel>().ToList();

                    // Filter by user context and bleeding status, then map directly to ChartsModel structural format
                    var data = cyclesList
                        .Where(x => x.AuthUsers_UnixTS == authUsers_UnixTS && x.bleeding == true)
                        .OrderBy(x => x.RecordDate)
                        .Select(x => new ChartsModel
                        {
                            Int__Date = x.RecordDate ?? DateTime.MinValue,
                            Int__Value = x.intensity
                        })
                        .Cast<object?>()
                        .ToList();

                    return new LocalQueryResult
                    {
                        success = true,
                        out_list = data,
                        out_data = data.FirstOrDefault()
                    };
                }
                catch (Exception ex)
                {
                    return new LocalQueryResult
                    {
                        success = false,
                        out_err = $"SelectTrendsBleeding>>Cycles Error: {ex.Message}",
                        out_list = new List<object?>()
                    };
                }
            });

            executor.RegisterRead("SelectTrendsPain>>Cycles", ctx =>
            {
                try
                {
                    var authUsers_UnixTS = ctx.Parameters.GetValueOrDefault("@AuthUsers_UnixTS", "");

                    if (string.IsNullOrEmpty(authUsers_UnixTS))
                    {
                        return new LocalQueryResult
                        {
                            success = false,
                            out_err = "AuthUsers_UnixTS is empty",
                            out_list = new List<object?>()
                        };
                    }

                    // Secure access to RAM cache using TryGetValue
                    if (!localStorage.RamCache.TryGetValue("Cycles", out var cyclesCache) || cyclesCache == null)
                    {
                        return new LocalQueryResult
                        {
                            success = false,
                            out_err = "Cycles cache not available",
                            out_list = new List<object?>()
                        };
                    }

                    var cyclesList = cyclesCache.Cast<CyclesModel>().ToList();

                    // Filter by user context and pain evaluation score, then map to ChartsModel structural format
                    var data = cyclesList
                        .Where(x => x.AuthUsers_UnixTS == authUsers_UnixTS && x.pain > 0)
                        .OrderBy(x => x.RecordDate)
                        .Select(x => new ChartsModel
                        {
                            Int__Date = x.RecordDate ?? DateTime.MinValue,
                            Int__Value = x.pain
                        })
                        .Cast<object?>()
                        .ToList();

                    return new LocalQueryResult
                    {
                        success = true,
                        out_list = data,
                        out_data = data.FirstOrDefault()
                    };
                }
                catch (Exception ex)
                {
                    return new LocalQueryResult
                    {
                        success = false,
                        out_err = $"SelectTrendsPain>>Cycles Error: {ex.Message}",
                        out_list = new List<object?>()
                    };
                }
            });


            // =========================================================
            // SAVE - Save>>Cycles
            // =========================================================
            executor.RegisterSave("Save>>Cycles", async ctx =>
            {
                try
                {
                    // ====================================================================
                    // READ PARAMETERS
                    // ====================================================================
                    var unixTS = ctx.Parameters.GetValueOrDefault("@UnixTS", "");
                    var authUsers_UnixTS = ctx.Parameters.GetValueOrDefault("@AuthUsers_UnixTS", "");
                    var details = ctx.Parameters.GetValueOrDefault("@Details", "");
                    var recordDate = ctx.Parameters.GetValueOrDefault("@RecordDate", "");
                    var bleeding = ctx.Parameters.GetValueOrDefault("@bleeding", "");
                    var intensity = ctx.Parameters.GetValueOrDefault("@intensity", "");
                    var pain = ctx.Parameters.GetValueOrDefault("@pain", "");
                    var headache = ctx.Parameters.GetValueOrDefault("@headache", "");
                    var fatigue = ctx.Parameters.GetValueOrDefault("@fatigue", "");
                    var nausea = ctx.Parameters.GetValueOrDefault("@nausea", "");
                    var cramps = ctx.Parameters.GetValueOrDefault("@cramps", "");
                    var createdAt = ctx.Parameters.GetValueOrDefault("@created_at", "");
                    var updatedAt = ctx.Parameters.GetValueOrDefault("@updated_at", "");
                    var lastUpdateUnixTS = ctx.Parameters.GetValueOrDefault("@LastUpdateUnixTS", "");

                    var storageType = pFemmeExample.Shared.Global.Configuration.ConfigGeneral.LocalStorageType;

                    // ====================================================================
                    // CONVERSIONS VIA AotConverter
                    // ====================================================================
                    // DateTime? (RecordDate)
                    DateTime? convertedRecordDate = null;
                    if (!string.IsNullOrEmpty(recordDate))
                    {
                        try
                        {
                            convertedRecordDate = AotConverter.ConvertTo<DateTime>(recordDate);
                        }
                        catch
                        {
                            if (long.TryParse(recordDate, out long unixDate))
                                convertedRecordDate = DateTimeOffset.FromUnixTimeSeconds(unixDate).DateTime;
                        }
                    }

                    // bool (bleeding)
                    bool convertedBleeding = AotConverter.ConvertTo<bool>(bleeding);

                    // int values (intensity, pain, headache, fatigue, nausea, cramps)
                    int convertedIntensity = AotConverter.ConvertTo<int>(intensity);
                    int convertedPain = AotConverter.ConvertTo<int>(pain);
                    int convertedHeadache = AotConverter.ConvertTo<int>(headache);
                    int convertedFatigue = AotConverter.ConvertTo<int>(fatigue);
                    int convertedNausea = AotConverter.ConvertTo<int>(nausea);
                    int convertedCramps = AotConverter.ConvertTo<int>(cramps);

                    // DateTime? (created_at, updated_at)
                    DateTime? convertedCreatedAt = null;
                    if (!string.IsNullOrEmpty(createdAt))
                    {
                        try
                        {
                            convertedCreatedAt = AotConverter.ConvertTo<DateTime>(createdAt);
                        }
                        catch
                        {
                            if (long.TryParse(createdAt, out long unixCreated))
                                convertedCreatedAt = DateTimeOffset.FromUnixTimeSeconds(unixCreated).DateTime;
                        }
                    }

                    DateTime? convertedUpdatedAt = null;
                    if (!string.IsNullOrEmpty(updatedAt))
                    {
                        try
                        {
                            convertedUpdatedAt = AotConverter.ConvertTo<DateTime>(updatedAt);
                        }
                        catch
                        {
                            if (long.TryParse(updatedAt, out long unixUpdated))
                                convertedUpdatedAt = DateTimeOffset.FromUnixTimeSeconds(unixUpdated).DateTime;
                        }
                    }

                    // long (LastUpdateUnixTS)
                    long convertedLastUpdateUnixTS = AotConverter.ConvertTo<long>(lastUpdateUnixTS);
                    if (convertedLastUpdateUnixTS == 0)
                        convertedLastUpdateUnixTS = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                    // ====================================================================
                    // CACHE VALIDATION
                    // ====================================================================
                    if (!localStorage.RamCache.TryGetValue("Cycles", out var cyclesCache) || cyclesCache == null)
                    {
                        return new ScalarModel
                        {
                            out_err = "Cycles cache not available",
                            out_value_str = "not_saved"
                        };
                    }

                    var ramList = cyclesCache.Cast<CyclesModel>().ToList();

                    // Check if record already exists based on structural unique key
                    var existingEntry = ramList.FirstOrDefault(x => x.UnixTS == unixTS);
                    CyclesModel updatedEntry;
                    bool isInsert = existingEntry == null;

                    if (isInsert)
                    {
                        // INSERT: Generate unique incremental ID within RAM scope copy
                        int newId = ramList.Any() ? ramList.Max(x => x.ID) + 1 : 1;

                        updatedEntry = new CyclesModel
                        {
                            ID = newId,
                            UnixTS = unixTS,
                            AuthUsers_UnixTS = authUsers_UnixTS,
                            Details = details,
                            RecordDate = convertedRecordDate,
                            bleeding = convertedBleeding,
                            intensity = convertedIntensity,
                            pain = convertedPain,
                            headache = convertedHeadache,
                            fatigue = convertedFatigue,
                            nausea = convertedNausea,
                            cramps = convertedCramps,
                            created_at = convertedCreatedAt ?? DateTime.UtcNow,
                            updated_at = convertedUpdatedAt ?? DateTime.UtcNow,
                            LastUpdateUnixTS = convertedLastUpdateUnixTS
                        };
                    }
                    else
                    {
                        // UPDATE: Prepare modified object preserving structural key definitions
                        updatedEntry = new CyclesModel
                        {
                            ID = existingEntry!.ID,
                            UnixTS = existingEntry.UnixTS,
                            AuthUsers_UnixTS = authUsers_UnixTS,
                            Details = details,
                            RecordDate = convertedRecordDate ?? existingEntry.RecordDate,
                            bleeding = convertedBleeding,
                            intensity = convertedIntensity,
                            pain = convertedPain,
                            headache = convertedHeadache,
                            fatigue = convertedFatigue,
                            nausea = convertedNausea,
                            cramps = convertedCramps,
                            created_at = convertedCreatedAt ?? existingEntry.created_at,
                            updated_at = convertedUpdatedAt ?? DateTime.UtcNow,
                            LastUpdateUnixTS = convertedLastUpdateUnixTS
                        };
                    }

                    // ============================================================
                    // DETERMINE SOURCE FOR RAM (JSON or MEMORY)
                    // ============================================================
                    CyclesModel dataToSave = updatedEntry;

                    if (storageType == LOCAL_STORAGE_TYPE.JSON_HYBRID)
                    {
                        // AOT-safe serialization using specialized pFemmeJsonContext
                        var json = System.Text.Json.JsonSerializer.Serialize(
                            updatedEntry,
                            pFemmeExample.JsonContexts.pFemmeJsonContext.Default.CyclesModel);

                        var fileName = $"Cycles/{unixTS}.json";

                        // Save to physical JSON file
                        var writeResult = await localJsonFile.WritePhysicalFileAsync(localStorage.UserAccountHashed, fileName, json);
                        if (!writeResult.out_value_bool)
                        {
                            return new ScalarModel
                            {
                                out_err = $"JSON save failed: {writeResult.out_err}",
                                out_value_str = "not_saved"
                            };
                        }

                        // Reload from JSON file to guarantee 100% data consistency
                        var reloadedJson = await localJsonFile.ReadFileAsync(localStorage.UserAccountHashed, fileName);
                        if (reloadedJson != null)
                        {
                            var reloadedEntry = System.Text.Json.JsonSerializer.Deserialize<CyclesModel>(
                                reloadedJson,
                                pFemmeExample.JsonContexts.pFemmeJsonContext.Default.CyclesModel);

                            if (reloadedEntry != null)
                            {
                                dataToSave = reloadedEntry;
                            }
                        }
                    }

                    // ============================================================
                    // UPDATE RAM
                    // ============================================================
                    if (isInsert)
                    {
                        ramList.Add(dataToSave);
                    }
                    else
                    {
                        var index = ramList.FindIndex(x => x.UnixTS == unixTS);
                        if (index >= 0)
                        {
                            ramList[index] = dataToSave;
                        }
                    }

                    localStorage.RamCache["Cycles"] = ramList.Cast<object>().ToList();

                    var returnStatus = isInsert ? "saved" : "updated";
                    return new ScalarModel
                    {
                        out_value_str = $"{returnStatus}:{dataToSave.ID}:{authUsers_UnixTS}"
                    };
                }
                catch (Exception ex)
                {
                    return new ScalarModel
                    {
                        out_value_bool = false,
                        out_err = $"Save>>Cycles Error: {ex.Message}",
                        out_value_str = "not_saved"
                    };
                }
            });

            // ====================================================================
            // DELETE - Delete>>Cycles (Cascading Atomic Consistency)
            // ====================================================================
            executor.RegisterSave("Delete>>Cycles", async ctx =>
            {
                try
                {
                    var authUsers_UnixTS = ctx.Parameters.GetValueOrDefault("@AuthUsers_UnixTS", "");
                    var storageType = pFemmeExample.Shared.Global.Configuration.ConfigGeneral.LocalStorageType;

                    if (string.IsNullOrEmpty(authUsers_UnixTS))
                    {
                        return new ScalarModel
                        {
                            out_value_bool = false,
                            out_value_str = "not_deleted",
                            out_err = "Delete>>Cycles Error: AuthUsers_UnixTS is empty"
                        };
                    }

                    // ====================================================================
                    // PHASE 1: CYCLES CLEANUP (Main Table)
                    // ====================================================================
                    if (!localStorage.RamCache.TryGetValue("Cycles", out var cyclesCache) || cyclesCache == null)
                    {
                        return new ScalarModel
                        {
                            out_value_bool = false,
                            out_err = "Delete>>Cycles Error: Cycles cache not available",
                            out_value_str = "not_deleted"
                        };
                    }

                    var currentCycles = cyclesCache.Cast<CyclesModel>().ToList();
                    var cyclesToKeepInRam = new List<CyclesModel>();
                    var cyclesFailedToDelete = new List<CyclesModel>();

                    if (storageType == LOCAL_STORAGE_TYPE.JSON_HYBRID)
                    {
                        foreach (var item in currentCycles)
                        {
                            if (item.AuthUsers_UnixTS == authUsers_UnixTS)
                            {
                                // Target specific file using the unique record UnixTS string key
                                var fileName = $"Cycles/{item.UnixTS}.json";

                                // Rely entirely on the cleaned service layer (True = deleted or non-existent)
                                bool deleteSuccess = await localJsonFile.DeletePhysicalFileAsync(localStorage.UserAccountHashed, fileName);

                                if (!deleteSuccess)
                                {
                                    cyclesFailedToDelete.Add(item);
                                }
                            }
                            else
                            {
                                cyclesToKeepInRam.Add(item);
                            }
                        }

                        // If any targeted physical file failed to delete, roll back RAM to match disk state and abort
                        if (cyclesFailedToDelete.Count > 0)
                        {
                            cyclesToKeepInRam.AddRange(cyclesFailedToDelete);
                            localStorage.RamCache["Cycles"] = cyclesToKeepInRam.Cast<object>().ToList();

                            return new ScalarModel
                            {
                                out_value_bool = false,
                                out_err = "Delete>>Cycles Error: Core consistency partial fail on physical file deletion for Cycles table.",
                                out_value_str = "not_deleted"
                            };
                        }
                    }
                    else
                    {
                        // Pure MEMORY mode filtering
                        cyclesToKeepInRam = currentCycles.Where(x => x.AuthUsers_UnixTS != authUsers_UnixTS).ToList();
                    }

                    // ATOMIC RAM COMMIT FOR PHASE 1: Disk and RAM for Cycles are now 100% synchronized
                    localStorage.RamCache["Cycles"] = cyclesToKeepInRam.Cast<object>().ToList();


                    //// ====================================================================
                    //// PHASE 2: SHARINGUSERS CLEANUP (Cascading Table)
                    //// ====================================================================
                    //if (localStorage.RamCache.TryGetValue("SharingUsers", out var sharingCache) && sharingCache != null)
                    //{
                    //    var currentSharing = sharingCache.Cast<SharingUsersModel>().ToList();
                    //    var sharingToKeepInRam = new List<SharingUsersModel>();
                    //    var sharingFailedToDelete = new List<SharingUsersModel>();

                    //    if (storageType == LOCAL_STORAGE_TYPE.JSON_HYBRID)
                    //    {
                    //        foreach (var item in currentSharing)
                    //        {
                    //            if (item.AuthUsers_UnixTS == authUsers_UnixTS)
                    //            {
                    //                var fileName = $"SharingUsers/{item.UnixTS}.json";
                    //                bool deleteSuccess = await localJsonFile.DeletePhysicalFileAsync(localStorage.UserAccountHashed, fileName);

                    //                if (!deleteSuccess)
                    //                {
                    //                    sharingFailedToDelete.Add(item);
                    //                }
                    //            }
                    //            else
                    //            {
                    //                sharingToKeepInRam.Add(item);
                    //            }
                    //        }

                    //        // If any cascading sharing file failed to delete, update RAM with what failed and abort
                    //        if (sharingFailedToDelete.Count > 0)
                    //        {
                    //            sharingToKeepInRam.AddRange(sharingFailedToDelete);
                    //            localStorage.RamCache["SharingUsers"] = sharingToKeepInRam.Cast<object>().ToList();

                    //            return new ScalarModel
                    //            {
                    //                out_value_bool = false,
                    //                out_err = "Delete>>Cycles Error: Cycles table cleared successfully, but cascading SharingUsers table encountered a physical file deletion partial fail.",
                    //                out_value_str = "not_deleted"
                    //            };
                    //        }
                    //    }
                    //    else
                    //    {
                    //        // Pure MEMORY mode filtering
                    //        sharingToKeepInRam = currentSharing.Where(x => x.AuthUsers_UnixTS != authUsers_UnixTS).ToList();
                    //    }

                    //    // ATOMIC RAM COMMIT FOR PHASE 2: Succeeded completely
                    //    localStorage.RamCache["SharingUsers"] = sharingToKeepInRam.Cast<object>().ToList();
                    //}

                    // ============================================================
                    // PHASE 3: FINAL RETURN
                    // ============================================================
                    return new ScalarModel
                    {
                        out_value_bool = true,
                        out_value_str = $"deleted:0:{authUsers_UnixTS}"
                    };
                }
                catch (Exception ex)
                {
                    return new ScalarModel
                    {
                        out_value_bool = false,
                        out_err = $"Delete>>Cycles Error: {ex.Message}",
                        out_value_str = "not_deleted"
                    };
                }
            });

            executor.RegisterSave("DeleteUnixTS>>Cycles", async ctx =>
            {
                try
                {
                    var unixTS = ctx.Parameters.GetValueOrDefault("@UnixTS", "");
                    var authUsers_UnixTS = ctx.Parameters.GetValueOrDefault("@AuthUsers_UnixTS", "");
                    var storageType = pFemmeExample.Shared.Global.Configuration.ConfigGeneral.LocalStorageType;

                    if (string.IsNullOrEmpty(unixTS))
                    {
                        return new ScalarModel
                        {
                            out_value_bool = false,
                            out_value_str = "not_deleted",
                            out_err = "DeleteUnixTS>>Cycles Error: UnixTS is empty"
                        };
                    }

                    if (string.IsNullOrEmpty(authUsers_UnixTS))
                    {
                        return new ScalarModel
                        {
                            out_value_bool = false,
                            out_value_str = "not_deleted",
                            out_err = "DeleteUnixTS>>Cycles Error: AuthUsers_UnixTS is empty"
                        };
                    }

                    // ====================================================================
                    // PHASE 1: FIND AND REMOVE THE SPECIFIC CYCLE
                    // ====================================================================
                    if (!localStorage.RamCache.TryGetValue("Cycles", out var cyclesCache) || cyclesCache == null)
                    {
                        return new ScalarModel
                        {
                            out_value_bool = false,
                            out_err = "DeleteUnixTS>>Cycles Error: Cycles cache not available",
                            out_value_str = "not_deleted"
                        };
                    }

                    var currentCycles = cyclesCache.Cast<CyclesModel>().ToList();
                    var cycleToDelete = currentCycles.FirstOrDefault(x => x.UnixTS == unixTS && x.AuthUsers_UnixTS == authUsers_UnixTS);

                    if (cycleToDelete == null)
                    {
                        return new ScalarModel
                        {
                            out_value_bool = true,
                            out_value_str = "deleted:0:" + authUsers_UnixTS,
                            out_err = ""
                        };
                    }

                    var cyclesToKeep = currentCycles.Where(x => x.UnixTS != unixTS || x.AuthUsers_UnixTS != authUsers_UnixTS).ToList();

                    // ====================================================================
                    // PHASE 2: DELETE PHYSICAL FILE (if JSON_HYBRID mode)
                    // ====================================================================
                    if (storageType == LOCAL_STORAGE_TYPE.JSON_HYBRID)
                    {
                        var fileName = $"Cycles/{unixTS}.json";
                        bool deleteSuccess = await localJsonFile.DeletePhysicalFileAsync(localStorage.UserAccountHashed, fileName);

                        if (!deleteSuccess)
                        {
                            return new ScalarModel
                            {
                                out_value_bool = false,
                                out_err = $"DeleteUnixTS>>Cycles Error: Failed to delete physical file for {fileName}",
                                out_value_str = "not_deleted"
                            };
                        }
                    }

                    // ====================================================================
                    // PHASE 3: UPDATE RAM CACHE
                    // ====================================================================
                    localStorage.RamCache["Cycles"] = cyclesToKeep.Cast<object>().ToList();

                    return new ScalarModel
                    {
                        out_value_bool = true,
                        out_value_str = $"deleted:0:{authUsers_UnixTS}"
                    };
                }
                catch (Exception ex)
                {
                    return new ScalarModel
                    {
                        out_value_bool = false,
                        out_err = $"DeleteUnixTS>>Cycles Error: {ex.Message}",
                        out_value_str = "not_deleted"
                    };
                }
            });

        }



        // =========================================================
        // AUTH USERS
        // =========================================================
        private static void RegisterAuthUsers(
            ILocalQueryExecutor executor, 
            ILocalStorage localStorage,
            ILocalJsonFile localJsonFile)
        {
            // =========================================================
            // AUTH
            // =========================================================
            executor.RegisterAuth("Register>>AuthUsers", async ctx =>
            {
                var res = new ClientStorageModel();
                // Fallback: Falls der Context kein Storage enthält, nutzen wir das Service-Objekt aus der Methode
                var currentStorage = ctx.Storage ?? localStorage;

                if (currentStorage == null)
                {
                    res.out_err = "Storage provider not available";
                    return res;
                }

                try
                {
                    // 1. Parameter extrahieren
                    var emailHash = ctx.Parameters.GetValueOrDefault("@EmailHash", "");
                    var passwordHash = ctx.Parameters.GetValueOrDefault("@PasswordHash", "");
                    var isRegistration = ctx.Parameters.GetValueOrDefault("@Int__Registration", "0") == "1";
                    var unixTS = ctx.Parameters.GetValueOrDefault("@UnixTS", "");
                    var forceUpdate = ctx.Parameters.ContainsKey("cmd_force_update_local_credentials");

                    var storageType = pFemmeExample.Shared.Global.Configuration.ConfigGeneral.LocalStorageType;

                    // 2. Cache Validierung
                    if (!currentStorage.RamCache.TryGetValue("AuthUsers", out var usersCache) || usersCache == null)
                    {
                        res.out_err = "AuthUsers cache not available";
                        return res;
                    }

                    var usersList = usersCache.Cast<AuthUsersModel>().ToList();
                    var user = usersList.FirstOrDefault(x => x.EmailHash == emailHash);

                    // 3. Logik: Update (Passwort-Sync) oder Register (Neuanlage)
                    AuthUsersModel dataToSave;

                    if (user != null)
                    {
                        bool passwordMatches = user.PasswordHash == passwordHash;

                        if (passwordMatches || forceUpdate)
                        {
                            user.PasswordHash = passwordHash;
                            user.LastUpdateUnixTS = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                            res.UnixTS = user.UnixTS ?? "";
                            res.WebApiToken = API_CONST.TOKEN_LOCAL_ONLY;
                            dataToSave = user;
                        }
                        else
                        {
                            res.out_err = "no_user";
                            return res;
                        }
                    }
                    else if (isRegistration)
                    {
                        dataToSave = new AuthUsersModel
                        {
                            UnixTS = unixTS,
                            EmailHash = emailHash,
                            PasswordHash = passwordHash,
                            active = true
                        };
                        res.UnixTS = dataToSave.UnixTS ?? "";
                        res.WebApiToken = API_CONST.TOKEN_LOCAL_ONLY;
                    }
                    else
                    {
                        res.out_err = "no_user";
                        return res;
                    }

                    // 4. JSON-PERSISTENZ (JSON-FIRST)
                    if (storageType == LOCAL_STORAGE_TYPE.JSON_HYBRID)
                    {
                        var fileName = $"AuthUsers/{dataToSave.EmailHash}.json";

                        // AOT-safe serialization using JsonContext.Default
                        var json = System.Text.Json.JsonSerializer.Serialize(dataToSave, JsonContext.Default.AuthUsersModel);

                        var writeResult = await localJsonFile.WritePhysicalFileAsync(localStorage.UserAccountHashed, fileName, json);
                        if (!writeResult.out_value_bool)
                        {
                            res.out_err = $"JSON save failed: {writeResult.out_err}";
                            return res;
                        }

                        // Reload from JSON to ensure consistency
                        var reloadedJson = await localJsonFile.ReadFileAsync(localStorage.UserAccountHashed, fileName);
                        if (reloadedJson != null)
                        {
                            var reloadedUser = System.Text.Json.JsonSerializer.Deserialize<AuthUsersModel>(
                                reloadedJson,
                                JsonContext.Default.AuthUsersModel);

                            if (reloadedUser != null)
                            {
                                dataToSave = reloadedUser; // Use reloaded data for RAM
                            }
                        }
                    }

                    // 5. RAM UPDATE
                    if (isRegistration)
                    {
                        usersList.Add(dataToSave);
                    }
                    else
                    {
                        var index = usersList.FindIndex(x => x.EmailHash == emailHash);
                        if (index != -1) usersList[index] = dataToSave;
                    }

                    currentStorage.RamCache["AuthUsers"] = usersList.Cast<object>().ToList();
                }
                catch (Exception ex)
                {
                    res.out_err = $"AuthUsers Handler Error: {ex.Message}";
                }

                return res;
            });


            // =========================
            // SCALAR
            // =========================
            executor.RegisterScalar("SelectAuthUsersEmail", ctx =>
            {
                var emailHash = ctx.Parameters.GetValueOrDefault("@EmailHash", "");
                var passwordHash = ctx.Parameters.GetValueOrDefault("@PasswordHash", "");

                // Zugriff direkt auf localStorage (statt ctx.Storage)
                if (!localStorage.RamCache.TryGetValue("AuthUsers", out var usersCache))
                {
                    return new ScalarModel
                    {
                        out_value_str = string.Empty,
                        out_err = "AuthUsers cache not available"
                    };
                }

                //var users = usersCache.Cast<AuthUsersModel>();
                var users = usersCache.Cast<AuthUsersModel>().ToList();

                var unixTS = users
                    .FirstOrDefault(x =>
                        x.EmailHash == emailHash &&
                        x.PasswordHash == passwordHash &&
                        x.active == true)
                    ?.UnixTS ?? string.Empty;

                return new ScalarModel
                {
                    out_value_str = unixTS
                };
            });

            executor.RegisterScalar("SelectTermsAccepted>>AuthUsers", ctx =>
            {
                var unixTS = ctx.Parameters.GetValueOrDefault("@AuthUsers_UnixTS", "");

                if (!localStorage.RamCache.TryGetValue("AuthUsers", out var usersCache))
                {
                    return new ScalarModel
                    {
                        out_value_bool = false,
                        out_value_str = "0"
                    };
                }

                //var users = usersCache.Cast<AuthUsersModel>();
                var users = usersCache.Cast<AuthUsersModel>().ToList();

                bool value = users.FirstOrDefault(x => x.UnixTS == unixTS && x.active == true)
                    ?.TermsAccepted ?? false;

                return new ScalarModel
                {
                    out_value_bool = value,
                    out_value_str = value ? "1" : "0"
                };
            });

            executor.RegisterScalar("ExistsUnixTS>>AuthUsers", ctx =>
            {
                var unixTS = ctx.Parameters.GetValueOrDefault("@UnixTS", "");

                if (!localStorage.RamCache.TryGetValue("AuthUsers", out var usersCache))
                {
                    return new ScalarModel
                    {
                        out_value_bool = false,
                        out_value_str = "0"
                    };
                }

                //var users = usersCache.Cast<AuthUsersModel>();
                var users = usersCache.Cast<AuthUsersModel>().ToList();

                bool exists = users.Any(x => x.UnixTS == unixTS && x.active == true);

                return new ScalarModel
                {
                    out_value_bool = exists,
                    out_value_str = exists ? "1" : "0"
                };
            });

            executor.RegisterScalar("CheckPassword>>AuthUsers", ctx =>
            {
                var unixTS = ctx.Parameters.GetValueOrDefault("@UnixTS", "");
                var passwordHash = ctx.Parameters.GetValueOrDefault("@PasswordHash", "");

                if (!localStorage.RamCache.TryGetValue("AuthUsers", out var usersCache))
                {
                    return new ScalarModel
                    {
                        out_value_bool = false,
                        out_value_str = "0"
                    };
                }

                //var users = usersCache.Cast<AuthUsersModel>();
                var users = usersCache.Cast<AuthUsersModel>().ToList();

                bool exists = users.Any(x =>
                    x.UnixTS == unixTS &&
                    x.PasswordHash == passwordHash &&
                    x.active == true
                );

                return new ScalarModel
                {
                    out_value_bool = exists,
                    out_value_str = exists ? "1" : "0"
                };
            });

            executor.RegisterScalar("CheckEmail>>AuthUsers", ctx =>
            {
                var unixTS = ctx.Parameters.GetValueOrDefault("@UnixTS", "");
                var emailHash = ctx.Parameters.GetValueOrDefault("@EmailHash", "");

                if (!localStorage.RamCache.TryGetValue("AuthUsers", out var usersCache))
                {
                    return new ScalarModel
                    {
                        out_value_bool = false,
                        out_value_str = "0"
                    };
                }

                //var users = usersCache.Cast<AuthUsersModel>();
                var users = usersCache.Cast<AuthUsersModel>().ToList();

                bool exists = users.Any(x =>
                    x.UnixTS == unixTS &&
                    x.EmailHash == emailHash &&
                    x.active == true
                );

                return new ScalarModel
                {
                    out_value_bool = exists,
                    out_value_str = exists ? "1" : "0"
                };
            });

            executor.RegisterScalar("ExistsEmailHashPasswordHash>>AuthUsers", ctx =>
            {
                var emailHash = ctx.Parameters.GetValueOrDefault("@EmailHash", "");
                var passwordHash = ctx.Parameters.GetValueOrDefault("@PasswordHash", "");

                if (!localStorage.RamCache.TryGetValue("AuthUsers", out var usersCache))
                {
                    return new ScalarModel
                    {
                        out_value_bool = false,
                        out_value_str = "0"
                    };
                }

                //var users = usersCache.Cast<AuthUsersModel>();
                var users = usersCache.Cast<AuthUsersModel>().ToList(); 

                bool exists = users.Any(x =>
                    x.EmailHash == emailHash &&
                    x.PasswordHash == passwordHash &&
                    x.active == true
                );

                return new ScalarModel
                {
                    out_value_bool = exists,
                    out_value_str = exists ? "1" : "0"
                };
            });

            executor.RegisterScalar("SelectIdP>>AuthUsers", ctx =>
            {
                var unixTS = ctx.Parameters.GetValueOrDefault("@AuthUsers_UnixTS", "");

                if (!localStorage.RamCache.TryGetValue("AuthUsers", out var usersCache))
                {
                    return new ScalarModel
                    {
                        out_value_str = string.Empty
                    };
                }

                //var users = usersCache.Cast<AuthUsersModel>();
                var users = usersCache.Cast<AuthUsersModel>().ToList();

                var value = users.FirstOrDefault(x => x.UnixTS == unixTS && x.active == true)
                    ?.IdP ?? string.Empty;

                return new ScalarModel
                {
                    out_value_str = value
                };
            });


            // =========================
            // SAVE
            // =========================
            executor.RegisterSave("UpdateTermsAccepted>>AuthUsers", async ctx =>
            {
                var unixTS = ctx.Parameters.GetValueOrDefault("@UnixTS", "");
                var storageType = pFemmeExample.Shared.Global.Configuration.ConfigGeneral.LocalStorageType;

                // Check RAM cache
                if (!localStorage.RamCache.TryGetValue("AuthUsers", out var usersCache))
                {
                    return new ScalarModel { out_err = "AuthUsers cache not available" };
                }

                //var users = usersCache.Cast<AuthUsersModel>();
                var users = usersCache.Cast<AuthUsersModel>().ToList();
                var user = users.FirstOrDefault(x => x.UnixTS == unixTS && x.active == true);

                if (user == null)
                {
                    return new ScalarModel { out_value_str = "not_updated" };
                }

                // ============================================================
                // 1. PREPARE DATA FOR SAVING (do NOT modify original RAM yet!)
                // ============================================================
                var updatedUser = new AuthUsersModel
                {
                    UnixTS = user.UnixTS,
                    EmailHash = user.EmailHash,
                    PasswordHash = user.PasswordHash,
                    TermsAccepted = true,  // changed
                    active = user.active,
                    IdP = user.IdP,
                    IdPClientIdent = user.IdPClientIdent,
                    IdPToken = user.IdPToken,
                    LastUpdateUnixTS = DateTimeOffset.UtcNow.ToUnixTimeSeconds()  // changed
                };

                // ============================================================
                // 2. DETERMINE SOURCE FOR RAM (JSON or MEMORY)
                // ============================================================
                AuthUsersModel dataToSave = updatedUser;  // Default: use updatedUser for MEMORY

                if (storageType == LOCAL_STORAGE_TYPE.JSON_HYBRID)
                {
                    // AOT-safe serialization
                    var json = System.Text.Json.JsonSerializer.Serialize(updatedUser, JsonContext.Default.AuthUsersModel);
                    var fileName = $"AuthUsers/{unixTS}.json";

                    // Save to JSON file
                    var writeResult = await localJsonFile.WritePhysicalFileAsync(localStorage.UserAccountHashed, fileName, json);
                    if (!writeResult.out_value_bool)
                    {
                        return new ScalarModel { out_err = $"JSON save failed: {writeResult.out_err}" };
                    }

                    // Reload from JSON to ensure consistency
                    var reloadedJson = await localJsonFile.ReadFileAsync(localStorage.UserAccountHashed, fileName);
                    if (reloadedJson != null)
                    {
                        var reloadedUser = System.Text.Json.JsonSerializer.Deserialize<AuthUsersModel>(
                            reloadedJson,
                            JsonContext.Default.AuthUsersModel);

                        if (reloadedUser != null)
                        {
                            dataToSave = reloadedUser;  // Use reloaded data for RAM
                        }
                    }
                }

                // ============================================================
                // 3. UPDATE RAM (always - but AFTER successful JSON operation)
                // ============================================================
                var ramList = usersCache.Cast<AuthUsersModel>().ToList();
                var index = ramList.FindIndex(x => x.UnixTS == unixTS);
                if (index >= 0)
                {
                    ramList[index] = dataToSave;
                    localStorage.RamCache["AuthUsers"] = ramList.Cast<object>().ToList();
                }

                return new ScalarModel { out_value_str = $"updated:{unixTS}:{unixTS}" };
            });

            executor.RegisterSave("ChangePassword>>AuthUsers", async ctx =>
            {
                try
                {
                    var unixTS = ctx.Parameters.GetValueOrDefault("@UnixTS", "");
                    var oldPasswordHash = ctx.Parameters.GetValueOrDefault("@PasswordHash", "");
                    var newPasswordHash = ctx.Parameters.GetValueOrDefault("@PasswordHashNew", "");
                    var lastUpdateUnixTS = ctx.Parameters.GetValueOrDefault("@LastUpdateUnixTS", "");
                    var storageType = pFemmeExample.Shared.Global.Configuration.ConfigGeneral.LocalStorageType;

                    // Check RAM cache
                    if (!localStorage.RamCache.TryGetValue("AuthUsers", out var usersCache))
                    {
                        return new ScalarModel
                        {
                            out_err = "AuthUsers cache not available",
                            out_value_str = "not_updated:0:0"
                        };
                    }

                    //var users = usersCache.Cast<AuthUsersModel>();
                    var users = usersCache.Cast<AuthUsersModel>().ToList();

                    // 1. Find user with UnixTS + old password + active = true
                    var user = users.FirstOrDefault(x =>
                        x.UnixTS == unixTS &&
                        x.PasswordHash == oldPasswordHash &&
                        x.active == true);

                    if (user == null)
                    {
                        // Check if external IdP user (cannot change password)
                        var isExternalUser = users.Any(x =>
                            x.UnixTS == unixTS &&
                            !string.IsNullOrEmpty(x.IdP) &&
                            x.active == true);

                        return new ScalarModel
                        {
                            out_value_str = isExternalUser ? "not_updated:-1:-1" : "not_updated:0:0"
                        };
                    }

                    // ============================================================
                    // 1. PREPARE DATA FOR SAVING (do NOT modify original RAM yet!)
                    // ============================================================
                    var lastUpdate = long.TryParse(lastUpdateUnixTS, out var parsed)
                        ? parsed
                        : DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                    var updatedUser = new AuthUsersModel
                    {
                        UnixTS = user.UnixTS,
                        EmailHash = user.EmailHash,
                        PasswordHash = newPasswordHash,  // changed
                        TermsAccepted = user.TermsAccepted,
                        active = user.active,
                        IdP = user.IdP,
                        IdPClientIdent = user.IdPClientIdent,
                        IdPToken = user.IdPToken,
                        LastUpdateUnixTS = lastUpdate  // changed
                    };

                    // ============================================================
                    // 2. DETERMINE SOURCE FOR RAM (JSON or MEMORY)
                    // ============================================================
                    AuthUsersModel dataToSave = updatedUser;  // Default for MEMORY

                    if (storageType == LOCAL_STORAGE_TYPE.JSON_HYBRID)
                    {
                        // AOT-safe serialization
                        var json = System.Text.Json.JsonSerializer.Serialize(updatedUser, JsonContext.Default.AuthUsersModel);
                        var fileName = $"AuthUsers/{unixTS}.json";

                        // Save to JSON file
                        var writeResult = await localJsonFile.WritePhysicalFileAsync(localStorage.UserAccountHashed, fileName, json);
                        if (!writeResult.out_value_bool)
                        {
                            return new ScalarModel
                            {
                                out_err = $"JSON save failed: {writeResult.out_err}",
                                out_value_str = "not_updated:0:0"
                            };
                        }

                        // Reload from JSON to ensure consistency
                        var reloadedJson = await localJsonFile.ReadFileAsync(localStorage.UserAccountHashed, fileName);
                        if (reloadedJson != null)
                        {
                            var reloadedUser = System.Text.Json.JsonSerializer.Deserialize<AuthUsersModel>(
                                reloadedJson,
                                JsonContext.Default.AuthUsersModel);

                            if (reloadedUser != null)
                            {
                                dataToSave = reloadedUser;
                            }
                        }
                    }

                    // ============================================================
                    // 3. UPDATE RAM (always - but AFTER successful JSON operation)
                    // ============================================================
                    var ramList = usersCache.Cast<AuthUsersModel>().ToList();
                    var index = ramList.FindIndex(x => x.UnixTS == unixTS);
                    if (index >= 0)
                    {
                        ramList[index] = dataToSave;
                        localStorage.RamCache["AuthUsers"] = ramList.Cast<object>().ToList();
                    }

                    // 4. Verify: Was the new password set correctly?
                    var updatedUserList = localStorage.RamCache["AuthUsers"].Cast<AuthUsersModel>().ToList();
                    var verifiedUser = updatedUserList.FirstOrDefault(x =>
                        x.UnixTS == unixTS &&
                        x.PasswordHash == newPasswordHash &&
                        x.active == true);

                    if (verifiedUser != null)
                    {
                        return new ScalarModel { out_value_str = $"updated:{unixTS}:{unixTS}" };
                    }
                    else
                    {
                        return new ScalarModel { out_value_str = "not_updated:0:0" };
                    }
                }
                catch (Exception ex)
                {
                    return new ScalarModel
                    {
                        out_value_bool = false,
                        out_err = $"ChangePassword Error: {ex.Message}",
                        out_value_str = "not_updated:0:0"
                    };
                }
            });


            // =========================================================
            // DELETE
            // =========================================================
            executor.RegisterSave("DeleteLocalData", async ctx =>
            {
                try
                {
                    if (pFemmeExample.Shared.Global.Catalog.Sections.TablesMSSQL == null)
                    {
                        return new ScalarModel { out_value_bool = false, out_err = "DeleteLocalData Error: TablesMSSQL is null", out_value_str = "not_deleted" };
                    }

                    var storageType = pFemmeExample.Shared.Global.Configuration.ConfigGeneral.LocalStorageType;

                    if (string.IsNullOrEmpty(localStorage.UserAccountHashed))
                    {
                        return new ScalarModel { out_value_bool = false, out_err = "DeleteLocalData Error: UserAccountHashed parameter is missing", out_value_str = "not_deleted" };
                    }

                    foreach (var tableName in pFemmeExample.Shared.Global.Catalog.Sections.TablesMSSQL)
                    {
                        // CRITICAL: Protect the user account credentials under all circumstances!
                        if (tableName == "AuthUsers") continue;

                        // 1. Physische Daten löschen (Disk First)
                        if (storageType == LOCAL_STORAGE_TYPE.JSON_HYBRID)
                        {
                            bool deleteSuccess = await localJsonFile.DeleteTableFilesAsync(localStorage.UserAccountHashed, tableName);

                            if (!deleteSuccess)
                            {
                                return new ScalarModel
                                {
                                    out_value_bool = false,
                                    out_err = $"DeleteLocalData Error: Failed to physically clear table {tableName}.",
                                    out_value_str = "not_deleted"
                                };
                            }
                        }

                        // 2. RAM nur löschen, wenn Disk erfolgreich war
                        localStorage.RamCache[tableName] = new List<object>();
                    }

                    return new ScalarModel
                    {
                        out_value_bool = true,
                        out_value_str = "deleted:0:0"
                    };
                }
                catch (Exception ex)
                {
                    return new ScalarModel
                    {
                        out_value_bool = false,
                        out_err = $"DeleteLocalData Error: {ex.Message}",
                        out_value_str = "not_deleted"
                    };
                }
            });

            executor.RegisterSave("DeleteLocalAccount", async ctx =>
            {
                try
                {
                    if (pFemmeExample.Shared.Global.Catalog.Sections.TablesMSSQL == null)
                    {
                        return new ScalarModel
                        {
                            out_value_bool = false,
                            out_err = "DeleteLocalAccount Error: TablesMSSQL is null",
                            out_value_str = "not_deleted"
                        };
                    }

                    var storageType = pFemmeExample.Shared.Global.Configuration.ConfigGeneral.LocalStorageType;

                    // 1. Physische Daten löschen (Disk First)
                    bool diskPurgeSuccess = true;
                    if (storageType == LOCAL_STORAGE_TYPE.JSON_HYBRID)
                    {
                        diskPurgeSuccess = await localJsonFile.DeleteAllFilesAsync(localStorage.UserAccountHashed);
                    }

                    // 2. RAM nur löschen, wenn Disk erfolgreich war
                    if (diskPurgeSuccess)
                    {
                        foreach (var tableName in pFemmeExample.Shared.Global.Catalog.Sections.TablesMSSQL)
                        {
                            localStorage.RamCache[tableName] = new List<object>();
                        }

                        return new ScalarModel
                        {
                            out_value_bool = true,
                            out_value_str = "deleted:0:0"
                        };
                    }
                    else
                    {
                        return new ScalarModel
                        {
                            out_value_bool = false,
                            out_err = "DeleteLocalAccount Error: Failed to physically remove user storage.",
                            out_value_str = "not_deleted"
                        };
                    }
                }
                catch (Exception ex)
                {
                    return new ScalarModel
                    {
                        out_value_bool = false,
                        out_err = $"DeleteLocalAccount Error: {ex.Message}",
                        out_value_str = "not_deleted"
                    };
                }
            });


            // =========================
            // SERVER ONLY
            // =========================
            executor.RegisterScalar("SelectByIdPClientIdent>>AuthUsers", ctx => new ScalarModel { out_value_str = "-1" });
            executor.RegisterScalar("DeleteOtpByAuthUsers_UnixTS>>AuthUsers", ctx => new ScalarModel { out_value_str = "-1" });
            executor.RegisterScalar("UpdateIdPToken>>AuthUsers", ctx => new ScalarModel { out_value_str = "-1" });
            executor.RegisterScalar("ExistsOtp>>AuthUsers", ctx => new ScalarModel { out_value_str = "-1" });
            executor.RegisterScalar("ResetLoginAttempts>>AuthUsers", ctx => new ScalarModel { out_value_str = "-1" });
            executor.RegisterScalar("DeleteOtp>>AuthUsers", ctx => new ScalarModel { out_value_str = "-1" });
            executor.RegisterScalar("CheckAccount>>AuthUsers", ctx => new ScalarModel { out_value_str = "-1" });
            executor.RegisterScalar("SaveOtp>>AuthUsers", ctx => new ScalarModel { out_value_str = "-1" });
            executor.RegisterScalar("ResetFailedAttempts>>AuthUsers", ctx => new ScalarModel { out_value_str = "-1" });
            executor.RegisterScalar("SelectOtp>>AuthUsers", ctx => new ScalarModel { out_value_str = "-1" });

        }



        // =========================================================
        // AUTH USERS EXTEND
        // =========================================================
        private static void RegisterAuthUsersExtend(
            ILocalQueryExecutor executor,
            ILocalStorage localStorage,
            ILocalJsonFile localJsonFile)
        {
            // =========================================================
            // SCALAR
            // =========================================================
            executor.RegisterScalar("SelectAlias>>AuthUsersExtend", ctx =>
            {
                var unixTS = ctx.Parameters.GetValueOrDefault("@AuthUsers_UnixTS", "");

                if (!localStorage.RamCache.TryGetValue("AuthUsersExtend", out var cache) || cache == null)
                {
                    return new ScalarModel { out_value_str = string.Empty };
                }

                //var list = cache.Cast<AuthUsersExtendModel>();
                var list = cache.Cast<AuthUsersExtendModel>().ToList();
                var value = list.FirstOrDefault(x => x.AuthUsers_UnixTS == unixTS)?.DisplayName ?? string.Empty;

                return new ScalarModel
                {
                    out_value_str = value
                };
            });

            executor.RegisterScalar("ExistsDisplayName>>AuthUsersExtend", ctx =>
            {
                var unixTS = ctx.Parameters.GetValueOrDefault("@AuthUsers_UnixTS", "");
                var displayName = ctx.Parameters.GetValueOrDefault("@DisplayName", "");

                if (!localStorage.RamCache.TryGetValue("AuthUsersExtend", out var cache) || cache == null)
                {
                    return new ScalarModel { out_value_bool = false, out_value_str = "0" };
                }

                var list = cache.Cast<AuthUsersExtendModel>().ToList();
                bool exists = list.Any(x => x.AuthUsers_UnixTS == unixTS && x.DisplayName == displayName);

                return new ScalarModel
                {
                    out_value_bool = exists,
                    out_value_str = exists ? "1" : "0"
                };
            });

            executor.RegisterScalar("ExistsByAuthUsers_UnixTS>>AuthUsersExtend", ctx =>
            {
                var unixTS = ctx.Parameters.GetValueOrDefault("@AuthUsers_UnixTS", "");

                if (!localStorage.RamCache.TryGetValue("AuthUsersExtend", out var cache) || cache == null)
                {
                    return new ScalarModel { out_value_bool = false, out_value_str = "0" };
                }

                var list = cache.Cast<AuthUsersExtendModel>().ToList();
                bool exists = list.Any(x => x.AuthUsers_UnixTS == unixTS);

                return new ScalarModel
                {
                    out_value_bool = exists,
                    out_value_str = exists ? "1" : "0"
                };
            });


            // =========================
            // READ
            // =========================
            executor.RegisterRead("Select>>AuthUsersExtend", ctx =>
            {
                var unixTS = ctx.Parameters.GetValueOrDefault("@AuthUsers_UnixTS", "");

                var data = ctx.Storage?.RamCache["AuthUsersExtend"]
                    .Cast<AuthUsersExtendModel>()
                    .Where(x => x.AuthUsers_UnixTS == unixTS)
                    .Take(1)
                    .Select(x => new AuthUsersExtendModel
                    {
                        ID = x.ID,
                        UnixTS = x.UnixTS,
                        AuthUsers_UnixTS = x.AuthUsers_UnixTS,
                        DisplayName = x.DisplayName,
                        imgJpegThumbnail = x.imgJpegThumbnail ?? string.Empty,
                        LastUpdateUnixTS = x.LastUpdateUnixTS
                    })
                    .Cast<object>()
                    .ToList() ?? new List<object>();

                return new LocalQueryResult
                {
                    success = true,
                    out_list = data.Cast<object?>().ToList()
                };
            });

            executor.RegisterRead("SelectAuthUsersData>>AuthUsersExtend", ctx =>
            {
                try
                {
                    var authUsers_UnixTS = ctx.Parameters.GetValueOrDefault("@AuthUsers_UnixTS", "");

                    if (string.IsNullOrEmpty(authUsers_UnixTS))
                    {
                        return new LocalQueryResult
                        {
                            success = false,
                            out_err = "AuthUsers_UnixTS is empty",
                            out_list = new List<object?>()
                        };
                    }

                    // Fetch data from AuthUsers RAM cache
                    if (!localStorage.RamCache.TryGetValue("AuthUsers", out var authCache) || authCache == null)
                    {
                        return new LocalQueryResult
                        {
                            success = false,
                            out_err = "AuthUsers cache not available",
                            out_list = new List<object?>()
                        };
                    }

                    var user = authCache.Cast<AuthUsersModel>()
                        .FirstOrDefault(x => x.UnixTS == authUsers_UnixTS && x.active == true);

                    // Fetch data from AuthUsersExtend RAM cache
                    if (!localStorage.RamCache.TryGetValue("AuthUsersExtend", out var extendCache) || extendCache == null)
                    {
                        return new LocalQueryResult
                        {
                            success = false,
                            out_err = "AuthUsersExtend cache not available",
                            out_list = new List<object?>()
                        };
                    }

                    var extend = extendCache.Cast<AuthUsersExtendModel>()
                        .FirstOrDefault(x => x.AuthUsers_UnixTS == authUsers_UnixTS);

                    // Combine results into an anonymous object
                    var resultData = new
                    {
                        EmailHash = "empty for security reasons",
                        PasswordHash = "empty for security reasons",
                        active = user?.active ?? false,
                        TermsAccepted = user?.TermsAccepted ?? false,
                        IdP = user?.IdP ?? string.Empty,
                        DisplayName = extend?.DisplayName ?? string.Empty,
                        imgJpegThumbnail = extend?.imgJpegThumbnail ?? string.Empty
                    };

                    return new LocalQueryResult
                    {
                        success = true,
                        out_list = new List<object?> { resultData },
                        out_data = resultData
                    };
                }
                catch (Exception ex)
                {
                    return new LocalQueryResult
                    {
                        success = false,
                        out_err = $"SelectAuthUsersData>>AuthUsersExtend Error: {ex.Message}",
                        out_list = new List<object?>()
                    };
                }
            });

            executor.RegisterRead("SelectByDisplayName>>AuthUsersExtend", ctx =>
            {
                try
                {
                    var displayName = ctx.Parameters.GetValueOrDefault("@DisplayName", "");

                    if (string.IsNullOrEmpty(displayName))
                    {
                        return new LocalQueryResult
                        {
                            success = false,
                            out_err = "DisplayName is empty",
                            out_list = new List<object?>()
                        };
                    }

                    // Fetch data from AuthUsersExtend RAM cache
                    if (!localStorage.RamCache.TryGetValue("AuthUsersExtend", out var extendCache) || extendCache == null)
                    {
                        return new LocalQueryResult
                        {
                            success = false,
                            out_err = "AuthUsersExtend cache not available",
                            out_list = new List<object?>()
                        };
                    }

                    var extendList = extendCache.Cast<AuthUsersExtendModel>().ToList();
                    var extend = extendList.FirstOrDefault(x => x.DisplayName == displayName);

                    if (extend == null)
                    {
                        // No record found -> return empty list (matching SQL behavior)
                        return new LocalQueryResult
                        {
                            success = true,
                            out_list = new List<object?>(),
                            out_data = null
                        };
                    }

                    // Fetch matching AuthUsers record (simulating a LEFT JOIN)
                    if (!localStorage.RamCache.TryGetValue("AuthUsers", out var authCache) || authCache == null)
                    {
                        return new LocalQueryResult
                        {
                            success = false,
                            out_err = "AuthUsers cache not available",
                            out_list = new List<object?>()
                        };
                    }

                    var users = authCache.Cast<AuthUsersModel>().ToList();
                    var user = users.FirstOrDefault(x => x.UnixTS == extend.AuthUsers_UnixTS);

                    // Combine results into an anonymous object
                    var resultData = new
                    {
                        extend.ID,
                        extend.UnixTS,
                        extend.AuthUsers_UnixTS,
                        extend.DisplayName,
                        imgJpegThumbnail = extend.imgJpegThumbnail ?? string.Empty,
                        extend.LastUpdateUnixTS,
                        Int__AuthUsers_UnixTS = user?.UnixTS ?? string.Empty
                    };

                    return new LocalQueryResult
                    {
                        success = true,
                        out_list = new List<object?> { resultData },
                        out_data = resultData
                    };
                }
                catch (Exception ex)
                {
                    return new LocalQueryResult
                    {
                        success = false,
                        out_err = $"SelectByDisplayName>>AuthUsersExtend Error: {ex.Message}",
                        out_list = new List<object?>()
                    };
                }
            });


            // =========================================================
            // SAVE
            // =========================================================
            executor.RegisterSave("Save>>AuthUsersExtend", async ctx =>
            {
                try
                {
                    var unixTS = ctx.Parameters.GetValueOrDefault("@UnixTS", "");
                    var authUsers_UnixTS = ctx.Parameters.GetValueOrDefault("@AuthUsers_UnixTS", "");
                    var displayName = ctx.Parameters.GetValueOrDefault("@DisplayName", "");
                    var imgJpegThumbnail = ctx.Parameters.GetValueOrDefault("@imgJpegThumbnail", "");
                    var lastUpdateUnixTS = ctx.Parameters.GetValueOrDefault("@LastUpdateUnixTS", "");

                    var storageType = pFemmeExample.Shared.Global.Configuration.ConfigGeneral.LocalStorageType;

                    // Verify RAM cache accessibility
                    if (!localStorage.RamCache.TryGetValue("AuthUsersExtend", out var extendCache) || extendCache == null)
                    {
                        return new ScalarModel
                        {
                            out_err = "AuthUsersExtend cache not available",
                            out_value_str = "not_saved"
                        };
                    }

                    var ramList = extendCache.Cast<AuthUsersExtendModel>().ToList();

                    // 1. Validation: Does the DisplayName already exist for ANOTHER user?
                    var existingWithSameDisplayName = ramList.FirstOrDefault(x =>
                        x.AuthUsers_UnixTS != authUsers_UnixTS &&
                        x.DisplayName == displayName);

                    if (existingWithSameDisplayName != null)
                    {
                        return new ScalarModel
                        {
                            out_value_str = $"record_exists_no_adding:{unixTS}:{authUsers_UnixTS}"
                        };
                    }

                    // 2. Check if a record already exists for this specific user
                    var existingEntry = ramList.FirstOrDefault(x => x.AuthUsers_UnixTS == authUsers_UnixTS);
                    AuthUsersExtendModel updatedEntry;
                    bool isInsert = existingEntry == null;

                    var calculatedLastUpdate = long.TryParse(lastUpdateUnixTS, out var lastUpdate)
                        ? lastUpdate
                        : DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                    if (isInsert)
                    {
                        // INSERT: Generate unique incremental ID within RAM scope
                        int newId = ramList.Any() ? ramList.Max(x => x.ID) + 1 : 1;

                        updatedEntry = new AuthUsersExtendModel
                        {
                            ID = newId,
                            UnixTS = unixTS,
                            AuthUsers_UnixTS = authUsers_UnixTS,
                            DisplayName = displayName,
                            imgJpegThumbnail = string.IsNullOrEmpty(imgJpegThumbnail) ? null : imgJpegThumbnail,
                            LastUpdateUnixTS = calculatedLastUpdate
                        };
                    }
                    else
                    {
                        // UPDATE: Prepare modified object preserving original structural keys
                        updatedEntry = new AuthUsersExtendModel
                        {
                            ID = existingEntry!.ID,
                            UnixTS = existingEntry.UnixTS,
                            AuthUsers_UnixTS = existingEntry.AuthUsers_UnixTS,
                            DisplayName = displayName,
                            imgJpegThumbnail = string.IsNullOrEmpty(imgJpegThumbnail) ? null : imgJpegThumbnail,
                            LastUpdateUnixTS = calculatedLastUpdate
                        };
                    }

                    // ============================================================
                    // 3. DETERMINE SOURCE FOR RAM (JSON or MEMORY)
                    // ============================================================
                    AuthUsersExtendModel dataToSave = updatedEntry; // Default: use prepared local object for MEMORY mode

                    if (storageType == LOCAL_STORAGE_TYPE.JSON_HYBRID)
                    {
                        // AOT-safe serialization using preconfigured JsonContext
                        var json = System.Text.Json.JsonSerializer.Serialize(updatedEntry, JsonContext.Default.AuthUsersExtendModel);
                        var fileName = $"AuthUsersExtend/{authUsers_UnixTS}.json";

                        // Save to JSON file
                        var writeResult = await localJsonFile.WritePhysicalFileAsync(localStorage.UserAccountHashed, fileName, json);
                        if (!writeResult.out_value_bool)
                        {
                            return new ScalarModel
                            {
                                out_err = $"JSON save failed: {writeResult.out_err}",
                                out_value_str = "not_saved"
                            };
                        }

                        // Reload from JSON file to ensure 100% data consistency
                        var reloadedJson = await localJsonFile.ReadFileAsync(localStorage.UserAccountHashed, fileName);
                        if (reloadedJson != null)
                        {
                            var reloadedEntry = System.Text.Json.JsonSerializer.Deserialize<AuthUsersExtendModel>(
                                reloadedJson,
                                JsonContext.Default.AuthUsersExtendModel);

                            if (reloadedEntry != null)
                            {
                                dataToSave = reloadedEntry; // Use verified file data for RAM cache update
                            }
                        }
                    }

                    // ============================================================
                    // 4. UPDATE RAM (Executed only after successful persistence operations)
                    // ============================================================
                    if (isInsert)
                    {
                        ramList.Add(dataToSave);
                    }
                    else
                    {
                        var index = ramList.FindIndex(x => x.AuthUsers_UnixTS == authUsers_UnixTS);
                        if (index >= 0)
                        {
                            ramList[index] = dataToSave;
                        }
                    }

                    localStorage.RamCache["AuthUsersExtend"] = ramList.Cast<object>().ToList();

                    var returnStatus = isInsert ? "saved" : "updated";
                    return new ScalarModel
                    {
                        out_value_str = $"{returnStatus}:{unixTS}:{authUsers_UnixTS}"
                    };
                }
                catch (Exception ex)
                {
                    return new ScalarModel
                    {
                        out_value_bool = false,
                        out_err = $"Save>>AuthUsersExtend Error: {ex.Message}",
                        out_value_str = "not_saved"
                    };
                }
            });


            // ====================================================================
            // DELETE
            // ====================================================================
            executor.RegisterSave("Delete>>AuthUsersExtend", async ctx =>
            {
                try
                {
                    var authUsers_UnixTS = ctx.Parameters.GetValueOrDefault("@AuthUsers_UnixTS", "");
                    var storageType = pFemmeExample.Shared.Global.Configuration.ConfigGeneral.LocalStorageType;

                    if (string.IsNullOrEmpty(authUsers_UnixTS))
                    {
                        return new ScalarModel { out_value_bool = false, out_value_str = "not_deleted", out_err = "AuthUsers_UnixTS empty" };
                    }

                    if (!localStorage.RamCache.TryGetValue("AuthUsersExtend", out var extendCache) || extendCache == null)
                    {
                        return new ScalarModel { out_value_bool = false, out_value_str = "not_deleted", out_err = "Cache not available" };
                    }

                    var ramList = extendCache.Cast<AuthUsersExtendModel>().ToList();
                    var entryToDelete = ramList.FirstOrDefault(x => x.AuthUsers_UnixTS == authUsers_UnixTS);

                    if (entryToDelete == null)
                    {
                        // Nichts zu tun, Zustand ist "erfolgreich bereinigt"
                        return new ScalarModel { out_value_bool = true, out_value_str = $"deleted:0:{authUsers_UnixTS}" };
                    }

                    // 1. Physische Löschung (Disk-First)
                    if (storageType == LOCAL_STORAGE_TYPE.JSON_HYBRID)
                    {
                        var fileName = $"AuthUsersExtend/{entryToDelete.UnixTS}.json";
                        bool deleteSuccess = await localJsonFile.DeletePhysicalFileAsync(localStorage.UserAccountHashed, fileName);

                        if (!deleteSuccess)
                        {
                            // ABBRUCH: Datei konnte nicht gelöscht werden, RAM bleibt unangetastet!
                            return new ScalarModel { out_value_bool = false, out_value_str = "not_deleted", out_err = "Physical deletion failed" };
                        }
                    }

                    // 2. RAM-Synchronisation: ERST JETZT, wenn Disk-Operation erfolgreich war!
                    ramList.Remove(entryToDelete);
                    localStorage.RamCache["AuthUsersExtend"] = ramList.Cast<object>().ToList();

                    return new ScalarModel
                    {
                        out_value_bool = true,
                        out_value_str = $"deleted:0:{authUsers_UnixTS}"
                    };
                }
                catch (Exception ex)
                {
                    return new ScalarModel { out_value_bool = false, out_value_str = "not_deleted", out_err = ex.Message };
                }
            });
        }



        // =========================================================
        // APP PARAMETER (SETTINGS)
        // =========================================================
        private static void RegisterAppParameter(
            ILocalQueryExecutor executor,
            ILocalStorage localStorage,
            ILocalJsonFile localJsonFile,
            IAppStateBase appState)
        {
            // =========================================================
            // SCALAR
            // =========================================================
            executor.RegisterScalar("SelectAppSettings>>AppParameter", ctx =>
            {
                var emailHash = ctx.Parameters.GetValueOrDefault("@EmailHash", "");
                var passwordHash = ctx.Parameters.GetValueOrDefault("@PasswordHash", "");
                var parameterName = ctx.Parameters.GetValueOrDefault("@ParameterName", "");
                var scope = ctx.Parameters.GetValueOrDefault("@Scope", "");

                string unixTS = string.Empty;
                if (localStorage.RamCache.TryGetValue("AuthUsers", out var authCache) && authCache != null)
                {
                    var users = authCache.Cast<AuthUsersModel>().ToList();
                    unixTS = users.FirstOrDefault(x =>
                        x.EmailHash == emailHash &&
                        x.PasswordHash == passwordHash &&
                        x.active == true
                    )?.UnixTS ?? string.Empty;
                }

                if (!localStorage.RamCache.TryGetValue("AppParameter", out var paramCache) || paramCache == null)
                {
                    return new ScalarModel { out_value_str = string.Empty };
                }

                var list = paramCache.Cast<AppParameterModel>().ToList();
                var value = list.FirstOrDefault(x =>
                    x.AuthUsers_UnixTS == unixTS &&
                    x.ParameterName == parameterName &&
                    x.Scope == scope
                )?.ParameterValue ?? string.Empty;

                return new ScalarModel
                {
                    out_value_str = value
                };
            });

            executor.RegisterScalar("ExistsStoreUrl>>AppParameter", ctx =>
            {
                if (!localStorage.RamCache.TryGetValue("AppParameter", out var paramCache) || paramCache == null)
                {
                    return new ScalarModel { out_value_bool = false, out_value_str = "0" };
                }

                var list = paramCache.Cast<AppParameterModel>().ToList();
                bool exists = list.Any(x =>
                    x.Scope == "app" &&
                    (x.ParameterName?.Contains("StoreUrl_") ?? false) &&
                    (x.AuthUsers_UnixTS?.Length ?? 0) < 35
                );

                return new ScalarModel
                {
                    out_value_bool = exists,
                    out_value_str = exists ? "1" : "0"
                };
            });

            executor.RegisterScalar("CheckBackupCode>>AppParameter", ctx =>
            {
                var authUsers_UnixTS = ctx.Parameters.GetValueOrDefault("@AuthUsers_UnixTS", "");
                var otpBackupCode = ctx.Parameters.GetValueOrDefault("@OtpBackupCode", "");

                if (!localStorage.RamCache.TryGetValue("AppParameter", out var paramCache) || paramCache == null)
                {
                    return new ScalarModel { out_value_bool = false, out_value_str = "0" };
                }

                var list = paramCache.Cast<AppParameterModel>().ToList();
                bool exists = list.Any(x =>
                    x.AuthUsers_UnixTS == authUsers_UnixTS &&
                    x.ParameterValue == otpBackupCode &&
                    x.ParameterName == "OtpBackupCode" &&
                    x.Scope == "config"
                );

                return new ScalarModel
                {
                    out_value_bool = exists,
                    out_value_str = exists ? "1" : "0"
                };
            });

            //executor.RegisterScalar("ExistsStoreUrl>>AppParameter", ctx =>
            //{
            //    if (!localStorage.RamCache.TryGetValue("AppParameter", out var paramCache) || paramCache == null)
            //    {
            //        return new ScalarModel { out_value_bool = false, out_value_str = "0" };
            //    }

            //    var list = paramCache.Cast<AppParameterModel>().ToList();
            //    bool exists = list.Any(x =>
            //        x.Scope == "app" &&
            //        (x.ParameterName?.Contains("StoreUrl_") ?? false) &&
            //        (x.AuthUsers_UnixTS?.Length ?? 0) < 35
            //    );

            //    return new ScalarModel
            //    {
            //        out_value_bool = exists,
            //        out_value_str = exists ? "1" : "0"
            //    };
            //});

            executor.RegisterScalar("SelectAppSettings>>AppParameter", ctx =>
            {
                var authUsers_UnixTS = ctx.Parameters.GetValueOrDefault("@AuthUsers_UnixTS", "");
                var emailHash = ctx.Parameters.GetValueOrDefault("@EmailHash", "");
                var passwordHash = ctx.Parameters.GetValueOrDefault("@PasswordHash", "");
                var parameterName = ctx.Parameters.GetValueOrDefault("@ParameterName", "");
                var scope = ctx.Parameters.GetValueOrDefault("@Scope", "");

                string unixTS = authUsers_UnixTS;

                if (string.IsNullOrEmpty(unixTS) && !string.IsNullOrEmpty(emailHash) && !string.IsNullOrEmpty(passwordHash))
                {
                    if (localStorage.RamCache.TryGetValue("AuthUsers", out var authCache) && authCache != null)
                    {
                        var users = authCache.Cast<AuthUsersModel>().ToList();
                        unixTS = users.FirstOrDefault(x =>
                            x.EmailHash == emailHash &&
                            x.PasswordHash == passwordHash &&
                            x.active == true
                        )?.UnixTS ?? string.Empty;
                    }
                }

                if (!localStorage.RamCache.TryGetValue("AppParameter", out var paramCache) || paramCache == null)
                {
                    return new ScalarModel { out_value_str = string.Empty };
                }

                var appParamList = paramCache.Cast<AppParameterModel>().ToList();
                var value = appParamList.FirstOrDefault(x =>
                    x.AuthUsers_UnixTS == unixTS &&
                    x.ParameterName == parameterName &&
                    x.Scope == scope
                )?.ParameterValue ?? string.Empty;

                return new ScalarModel
                {
                    out_value_str = value
                };
            });


            // =========================================================
            // READ
            // =========================================================
            executor.RegisterRead("Select>>AppParameter", ctx =>
            {
                try
                {
                    var authUsers_UnixTS = ctx.Parameters.GetValueOrDefault("@AuthUsers_UnixTS", "");
                    var scope = ctx.Parameters.GetValueOrDefault("@Scope", "");

                    if (string.IsNullOrEmpty(authUsers_UnixTS))
                    {
                        return new LocalQueryResult
                        {
                            success = false,
                            out_err = "AuthUsers_UnixTS is empty",
                            out_list = new List<object?>()
                        };
                    }

                    if (!localStorage.RamCache.TryGetValue("AppParameter", out var paramCache) || paramCache == null)
                    {
                        return new LocalQueryResult
                        {
                            success = false,
                            out_err = "AppParameter cache not available",
                            out_list = new List<object?>()
                        };
                    }

                    var appParamList = paramCache.Cast<AppParameterModel>().ToList();

                    // Filter by AuthUsers_UnixTS and Scope criteria
                    var data = appParamList
                        .Where(x => x.AuthUsers_UnixTS == authUsers_UnixTS)
                        .Where(x => string.IsNullOrEmpty(scope) || x.Scope == scope)
                        .Select(x => new AppParameterModel
                        {
                            ID = x.ID,
                            UnixTS = x.UnixTS,
                            ParameterName = x.ParameterName,
                            ParameterValue = x.ParameterValue,
                            Details = x.Details,
                            Scope = x.Scope,
                            AuthUsers_UnixTS = x.AuthUsers_UnixTS,
                            LastUpdateUnixTS = x.LastUpdateUnixTS
                        })
                        .Cast<object?>()
                        .ToList();

                    return new LocalQueryResult
                    {
                        success = true,
                        out_list = data,
                        out_data = data.FirstOrDefault()
                    };
                }
                catch (Exception ex)
                {
                    return new LocalQueryResult
                    {
                        success = false,
                        out_err = $"Select>>AppParameter Error: {ex.Message}",
                        out_list = new List<object?>()
                    };
                }
            });

            executor.RegisterRead("SelectStoreUrl>>AppParameter", ctx =>
            {
                try
                {
                    if (!localStorage.RamCache.TryGetValue("AppParameter", out var paramCache) || paramCache == null)
                    {
                        return new LocalQueryResult
                        {
                            success = false,
                            out_err = "AppParameter cache not available",
                            out_list = new List<object?>()
                        };
                    }

                    var appParamList = paramCache.Cast<AppParameterModel>().ToList();

                    // Filter criteria:
                    // - AuthUsers_UnixTS length < 35 (global configuration entries, not user-specific)
                    // - Scope must be 'app'
                    // - ParameterName must start with 'StoreUrl_'
                    var data = appParamList
                        .Where(x => (x.AuthUsers_UnixTS?.Length ?? 0) < 35)
                        .Where(x => x.Scope == "app")
                        .Where(x => x.ParameterName != null && x.ParameterName.StartsWith("StoreUrl_"))
                        .Select(x => new AppParameterModel
                        {
                            ID = x.ID,
                            UnixTS = x.UnixTS,
                            ParameterName = x.ParameterName,
                            ParameterValue = x.ParameterValue,
                            Details = x.Details,
                            Scope = x.Scope,
                            AuthUsers_UnixTS = x.AuthUsers_UnixTS,
                            LastUpdateUnixTS = x.LastUpdateUnixTS
                        })
                        .Cast<object?>()
                        .ToList();

                    return new LocalQueryResult
                    {
                        success = true,
                        out_list = data,
                        out_data = data.FirstOrDefault()
                    };
                }
                catch (Exception ex)
                {
                    return new LocalQueryResult
                    {
                        success = false,
                        out_err = $"SelectStoreUrl>>AppParameter Error: {ex.Message}",
                        out_list = new List<object?>()
                    };
                }
            });


            // =========================================================
            // SAVE
            // =========================================================
            executor.RegisterSave("Save>>AppParameter", async ctx =>
            {
                try
                {
                    var unixTS = ctx.Parameters.GetValueOrDefault("@UnixTS", "");
                    var parameterName = ctx.Parameters.GetValueOrDefault("@ParameterName", "");
                    var parameterValue = ctx.Parameters.GetValueOrDefault("@ParameterValue", "");
                    var details = ctx.Parameters.GetValueOrDefault("@Details", "");
                    var scope = ctx.Parameters.GetValueOrDefault("@Scope", "");
                    var authUsers_UnixTS = ctx.Parameters.GetValueOrDefault("@AuthUsers_UnixTS", "");
                    var lastUpdateUnixTS = ctx.Parameters.GetValueOrDefault("@LastUpdateUnixTS", "");

                    var storageType = pFemmeExample.Shared.Global.Configuration.ConfigGeneral.LocalStorageType;

                    // Verify RAM cache accessibility
                    if (!localStorage.RamCache.TryGetValue("AppParameter", out var paramCache) || paramCache == null)
                    {
                        return new ScalarModel
                        {
                            out_err = "AppParameter cache not available",
                            out_value_str = "not_saved"
                        };
                    }

                    var ramList = paramCache.Cast<AppParameterModel>().ToList();

                    // 1. Check if a record already exists based on composite business keys
                    var existingEntry = ramList.FirstOrDefault(x =>
                        x.ParameterName == parameterName &&
                        x.AuthUsers_UnixTS == authUsers_UnixTS &&
                        x.Scope == scope);

                    AppParameterModel updatedEntry;
                    bool isInsert = existingEntry == null;

                    var calculatedLastUpdate = long.TryParse(lastUpdateUnixTS, out var lastUpdate)
                        ? lastUpdate
                        : DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                    if (isInsert)
                    {
                        // INSERT: Generate unique incremental ID within RAM scope
                        int newId = ramList.Any() ? ramList.Max(x => x.ID) + 1 : 1;

                        updatedEntry = new AppParameterModel
                        {
                            ID = newId,
                            UnixTS = unixTS,
                            ParameterName = parameterName,
                            ParameterValue = parameterValue,
                            Details = details,
                            Scope = scope,
                            AuthUsers_UnixTS = authUsers_UnixTS,
                            LastUpdateUnixTS = calculatedLastUpdate
                        };
                    }
                    else
                    {
                        // UPDATE: Prepare modified object preserving original structural keys
                        updatedEntry = new AppParameterModel
                        {
                            ID = existingEntry!.ID,
                            UnixTS = existingEntry.UnixTS,
                            ParameterName = existingEntry.ParameterName,
                            ParameterValue = parameterValue,
                            Details = details,
                            Scope = existingEntry.Scope,
                            AuthUsers_UnixTS = existingEntry.AuthUsers_UnixTS,
                            LastUpdateUnixTS = calculatedLastUpdate
                        };
                    }

                    // ============================================================
                    // 2. DETERMINE SOURCE FOR RAM (JSON or MEMORY)
                    // ============================================================
                    AppParameterModel dataToSave = updatedEntry; // Default: use prepared local object for MEMORY mode

                    if (storageType == LOCAL_STORAGE_TYPE.JSON_HYBRID)
                    {
                        // AOT-safe serialization using preconfigured JsonContext
                        var json = System.Text.Json.JsonSerializer.Serialize(updatedEntry, JsonContext.Default.AppParameterModel);

                        // Define unique file identifier name using UnixTS fallback to business identifiers
                        var fileIdentifier = !string.IsNullOrEmpty(updatedEntry.UnixTS)
                            ? updatedEntry.UnixTS
                            : $"{updatedEntry.Scope}_{updatedEntry.ParameterName}";

                        var fileName = $"AppParameter/{fileIdentifier}.json";

                        // Save to JSON file
                        var writeResult = await localJsonFile.WritePhysicalFileAsync(localStorage.UserAccountHashed, fileName, json);
                        if (!writeResult.out_value_bool)
                        {
                            return new ScalarModel
                            {
                                out_err = $"JSON save failed: {writeResult.out_err}",
                                out_value_str = "not_saved"
                            };
                        }

                        // Reload from JSON file to ensure 100% data consistency
                        var reloadedJson = await localJsonFile.ReadFileAsync(localStorage.UserAccountHashed, fileName);
                        if (reloadedJson != null)
                        {
                            var reloadedEntry = System.Text.Json.JsonSerializer.Deserialize<AppParameterModel>(
                                reloadedJson,
                                JsonContext.Default.AppParameterModel);

                            if (reloadedEntry != null)
                            {
                                dataToSave = reloadedEntry; // Use verified file data for RAM cache update
                            }
                        }
                    }

                    // ============================================================
                    // 3. UPDATE RAM (Executed only after successful persistence operations)
                    // ============================================================
                    if (isInsert)
                    {
                        ramList.Add(dataToSave);
                    }
                    else
                    {
                        var index = ramList.FindIndex(x =>
                            x.ParameterName == updatedEntry.ParameterName &&
                            x.AuthUsers_UnixTS == updatedEntry.AuthUsers_UnixTS &&
                            x.Scope == updatedEntry.Scope);

                        if (index >= 0)
                        {
                            ramList[index] = dataToSave;
                        }
                    }

                    localStorage.RamCache["AppParameter"] = ramList.Cast<object>().ToList();

                    var returnStatus = isInsert ? "saved" : "updated";
                    return new ScalarModel
                    {
                        out_value_str = $"{returnStatus}:{unixTS}:{authUsers_UnixTS}"
                    };
                }
                catch (Exception ex)
                {
                    return new ScalarModel
                    {
                        out_value_bool = false,
                        out_err = $"Save>>AppParameter Error: {ex.Message}",
                        out_value_str = "not_saved"
                    };
                }
            });

            executor.RegisterSave("SaveJson>>AppParameter", async ctx =>
            {
                if(appState != null) await appState.Log("[LocalDbQueryRegistry SaveJson>>AppParameter] START");

                try
                {
                    var json = ctx.Parameters.GetValueOrDefault("@Json", "");
                    var authUsers_UnixTS = ctx.Parameters.GetValueOrDefault("@AuthUsers_UnixTS", "");
                    var storageType = pFemmeExample.Shared.Global.Configuration.ConfigGeneral.LocalStorageType;

                    if (string.IsNullOrEmpty(json))
                    {
                        if (appState != null) await appState.Log("[LocalDbQueryRegistry SaveJson>>AppParameter] json is null or empty");
                        return new ScalarModel
                        {
                            out_value_str = "not_saved",
                            out_err = "Json parameter is empty"
                        };
                    }

                    // AOT-safe deserialization using the direct JsonContext specialized for the incoming array structure
                    var jsonData = System.Text.Json.JsonSerializer.Deserialize(
                        json,
                        JsonContext.Default.ListAppParameterJsonModel);

                    if (jsonData == null || !jsonData.Any())
                    {
                        if (appState != null) await appState.Log("[LocalDbQueryRegistry SaveJson>>AppParameter] jsonData is null or empty");
                        return new ScalarModel
                        {
                            out_value_str = "not_saved",
                            out_err = "Invalid JSON format or empty array"
                        };
                    }

                    // Verify RAM cache accessibility
                    if (!localStorage.RamCache.TryGetValue("AppParameter", out var paramCache) || paramCache == null)
                    {
                        if (appState != null) await appState.Log("[LocalDbQueryRegistry SaveJson>>AppParameter] AppParameter cache not available");
                        return new ScalarModel
                        {
                            out_err = "AppParameter cache not available",
                            out_value_str = "not_saved"
                        };
                    }

                    // Work with a local copy to ensure atomicity and prevent partial/dirty RAM modifications
                    var ramList = paramCache.Cast<AppParameterModel>().ToList();
                    var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                    foreach (var item in jsonData)
                    {
                        var existingEntry = ramList.FirstOrDefault(x =>
                            x.ParameterName == item.ParameterName &&
                            x.AuthUsers_UnixTS == item.AuthUsers_UnixTS);

                        AppParameterModel updatedEntry;
                        bool isInsert = existingEntry == null;
                        var calculatedLastUpdate = item.LastUpdateUnixTS > 0 ? item.LastUpdateUnixTS : now;

                        if (appState != null) 
                            await appState.Log($"[LocalDbQueryRegistry SaveJson>>AppParameter] item={item.ParameterName} AuthUsers_UnixTS={item.UnixTS} isInsert={isInsert}");

                        if (isInsert)
                        {
                            // INSERT: Generate unique incremental ID within local RAM scope copy
                            int newId = ramList.Any() ? ramList.Max(x => x.ID) + 1 : 1;

                            updatedEntry = new AppParameterModel
                            {
                                ID = newId,
                                UnixTS = item.UnixTS ?? Guid.NewGuid().ToString(),
                                ParameterName = item.ParameterName,
                                ParameterValue = item.ParameterValue,
                                Details = string.Empty,
                                Scope = "set", // Enforcing legacy defaults
                                AuthUsers_UnixTS = item.AuthUsers_UnixTS,
                                LastUpdateUnixTS = calculatedLastUpdate
                            };
                        }
                        else
                        {
                            // UPDATE: Prepare modified object preserving structural key definitions
                            updatedEntry = new AppParameterModel
                            {
                                ID = existingEntry!.ID,
                                UnixTS = existingEntry.UnixTS,
                                ParameterName = existingEntry.ParameterName,
                                ParameterValue = item.ParameterValue,
                                Details = existingEntry.Details,
                                Scope = existingEntry.Scope,
                                AuthUsers_UnixTS = existingEntry.AuthUsers_UnixTS,
                                LastUpdateUnixTS = calculatedLastUpdate
                            };
                        }

                        // ============================================================
                        // DETERMINE SOURCE FOR RAM (JSON or MEMORY)
                        // ============================================================
                        AppParameterModel dataToSave = updatedEntry;

                        if (storageType == LOCAL_STORAGE_TYPE.JSON_HYBRID)
                        {
                            // AOT-safe serialization for the physical single record model
                            var singleJson = System.Text.Json.JsonSerializer.Serialize(updatedEntry, JsonContext.Default.AppParameterModel);

                            if (appState != null) await appState.Log($"[LocalDbQueryRegistry SaveJson>>AppParameter] JsonSerializer={singleJson}");

                            var fileIdentifier = !string.IsNullOrEmpty(updatedEntry.UnixTS)
                                ? updatedEntry.UnixTS
                                : $"{updatedEntry.Scope}_{updatedEntry.ParameterName}";

                            var fileName = $"AppParameter/{fileIdentifier}.json";

                            // Save individual parameter record to file system
                            var writeResult = await localJsonFile.WritePhysicalFileAsync(localStorage.UserAccountHashed, fileName, singleJson);

                            if (appState != null) 
                                await appState.Log($"[LocalDbQueryRegistry SaveJson>>AppParameter] WritePhysicalFileAsync UserAccountHashed={localStorage.UserAccountHashed} fileName={fileName} singleJson={singleJson} writeResult.out_value_bool={writeResult.out_value_bool}");

                            if (!writeResult.out_value_bool)
                            {
                                return new ScalarModel
                                {
                                    out_err = $"JSON bulk item save failed: {writeResult.out_err}",
                                    out_value_str = "not_saved"
                                };
                            }

                            // Immediately read back from storage to confirm complete file integrity
                            var reloadedJson = await localJsonFile.ReadFileAsync(localStorage.UserAccountHashed, fileName);

                            if (appState != null)
                                await appState.Log($"[LocalDbQueryRegistry SaveJson>>AppParameter] ReadFileAsync UserAccountHashed={localStorage.UserAccountHashed} fileName={fileName}", data: reloadedJson);

                            if (reloadedJson != null)
                            {
                                var reloadedEntry = System.Text.Json.JsonSerializer.Deserialize<AppParameterModel>(
                                    reloadedJson,
                                    JsonContext.Default.AppParameterModel);

                                if (reloadedEntry != null)
                                {
                                    dataToSave = reloadedEntry;
                                }
                            }
                        }

                        // Commit change locally to our processing array copy list
                        if (isInsert)
                        {
                            ramList.Add(dataToSave);
                        }
                        else
                        {
                            var index = ramList.FindIndex(x =>
                                x.ParameterName == updatedEntry.ParameterName &&
                                x.AuthUsers_UnixTS == updatedEntry.AuthUsers_UnixTS);

                            if (index >= 0)
                            {
                                ramList[index] = dataToSave;
                            }
                        }
                    }

                    // ============================================================
                    // ATOMIC RAM COMMIT
                    // ============================================================
                    localStorage.RamCache["AppParameter"] = ramList.Cast<object>().ToList();

                    if (appState != null) await appState.Log("[LocalDbQueryRegistry SaveJson>>AppParameter] END");

                    return new ScalarModel
                    {
                        out_value_str = $"updated:-1:{authUsers_UnixTS}"
                    };
                }
                catch (System.Text.Json.JsonException ex)
                {
                    if (appState != null) await appState.Error("[LocalDbQueryRegistry SaveJson>>AppParameter] ERROR : " + ex.Message);
                    return new ScalarModel
                    {
                        out_value_bool = false,
                        out_err = $"SaveJson>>AppParameter JSON Parse Error: {ex.Message}",
                        out_value_str = "not_saved"
                    };
                }
                catch (Exception ex)
                {
                    if (appState != null) await appState.Error("[LocalDbQueryRegistry SaveJson>>AppParameter] ERROR : " + ex.Message);
                    return new ScalarModel
                    {
                        out_value_bool = false,
                        out_err = $"SaveJson>>AppParameter Error: {ex.Message}",
                        out_value_str = "not_saved"
                    };
                }
            });


            // ====================================================================
            // DELETE
            // ====================================================================
            executor.RegisterSave("DeleteAuthUsers_UnixTS>>AppParameter", async ctx =>
            {
                try
                {
                    var authUsers_UnixTS = ctx.Parameters.GetValueOrDefault("@AuthUsers_UnixTS", "");
                    var storageType = pFemmeExample.Shared.Global.Configuration.ConfigGeneral.LocalStorageType;

                    if (string.IsNullOrEmpty(authUsers_UnixTS))
                    {
                        return new ScalarModel { out_value_bool = false, out_value_str = "not_deleted", out_err = "AuthUsers_UnixTS empty" };
                    }

                    if (!localStorage.RamCache.TryGetValue("AppParameter", out var paramCache) || paramCache == null)
                    {
                        return new ScalarModel { out_value_bool = false, out_value_str = "not_deleted", out_err = "Cache not available" };
                    }

                    var currentRecords = paramCache.Cast<AppParameterModel>().ToList();
                    // Identifiziere alle Datensätze, die gelöscht werden müssen
                    var targets = currentRecords.Where(x => x.AuthUsers_UnixTS == authUsers_UnixTS).ToList();

                    if (targets.Count == 0)
                    {
                        return new ScalarModel { out_value_bool = true, out_value_str = $"deleted:{authUsers_UnixTS}:{authUsers_UnixTS}" };
                    }

                    // 1. Physische Löschung (Disk-First)
                    if (storageType == LOCAL_STORAGE_TYPE.JSON_HYBRID)
                    {
                        foreach (var item in targets)
                        {
                            var fileIdentifier = !string.IsNullOrEmpty(item.UnixTS) ? item.UnixTS : $"{item.Scope}_{item.ParameterName}";
                            var fileName = $"AppParameter/{fileIdentifier}.json";

                            bool deleteSuccess = await localJsonFile.DeletePhysicalFileAsync(localStorage.UserAccountHashed, fileName);

                            // Wenn auch nur eine Datei fehlschlägt, brechen wir ab, um RAM-Konsistenz zu wahren
                            if (!deleteSuccess)
                            {
                                return new ScalarModel { out_value_bool = false, out_value_str = "not_deleted", out_err = "Partial physical deletion failed" };
                            }
                        }
                    }

                    // 2. RAM-Synchronisation: Erst nach erfolgreicher physischer Löschung
                    currentRecords.RemoveAll(x => x.AuthUsers_UnixTS == authUsers_UnixTS);
                    localStorage.RamCache["AppParameter"] = currentRecords.Cast<object>().ToList();

                    return new ScalarModel
                    {
                        out_value_bool = true,
                        out_value_str = $"deleted:{authUsers_UnixTS}:{authUsers_UnixTS}"
                    };
                }
                catch (Exception ex)
                {
                    return new ScalarModel { out_value_bool = false, out_value_str = "not_deleted", out_err = ex.Message };
                }
            });
        }



        // =========================================================
        // GENERAL - Metaprogramming & Structure Checks
        // =========================================================
        private static void RegisterGeneral(
            ILocalQueryExecutor executor,
            ILocalStorage localStorage)
        {
            executor.RegisterScalar("TableExists", ctx =>
            {
                var tableName = ctx.Parameters.GetValueOrDefault("@TableName", "");

                bool exists = localStorage.RamCache.ContainsKey(tableName);

                return new ScalarModel
                {
                    out_value_bool = exists,
                    out_value_str = exists ? "1" : "0"
                };
            });

            executor.RegisterScalar("CheckMultipleTablesExist", ctx =>
            {
                var tableList = ctx.Parameters.GetValueOrDefault("@TableList", "");

                var tables = tableList
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .ToList();

                // Check if AT LEAST one of the tables provided in the comma-separated list exists in RAM cache
                bool exists = tables.Any(t => localStorage.RamCache.ContainsKey(t));

                return new ScalarModel
                {
                    out_value_bool = exists,
                    out_value_str = exists ? "1" : "0"
                };
            });
        }

    }
}