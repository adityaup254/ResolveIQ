using Microsoft.EntityFrameworkCore;
using ResolveIQ.Models;
using ResolveIQ.Services;
using System;

namespace ResolveIQ.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Technician> Technicians { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Ticket> Tickets { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Relationships (Explicit configuration, though conventions cover most)
            
            // Ticket -> User (One to Many)
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.User)
                .WithMany(u => u.Tickets)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Ticket -> Technician (One to Many)
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Technician)
                .WithMany(tech => tech.Tickets)
                .HasForeignKey(t => t.TechnicianId)
                .OnDelete(DeleteBehavior.SetNull);

            // Ticket -> Category (One to Many)
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Category)
                .WithMany(c => c.Tickets)
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed Data
            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, FullName = "Admin User", Email = "admin@resolveiq.com", Password = PasswordHelper.HashPassword("Admin@123"), Role = "Admin" },
                new User { Id = 2, FullName = "Normal User", Email = "user@resolveiq.com", Password = PasswordHelper.HashPassword("User@123"), Role = "User" },
                new User { Id = 3, FullName = "Sample Tech", Email = "technician@resolveiq.com", Password = PasswordHelper.HashPassword("Tech@123"), Role = "Technician" }
            );

            modelBuilder.Entity<Technician>().HasData(
                new Technician { Id = 1, FullName = "Tech Bob", Email = "bob@resolveiq.com", Phone = "1234567890" },
                new Technician { Id = 2, FullName = "Tech Alice", Email = "alice@resolveiq.com", Phone = "0987654321" },
                new Technician { Id = 3, FullName = "Sample Tech", Email = "technician@resolveiq.com", Phone = "0000000000" }
            );

            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Hardware" },
                new Category { Id = 2, Name = "Software" },
                new Category { Id = 3, Name = "Network" },
                new Category { Id = 4, Name = "Account" },
                new Category { Id = 5, Name = "Other" }
            );

            modelBuilder.Entity<Ticket>().HasData(
                new Ticket 
                { 
                    Id = 1, 
                    Title = "Laptop not starting", 
                    Description = "When I press the power button, nothing happens.", 
                    Priority = "High", 
                    Status = "Open", 
                    CreatedDate = new DateTime(2023, 10, 1), 
                    UserId = 1, 
                    CategoryId = 1 
                },
                new Ticket 
                { 
                    Id = 2, 
                    Title = "Cannot access email", 
                    Description = "Outlook is throwing an error.", 
                    Priority = "Medium", 
                    Status = "Assigned", 
                    CreatedDate = new DateTime(2023, 10, 2), 
                    UserId = 2, 
                    TechnicianId = 1,
                    CategoryId = 2 
                },
                new Ticket 
                { 
                    Id = 3, 
                    Title = "Wi-Fi keeps dropping", 
                    Description = "Connection is unstable in my area.", 
                    Priority = "Low", 
                    Status = "Open", 
                    CreatedDate = new DateTime(2023, 10, 3), 
                    UserId = 1, 
                    CategoryId = 3 
                },
                new Ticket 
                { 
                    Id = 4, 
                    Title = "Need software installed", 
                    Description = "Please install Visual Studio.", 
                    Priority = "Medium", 
                    Status = "Closed", 
                    CreatedDate = new DateTime(2023, 10, 4), 
                    UpdatedDate = new DateTime(2023, 10, 5),
                    ResolutionNote = "Installed Visual Studio 2022.",
                    UserId = 2, 
                    TechnicianId = 2,
                    CategoryId = 2 
                },
                new Ticket 
                { 
                    Id = 5, 
                    Title = "Password reset required", 
                    Description = "Forgot my VPN password.", 
                    Priority = "High", 
                    Status = "Assigned", 
                    CreatedDate = new DateTime(2023, 10, 6), 
                    UserId = 1, 
                    TechnicianId = 2,
                    CategoryId = 4 
                }
            );
        }
    }
}
