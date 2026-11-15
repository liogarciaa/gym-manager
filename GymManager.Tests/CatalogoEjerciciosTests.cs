using GymManager.Services;
using Xunit;

namespace GymManager.Tests;

public sealed class CatalogoEjerciciosTests
{
    [Fact]
    public void SugiereEjerciciosComunesIgnorandoTildesYMayusculas()
    {
        IReadOnlyList<string> sugerencias = CatalogoEjercicios.ObtenerSugerencias("elevacion");

        Assert.Contains("Elevaciones laterales con mancuernas", sugerencias);
    }

    [Fact]
    public void IncluyeEjerciciosUsadosEnRutinasYLimitaResultados()
    {
        string[] ejerciciosGuardados =
        [
            "Peso muerto a una pierna",
            "Press landmine",
            "Remo en banco inclinado"
        ];

        IReadOnlyList<string> sugerencias = CatalogoEjercicios.ObtenerSugerencias("peso", ejerciciosGuardados, 1);

        Assert.Single(sugerencias);
        Assert.Equal("Peso muerto", sugerencias[0]);
    }

    [Fact]
    public void NoMuestraSugerenciasSiLaBusquedaEstaVacia()
    {
        Assert.Empty(CatalogoEjercicios.ObtenerSugerencias(" "));
    }
}
