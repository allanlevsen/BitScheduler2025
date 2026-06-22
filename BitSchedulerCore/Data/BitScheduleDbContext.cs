namespace BitSchedulerCore.Data
{
    using Microsoft.EntityFrameworkCore.ChangeTracking;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
    using BitSchedulerCore;

    namespace BitTimeScheduler.Data
    {
        public class BitScheduleDbContext : DbContext
        {
            private static readonly ValueConverter<UInt128, byte[]> UInt128ToBytesConverter = new(
                value => ConvertUInt128ToBytes(value, 12),
                value => ConvertBytesToUInt128(value));

            private static readonly ValueComparer<UInt128> UInt128Comparer = new(
                (left, right) => left == right,
                value => value.GetHashCode(),
                value => value);

            private static readonly ValueConverter<uint, long> UInt32ToInt64Converter = new(
                value => value,
                value => checked((uint)value));

            public BitScheduleDbContext(DbContextOptions<BitScheduleDbContext> options)
                : base(options)
            {
            }

            public DbSet<BitDay> BitDays { get; set; }
            public DbSet<BitReservation> BitReservations { get; set; }
            public DbSet<BitEvent> BitEvents { get; set; }
            public DbSet<BitResourceType> BitResourceTypes { get; set; }
            public DbSet<BitResource> BitResources { get; set; }
            public DbSet<BitClient> BitClients { get; set; }
            public DbSet<BitResourceScheduleRange> BitResourceScheduleRanges { get; set; }
            public DbSet<HexGridVersion> HexGridVersions { get; set; }
            public DbSet<HexGridCell> HexGridCells { get; set; }
            public DbSet<HexGridCellVertex> HexGridCellVertices { get; set; }
            public DbSet<HexGridNeighbor> HexGridNeighbors { get; set; }
            public DbSet<HexGridSearchRing> HexGridSearchRings { get; set; }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                // Configure BitClient.
                modelBuilder.Entity<BitClient>(entity =>
                {
                    entity.HasKey(e => e.BitClientId);
                    entity.Property(e => e.Name)
                          .IsRequired()
                          .HasMaxLength(200);

                    // One client has many resources.
                    entity.HasMany(e => e.BitResources)
                          .WithOne(r => r.BitClient)
                          .HasForeignKey(r => r.BitClientId)
                          .OnDelete(DeleteBehavior.Cascade);

                    // One client has many reservations.
                    entity.HasMany(e => e.BitReservations)
                          .WithOne(r => r.BitClient)
                          .HasForeignKey(r => r.BitClientId)
                          .OnDelete(DeleteBehavior.Cascade);

                    entity.HasMany(e => e.BitEvents)
                          .WithOne(r => r.BitClient)
                          .HasForeignKey(r => r.BitClientId)
                          .OnDelete(DeleteBehavior.Cascade);

                    entity.HasMany(e => e.BitResourceScheduleRanges)
                          .WithOne(r => r.BitClient)
                          .HasForeignKey(r => r.BitClientId)
                          .OnDelete(DeleteBehavior.Cascade);
                });

                // Configure BitResourceType.
                modelBuilder.Entity<BitResourceType>(entity =>
                {
                    entity.HasKey(e => e.BitResourceTypeId);
                    entity.Property(e => e.Name)
                          .IsRequired()
                          .HasMaxLength(100);

                    // One resource type has many resources.
                    entity.HasMany(e => e.BitResources)
                          .WithOne(r => r.BitResourceType)
                          .HasForeignKey(r => r.BitResourceTypeId)
                          .OnDelete(DeleteBehavior.Restrict);
                });

                // Configure BitResource.
                modelBuilder.Entity<BitResource>(entity =>
                {
                    entity.HasKey(e => e.BitResourceId);
                    entity.Property(e => e.FirstName)
                          .IsRequired()
                          .HasMaxLength(50);
                    entity.Property(e => e.LastName)
                          .IsRequired()
                          .HasMaxLength(50);
                    entity.Property(e => e.EmailAddress)
                          .IsRequired()
                          .HasMaxLength(100);

                    entity.HasMany(e => e.BitResourceScheduleRanges)
                          .WithOne(r => r.BitResource)
                          .HasForeignKey(r => r.BitResourceId)
                          .OnDelete(DeleteBehavior.Cascade);

                    entity.HasMany(e => e.BitEvents)
                          .WithOne(r => r.BitResource)
                          .HasForeignKey(r => r.BitResourceId)
                          .OnDelete(DeleteBehavior.Cascade);
                });

                // Configure BitReservation.
                modelBuilder.Entity<BitReservation>(entity =>
                {
                    entity.HasKey(e => e.BitReservationId);

                    entity.Property(e => e.BitClientId)
                          .IsRequired();

                    entity.Property(e => e.Date)
                          .HasColumnType("date")
                          .IsRequired();

                    entity.Property(e => e.BitResourceId)
                          .IsRequired();

                    entity.Property(e => e.StartBlock)
                          .IsRequired();

                    entity.Property(e => e.SlotLength)
                          .IsRequired();

                    // Relationship: BitReservation -> BitClient.
                    entity.HasOne(e => e.BitClient)
                          .WithMany(c => c.BitReservations)
                          .HasForeignKey(e => e.BitClientId)
                          .OnDelete(DeleteBehavior.Cascade);

                    // Relationship: BitReservation -> BitResource.
                    entity.HasOne(e => e.BitResource)
                          .WithMany(r => r.BitReservations)
                          .HasForeignKey(e => e.BitResourceId)
                          .OnDelete(DeleteBehavior.Cascade);
                });

                modelBuilder.Entity<BitEvent>(entity =>
                {
                    entity.HasKey(e => e.BitEventId);

                    entity.Property(e => e.StartDateTime)
                          .HasColumnType("timestamp with time zone")
                          .IsRequired();

                    entity.Property(e => e.EndDateTime)
                          .HasColumnType("timestamp with time zone")
                          .IsRequired();

                    entity.Property(e => e.StartAddress)
                          .HasMaxLength(500);

                    entity.Property(e => e.EndAddress)
                          .HasMaxLength(500);

                    entity.Property(e => e.EventType)
                          .HasMaxLength(100);

                    entity.Property(e => e.CreatedBy)
                          .IsRequired()
                          .HasMaxLength(200);

                    entity.Property(e => e.UpdatedBy)
                          .IsRequired()
                          .HasMaxLength(200);

                    entity.HasIndex(e => new { e.BitClientId, e.BitResourceId, e.StartDateTime, e.EndDateTime });
                    entity.HasIndex(e => new { e.BitResourceId, e.StartDateTime });
                    entity.HasIndex(e => new { e.BitClientId, e.EventType, e.StartDateTime });
                });

                // (Assume your BitDay configuration remains unchanged.)
                modelBuilder.Entity<BitDay>(entity =>
                {
                    entity.HasKey(e => e.BitDayId);
                    entity.Ignore(e => e.Bits);
                    entity.Ignore(e => e.IsFree);

                    // For BitDay, you might have a unique constraint for a given client and date.
                    entity.HasIndex(e => new { e.ClientId, e.Date }).IsUnique();

                    entity.Property(e => e.Date)
                          .HasColumnType("date")
                          .IsRequired();

                    entity.Property(e => e.DayData)
                          .HasConversion(UInt128ToBytesConverter)
                          .Metadata.SetValueComparer(UInt128Comparer);

                    entity.Property(e => e.DayData)
                          .HasColumnType("bytea")
                          .IsRequired();

                    entity.Property(e => e.Metadata)
                          .HasConversion(UInt32ToInt64Converter)
                          .HasColumnType("bigint")
                          .IsRequired();
                });

                modelBuilder.Entity<BitResourceScheduleRange>(entity =>
                {
                    entity.HasKey(e => e.BitResourceScheduleRangeId);

                    entity.Property(e => e.BitClientId)
                          .IsRequired();

                    entity.Property(e => e.BitResourceId)
                          .IsRequired();

                    entity.Property(e => e.StartDate)
                          .HasColumnType("date")
                          .IsRequired();

                    entity.Property(e => e.EndDate)
                          .HasColumnType("date")
                          .IsRequired();

                    entity.Property(e => e.Payload)
                          .HasColumnType("bytea")
                          .IsRequired();

                    entity.Property(e => e.CreatedBy)
                          .IsRequired();

                    entity.Property(e => e.CreatedDate)
                          .IsRequired();

                    entity.Property(e => e.UpdatedBy)
                          .IsRequired();

                    entity.HasIndex(e => new { e.BitResourceId, e.StartDate, e.EndDate });
                    entity.HasIndex(e => new { e.BitClientId, e.BitResourceId, e.StartDate, e.EndDate }).IsUnique();
                });

                modelBuilder.Entity<HexGridVersion>(entity =>
                {
                    entity.HasKey(e => e.Id);

                    entity.Property(e => e.AreaName)
                          .IsRequired()
                          .HasMaxLength(100);

                    entity.Property(e => e.Name)
                          .IsRequired()
                          .HasMaxLength(200);

                    entity.Property(e => e.CreatedUtc)
                          .IsRequired();

                    entity.HasIndex(e => new { e.AreaName, e.IsActive });
                });

                modelBuilder.Entity<HexGridCell>(entity =>
                {
                    entity.HasKey(e => e.Id);

                    entity.Property(e => e.AreaName)
                          .HasMaxLength(100);

                    entity.Property(e => e.CreatedUtc)
                          .IsRequired();

                    entity.HasOne(e => e.HexGridVersion)
                          .WithMany(v => v.Cells)
                          .HasForeignKey(e => e.HexGridVersionId)
                          .OnDelete(DeleteBehavior.Restrict);

                    entity.HasIndex(e => new { e.HexGridVersionId, e.Q, e.R }).IsUnique();
                    entity.HasIndex(e => e.Id);
                    entity.HasIndex(e => e.IsActive);
                    entity.HasIndex(e => new { e.AreaName, e.IsActive });
                });

                modelBuilder.Entity<HexGridCellVertex>(entity =>
                {
                    entity.HasKey(e => e.Id);

                    entity.HasOne(e => e.HexGridCell)
                          .WithMany(c => c.Vertices)
                          .HasForeignKey(e => e.HexGridCellId)
                          .OnDelete(DeleteBehavior.Cascade);

                    entity.HasIndex(e => new { e.HexGridCellId, e.VertexOrder }).IsUnique();
                });

                modelBuilder.Entity<HexGridNeighbor>(entity =>
                {
                    entity.HasKey(e => e.Id);

                    entity.Property(e => e.Direction)
                          .HasConversion<int>()
                          .IsRequired();

                    entity.HasOne(e => e.HexGridCell)
                          .WithMany(c => c.Neighbors)
                          .HasForeignKey(e => e.HexGridCellId)
                          .OnDelete(DeleteBehavior.Cascade);

                    entity.HasOne(e => e.NeighborHexGridCell)
                          .WithMany(c => c.NeighborOf)
                          .HasForeignKey(e => e.NeighborHexGridCellId)
                          .OnDelete(DeleteBehavior.Restrict);

                    entity.HasIndex(e => new { e.HexGridCellId, e.Direction }).IsUnique();
                    entity.HasIndex(e => e.NeighborHexGridCellId);
                });

                modelBuilder.Entity<HexGridSearchRing>(entity =>
                {
                    entity.HasKey(e => e.Id);

                    entity.HasOne(e => e.HexGridCell)
                          .WithMany(c => c.SearchRings)
                          .HasForeignKey(e => e.HexGridCellId)
                          .OnDelete(DeleteBehavior.Cascade);

                    entity.HasOne(e => e.NearbyHexGridCell)
                          .WithMany(c => c.NearbySearchRings)
                          .HasForeignKey(e => e.NearbyHexGridCellId)
                          .OnDelete(DeleteBehavior.Restrict);

                    entity.HasIndex(e => new { e.HexGridCellId, e.NearbyHexGridCellId }).IsUnique();
                    entity.HasIndex(e => new { e.HexGridCellId, e.RingDistance });
                });

                base.OnModelCreating(modelBuilder);
            }

            private static byte[] ConvertUInt128ToBytes(UInt128 value, int byteCount)
            {
                var bytes = new byte[byteCount];
                for (var index = 0; index < byteCount; index++)
                {
                    bytes[index] = (byte)(value >> (index * 8));
                }

                return bytes;
            }

            private static UInt128 ConvertBytesToUInt128(byte[] bytes)
            {
                UInt128 value = 0;
                for (var index = 0; index < bytes.Length; index++)
                {
                    value |= (UInt128)bytes[index] << (index * 8);
                }

                return value;
            }
        }
    }
}
