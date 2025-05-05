using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks; // Required for asynchronous operations (Task, async, await)
using BitSchedulerCore.Data.BitTimeScheduler.Data; // Required for the DbContext
using BitSchedulerCore.Models; // Contains AuditableEntity, used by BitReservation (though not directly here)
using BitSchedulerCore.Services; // Required for BitScheduleDataService
using BitTimeScheduler.Models; // Contains supporting model classes like BitDateRange, BitTimeRange, etc.
using Microsoft.EntityFrameworkCore; // Required for DbContext operations (SaveChangesAsync, Entry, EntityState)
using Microsoft.Extensions.Logging; // Required for ILogger

namespace BitTimeScheduler
{
    /// <summary>
    /// Manages schedule data for a specific client based on a defined configuration.
    /// This class acts as a central point for loading, reading, and writing schedule information.
    /// It holds schedule data (BitDay objects) in memory using a Dictionary for efficient date-based lookups.
    /// It interacts with a BitScheduleDataService for loading data and directly with BitScheduleDbContext for saving changes.
    /// Includes logging via ILogger.
    /// </summary>
    public class BitSchedule
    {
        // --- Private Fields ---

        /// <summary>
        /// In-memory storage for BitDay objects, keyed by the Date part of the DateTime.
        /// Using a Dictionary allows for fast O(1) average time complexity for lookups by date.
        /// </summary>
        private Dictionary<DateTime, BitDay> _scheduleData;

        /// <summary>
        /// Holds the current configuration settings (date range, active days, etc.)
        /// that define the scope of the schedule data managed by this instance.
        /// </summary>
        private BitScheduleConfiguration _configuration;

        /// <summary>
        /// Service responsible for loading schedule data from the persistent store.
        /// Marked readonly as it's injected via the constructor and shouldn't change afterwards.
        /// </summary>
        private readonly BitScheduleDataService _dataService;

        /// <summary>
        /// Entity Framework DbContext instance used for persisting changes (saving updates/inserts) to the database.
        /// Marked readonly as it's injected via the constructor.
        /// </summary>
        private readonly BitScheduleDbContext _dbContext;

        /// <summary>
        /// Logger instance injected for logging messages throughout the class lifecycle and operations.
        /// Marked readonly as it's injected via the constructor.
        /// </summary>
        private readonly ILogger<BitSchedule> _logger;

        // --- Public Properties ---

        /// <summary>
        /// Flag indicating whether the configuration or in-memory data might be out of sync
        /// with the persistent store or the applied configuration since the last data load.
        /// Set to true when configuration changes. Reset to false after LoadScheduleData completes successfully.
        /// </summary>
        public bool IsDirty { get; set; } = false;

        /// <summary>
        /// Timestamp indicating when the internal `_scheduleData` was last successfully loaded or refreshed.
        /// </summary>
        public DateTime LastRefreshed { get; set; }

        /// <summary>
        /// The specific Client ID for which this schedule instance is managing data.
        /// Used when loading data and creating new BitDay entries.
        /// </summary>
        public int ClientId { get; set; }

