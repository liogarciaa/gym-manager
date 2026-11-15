namespace GymManager.Models;

public class Rutina
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public List<EjercicioRutina> Ejercicios { get; set; } = new();
    public List<SocioRutina> Asignaciones { get; set; } = new();
}

public class EjercicioRutina
{
    public int Id { get; set; }
    public int RutinaId { get; set; }
    public int Orden { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Series { get; set; } = 3;
    public string Repeticiones { get; set; } = "10";
    public int? DescansoSegundos { get; set; }
    public string Notas { get; set; } = string.Empty;
    public Rutina? Rutina { get; set; }
}

public class SocioRutina
{
    public int Id { get; set; }
    public int SocioId { get; set; }
    public int RutinaId { get; set; }
    public DateTime FechaAsignacion { get; set; } = DateTime.Now;
    public Socio? Socio { get; set; }
    public Rutina? Rutina { get; set; }
}
