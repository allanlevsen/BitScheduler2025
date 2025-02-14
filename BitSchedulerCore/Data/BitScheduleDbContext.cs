namespace BitSchedulerCore.Data
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.SqlServer;
    using global::BitTimeScheduler;

    namespace BitTimeScheduler.Data
    {
        public class BitScheduleDbContext : DbContext
        {
            public BitScheduleDbContext(DbContextOptions<BitScheduleDbContext> options)
                : base(options)
            {
            }

            public DbSet<BitDay> BitDays { get; set; }
            public DbSet<BitReservation> BitReservations { get; set; }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                // Configure BitDay.
                modelBuilder.Entity<BitDay>(entity =>
                {
                    // Use BitDayId as the primary key.
                    entity.HasKey(e => e.BitDayId);

                    // Create a unique index on (ClientId, Date) to enforce one day per client.
                    entity.HasIndex(e => new { e.ClientId, e.Date }).IsUnique();

                    entity.Property(e => e.Date)
                          .HasColumnType("date")
                          .IsRequired();

                    entity.Property(e => e.BitsLow)
                          .HasColumnType("bigint")
                          .IsRequired();

                    entity.Property(e => e.BitsHigh)
                          .HasColumnType("bigint")
                          .IsRequired();

                    entity.Property(e => e.IsFree)
                          .HasColumnType("bit")
                          .IsRequired();
                });

                // Configure BitReservation.
                modelBuilder.Entity<BitReservation>(entity =>
                {
                    entity.HasKey(e => e.BitReservationId);

                    entity.Property(e => e.ClientId)
                          .IsRequired();

                    entity.Property(e => e.Date)
                          .HasColumnType("date")
                          .IsRequired();

                    entity.Property(e => e.ResourceId)
                          .IsRequired()
                          .HasMaxLength(100);

                    entity.Property(e => e.StartBlock)
                          .IsRequired();

                    entity.Property(e => e.SlotLength)
                          .IsRequired();
                });

                base.OnModelCreating(modelBuilder);
            }
        }
    }
}