        /// <summary>
        /// Gets or sets the schedule configuration.
        /// When setting a new configuration:
        /// 1. Unsubscribes from the old configuration's PropertyChanged event (if any).
        /// 2. Assigns the new configuration.
        /// 3. Subscribes to the new configuration's PropertyChanged event.
        /// 4. Checks if relevant properties (DateRange, ActiveDays) actually changed compared to the old configuration.
        /// 5. Sets IsDirty flag if a relevant change occurred.
        /// 6. If AutoRefreshOnConfigurationChange is true AND a relevant change occurred, triggers LoadScheduleData().
        /// Logs information about the change and potential auto-refresh.
        /// </summary>
        public BitScheduleConfiguration Configuration
        {
            get { return _configuration; }
            set
            {
                // Avoid unnecessary work if the same instance is assigned
                if (_configuration == value)
                {
                    _logger.LogTrace("Attempted to set the same BitScheduleConfiguration instance. No action taken.");
                    return;
                }

                _logger.LogInformation("BitScheduleConfiguration is being changed for ClientId {ClientId}.", ClientId);

                // Store the old configuration for comparison later
                var oldConfiguration = _configuration;

                // Unsubscribe from the old configuration's change notifications to prevent memory leaks
                if (_configuration != null)
                {
                    _configuration.PropertyChanged -= OnConfigurationChanged;
                    _logger.LogTrace("Unsubscribed from PropertyChanged event of the old configuration.");
                }

                // Assign the new configuration instance
                _configuration = value;

                // Determine if the change affects the data scope
                bool configActuallyChanged = ConfigurationHasChanged(oldConfiguration, _configuration);

                // Mark as dirty if the configuration affecting the data scope has changed
                // Keep existing IsDirty state if it was already true
                if (configActuallyChanged)
                {
                    _logger.LogDebug("Configuration change detected that affects data scope. Marking IsDirty=true for ClientId {ClientId}.", ClientId);
                    IsDirty = true;
                }

                // Subscribe to the new configuration's change notifications
                if (_configuration != null)
                {
                    _configuration.PropertyChanged += OnConfigurationChanged;
                    _logger.LogTrace("Subscribed to PropertyChanged event of the new configuration.");
                }
                else
                {
                    _logger.LogWarning("New BitScheduleConfiguration is null for ClientId {ClientId}. Cannot subscribe to changes.", ClientId);
                }

                // Automatically refresh data if the flag is set AND a relevant change occurred
                if (configActuallyChanged && _configuration != null && _configuration.AutoRefreshOnConfigurationChange)
                {
                    _logger.LogInformation("AutoRefresh is enabled and configuration changed. Triggering LoadScheduleData for ClientId {ClientId}.", ClientId);
                    // Load data synchronously here. Be cautious if loading can be time-consuming.
                    LoadScheduleData();
                }
                else if (configActuallyChanged)
                {
                    _logger.LogDebug("Configuration changed, but AutoRefresh is disabled or not applicable. Data reload not triggered automatically for ClientId {ClientId}.", ClientId);
                }
            }
        }

        // --- Helper Methods ---

        /// <summary>
        /// Compares two configuration objects to see if properties relevant to data loading (DateRange, ActiveDays) have changed.
        /// </summary>
        /// <param name="oldConfig">The previous configuration object.</param>
        /// <param name="newConfig">The new configuration object.</param>
        /// <returns>True if DateRange or ActiveDays have changed, false otherwise.</returns>
        private bool ConfigurationHasChanged(BitScheduleConfiguration oldConfig, BitScheduleConfiguration newConfig)
        {
            // If both are null, no change.
            if (oldConfig == null && newConfig == null) return false;
            // If one is null and the other isn't, it's a change.
            if (oldConfig == null || newConfig == null) return true;

            // Check if DateRange boundaries have changed.
            if (oldConfig.DateRange == null || newConfig.DateRange == null ||
                !oldConfig.DateRange.StartDate.Equals(newConfig.DateRange.StartDate) ||
                !oldConfig.DateRange.EndDate.Equals(newConfig.DateRange.EndDate))
            {
                return true;
            }

            // Check if ActiveDays array has changed. SequenceEqual checks content and order.
            bool oldDaysNull = oldConfig.ActiveDays == null || oldConfig.ActiveDays.Length == 0;
            bool newDaysNull = newConfig.ActiveDays == null || newConfig.ActiveDays.Length == 0;

            if (oldDaysNull != newDaysNull) return true; // Change if one is null/empty and other isn't
            if (!oldDaysNull && !newConfig.ActiveDays.SequenceEqual(oldConfig.ActiveDays)) return true; // Change if content/order differs

            // If none of the above conditions met, the relevant parts haven't changed.
            return false;
        }

        // --- Event Handlers ---

        /// <summary>
        /// Handles the PropertyChanged event raised by the associated BitScheduleConfiguration object.
        /// Logs the change, sets the IsDirty flag, and triggers a data reload if required by AutoRefresh settings.
        /// </summary>
        /// <param name="sender">The configuration object that raised the event.</param>
        /// <param name="e">Event arguments containing the name of the changed property.</param>
        protected void OnConfigurationChanged(object sender, PropertyChangedEventArgs e)
        {
            _logger.LogDebug("Configuration property changed: {PropertyName} for ClientId {ClientId}. Setting IsDirty=true.", e.PropertyName, ClientId);
            // Always mark the schedule as potentially dirty when any configuration property changes.
            IsDirty = true;

            // Determine if the specific property change necessitates reloading the data
            bool requiresReload = e.PropertyName == nameof(BitScheduleConfiguration.DateRange) ||
                                  e.PropertyName == nameof(BitScheduleConfiguration.ActiveDays);

            // Reload data only if auto-refresh is enabled AND the change affects the data scope.
            if (requiresReload && _configuration != null && _configuration.AutoRefreshOnConfigurationChange)
            {
                _logger.LogInformation("AutoRefresh enabled and relevant property {PropertyName} changed. Triggering LoadScheduleData for ClientId {ClientId}.", e.PropertyName, ClientId);
                LoadScheduleData();
            }
            else if (requiresReload)
            {
                _logger.LogDebug("Relevant configuration property {PropertyName} changed, but AutoRefresh is disabled. Data reload not triggered automatically for ClientId {ClientId}.", e.PropertyName, ClientId);
            }
        }

