using GymManager.Models;
using GymManager.Services;
using Xunit;

namespace GymManager.Tests;

public sealed class RutinaTextoServiceTests
{
    [Fact]
    public void FormateaRutinaConEjerciciosEnOrdenYDetalles()
    {
        var rutina = new Rutina
        {
            Nombre = "Fuerza",
            Descripcion = "Día de tren superior",
            Ejercicios =
            [
                new EjercicioRutina
                {
                    Orden = 2,
                    Nombre = "Remo",
                    Series = 4,
                    Repeticiones = "8-10",
                    DescansoSegundos = 90,
                    Notas = "Espalda recta"
                },
                new EjercicioRutina
                {
                    Orden = 1,
                    Nombre = "Press",
                    Series = 3,
                    Repeticiones = "10"
                }
            ]
        };

        string texto = RutinaTextoService.Formatear(rutina);

        Assert.True(texto.IndexOf("1. Press", StringComparison.Ordinal) < texto.IndexOf("2. Remo", StringComparison.Ordinal));
        Assert.Contains("3 series x 10 repeticiones", texto);
        Assert.Contains("Descanso: 90 segundos", texto);
        Assert.Contains("Notas: Espalda recta", texto);
    }

    [Fact]
    public void ValidaElLimiteDeBytesDelContenidoQrEnUtf8()
    {
        string textoValido = new('á', RutinaTextoService.MaximoBytesQr / 2);
        RutinaTextoService.ValidarTamanoQr(textoValido);

        string textoDemasiadoLargo = new('á', (RutinaTextoService.MaximoBytesQr / 2) + 1);
        Assert.Throws<InvalidOperationException>(() => RutinaTextoService.ValidarTamanoQr(textoDemasiadoLargo));
    }

    [Fact]
    public void GeneraImagenQrPngParaTextoDeRutina()
    {
        byte[] imagen = RutinaQrService.CrearImagen("RUTINA: Fuerza\n1. Press\n3 series x 10 repeticiones");

        Assert.True(imagen.Length > 8);
        Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, imagen[..8]);
    }
}
