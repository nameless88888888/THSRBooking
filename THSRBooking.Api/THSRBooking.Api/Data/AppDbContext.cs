using Microsoft.EntityFrameworkCore;
using THSRBooking.Api.Models;

namespace THSRBooking.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Train> Trains { get; set; }
        public DbSet<TrainStop> TrainStops { get; set; }
        public DbSet<Station> Stations { get; set; }
        public DbSet<TimetableRaw> TimetableRaw { get; set; }

        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderTicket> OrderTickets { get; set; }
        public DbSet<SeatReservation> SeatReservations { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<TicketFare> TicketFares { get; set; }
        public DbSet<SeatLayout> SeatLayouts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Train>()
                .ToTable("Trains")
                .HasKey(t => t.TrainId);

            modelBuilder.Entity<TrainStop>()
                .ToTable("TrainStops")
                .HasKey(ts => ts.TrainStopId);

            modelBuilder.Entity<Station>()
                .ToTable("Stations")
                .HasKey(s => s.StationId);

            modelBuilder.Entity<Order>()
                .ToTable("Orders")
                .HasKey(o => o.OrderId);

            modelBuilder.Entity<OrderTicket>()
                .ToTable("OrderTickets")
                .HasKey(ot => ot.OrderTicketId);

            modelBuilder.Entity<SeatReservation>()
                .ToTable("SeatReservations")
                .HasKey(sr => sr.SeatReservationId);

            // 🔴 重點：告訴 EF 這張表沒有主鍵，只用來查詢
            modelBuilder.Entity<TimetableRaw>()
                .ToTable("TimetableRaw")
                .HasNoKey();
            modelBuilder.Entity<User>()
            .ToTable("Users")
            .HasKey(u => u.UserId);
            modelBuilder.Entity<SeatLayout>(entity =>
            {
                entity.HasKey(s => s.SeatLayoutId);

                entity.HasIndex(s => new { s.CarNumber, s.SeatRow, s.SeatColumn })
                      .IsUnique();

                entity.Property(s => s.SeatColumn)
                      .HasMaxLength(1)
                      .IsRequired();
            });
        }
    }
}