        // --- Constructors ---

        /// <summary>
        /// Initializes a new instance of the BitSchedule class with dependency injection.
        /// Requires ClientId, Configuration, DataService, DbContext, and Logger.
        /// Logs the initialization process and loads initial data.
        /// </summary>
        /// <param name="clientId">The ID of the client whose schedule this instance manages.</param>
        /// <param name="configuration">The schedule configuration defining the data scope.</param>
        /// <param name="dataService">The service used to load data from the persistent store.</param>
        /// <param name="dbContext">The DbContext used to save changes to the persistent store.</param>
        /// <param name="logger">The ILogger instance for logging.</param>
        public BitSchedule(int clientId, BitScheduleConfiguration configuration, BitScheduleDataService dataService, BitScheduleDbContext dbContext, ILogger<BitSchedule> logger)
        {
            // Validate and store injected dependencies
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            this._configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _logger.LogInformation("Initializing BitSchedule instance for ClientId {ClientId}...", clientId);

            // Initialize the in-memory data store
            _scheduleData = new Dictionary<DateTime, BitDay>();

            // Set the operational context
            this.ClientId = clientId;

            // Subscribe to configuration changes
            this._configuration.PropertyChanged += OnConfigurationChanged;
            _logger.LogTrace("Subscribed to PropertyChanged event of the initial configuration.");

            // Perform the initial data load
            _logger.LogDebug("Performing initial data load for ClientId {ClientId}.", clientId);
            LoadScheduleData(); // Load initial data
            _logger.LogInformation("BitSchedule instance for ClientId {ClientId} initialized.", clientId);
        }

        // --- Data Loading ---

        /// <summary>
        /// Loads or reloads the schedule data from the persistent store using the `_dataService`.
        /// It uses the current `_configuration` (DateRange) and `ClientId` to fetch the relevant `BitDay` objects.
        /// The internal `_scheduleData` dictionary is then populated with the loaded data.
        /// Updates `LastRefreshed` timestamp and resets the `IsDirty` flag on success.
        /// Logs the process and any errors encountered.
        /// </summary>
        public void LoadScheduleData()
        {
            _logger.LogInformation("Attempting to load schedule data for ClientId {ClientId}.", ClientId);

            // Cannot load data without configuration or the data service.
            if (_configuration == null || _dataService == null || _configuration.DateRange == null)
            {
                _logger.LogWarning("Cannot load schedule data for ClientId {ClientId}: Configuration, DateRange, or DataService is null.", ClientId);
                // Ensure data is cleared and state reflects inability to load
                _scheduleData = new Dictionary<DateTime, BitDay>();
                LastRefreshed = DateTime.Now; // Still update refresh time to indicate an attempt was made
                IsDirty = false; // Reset dirty flag, although data is empty/stale
                return;
            }

            _logger.LogDebug("Loading data for ClientId {ClientId} with DateRange {StartDate} to {EndDate}.",
                ClientId, _configuration.DateRange.StartDate, _configuration.DateRange.EndDate);

            try
            {
                // Call the data service to retrieve BitDay records
                List<BitDay> loadedDays = _dataService.LoadScheduleData(_configuration, this.ClientId);

                // Re-populate the internal dictionary using Date.Date as the key.
                _scheduleData = loadedDays.ToDictionary(d => d.Date.Date);

                // Update state to reflect successful load
                LastRefreshed = DateTime.Now;
                IsDirty = false;

                _logger.LogInformation("Successfully loaded {Count} BitDay records for ClientId {ClientId}. LastRefreshed: {LastRefreshed}, IsDirty: {IsDirty}.",
                    _scheduleData.Count, ClientId, LastRefreshed, IsDirty);
            }
            catch (Exception ex)
            {
                // Log the error details using the structured logging exception overload.
                _logger.LogError(ex, "Error loading schedule data for ClientId {ClientId}. Configuration: {@Configuration}", ClientId, _configuration);
                _scheduleData = new Dictionary<DateTime, BitDay>(); // Clear potentially corrupt data
                IsDirty = true; // Mark as dirty because the load failed
                // Depending on requirements, this exception might need to be propagated.
            }
        }

