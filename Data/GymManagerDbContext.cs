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
        public DbSet<LlaveroMaestro> LlaverosMaestros => Set<LlaveroMaestro>();
        public DbSet<Rutina> Rutinas => Set<Rutina>();
        public DbSet<EjercicioRutina> EjerciciosRutina => Set<EjercicioRutina>();
        public DbSet<SocioRutina> SociosRutinas => Set<SocioRutina>();

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

            modelBuilder.Entity<LlaveroMaestro>(entity =>
            {
                entity.HasKey(llavero => llavero.Id);
                entity.Property(llavero => llavero.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(llavero => llavero.CodigoAcceso).IsRequired().HasMaxLength(100);
                entity.HasIndex(llavero => llavero.CodigoAcceso).IsUnique();
            });

            modelBuilder.Entity<Rutina>(entity =>
            {
                entity.HasKey(rutina => rutina.Id);
                entity.Property(rutina => rutina.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(rutina => rutina.Descripcion).HasMaxLength(500);
            });

            modelBuilder.Entity<EjercicioRutina>(entity =>
            {
                entity.HasKey(ejercicio => ejercicio.Id);
                entity.Property(ejercicio => ejercicio.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(ejercicio => ejercicio.Repeticiones).IsRequired().HasMaxLength(30);
                entity.Property(ejercicio => ejercicio.Notas).HasMaxLength(300);
                entity.HasOne(ejercicio => ejercicio.Rutina)
                    .WithMany(rutina => rutina.Ejercicios)
                    .HasForeignKey(ejercicio => ejercicio.RutinaId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SocioRutina>(entity =>
            {
                entity.HasKey(asignacion => asignacion.Id);
                entity.HasOne(asignacion => asignacion.Socio)
                    .WithMany(socio => socio.RutinasAsignadas)
                    .HasForeignKey(asignacion => asignacion.SocioId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(asignacion => asignacion.Rutina)
                    .WithMany(rutina => rutina.Asignaciones)
                    .HasForeignKey(asignacion => asignacion.RutinaId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(asignacion => new { asignacion.SocioId, asignacion.RutinaId }).IsUnique();
            });
        }
    }
}