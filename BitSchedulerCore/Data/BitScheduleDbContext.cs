namespace BitSchedulerCore.Data
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
    using global::BitTimeScheduler;

    namespace BitTimeScheduler.Data
    {
        public class BitScheduleDbContext : DbContext
        {
            private static readonly ValueConverter<ulong, decimal> ULongToDecimalConverter = new(
                value => Convert.ToDecimal(value),
                value => Convert.ToUInt64(value));

            public BitScheduleDbContext(DbContextOptions<BitScheduleDbContext> options)
                : base(options)
            {
            }

            public DbSet<BitDay> BitDays { get; set; }
            public DbSet<BitReservation> BitReservations { get; set; }
            public DbSet<BitResourceType> BitResourceTypes { get; set; }
            public DbSet<BitResource> BitResources { get; set; }
            public DbSet<BitClient> BitClients { get; set; }
            public DbSet<BitResourceScheduleRange> BitResourceScheduleRanges { get; set; }



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

                // (Assume your BitDay configuration remains unchanged.)
                modelBuilder.Entity<BitDay>(entity =>
                {
                    entity.HasKey(e => e.BitDayId);

                    // For BitDay, you might have a unique constraint for a given client and date.
                    entity.HasIndex(e => new { e.ClientId, e.Date }).IsUnique();

                    entity.Property(e => e.Date)
                          .HasColumnType("date")
                          .IsRequired();

                    // For BitsLow and BitsHigh, you may need a value converter for ulong -> long.
                    // (Assume you already have that set up in your current code.)
                    entity.Property(e => e.BitsLow)
                          .HasConversion(ULongToDecimalConverter)
                          .HasColumnType("numeric(20,0)")
                          .IsRequired();
                    entity.Property(e => e.BitsHigh)
                          .HasConversion(ULongToDecimalConverter)
                          .HasColumnType("numeric(20,0)")
                          .IsRequired();

                    entity.Property(e => e.IsFree)
                          .HasColumnType("boolean")
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

                base.OnModelCreating(modelBuilder);
            }
        }
    }
}
