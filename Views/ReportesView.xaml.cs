using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using GymManager.Data;
using GymManager.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GymManager.Views
{
    public partial class ReportesView : UserControl
    {
        private bool _inicializado = false;
        private ReporteMensualDto _reporteActual = new();

        public ReportesView()
        {
            InitializeComponent();
            QuestPDF.Settings.License = LicenseType.Community;

            // Habilitar fuentes del sistema (Windows)
            QuestPDF.Settings.UseSystemFonts = true;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (_inicializado) return;

            // Cargar combo de meses
            for (int m = 1; m <= 12; m++)
            {
                string nombreMes = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m);
                nombreMes = char.ToUpper(nombreMes[0]) + nombreMes.Substring(1);
                CboMes.Items.Add(new ComboBoxItem { Content = nombreMes, Tag = m });
            }

            // Cargar últimos 5 años
            int anioActual = DateTime.Today.Year;
            for (int y = anioActual; y <= anioActual + 4; y++)
            {
                CboAnio.Items.Add(new ComboBoxItem { Content = y.ToString(), Tag = y });
            }

            CboMes.SelectedIndex = DateTime.Today.Month - 1;
            CboAnio.SelectedIndex = 0;

            _inicializado = true;
            GenerarReporte();
        }

        private void FiltroPeriodo_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_inicializado) return;
            GenerarReporte();
        }

        private void BtnIrDeudores_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow is GymManager.MainWindow mainWindow)
                mainWindow.NavegarADeudores();
        }

        private void GenerarReporte()
        {
            if (CboMes.SelectedItem is not ComboBoxItem itemMes || CboAnio.SelectedItem is not ComboBoxItem itemAnio)
                return;

            int mes = (int)itemMes.Tag;
            int anio = (int)itemAnio.Tag;

            DateTime inicioMes = new DateTime(anio, mes, 1);
            DateTime finMes = inicioMes.AddMonths(1).AddTicks(-1);

            using (var db = new GymDbContext())
            {
                // 1. Pagos del mes (Caja percibida en este período)
                var pagos = db.Pagos
                              .Where(p => p.FechaPago >= inicioMes && p.FechaPago <= finMes)
                              .ToList();

                decimal totalRecaudado = pagos.Sum(p => p.Monto);
                int cantidadPagos = pagos.Count(p => p.Monto > 0);
                int bonificaciones = pagos.Count(p => p.Monto == 0 || p.FormaDePago == "Bonificación / Regalo");

                // Agrupado por método de pago
                var desglose = pagos
                    .GroupBy(p => string.IsNullOrWhiteSpace(p.FormaDePago) ? "No especificado" : p.FormaDePago)
                    .Select(g => new ResumenMetodoPagoDto
                    {
                        Metodo = g.Key,
                        Cantidad = g.Count(),
                        Total = g.Sum(x => x.Monto)
                    })
                    .OrderByDescending(x => x.Total)
                    .ToList();

                // 2. Socios con Cobertura Vigente en este mes
                // Considera a quienes pagaron hasta este mes y cuyo vencimiento cubre (o supera) el inicio del período
                int sociosConCuotaVigente = db.Socios
                    .Count(s => !s.Eliminado &&
                                s.FechaVencimiento.HasValue &&
                                s.FechaVencimiento.Value.Date >= inicioMes.Date &&
                                s.Pagos.Any(p => p.FechaPago <= finMes));

                // 3. Asistencias registradas durante el mes
                int totalAsistencias = db.Asistencias.Count(a => a.FechaHora >= inicioMes && a.FechaHora <= finMes);

                // 4. Deudores al día de hoy
                DateTime hoy = DateTime.Today;
                int deudores = db.Socios.Count(s => !s.Eliminado && s.EstaActivo && (!s.FechaVencimiento.HasValue || s.FechaVencimiento.Value.Date < hoy));

                // Guardar en DTO con la métrica de cobertura asignada
                _reporteActual = new ReporteMensualDto
                {
                    Mes = mes,
                    Anio = anio,
                    TotalRecaudado = totalRecaudado,
                    CantidadPagosTotal = cantidadPagos,
                    CantidadBonificaciones = bonificaciones,
                    NuevosSocios = sociosConCuotaVigente, // Asignado para reflejar socios activos del período
                    TotalAsistencias = totalAsistencias,
                    TotalDeudoresActivos = deudores,
                    DesgloseMetodos = desglose
                };
            }

            // Pintar en pantalla
            TxtTotalRecaudado.Text = _reporteActual.TotalRecaudado.ToString("C2");
            TxtCantidadPagos.Text = $"{_reporteActual.CantidadPagosTotal} cobros realizados";
            TxtNuevosSocios.Text = _reporteActual.NuevosSocios.ToString();
            TxtTotalAsistencias.Text = _reporteActual.TotalAsistencias.ToString();
            TxtBonificaciones.Text = _reporteActual.CantidadBonificaciones.ToString();

            GridMetodos.ItemsSource = _reporteActual.DesgloseMetodos;
            TxtMorosidadResumen.Text = $"Actualmente existen {_reporteActual.TotalDeudoresActivos} socios con cuota vencida o sin regularizar.";
        }

        private void BtnDescargarPdf_Click(object sender, RoutedEventArgs e)
        {
            string nombreMes = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(_reporteActual.Mes);
            nombreMes = char.ToUpper(nombreMes[0]) + nombreMes.Substring(1);

            var sfd = new SaveFileDialog
            {
                FileName = $"Reporte_GymManager_{nombreMes}_{_reporteActual.Anio}.pdf",
                Filter = "Documento PDF (*.pdf)|*.pdf"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    GenerarDocumentoPdf(sfd.FileName, _reporteActual, nombreMes);
                    MessageBox.Show("El reporte PDF se generó con éxito.", "Reporte Creado", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ocurrió un error al generar el PDF: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void GenerarDocumentoPdf(string filePath, ReporteMensualDto data, string nombreMes)
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(35);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                    // Encabezado
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("GYM MANAGER").FontSize(22).Bold().FontColor("#1E293B");
                            col.Item().Text($"Balance Mensual - {nombreMes} {data.Anio}").FontSize(14).FontColor("#64748B");
                        });

                        row.ConstantItem(150).AlignRight().Column(col =>
                        {
                            col.Item().Text($"Emitido: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(9).FontColor("#94A3B8");
                        });
                    });

                    // Contenido
                    page.Content().PaddingVertical(20).Column(col =>
                    {
                        // Tarjetas de Resumen
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Border(1).BorderColor("#E2E8F0").Padding(10).Column(c =>
                            {
                                c.Item().Text("Total Recaudado").FontSize(10).FontColor("#64748B");
                                c.Item().Text(data.TotalRecaudado.ToString("C2")).FontSize(16).Bold().FontColor("#16A34A");
                                c.Item().Text($"{data.CantidadPagosTotal} cobros").FontSize(9).FontColor("#94A3B8");
                            });

                            r.Spacing(10);

                            r.RelativeItem().Border(1).BorderColor("#E2E8F0").Padding(10).Column(c =>
                            {
                                c.Item().Text("Socios Activos").FontSize(10).FontColor("#64748B");
                                c.Item().Text(data.NuevosSocios.ToString()).FontSize(16).Bold().FontColor("#2563EB");
                                c.Item().Text("Con cuota vigente").FontSize(9).FontColor("#94A3B8");
                            });

                            r.Spacing(10);

                            r.RelativeItem().Border(1).BorderColor("#E2E8F0").Padding(10).Column(c =>
                            {
                                c.Item().Text("Asistencias").FontSize(10).FontColor("#64748B");
                                c.Item().Text(data.TotalAsistencias.ToString()).FontSize(16).Bold().FontColor("#8B5CF6");
                                c.Item().Text("Check-ins").FontSize(9).FontColor("#94A3B8");
                            });

                            r.Spacing(10);

                            r.RelativeItem().Border(1).BorderColor("#E2E8F0").Padding(10).Column(c =>
                            {
                                c.Item().Text("Bonificaciones").FontSize(10).FontColor("#64748B");
                                c.Item().Text(data.CantidadBonificaciones.ToString()).FontSize(16).Bold().FontColor("#D97706");
                                c.Item().Text("Regalos ($0)").FontSize(9).FontColor("#94A3B8");
                            });
                        });

                        col.Item().PaddingTop(25).Text("Desglose por Método de Cobro").FontSize(13).Bold().FontColor("#1E293B");

                        // Tabla de métodos
                        col.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().BorderBottom(1).BorderColor("#CBD5E1").Padding(5).Text("Método").Bold();
                                header.Cell().BorderBottom(1).BorderColor("#CBD5E1").Padding(5).AlignCenter().Text("Operaciones").Bold();
                                header.Cell().BorderBottom(1).BorderColor("#CBD5E1").Padding(5).AlignRight().Text("Subtotal").Bold();
                            });

                            foreach (var item in data.DesgloseMetodos)
                            {
                                table.Cell().BorderBottom(1).BorderColor("#F1F5F9").Padding(5).Text(item.Metodo);
                                table.Cell().BorderBottom(1).BorderColor("#F1F5F9").Padding(5).AlignCenter().Text(item.Cantidad.ToString());
                                table.Cell().BorderBottom(1).BorderColor("#F1F5F9").Padding(5).AlignRight().Text(item.Total.ToString("C2")).Bold();
                            }
                        });

                        // Sección de Morosidad
                        col.Item().PaddingTop(25).Border(1).BorderColor("#FECACA").Background("#FEF2F2").Padding(12).Column(m =>
                        {
                            m.Item().Text("Observaciones de Cobranzas").Bold().FontColor("#991B1B");
                            m.Item().Text($"Al momento de emitir este balance, el sistema registra {data.TotalDeudoresActivos} socios activos con pagos pendientes o cuotas expiradas.").FontSize(10).FontColor("#B91C1C");
                        });
                    });

                    // Pie de Página
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                        x.Span(" de ");
                        x.TotalPages();
                    });
                });
            }).GeneratePdf(filePath);
        }
    }
}