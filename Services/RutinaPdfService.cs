using GymManager.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GymManager.Services;

public static class RutinaPdfService
{
    public static void Generar(
        string rutaArchivo,
        Rutina rutina,
        string nombreSocio,
        DateTime fechaAsignacion,
        string nombreGimnasio)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rutaArchivo);
        ArgumentNullException.ThrowIfNull(rutina);

        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.UseSystemFonts = true;

        Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(38);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(style => style.FontSize(10).FontFamily("Arial").FontColor("#1E293B"));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(column =>
                    {
                        column.Item().Text(string.IsNullOrWhiteSpace(nombreGimnasio) ? "GYM MANAGER" : nombreGimnasio)
                            .FontSize(18).Bold().FontColor("#183B38");
                        column.Item().Text("PLAN DE ENTRENAMIENTO").FontSize(9).Bold().FontColor("#64748B");
                    });
                    row.ConstantItem(125).AlignRight().Text($"Emitido: {DateTime.Now:dd/MM/yyyy}")
                        .FontSize(9).FontColor("#64748B");
                });

                page.Content().PaddingVertical(20).Column(column =>
                {
                    column.Spacing(14);

                    column.Item().Border(1).BorderColor("#DDE6E5").Background("#F5F9F8").Padding(14).Column(details =>
                    {
                        details.Item().Text(rutina.Nombre).FontSize(20).Bold().FontColor("#0F172A");
                        details.Item().PaddingTop(5).Text($"Socio: {nombreSocio}").FontSize(11).Bold();
                        details.Item().Text($"Asignada: {fechaAsignacion:dd/MM/yyyy}").FontSize(9).FontColor("#64748B");
                        if (!string.IsNullOrWhiteSpace(rutina.Descripcion))
                        {
                            details.Item().PaddingTop(6).Text(rutina.Descripcion.Trim()).FontSize(10);
                        }
                    });

                    column.Item().Text("EJERCICIOS").FontSize(12).Bold().FontColor("#183B38");

                    foreach (EjercicioRutina ejercicio in rutina.Ejercicios.OrderBy(ejercicio => ejercicio.Orden))
                    {
                        column.Item().BorderBottom(1).BorderColor("#E2E8F0").PaddingVertical(9).Row(row =>
                        {
                            row.ConstantItem(30).Text($"{ejercicio.Orden}.").Bold().FontColor("#64748B");
                            row.RelativeItem().Column(description =>
                            {
                                description.Item().Text(ejercicio.Nombre).FontSize(11).Bold();
                                if (!string.IsNullOrWhiteSpace(ejercicio.Notas))
                                {
                                    description.Item().PaddingTop(2).Text(ejercicio.Notas.Trim()).FontSize(9).FontColor("#64748B");
                                }
                            });
                            row.ConstantItem(115).AlignCenter().Text($"{ejercicio.Series} x {ejercicio.Repeticiones}").Bold();
                            row.ConstantItem(80).AlignRight().Text(
                                ejercicio.DescansoSegundos is > 0 ? $"{ejercicio.DescansoSegundos} seg" : "—")
                                .FontColor("#64748B");
                        });
                    }

                    column.Item().PaddingTop(8).Text("Series x repeticiones                         Descanso entre series")
                        .FontSize(8).FontColor("#94A3B8");
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Página ");
                    text.CurrentPageNumber();
                    text.Span(" de ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf(rutaArchivo);
    }
}