        // --- Write Operations ---

        /// <summary>
        /// Writes (updates or creates) schedule information for a single day based on the request,
        /// and persists the change asynchronously to the database via the DbContext.
        /// Logs the steps and outcomes of the operation.
        /// </summary>
        /// <param name="request">A BitDayRequest containing the Date, StartTime, and EndTime to reserve.</param>
        /// <returns>A Task representing the asynchronous operation, yielding the updated or newly created BitDay object.</returns>
        /// <exception cref="DbUpdateException">Thrown if saving changes to the database fails.</exception>
        public async Task<BitDay> WriteDayAsync(BitDayRequest request)
        {
            DateTime targetDate = request.Date.Date;
            _logger.LogInformation("Attempting WriteDayAsync for ClientId {ClientId}, Date {TargetDate}, Time {StartTime} - {EndTime}.",
                ClientId, targetDate.ToShortDateString(), request.StartTime, request.EndTime);

            BitDay day;
            bool isNew = false; // Flag to track if we created a new BitDay entity

            // 1. Find or Create the BitDay object
            // Try to get the day from the in-memory dictionary first for efficiency.
            if (!_scheduleData.TryGetValue(targetDate, out day))
            {
                _logger.LogDebug("BitDay for Date {TargetDate} not found in memory cache for ClientId {ClientId}. Checking database or creating new.", targetDate.ToShortDateString(), ClientId);
                // Optional: If not in memory, could check the database directly if needed.
                // day = await _dbContext.BitDays.FirstOrDefaultAsync(d => d.Date == targetDate && d.ClientId == this.ClientId);
                // _logger.LogDebug("Database check for BitDay {TargetDate} returned: {Exists}", targetDate.ToShortDateString(), day != null);

                // If it doesn't exist in memory (and optionally wasn't found in DB), create a new one.
                if (day == null)
                {
                    _logger.LogDebug("Creating new BitDay for Date {TargetDate}, ClientId {ClientId}.", targetDate.ToShortDateString(), ClientId);
                    day = new BitDay(targetDate) { ClientId = this.ClientId }; // Assign the current ClientId
                    _scheduleData.Add(targetDate, day); // Add to the in-memory dictionary
                    _dbContext.BitDays.Add(day);        // Add to the DbContext's tracking; state will be 'Added'
                    isNew = true;
                }
                else
                {
                    _logger.LogDebug("BitDay for Date {TargetDate} found in database, adding to memory cache for ClientId {ClientId}.", targetDate.ToShortDateString(), ClientId);
                    // If found in DB but not memory, add it to the in-memory dictionary
                    _scheduleData.Add(targetDate, day);
                    // EF Core should start tracking it if it wasn't already.
                }
            }
            else
            {
                _logger.LogTrace("BitDay for Date {TargetDate} found in memory cache for ClientId {ClientId}.", targetDate.ToShortDateString(), ClientId);
            }

            // 2. Apply the Reservation
            // Convert request times to internal block representation
            int startBlock = BitDay.TimeToBlockIndex(request.StartTime);
            int length = (int)((request.EndTime - request.StartTime).TotalMinutes / 15); // Calculate slot length
            _logger.LogDebug("Attempting to reserve range: StartBlock {StartBlock}, Length {Length} on BitDay {TargetDate}.", startBlock, length, targetDate.ToShortDateString());

            // Attempt to reserve the time range on the BitDay object.
            bool reserved = day.ReserveRange(startBlock, length);
            _logger.LogInformation("ReserveRange result for Date {TargetDate}, Block {StartBlock}, Length {Length}: {Reserved}", targetDate.ToShortDateString(), startBlock, length, reserved);


            // 3. Persist Changes
            // Save changes if the reservation was successful OR if it's a brand new day
            if (reserved || isNew)
            {
                _logger.LogDebug("Proceeding to save changes for BitDay {TargetDate} (IsNew: {IsNew}, Reserved: {Reserved}).", targetDate.ToShortDateString(), isNew, reserved);
                // If the entity existed before (wasn't new), explicitly mark it as Modified
                if (!isNew)
                {
                    _logger.LogTrace("Marking existing BitDay {TargetDate} as Modified in DbContext.", targetDate.ToShortDateString());
                    _dbContext.Entry(day).State = EntityState.Modified;
                }
                else
                {
                    _logger.LogTrace("New BitDay {TargetDate} is already marked as Added in DbContext.", targetDate.ToShortDateString());
                }

                try
                {
                    _logger.LogInformation("Calling SaveChangesAsync for BitDay {TargetDate}, ClientId {ClientId}.", targetDate.ToShortDateString(), ClientId);
                    // Asynchronously save all tracked changes to the database.
                    int changes = await _dbContext.SaveChangesAsync();
                    _logger.LogInformation("SaveChangesAsync completed for BitDay {TargetDate}. {Changes} records affected.", targetDate.ToShortDateString(), changes);
                }
                catch (DbUpdateException ex)
                {
                    // Log the error with details
                    _logger.LogError(ex, "Error saving BitDay via DbUpdateException for Date {TargetDate}, ClientId {ClientId}. Inner: {InnerMessage}",
                        targetDate.ToShortDateString(), ClientId, ex.InnerException?.Message);
                    // Consider application-specific error handling (e.g., reverting in-memory changes)
                    throw; // Re-throw the exception so the caller is aware of the failure.
                }
                catch (Exception ex) // Catch other potential exceptions during save
                {
                    _logger.LogError(ex, "Unexpected error saving BitDay for Date {TargetDate}, ClientId {ClientId}.", targetDate.ToShortDateString(), ClientId);
                    throw;
                }
            }
            else
            {
                // Log the case where reservation failed and thus no save was attempted.
                _logger.LogWarning("Reservation failed for Date {TargetDate}, StartBlock {StartBlock}, Length {Length}. No changes will be saved for this request.",
                    targetDate.ToShortDateString(), startBlock, length);
            }

            // Return the (potentially modified or new) BitDay object.
            return day;
        }

