using System.Globalization;
using System.Text;
using GymManager.Models;

namespace GymManager.Services;

public static class RutinaTextoService
{
    public const int MaximoBytesQr = 2800;

    public static string Formatear(Rutina rutina)
    {
        ArgumentNullException.ThrowIfNull(rutina);

        var texto = new StringBuilder();
        texto.AppendLine($"RUTINA: {rutina.Nombre.Trim()}");
        if (!string.IsNullOrWhiteSpace(rutina.Descripcion))
        {
            texto.AppendLine(rutina.Descripcion.Trim());
        }

        texto.AppendLine();
        foreach (EjercicioRutina ejercicio in rutina.Ejercicios.OrderBy(ejercicio => ejercicio.Orden))
        {
            texto.AppendLine($"{ejercicio.Orden}. {ejercicio.Nombre.Trim()}");
            texto.AppendLine($"   {ejercicio.Series} series x {ejercicio.Repeticiones.Trim()} repeticiones");
            if (ejercicio.DescansoSegundos is > 0)
            {
                texto.AppendLine($"   Descanso: {ejercicio.DescansoSegundos} segundos");
            }
            if (!string.IsNullOrWhiteSpace(ejercicio.Notas))
            {
                texto.AppendLine($"   Notas: {ejercicio.Notas.Trim()}");
            }
        }

        return texto.ToString().TrimEnd();
    }

    public static void ValidarTamanoQr(string texto)
    {
        int cantidadBytes = Encoding.UTF8.GetByteCount(texto);
        if (cantidadBytes > MaximoBytesQr)
        {
            throw new InvalidOperationException(
                $"La rutina ocupa {cantidadBytes} bytes y supera el límite seguro del QR ({MaximoBytesQr} bytes). Reducí las notas o exportá el PDF.");
        }
    }

    public static string PrepararParaQr(string texto)
    {
        ArgumentNullException.ThrowIfNull(texto);

        string descompuesto = texto.Normalize(NormalizationForm.FormD);
        var resultado = new StringBuilder(descompuesto.Length);
        foreach (Rune caracter in descompuesto.EnumerateRunes())
        {
            if (Rune.GetUnicodeCategory(caracter) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            resultado.Append(caracter.Value <= 0x7F ? (char)caracter.Value : '?');
        }

        return resultado.ToString().Normalize(NormalizationForm.FormC);
    }
}
