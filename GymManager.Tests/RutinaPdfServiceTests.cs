using System.IO;
using GymManager.Models;
using GymManager.Services;
using Xunit;

namespace GymManager.Tests;

public sealed class RutinaPdfServiceTests
{
    [Fact]
    public void GeneraPdfConContenidoDeRutinaAsignada()
    {
        string ruta = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pdf");
        var rutina = new Rutina
        {
            Nombre = "Fuerza",
            Descripcion = "Entrenamiento inicial",
            Ejercicios =
            [
                new EjercicioRutina
                {
                    Orden = 1,
                    Nombre = "Press de banca",
                    Series = 3,
                    Repeticiones = "8-10",
                    DescansoSegundos = 90
                }
            ]
        };

        try
        {
            RutinaPdfService.Generar(ruta, rutina, "Ana Prueba", DateTime.Today, "Mi Gimnasio");

            byte[] contenido = File.ReadAllBytes(ruta);
            Assert.True(contenido.Length > 1000);
            Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(contenido, 0, 5));
        }
        finally
        {
            if (File.Exists(ruta)) File.Delete(ruta);
        }
    }
}