        /// <summary>
        /// Writes (reserves) a recurring time block across multiple days specified by the request,
        /// and persists the changes asynchronously to the database.
        /// Applies the reservation only to days within the date range that match the specified ActiveDays.
        /// Logs the process and outcomes.
        /// </summary>
        /// <param name="request">A BitScheduleRequest defining the DateRange, ActiveDays, and TimeBlock to reserve.</param>
        /// <returns>A Task representing the asynchronous operation, yielding true if all applicable reservations and the final save succeeded, false otherwise.</returns>
        public async Task<bool> WriteScheduleAsync(BitScheduleRequest request)
        {
            _logger.LogInformation("Attempting WriteScheduleAsync for ClientId {ClientId} from {StartDate} to {EndDate}, Time {StartTime} - {EndTime}.",
                ClientId, request.DateRange.StartDate.ToShortDateString(), request.DateRange.EndDate.ToShortDateString(), request.TimeBlock.StartTime, request.TimeBlock.EndTime);

            bool allReservationsSucceeded = true; // Track if individual reservations work
            List<BitDay> modifiedDaysForSave = new List<BitDay>(); // Track days successfully modified

            // Use a HashSet for efficient O(1) average lookup of active weekdays.
            HashSet<DayOfWeek> activeDaysSet = null;
            if (request.ActiveDays != null && request.ActiveDays.Length > 0)
            {
                activeDaysSet = new HashSet<DayOfWeek>(request.ActiveDays);
                _logger.LogDebug("ActiveDays filter applied: {@ActiveDays}", request.ActiveDays);
            }

            // Pre-calculate block information from the request's TimeBlock.
            int startBlock = BitDay.TimeToBlockIndex(request.TimeBlock.StartTime);
            int length = (int)((request.TimeBlock.EndTime - request.TimeBlock.StartTime).TotalMinutes / 15);
            DateTime start = request.DateRange.StartDate.Date;
            DateTime end = request.DateRange.EndDate.Date;
            _logger.LogDebug("Calculated reservation range: StartBlock {StartBlock}, Length {Length}.", startBlock, length);

            // 1. Apply Reservations in Memory
            _logger.LogInformation("Iterating through {Count} cached BitDays for ClientId {ClientId} to apply reservations.", _scheduleData.Count, ClientId);
            int consideredDays = 0;
            int successfulReservations = 0;
            int failedReservations = 0;

            // Iterate through the BitDay objects currently loaded in memory.
            foreach (BitDay day in _scheduleData.Values)
            {
                // Skip days outside the requested date range.
                if (day.Date < start || day.Date > end)
                    continue;

                // Skip days that don't match the active weekday criteria (if specified).
                if (activeDaysSet != null && !activeDaysSet.Contains(day.Date.DayOfWeek))
                {
                    continue;
                }

                consideredDays++;
                _logger.LogTrace("Considering BitDay {TargetDate} for reservation.", day.Date.ToShortDateString());

                // Attempt to reserve the time block on the current day.
                if (day.ReserveRange(startBlock, length))
                {
                    _logger.LogTrace("Reservation successful for BitDay {TargetDate}. Marking as modified.", day.Date.ToShortDateString());
                    // If successful, mark the entity as modified for EF Core tracking
                    // and add it to our list of days to be saved.
                    _dbContext.Entry(day).State = EntityState.Modified;
                    modifiedDaysForSave.Add(day);
                    successfulReservations++;
                }
                else
                {
                    // If any reservation fails (e.g., time slot conflict),
                    // mark the overall operation as failed.
                    allReservationsSucceeded = false;
                    failedReservations++;
                    _logger.LogWarning("Reservation failed for BitDay {TargetDate}, StartBlock {StartBlock}, Length {Length}. Operation will not save if any reservation fails.",
                        day.Date.ToShortDateString(), startBlock, length);
                    // Depending on requirements, could break here: // break;
                }
            }
            _logger.LogInformation("Reservation application complete for ClientId {ClientId}. Considered: {ConsideredDays}, Succeeded: {SuccessfulReservations}, Failed: {FailedReservations}. Overall success state: {AllSucceeded}",
                ClientId, consideredDays, successfulReservations, failedReservations, allReservationsSucceeded);


            // 2. Persist Changes (if all reservations succeeded)
            if (allReservationsSucceeded && modifiedDaysForSave.Count > 0)
            {
                _logger.LogInformation("All reservations succeeded. Attempting to save {Count} modified BitDays for ClientId {ClientId}.", modifiedDaysForSave.Count, ClientId);
                try
                {
                    // Asynchronously save all tracked changes to the database.
                    int changes = await _dbContext.SaveChangesAsync();
                    _logger.LogInformation("SaveChangesAsync completed successfully for bulk update. {Changes} records affected for ClientId {ClientId}.", changes, ClientId);
                    return true; // Operation succeeded fully
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Error saving bulk schedule update via DbUpdateException for ClientId {ClientId}. Inner: {InnerMessage}",
                        ClientId, ex.InnerException?.Message);
                    // If save fails, the operation ultimately failed.
                    return false;
                }
                catch (Exception ex) // Catch other potential exceptions during save
                {
                    _logger.LogError(ex, "Unexpected error saving bulk schedule update for ClientId {ClientId}.", ClientId);
                    return false;
                }
            }
            else
            {
                // Log why saving is skipped.
                if (!allReservationsSucceeded)
                {
                    _logger.LogWarning("Saving skipped because one or more reservations failed for ClientId {ClientId}. Reverting tracked changes.", ClientId);
                    // Revert the state of entities marked as Modified if the overall operation failed
                    // to prevent accidental saving later. Needs careful handling with DbContext lifetime.
                    foreach (var modifiedDay in modifiedDaysForSave)
                    {
                        var entry = _dbContext.Entry(modifiedDay);
                        if (entry.State == EntityState.Modified)
                        {
                            entry.State = EntityState.Unchanged; // Revert tracking state
                        }
                    }
                }
                else if (modifiedDaysForSave.Count == 0)
                {
                    _logger.LogInformation("No BitDays were modified or needed reservation. No changes to save for ClientId {ClientId}.", ClientId);
                }
                // Return false if reservations failed or true if they succeeded but nothing needed saving.
                // Let's return true only if everything (including potential save) worked.
                return allReservationsSucceeded && modifiedDaysForSave.Count == 0; // True only if no failures and nothing needed saving
            }
        }

