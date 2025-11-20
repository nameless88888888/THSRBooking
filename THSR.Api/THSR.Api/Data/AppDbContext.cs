using Microsoft.EntityFrameworkCore;
using THSR.Api.Models;

namespace THSR.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Station> Stations { get; set; }
        public DbSet<Train> Trains { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Passenger> Passenger { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<TrainStops> TrainStops { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Station>().ToTable("Station");
            modelBuilder.Entity<Train>().ToTable("Train");
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Passenger>().ToTable("Passenger");
            modelBuilder.Entity<Order>().ToTable("Orders");
            modelBuilder.Entity<Ticket>().ToTable("Tickets");

            modelBuilder.Entity<Train>()
                .HasOne(t => t.DepartureStation)
                .WithMany()
                .HasForeignKey(t => t.DepartureStationID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Train>()
                .HasOne(t => t.ArrivalStation)
                .WithMany()
                .HasForeignKey(t => t.ArrivalStationID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
