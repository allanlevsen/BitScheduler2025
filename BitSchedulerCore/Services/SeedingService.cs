using BitSchedulerCore.Data.BitTimeScheduler.Data;
using BitSchedulerCore;
using BitTimeScheduler.Models;
using Microsoft.EntityFrameworkCore;

namespace BitTimeScheduler.Services
{
    public class SeedingService
    {
        private readonly BitScheduleDbContext _dbContext;

        public SeedingService(BitScheduleDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task SeedAsync()
        {
            // 1. Seed BitResourceType
            if (!await _dbContext.BitResourceTypes.AnyAsync())
            {
                var resourceTypes = new List<BitResourceType>
                {
                    new BitResourceType { Name = "Person" },
                    new BitResourceType { Name = "Equipment" },
                    new BitResourceType { Name = "Meeting Room" }
                };

                _dbContext.BitResourceTypes.AddRange(resourceTypes);
                await _dbContext.SaveChangesAsync();
            }

            // 2. Seed BitClient with some sample companies.
            if (!await _dbContext.BitClients.AnyAsync())
            {
                var clients = new List<BitClient>
                {
                    new BitClient { Name = "Acme Corp" },
                    new BitClient { Name = "Globex Inc" },
                    new BitClient { Name = "Initech" },
                    new BitClient { Name = "Umbrella Corp" }
                };

                _dbContext.BitClients.AddRange(clients);
                await _dbContext.SaveChangesAsync();
            }

            // 3. Seed BitResource with 5 people and 5 equipment.
            if (!await _dbContext.BitResources.AnyAsync())
            {
                // Get the "Person" and "Equipment" resource types.
                var personType = await _dbContext.BitResourceTypes.FirstOrDefaultAsync(rt => rt.Name == "Person");
                var equipmentType = await _dbContext.BitResourceTypes.FirstOrDefaultAsync(rt => rt.Name == "Equipment");

                // For simplicity, choose the first client for assignment.
                var client = await _dbContext.BitClients.FirstOrDefaultAsync();

                var resources = new List<BitResource>();

                // 5 People.
                resources.Add(new BitResource
                {
                    FirstName = "John",
                    LastName = "Doe",
                    EmailAddress = "john.doe@example.com",
                    BitResourceTypeId = personType.BitResourceTypeId,
                    BitClientId = client.BitClientId
                });
                resources.Add(new BitResource
                {
                    FirstName = "Jane",
                    LastName = "Smith",
                    EmailAddress = "jane.smith@example.com",
                    BitResourceTypeId = personType.BitResourceTypeId,
                    BitClientId = client.BitClientId
                });
                resources.Add(new BitResource
                {
                    FirstName = "Alice",
                    LastName = "Johnson",
                    EmailAddress = "alice.johnson@example.com",
                    BitResourceTypeId = personType.BitResourceTypeId,
                    BitClientId = client.BitClientId
                });
                resources.Add(new BitResource
                {
                    FirstName = "Bob",
                    LastName = "Brown",
                    EmailAddress = "bob.brown@example.com",
                    BitResourceTypeId = personType.BitResourceTypeId,
                    BitClientId = client.BitClientId
                });
                resources.Add(new BitResource
                {
                    FirstName = "Charlie",
                    LastName = "Davis",
                    EmailAddress = "charlie.davis@example.com",
                    BitResourceTypeId = personType.BitResourceTypeId,
                    BitClientId = client.BitClientId
                });

                // 5 Equipment.
                resources.Add(new BitResource
                {
                    FirstName = "Projector",
                    LastName = "",
                    EmailAddress = "",
                    BitResourceTypeId = equipmentType.BitResourceTypeId,
                    BitClientId = client.BitClientId
                });
                resources.Add(new BitResource
                {
                    FirstName = "Laptop",
                    LastName = "",
                    EmailAddress = "",
                    BitResourceTypeId = equipmentType.BitResourceTypeId,
                    BitClientId = client.BitClientId
                });
                resources.Add(new BitResource
                {
                    FirstName = "Scanner",
                    LastName = "",
                    EmailAddress = "",
                    BitResourceTypeId = equipmentType.BitResourceTypeId,
                    BitClientId = client.BitClientId
                });
                resources.Add(new BitResource
                {
                    FirstName = "Printer",
                    LastName = "",
                    EmailAddress = "",
                    BitResourceTypeId = equipmentType.BitResourceTypeId,
                    BitClientId = client.BitClientId
                });
                resources.Add(new BitResource
                {
                    FirstName = "Tablet",
                    LastName = "",
                    EmailAddress = "",
                    BitResourceTypeId = equipmentType.BitResourceTypeId,
                    BitClientId = client.BitClientId
                });

                _dbContext.BitResources.AddRange(resources);
                await _dbContext.SaveChangesAsync();
            }
        }


        public DateTime LastDayOfMonth(DateTime date)
        {
            // Get the number of days in the month for the given year.
            int lastDay = DateTime.DaysInMonth(date.Year, date.Month);

            // Return a new DateTime with the same year and month, and the last day.
            return new DateTime(date.Year, date.Month, lastDay);
        }

        /// <summary>
        /// Retrieves a list of client IDs based on the provided clientId parameter.
        /// If clientId is greater than 0, the method returns a list containing just that clientId.
        /// If clientId is 0, the method queries the BitClients table from the database
        /// and returns a list of all BitClientId values.
        /// </summary>
        /// <param name="clientId">An integer representing a specific client ID, or 0 to indicate all clients.</param>
        /// <returns>A Task that resolves to a List&lt;int&gt; containing the client IDs.</returns>
        public async Task<List<int>> GetClientIdsAsync(int clientId)
        {
            if (clientId > 0)
            {
                // If clientId is greater than 0, return a list containing just that clientId.
                return new List<int> { clientId };
            }
            else
            {
                // Otherwise, read from the BitClients table and return all BitClientId values.
                return await _dbContext.BitClients
                    .Select(c => c.BitClientId)
                    .ToListAsync();
            }
        }

        /// <summary>
        /// Seeds schedule data for the month specified in the configuration.
        /// Checks if any BitDay records exist for the given month (using the DateRange in the configuration).
        /// If the month is empty, it calls the MockData service to generate mock data and then saves it to the database.
        /// </summary>
        /// <param name="config">The BitScheduleConfiguration that defines the month to seed.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public async Task SeedScheduleDataAsync(int clientId = 0)
        {
            // if paramter clientId = 0, then the data will be seeded for all clients
            List<int> clientIds = await GetClientIdsAsync(clientId);

            foreach (int currentClientId in clientIds)
            {
                // setup start and end date variables
                //
                DateTime today = DateTime.Today;
                int totalMonthsToGenerate = 6;

                for (int i = 0; i < totalMonthsToGenerate; i++)
                {
                    // Add i months to the first day of the current month.
                    DateTime sDate = new DateTime(today.Year, today.Month, 1).AddMonths(i);
                    DateTime eDate = LastDayOfMonth(sDate);

                    // Create a BitScheduleConfiguration for August 2025.
                    BitScheduleConfiguration config = new BitScheduleConfiguration
                    {
                        DateRange = new BitDateRange
                        {
                            StartDate = sDate,
                            EndDate = eDate
                        },
                        ActiveDays = new DayOfWeek[] { DayOfWeek.Sunday, DayOfWeek.Friday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday },
                        TimeBlock = new BitTimeRange() // not used during seeding.. random blocks generated
                    };

                    if (config == null)
                        throw new ArgumentNullException(nameof(config));

                    // Check if any BitDay records exist for this month.
                    bool dataExists = await _dbContext.BitDays
                        .AnyAsync(d => d.Date >= sDate && d.Date <= eDate && d.ClientId == currentClientId);

                    if (!dataExists)
                    {


                        // No data for the month exists; generate mock data using the provided configuration.
                        List<BitDay> mockDays = LoadMockData(config, currentClientId);

                        // Add the generated BitDay records to the database.
                        await _dbContext.BitDays.AddRangeAsync(mockDays);
                        await _dbContext.SaveChangesAsync();

                        Console.WriteLine($"Seeded {mockDays.Count} BitDay records for the month starting {sDate:yyyy-MM-dd}.");
                    }
                    else
                        Console.WriteLine($"Data already exists for the month starting {sDate:yyyy-MM-dd}. Skipping seeding.");
                }
            }
        }


        /// <summary>
        /// Generates mock schedule data based on the configuration.
        /// For each day in the date range, a new BitDay is created.
        /// If the day is active (i.e. its DayOfWeek is in ActiveDays), a few random reservations are made.
        /// </summary>
        private List<BitDay> LoadMockData(BitScheduleConfiguration config, int clientId)
        {
            List<BitDay> data = new List<BitDay>();
            Random rand = new Random();
            DateTime startDate = config.DateRange.StartDate.Date;
            DateTime endDate = config.DateRange.EndDate.Date;
            for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
            {
                BitDay day = new BitDay(date);
                day.ClientId = clientId;

                if (config.ActiveDays != null && config.ActiveDays.Length > 0)
                {
                    bool isActive = false;
                    foreach (DayOfWeek dow in config.ActiveDays)
                    {
                        if (date.DayOfWeek == dow)
                        {
                            isActive = true;
                            break;
                        }
                    }
                    if (isActive)
                    {
                        // Create between 1 and 3 random reservations.
                        int reservations = rand.Next(1, 4);
                        for (int i = 0; i < reservations; i++)
                        {
                            int startBlock = rand.Next(0, BitDay.TotalSlots - 4);
                            int length = rand.Next(1, 5);
                            day.ReserveRange(startBlock, length);

                        }
                    }
                }
                data.Add(day);
            }
            return data;
        }

    }
}