        // --- Read Operations ---

        /// <summary>
        /// Reads (retrieves) the schedule information for a single specific day from the internal data store.
        /// Performs an efficient lookup using the in-memory dictionary. Logs the lookup attempt.
        /// </summary>
        /// <param name="date">The date for which to retrieve the schedule.</param>
        /// <returns>
        /// The BitDay object for the specified date if found in the loaded data.
        /// Otherwise, returns a new, empty (free) BitDay object for that date, associated with the current ClientId.
        /// </returns>
        public BitDay ReadDay(DateTime date)
        {
            DateTime targetDate = date.Date;
            _logger.LogDebug("Attempting ReadDay for ClientId {ClientId}, Date {TargetDate}.", ClientId, targetDate.ToShortDateString());

            // Use the dictionary's TryGetValue for efficient O(1) average time lookup.
            if (_scheduleData.TryGetValue(targetDate, out var day))
            {
                _logger.LogTrace("BitDay found in memory cache for Date {TargetDate}.", targetDate.ToShortDateString());
                // Found the day in our in-memory data
                return day;
            }
            else
            {
                _logger.LogDebug("BitDay not found in memory cache for Date {TargetDate}. Returning new default BitDay.", targetDate.ToShortDateString());
                // The day was not found in the data loaded according to the current configuration.
                // Return a new, default (free) BitDay object for that date.
                return new BitDay(targetDate) { ClientId = this.ClientId };
            }
        }

