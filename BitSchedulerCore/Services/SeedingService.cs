using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BitSchedulerCore.Data.BitTimeScheduler.Data;
using BitSchedulerCore;
using BitTimeScheduler.Data;
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
    }
}

