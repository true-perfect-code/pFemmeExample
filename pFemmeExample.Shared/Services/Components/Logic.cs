using BlazorCore.Services.AppState;
using BlazorCore.Services.Dam;
using BlazorCore.Services.Platform;
using Microsoft.Extensions.DependencyInjection;
using pFemmeExample.Shared.Models;

namespace pFemmeExample.Shared.Services.Components
{
    public class LogicService
    {
        private readonly IServiceProvider _serviceProvider;

        private readonly IAppStateBase _appState;

        // App Services
        private readonly CyclesService _cyclesService;

        private readonly DateTime _today = DateTime.Now.Date; // Testing -> DateTime.Now.Date.AddDays(11);

        // Inject dependencies via DI
        public LogicService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            _appState = _serviceProvider.GetRequiredService<IAppStateBase>();

            // App Services
            _cyclesService = _serviceProvider.GetRequiredService<CyclesService>();
        }

        /// <summary>
        /// Loads historical cycle data, computes cycle durations, calculates the median interval length, 
        /// and determines the current fertility and infertility phases for the user.
        /// </summary>
        /// <returns>A read model container containing the calculated cycle phases within the primary list or error messages.</returns>
        public async Task<BlazorCore.Services.SqlClient.ReadModel<CyclePhasesModel?>> LoadCurrentPhase()
        {
            BlazorCore.Services.SqlClient.ReadModel<CyclePhasesModel?> result = new();

            try
            {
                // Fetch raw data from the cycles service
                var result_cycles = await _cyclesService.Load();

                // Logical condition check (ensuring data exists and no error is present)
                if (result_cycles != null && result_cycles.out_list != null && string.IsNullOrEmpty(result_cycles.out_err))
                {
                    // 1. Calculate start and end dates of all periods
                    var result_cycleDurations = LoadCycleDurations(result_cycles.out_list ?? new List<CyclesModel?>());
                    if (result_cycleDurations.Count == 0)
                    {
                        result.out_list = new List<CyclePhasesModel?>();
                        return result; // Returns an empty list safely if no periods exist
                    }

                    // 2. Calculate day intervals between periods
                    var result_cycleIntervals = CalculateIntervalBetweenCycles(result_cycleDurations);

                    // 3. Calculate median cycle length
                    var result_medianCycleLength = CalculateMedianCycleLength(result_cycleIntervals);

                    // Fallback to a standard default cycle length of 28 days if no median can be determined
                    double medianLength = result_medianCycleLength ?? 28.0;

                    // 4. Calculate fertility window offsets relative to cycle start
                    double startFertileWindowDays = medianLength - 14.0 - 5.0;
                    double endFertileWindowDays = medianLength - 14.0 + 1.0;

                    // Determine the last recorded cycle start day
                    DateTime? lastCycleStartDay = result_cycleDurations
                        .Where(c => c.FirstDayCycle.HasValue)
                        .Max(c => c.FirstDayCycle);

                    // We back up the real database value here into a dedicated variable 
                    // so it remains entirely untouched by the downstream projection loops.
                    DateTime? realDbLastCycleStartDay = lastCycleStartDay?.Date;

                    // === STEP A: LIST VIRTUALIZATION ===
                    // If the last cycle from the DB has completely passed, we virtually backfill the list
                    // until we have arrived at the current present day.
                    if (lastCycleStartDay.HasValue)
                    {
                        DateTime today = _today;
                        DateTime currentAnchor = lastCycleStartDay.Value.Date;

                        while (today >= currentAnchor.AddDays(Math.Ceiling(medianLength)))
                        {
                            currentAnchor = currentAnchor.AddDays(Math.Ceiling(medianLength));

                            var virtualCycle = new CycleDurationModel
                            {
                                FirstDayCycle = currentAnchor,
                                LastDayCycle = null
                            };
                            result_cycleDurations.Add(virtualCycle);
                        }

                        // Re-evaluate the last recorded start date (it now grabs the highest virtual date)
                        lastCycleStartDay = result_cycleDurations
                            .Where(c => c.FirstDayCycle.HasValue)
                            .Max(c => c.FirstDayCycle);
                    }

                    // === STEP B: PHASE CALCULATION WITH THE NEW ANCHOR ===
                    if (lastCycleStartDay.HasValue)
                    {
                        DateTime baseDate = lastCycleStartDay.Value.Date;

                        // Determine the real calendar dates of the fertility window for this specific cycle
                        DateTime startFertileWindowDate = baseDate.AddDays(Math.Ceiling(startFertileWindowDays));
                        DateTime endFertileWindowDate = baseDate.AddDays(Math.Ceiling(endFertileWindowDays));

                        DateTime today = _today;
                        int fertilePhase = 0;
                        int infertilePhase = 0;

                        // Check which of the two phases we are currently in
                        if (today >= startFertileWindowDate && today <= endFertileWindowDate)
                        {
                            // CASE A: The user is currently within the fertility window
                            fertilePhase = (endFertileWindowDate - today).Days + 1;
                            infertilePhase = 0;
                        }
                        else
                        {
                            // CASE B: The user is outside the window (hence infertile)
                            fertilePhase = 0;

                            if (today < startFertileWindowDate)
                            {
                                // The user is before the fertility window of this current cycle
                                infertilePhase = (startFertileWindowDate - today).Days;
                            }
                            else
                            {
                                // FIX: The user is past the current window at the tail end of the cycle.
                                // To prevent a UX jump, we project the fertility window of the next cycle
                                // and seamlessly calculate the remaining days until then.
                                DateTime nextCycleStart = baseDate.AddDays(Math.Ceiling(medianLength));
                                DateTime nextStartFertileWindowDate = nextCycleStart.AddDays(Math.Ceiling(startFertileWindowDays));

                                infertilePhase = (nextStartFertileWindowDate - today).Days;
                            }
                        }

                        // 6. Calculate min/max period duration in days (from historical data)
                        int avgDurationMin = 0;
                        int avgDurationMax = 0;

                        // Find the absolute highest/latest start date in the collection
                        DateTime? latestPeriodStart = result_cycleDurations
                            .Where(c => c != null && c.FirstDayCycle.HasValue)
                            .Max(c => c!.FirstDayCycle);

                        var validDurations = result_cycleDurations
                            .Where(c => c != null && c.FirstDayCycle.HasValue && c.LastDayCycle.HasValue)
                            // BIOLOGICAL FILTER: Exclude the latest tracked period if it started within the last 7 days.
                            // This prevents an ongoing, incomplete cycle from distorting the historical minimum duration down to 1 or 2 days.
                            .Where(c => !(c.FirstDayCycle == latestPeriodStart && (DateTime.Today - (c.FirstDayCycle?.Date ?? DateTime.Today)).Days <= 7))
                            .Select(c => (c.LastDayCycle!.Value - c.FirstDayCycle!.Value).Days + 1)
                            .ToList();

                        if (validDurations.Count > 0)
                        {
                            avgDurationMin = validDurations.Min();
                            avgDurationMax = validDurations.Max();
                        }

                        if (validDurations.Count > 0)
                        {
                            avgDurationMin = validDurations.Min();
                            avgDurationMax = validDurations.Max();
                        }

                        // Populate the results model
                        var computedPhase = new CyclePhasesModel
                        {
                            LastCycleStartDay = realDbLastCycleStartDay,           // Guaranteed to be the real DB value
                            LastCycleStartDayProjected = baseDate,                 // The calculated/projected value
                            fertile_phase = fertilePhase,
                            infertile_phase = infertilePhase,
                            avgDurationMin = avgDurationMin,
                            avgDurationMax = avgDurationMax,
                            MedianCycleLength = medianLength
                        };

                        // Add to the return list
                        result.out_list = new List<CyclePhasesModel?> { computedPhase };
                    }
                    else
                    {
                        result.out_list = new List<CyclePhasesModel?>();
                    }
                }
                else
                {
                    result.out_err = result_cycles?.out_err ?? "Failed to load raw cycle data records.";
                    result.out_list = new List<CyclePhasesModel?>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in LoadCurrentPhase: {ex.Message}");
                result.out_err = ex.Message;
                result.out_list = new List<CyclePhasesModel?>();
            }

            return result;
        }

        /// <summary>
        /// Retrieves the historical cycle data and calculates the median duration of all recorded cycles.
        /// If no historical data is available to determine a median, it defaults to a standard value of 28 days.
        /// </summary>
        /// <returns>A scalar model containing the calculated median cycle length as a double value.</returns>
        public async Task<BlazorCore.Services.SqlClient.ScalarModel> LoadMedianCycleLength()
        {
            BlazorCore.Services.SqlClient.ScalarModel result = new();
            try
            {
                // Fetch raw data from the cycles service
                var result_cycles = await _cyclesService.Load();

                // 1. Calculate start and end dates of all periods
                var result_cycleDurations = LoadCycleDurations(result_cycles.out_list ?? new List<CyclesModel?>());

                if (result_cycleDurations != null && result_cycleDurations.Count != 0)
                {
                    // 2. Calculate day intervals between periods
                    var result_cycleIntervals = CalculateIntervalBetweenCycles(result_cycleDurations);

                    // 3. Calculate median cycle length
                    var result_medianCycleLength = CalculateMedianCycleLength(result_cycleIntervals);

                    // Fallback to 28 days if no intervals can be calculated
                    result.out_value_dbl = result_medianCycleLength ?? 28.0;
                }
                else
                {
                    // Standard default if no cycle data exists
                    result.out_value_dbl = 28.0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in LoadMedianCycleLength: {ex.Message}");
                result.out_err = ex.Message;
                // Optional: result.out_value_dbl = 28.0; // Sicherstellen, dass auch im Fehlerfall ein Default da ist
            }

            return result;
        }

        /// <summary>
        /// Loads raw cycle records, computes chronological intervals between consecutive cycles, 
        /// and returns a list of intervals mapped to the standard chart model structure.
        /// This replicates the logic of selecting the day differences and start days from the cycle intervals table.
        /// </summary>
        /// <returns>A read model container containing the mapped cycle interval charts records or error messages.</returns>
        public async Task<BlazorCore.Services.SqlClient.ReadModel<ChartsModel?>> SelectTrendsCycle()
        {
            BlazorCore.Services.SqlClient.ReadModel<ChartsModel?> result = new();

            try
            {
                // Fetch raw data from the cycles service
                var result_cycles = await _cyclesService.Load();

                // Logical condition check (ensuring data exists and no error is present)
                if (result_cycles != null && result_cycles.out_list != null && string.IsNullOrEmpty(result_cycles.out_err))
                {
                    // 1. Calculate start and end dates of all periods from the past year
                    var result_cycleDurations = LoadCycleDurations(result_cycles.out_list ?? new List<CyclesModel?>());
                    if (result_cycleDurations.Count == 0)
                    {
                        result.out_list = new List<ChartsModel?>();
                        return result;
                    }

                    // 2. Calculate day intervals between periods
                    var result_cycleIntervals = CalculateIntervalBetweenCycles(result_cycleDurations);

                    // 3. Project and map to the requested structure (reflecting the T-SQL query)
                    result.out_list = result_cycleIntervals
                        .Where(i => i != null)
                        .Select(i => new ChartsModel
                        {
                            Int__Value = i.DayDifferenceToLastCycle, // Implicit conversion from int to double?
                            Int__Label = string.Empty,               // Reflects: '' AS Int__Label
                            Int__Date = i.FirstDayCycle // Safe fallback for non-nullable DateTime
                        })
                        .ToList()!;
                }
                else
                {
                    result.out_err = result_cycles?.out_err ?? "Failed to load raw cycle data records.";
                    result.out_list = new List<ChartsModel?>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in LoadIntervalsBetweenCyclesData: {ex.Message}");
                result.out_err = ex.Message;
                result.out_list = new List<ChartsModel?>();
            }

            return result;
        }

        /// <summary>
        /// Loads raw cycle records, computes individual cycle durations, and returns a historically 
        /// ordered list of periods including their start dates, end dates, and total bleeding duration.
        /// This replicates the logic of calculating the day difference between the first and last day of a cycle.
        /// </summary>
        /// <returns>A read model container containing the chronological cycle summary history records or error messages.</returns>
        public async Task<BlazorCore.Services.SqlClient.ReadModel<CycleSummaryModel?>> SelectCycleHistory()
        {
            BlazorCore.Services.SqlClient.ReadModel<CycleSummaryModel?> result = new();

            try
            {
                // Fetch raw data from the cycles service
                var result_cycles = await _cyclesService.Load();

                // Logical condition check (ensuring data exists and no error is present)
                if (result_cycles != null && result_cycles.out_list != null && string.IsNullOrEmpty(result_cycles.out_err))
                {
                    // 1. Calculate start and end dates of all periods from the past year
                    var result_cycleDurations = LoadCycleDurations(result_cycles.out_list ?? new List<CyclesModel?>());
                    if (result_cycleDurations.Count == 0)
                    {
                        result.out_list = new List<CycleSummaryModel?>();
                        return result;
                    }

                    // 2. Filter, order chronologically, and map to final business model (Reflects T-SQL: ORDER BY FirstDayCycle)
                    result.out_list = result_cycleDurations
                        .Where(i => i != null && i.FirstDayCycle.HasValue)
                        .OrderBy(i => i.FirstDayCycle!.Value)
                        .Select(i => {
                            // Safe assignment of the non-nullable start date
                            DateTime start = i.FirstDayCycle!.Value;

                            return new CycleSummaryModel
                            {
                                FirstDayCycle = start,
                                LastDayCycle = i.LastDayCycle,
                                // Null-safe duration check: if LastDayCycle is present compute delta, otherwise fallback to 0
                                Duration = i.LastDayCycle.HasValue
                                    ? (i.LastDayCycle.Value - start).Days + 1
                                    : 0
                            };
                        })
                        .ToList()!;
                }
                else
                {
                    result.out_err = result_cycles?.out_err ?? "Failed to load raw cycle data records.";
                    result.out_list = new List<CycleSummaryModel?>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SelectCycleHistory: {ex.Message}");
                result.out_err = ex.Message;
                result.out_list = new List<CycleSummaryModel?>();
            }

            return result;
        }

        /// <summary>
        /// Evaluates raw daily cycle tracking entries within a trailing one-year window, 
        /// reconstructing distinct menstruation periods by identifying contiguous blocks of active bleeding records.
        /// </summary>
        /// <param name="result_cycles">The complete list of raw daily cycle tracking records from the database.</param>
        /// <returns>A list of structured cycle duration records containing the determined start and end dates for each period.</returns>
        private List<CycleDurationModel> LoadCycleDurations(List<CyclesModel?> result_cycles)
        {
            // Return early if no raw data is available to process
            if (result_cycles == null || result_cycles.Count == 0)
                return new List<CycleDurationModel>();

            List<CycleDurationModel> result_cycleDurations = new();

            try
            {
                // Define the tracking evaluation timeframe (rolling 12-month window)
                DateTime endDate = DateTime.Now.Date;
                DateTime startDate = endDate.AddYears(-1);

                string? currentAuthUser = _appState.UnixTS;

                // Filter records strictly by ownership and the defined date range boundary
                var filteredCycles = result_cycles
                    .Where(c => c != null &&
                                c.AuthUsers_UnixTS == currentAuthUser &&
                                c.RecordDate.HasValue &&
                                c.RecordDate.Value.Date >= startDate &&
                                c.RecordDate.Value.Date <= endDate)
                    .ToList();

                // Linearly iterate through each single day of the evaluation timeline to scan for bleeding states
                for (DateTime currentDate = startDate; currentDate <= endDate; currentDate = currentDate.AddDays(1))
                {
                    DateTime previousDate = currentDate.AddDays(-1);
                    DateTime nextDate = currentDate.AddDays(1);

                    // Check if active bleeding was logged for the current date iteration
                    var currentDayRecord = filteredCycles.FirstOrDefault(c => c != null && c.RecordDate?.Date == currentDate && c.bleeding);

                    if (currentDayRecord != null)
                    {
                        // Detect Period Start: Bleeding today, but no bleeding occurred on the previous day
                        var previousDayRecord = filteredCycles.FirstOrDefault(c => c != null && c.RecordDate?.Date == previousDate);
                        bool isStartOfPeriod = previousDayRecord == null || !previousDayRecord.bleeding;

                        if (isStartOfPeriod)
                        {
                            result_cycleDurations.Add(new CycleDurationModel
                            {
                                FirstDayCycle = currentDate
                            });
                        }

                        // Detect Period End: Bleeding today, but no bleeding is recorded for the following day
                        var nextDayRecord = filteredCycles.FirstOrDefault(c => c != null && c.RecordDate?.Date == nextDate);
                        bool isEndOfPeriod = nextDayRecord == null || !nextDayRecord.bleeding;

                        if (isEndOfPeriod)
                        {
                            // Map the end date to the active tracking period block that was just initiated
                            var unfinishedPeriod = result_cycleDurations.LastOrDefault(c => c.LastDayCycle == null);
                            if (unfinishedPeriod != null)
                            {
                                unfinishedPeriod.LastDayCycle = currentDate;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in LoadCycleDurations: {ex.Message}");
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                return result_cycleDurations;
            }

            return result_cycleDurations;
        }

        /// <summary>
        /// Chronologically analyzes consecutive cycle tracking records to compute the absolute 
        /// day variance between their respective start dates. Intervals of 5 days or less are 
        /// omitted to filter out fragmented logging noise or mid-cycle anomalies.
        /// </summary>
        /// <param name="cycleDurations">The collection of determined historical cycle periods containing start anchors.</param>
        /// <returns>A list of computed cycle intervals indicating the elapsed day difference relative to the preceding cycle.</returns>
        private List<CycleIntervalModel> CalculateIntervalBetweenCycles(List<CycleDurationModel> cycleDurations)
        {
            // Return early if there are insufficient data points to execute a delta comparison
            if (cycleDurations == null || cycleDurations.Count < 2)
                return new List<CycleIntervalModel>();

            List<CycleIntervalModel> result_intervals = new();

            try
            {
                // Enforce chronological sorting based on verified cycle start dates
                var orderedCycles = cycleDurations
                    .Where(c => c != null && c.FirstDayCycle.HasValue)
                    .OrderBy(c => c.FirstDayCycle!.Value)
                    .ToList();

                // Linearly compare each cycle entry against its direct historical predecessor
                for (int i = 1; i < orderedCycles.Count; i++)
                {
                    DateTime currentFirstDay = orderedCycles[i].FirstDayCycle!.Value;
                    DateTime previousFirstDay = orderedCycles[i - 1].FirstDayCycle!.Value;

                    // Compute the total elapsed days between the two consecutive start anchors
                    int dayDifference = (currentFirstDay - previousFirstDay).Days;

                    // Biological filter: intervals must exceed 5 days to be considered a separate, valid cycle
                    if (dayDifference > 5)
                    {
                        result_intervals.Add(new CycleIntervalModel
                        {
                            FirstDayCycle = currentFirstDay,
                            DayDifferenceToLastCycle = dayDifference
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CalculateIntervalBetweenCycles: {ex.Message}");
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                return result_intervals;
            }

            return result_intervals;
        }

        /// <summary>
        /// Performs a statistical median extraction over the collected cycle intervals to determine 
        /// the user's standardized cycle length, mitigating the impact of extreme statistical outliers.
        /// </summary>
        /// <param name="intervals">The collection of computed day variances between consecutive cycles.</param>
        /// <returns>The calculated median cycle length as a double, or null if no intervals are available.</returns>
        private double? CalculateMedianCycleLength(List<CycleIntervalModel> intervals)
        {
            // Return null immediately if no interval data exists to analyze
            if (intervals == null || intervals.Count == 0)
                return null;

            try
            {
                // Isolate interval durations and sort them in ascending order to prepare for median evaluation
                var sortedDifferences = intervals
                    .Where(i => i != null)
                    .Select(i => i.DayDifferenceToLastCycle)
                    .OrderBy(d => d)
                    .ToList();

                int count = sortedDifferences.Count;
                if (count == 0) return null;

                // Determine median based on whether the dataset count is odd or even
                if (count % 2 != 0)
                {
                    // Odd dataset: Select the exact middle element
                    return (double)sortedDifferences[count / 2];
                }
                else
                {
                    // Even dataset: Compute the average of the two central elements
                    int middle1 = sortedDifferences[(count / 2) - 1];
                    int middle2 = sortedDifferences[count / 2];

                    return (middle1 + middle2) / 2.0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CalculateMedianCycleLength: {ex.Message}");
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                return null;
            }
        }

        /// <summary>
        /// Loads the cycle data and performs a comparison between cloud and local storage records.
        /// </summary>
        /// <remarks>
        /// This method retrieves base <see cref="CyclesModel"/> data, maps it to <see cref="CyclesCompareModel"/>,
        /// and delegates the data reconciliation to the Data Access Manager (DAM).
        /// </remarks>
        /// <returns>
        /// A <see cref="BlazorCore.Services.SqlClient.ReadModel{T}"/> containing the comparison results 
        /// or an error message if the operation fails.
        /// </returns>
        public async Task<BlazorCore.Services.SqlClient.ReadModel<CyclesCompareModel?>> LoadCompared()
        {
            BlazorCore.Services.SqlClient.ReadModel<CyclesCompareModel?> result = new();

            try
            {
                // Load data as CyclesModel first
                var result_cycle = await _cyclesService.Load();

                // Then map to CyclesCompareModel
                if (result_cycle != null
                    && string.IsNullOrEmpty(result_cycle.out_err)
                    && result_cycle.out_list != null
                    && result_cycle.out_list_cloud != null
                    && result_cycle.out_list_local != null)
                {
                    List<CyclesCompareModel?> items_cloud = new();
                    foreach (var item in result_cycle.out_list_cloud)
                    {
                        if (item == null) continue;

                        items_cloud.Add(new CyclesCompareModel
                        {
                            ID = item.ID,
                            UnixTS = item.UnixTS,
                            AuthUsers_UnixTS = item.AuthUsers_UnixTS,
                            RecordDate = item.RecordDate,
                            bleeding = item.bleeding,
                            intensity = item.intensity,
                            pain = item.pain,
                            headache = item.headache,
                            fatigue = item.fatigue,
                            nausea = item.nausea,
                            cramps = item.cramps,
                            Details = item.Details,
                            LastUpdateUnixTS = item.LastUpdateUnixTS
                        });
                    }
                    List<CyclesCompareModel?> items_local = new();
                    foreach (var item in result_cycle.out_list_local)
                    {
                        if (item == null) continue;

                        items_local.Add(new CyclesCompareModel
                        {
                            ID = item.ID,
                            UnixTS = item.UnixTS,
                            AuthUsers_UnixTS = item.AuthUsers_UnixTS,
                            RecordDate = item.RecordDate,
                            bleeding = item.bleeding,
                            intensity = item.intensity,
                            pain = item.pain,
                            headache = item.headache,
                            fatigue = item.fatigue,
                            nausea = item.nausea,
                            cramps = item.cramps,
                            Details = item.Details,
                            LastUpdateUnixTS = item.LastUpdateUnixTS
                        });
                    }

                    if(items_cloud != null && items_local != null)
                    {
                        var result_cycle_compare = await _cyclesService.CompareLocalCloud(items_cloud, items_local);
                        result = result_cycle_compare ?? new BlazorCore.Services.SqlClient.ReadModel<CyclesCompareModel?>();
                    }
                }
            }
            catch (Exception ex)
            {
                result.out_err = ex.Message;
            }

            return result ?? new BlazorCore.Services.SqlClient.ReadModel<CyclesCompareModel?>();
        }

    }
}