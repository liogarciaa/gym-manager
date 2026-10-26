using Microsoft.EntityFrameworkCore;
using GymManager.Models;
using System.IO;
using System;

namespace GymManager.Data
{
    public class GymDbContext : DbContext
    {
        public DbSet<Socio> Socios => Set<Socio>();
        public DbSet<Pago> Pagos => Set<Pago>();
        public DbSet<Asistencia> Asistencias => Set<Asistencia>();
        public DbSet<Configuracion> Configuraciones => Set<Configuracion>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
            {
                DataSource = DatabasePaths.DatabasePath,
                Mode = Microsoft.Data.Sqlite.SqliteOpenMode.ReadWriteCreate
            }.ToString());
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Socio>(entity => 
            {
                entity.HasKey(s => s.Id);
                entity.Property(s => s.Nombre).IsRequired().HasMaxLength(50);
                entity.Property(s => s.Apellido).IsRequired().HasMaxLength(50);
                entity.Property(s => s.Dni).IsRequired().HasMaxLength(20);
                entity.Property(s => s.Telefono).HasMaxLength(30);
                // Ignoramos las propiedades calculadas para que EF no intente crear columnas
                entity.Ignore(s => s.NombreCompleto);
                entity.Ignore(s => s.EstadoCuota);
            });

            modelBuilder.Entity<Pago>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Monto).HasConversion<double>(); // SQLite almacena mejor los números con coma flotante/double
                entity.Property(p => p.FormaDePago).HasMaxLength(30);

                entity.HasOne(p => p.Socio)
                      .WithMany(s => s.Pagos)
                      .HasForeignKey(p => p.SocioId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Asistencia>(entity =>
{
                entity.HasKey(a => a.Id);

                entity.HasOne(a => a.Socio)
                    .WithMany(s => s.Asistencias)
                    .HasForeignKey(a => a.SocioId)
                    .OnDelete(DeleteBehavior.Cascade);
});

            modelBuilder.Entity<Configuracion>().HasData(
            new Configuracion
            {
                Id = 1,
                NombreGimnasio = "Mi Gimnasio",
                PrecioCuotaMensual = 25000m,
                DiasToleranciaMora = 0,
                TelefonoContacto = "",
                Direccion = ""
         }
);
        }
    }
}