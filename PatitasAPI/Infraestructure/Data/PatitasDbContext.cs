using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PatitasAPI.Core.Entities;

namespace PatitasAPI.Infraestructure.Data;

public class PatitasDbContext(DbContextOptions<PatitasDbContext> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<AppUser> AppUsers { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<Favorite> Favorites { get; set; }
    public DbSet<Form> Forms { get; set; }
    public DbSet<Pet> Pets { get; set; }
    public DbSet<Shelter> Shelters { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AppUser>(entity =>
        {
            entity.HasOne(u => u.Shelter)
                .WithMany(s => s.Owners) 
                .HasForeignKey(u => u.ShelterId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull); // Si c borra el refugio, se mantiene el usuario pero sin refugio
        });

        builder.Entity<Pet>(entity =>
        {
            entity.Property(p => p.Name).IsRequired().HasMaxLength(100);
            entity.HasOne(p => p.Shelter)
                .WithMany(s => s.Pets)
                .HasForeignKey(p => p.ShelterId)
                .OnDelete(DeleteBehavior.Cascade); // Borrado en cascada: si se borra el refugio se borran mascotas y favoritos

            entity.HasOne(p => p.Form)
                .WithMany(f => f.Pets)
                .HasForeignKey(p => p.FormId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Form>(entity =>
        {
            entity.Property(f => f.Name).IsRequired().HasMaxLength(100);
            entity.HasOne(f => f.Shelter)
                .WithMany(s => s.Forms)
                .HasForeignKey(f => f.ShelterId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Event>(entity =>
        {
            entity.HasOne(e => e.Shelter)
                .WithMany(s => s.Events)
                .HasForeignKey(e => e.ShelterId)
                .OnDelete(DeleteBehavior.Cascade); // Si c borra el refugio, se borran los eventos asociados
        });

        builder.Entity<Favorite>(entity =>
        {
            entity.HasKey(f => new { f.AppUserId, f.PetId });
            entity.HasOne(f => f.AppUser)
                .WithMany(u => u.Favorites)
                .HasForeignKey(f => f.AppUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(f => f.Pet)
                .WithMany(p => p.Favorites)
                .HasForeignKey(f => f.PetId)
                .OnDelete(DeleteBehavior.Cascade); // Borras la mascota -> Se quita de favoritos de todos
        });
    }
}