        /// <summary>
        /// Reads (retrieves) schedule data based on the criteria in the request,
        /// filtering the in-memory data store. Groups the results by month.
        /// Uses a HashSet for efficient filtering by DayOfWeek. Logs the operation details.
        /// </summary>
        /// <param name="request">A BitScheduleRequest containing the DateRange and ActiveDays to filter by.</param>
        /// <returns>A BitScheduleResponse containing the original request and a list of BitMonth objects.</returns>
        public BitScheduleResponse ReadSchedule(BitScheduleRequest request)
        {
            _logger.LogInformation("Attempting ReadSchedule for ClientId {ClientId} with Request: {@Request}", ClientId, request);

            // Prepare a HashSet for efficient DayOfWeek lookups if criteria are provided.
            HashSet<DayOfWeek> activeDaysSet = null;
            if (request.ActiveDays != null && request.ActiveDays.Length > 0)
            {
                activeDaysSet = new HashSet<DayOfWeek>(request.ActiveDays); // O(M) to create
                _logger.LogDebug("Applying ActiveDays filter: {@ActiveDaysSet}", activeDaysSet);
            }

            // Temporary list to hold days matching the criteria
            List<BitDay> filteredDays = new List<BitDay>();
            DateTime start = request.DateRange.StartDate.Date;
            DateTime end = request.DateRange.EndDate.Date;

            // Iterate through all the BitDay objects currently held in the memory dictionary. O(N).
            _logger.LogDebug("Filtering {Count} cached BitDays based on DateRange and ActiveDays.", _scheduleData.Count);
            foreach (BitDay day in _scheduleData.Values)
            {
                // Filter by DateRange
                if (day.Date < start || day.Date > end)
                    continue;

                // Filter by ActiveDays (if criteria exists) using the efficient HashSet lookup O(1) average.
                if (activeDaysSet != null && !activeDaysSet.Contains(day.Date.DayOfWeek))
                {
                    continue;
                }

                // If the day passes all filters, add it to our results list.
                filteredDays.Add(day);
            }
            _logger.LogInformation("Found {Count} BitDays matching filter criteria for ClientId {ClientId}.", filteredDays.Count, ClientId);


            // Group the filtered days by Year and Month using LINQ.
            _logger.LogDebug("Grouping {Count} filtered days by Year and Month.", filteredDays.Count);
            var monthGroups = filteredDays
                .GroupBy(d => new { d.Date.Year, d.Date.Month }) // Group by a composite key
                .OrderBy(g => g.Key.Year)                       // Order chronologically
                .ThenBy(g => g.Key.Month);

            // Prepare the list of BitMonth objects for the response.
            List<BitMonth> months = new List<BitMonth>();

            // Convert each group (representing a month) into a BitMonth object.
            foreach (var group in monthGroups)
            {
                _logger.LogTrace("Creating BitMonth for Year {Year}, Month {Month} with {Count} days.", group.Key.Year, group.Key.Month, group.Count());
                BitMonth month = BitMonth.CreateFromDays(group.ToList());
                months.Add(month);
            }

            // Construct and return the final response object.
            var response = new BitScheduleResponse
            {
                Request = request,            // Include the original request for context
                ScheduledMonths = months      // Include the list of populated BitMonth objects
            };
            _logger.LogInformation("ReadSchedule completed for ClientId {ClientId}. Returning {MonthCount} months.", ClientId, response.ScheduledMonths.Count);
            return response;
        }
    }
}