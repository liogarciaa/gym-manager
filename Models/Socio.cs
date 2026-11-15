using System;
using System.Collections.Generic;

namespace GymManager.Models
{
    public class Socio
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Dni { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;

        // Puede guardar el UID de un llavero NFC o el código de barras impreso
        public string? CodigoAcceso { get; set; }

        public DateTime FechaAlta { get; set; }
        public bool EstaActivo { get; set; }

        public bool Eliminado { get; set; } = false;

        // Nuevo: Fecha hasta la cual tiene cubierta la cuota
        public DateTime? FechaVencimiento { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Today;

        // Relación con Entity Framework: un socio puede tener muchos pagos
        public List<Pago> Pagos { get; set; } = new();

        public List<Asistencia> Asistencias { get; set; } = new();
        public List<SocioRutina> RutinasAsignadas { get; set; } = new();

        // Propiedades calculadas útiles para la UI
        public string NombreCompleto => $"{Nombre} {Apellido}";

        // Evalúa el estado de la cuota en tiempo real
        public string EstadoCuota
        {
            get
            {
                if (!EstaActivo) return "Inactivo";
                if (!FechaVencimiento.HasValue) return "Sin pagos";
                return FechaVencimiento.Value.Date >= DateTime.Today ? "Al día" : "Vencido";
            }
        }
    }
}