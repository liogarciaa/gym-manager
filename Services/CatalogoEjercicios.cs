using System.Globalization;
using System.Text;

namespace GymManager.Services;

public static class CatalogoEjercicios
{
    private static readonly string[] EjerciciosBase =
    [
        "Abdominales bicicleta",
        "Abdominales en máquina",
        "Abdominales crunch",
        "Abducción de cadera en máquina",
        "Aducción de cadera en máquina",
        "Aperturas con mancuernas",
        "Aperturas en máquina",
        "Bicicleta fija",
        "Burpees",
        "Caminata en cinta",
        "Curl de bíceps con barra",
        "Curl de bíceps con mancuernas",
        "Curl de bíceps en polea",
        "Curl femoral acostado",
        "Curl femoral sentado",
        "Elevación de cadera",
        "Elevación de gemelos de pie",
        "Elevación de gemelos sentado",
        "Elevación de piernas",
        "Elevaciones laterales con mancuernas",
        "Elevaciones posteriores con mancuernas",
        "Escaladora",
        "Extensión de cadera en polea",
        "Extensión de piernas en máquina",
        "Extensión de tríceps con cuerda",
        "Extensión de tríceps en polea",
        "Fondos en paralelas",
        "Fondos para tríceps",
        "Hip thrust",
        "Jalón al pecho",
        "Jalón al pecho con agarre cerrado",
        "Jalón al pecho con agarre supino",
        "Jumping jacks",
        "Peso muerto",
        "Peso muerto con mancuernas",
        "Peso muerto rumano",
        "Plancha abdominal",
        "Press Arnold",
        "Press de banca",
        "Press de banca con mancuernas",
        "Press de banca inclinado",
        "Press de hombros con mancuernas",
        "Press de hombros en máquina",
        "Press de piernas",
        "Press inclinado con mancuernas",
        "Press militar con barra",
        "Press cerrado",
        "Remo con barra",
        "Remo con mancuerna",
        "Remo en máquina",
        "Remo sentado en polea",
        "Sentadilla",
        "Sentadilla búlgara",
        "Sentadilla goblet",
        "Sentadilla en máquina Smith",
        "Sentadilla sumo",
        "Subida al cajón",
        "Step",
        "Superman",
        "Tracción en polea alta",
        "Zancadas con mancuernas"
    ];

    public static IReadOnlyList<string> ObtenerSugerencias(
        string busqueda,
        IEnumerable<string>? ejerciciosGuardados = null,
        int limite = 8)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limite);
        if (string.IsNullOrWhiteSpace(busqueda))
        {
            return [];
        }

        string criterio = Normalizar(busqueda.Trim());
        var nombres = EjerciciosBase
            .Concat(ejerciciosGuardados ?? [])
            .Where(nombre => !string.IsNullOrWhiteSpace(nombre))
            .Select(nombre => nombre.Trim())
            .Distinct(StringComparer.CurrentCultureIgnoreCase);

        return nombres
            .Select(nombre => new { Nombre = nombre, Normalizado = Normalizar(nombre) })
            .Where(ejercicio => ejercicio.Normalizado.Contains(criterio, StringComparison.Ordinal))
            .OrderBy(ejercicio => ejercicio.Normalizado.StartsWith(criterio, StringComparison.Ordinal) ? 0 : 1)
            .ThenBy(ejercicio => ejercicio.Nombre, StringComparer.CurrentCultureIgnoreCase)
            .Take(limite)
            .Select(ejercicio => ejercicio.Nombre)
            .ToArray();
    }

    private static string Normalizar(string texto)
    {
        string descompuesto = texto.Normalize(NormalizationForm.FormD);
        var resultado = new StringBuilder(descompuesto.Length);
        foreach (char caracter in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caracter) != UnicodeCategory.NonSpacingMark)
            {
                resultado.Append(char.ToLowerInvariant(caracter));
            }
        }

        return resultado.ToString().Normalize(NormalizationForm.FormC);
    }
}
