using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using GymManager.Data;

namespace GymManager.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
        }

        private void BtnIrControlAcceso_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow is GymManager.MainWindow mainWindow)
                mainWindow.NavegarAControlAcceso();
        }

        private void BtnIrSocios_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow is GymManager.MainWindow mainWindow)
                mainWindow.NavegarASocios();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CargarFechaEncabezado();
            CargarMetricas();
        }

        private void CargarFechaEncabezado()
        {
            string fechaFormateada = DateTime.Today.ToString("dddd, d 'de' MMMM", new CultureInfo("es-ES"));
            fechaFormateada = char.ToUpper(fechaFormateada[0]) + fechaFormateada.Substring(1);
            TxtFechaHoy.Text = fechaFormateada;
        }

        private void CargarMetricas()
        {
            DateTime hoy = DateTime.Today;
            DateTime hoyInicio = hoy;
            DateTime hoyFin = hoy.AddDays(1).AddTicks(-1);

            DateTime inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
            DateTime finMes = inicioMes.AddMonths(1).AddTicks(-1);

            using (var db = new GymDbContext())
            {
                // 1. Métricas Numéricas Generales
                int sociosActivos = db.Socios
                    .Count(s => !s.Eliminado && s.EstaActivo);

                int cuotasVencidas = db.Socios
                    .Count(s => !s.Eliminado && s.EstaActivo && (!s.FechaVencimiento.HasValue || s.FechaVencimiento.Value.Date < hoy));

                int asistenciasHoy = db.Asistencias
                    .Count(a => a.FechaHora >= hoyInicio && a.FechaHora <= hoyFin);

                int altasMes = db.Socios
                    .Count(s => !s.Eliminado && s.FechaRegistro >= inicioMes && s.FechaRegistro <= finMes);

                // 2. Obtener tolerancia configurada y filtrar socios en período de gracia
                int diasTolerancia = db.Configuraciones.Select(c => c.DiasToleranciaMora).FirstOrDefault();
                DateTime fechaLimiteTolerancia = hoy.AddDays(-diasTolerancia);

                var sociosEnTolerancia = db.Socios
                    .Where(s => !s.Eliminado && s.EstaActivo && 
                                s.FechaVencimiento.HasValue && 
                                s.FechaVencimiento.Value.Date < hoy && 
                                s.FechaVencimiento.Value.Date >= fechaLimiteTolerancia)
                    .OrderBy(s => s.FechaVencimiento)
                    .Take(6)
                    .ToList();

                // 3. Últimos ingresos registrados hoy
                var ultimosAccesos = db.Asistencias
                    .Include(a => a.Socio)
                    .Where(a => a.FechaHora >= hoyInicio && a.FechaHora <= hoyFin)
                    .OrderByDescending(a => a.FechaHora)
                    .Take(6)
                    .ToList()
                    .Select(a => new
                    {
                        Hora = a.FechaHora.ToString("hh:mm tt", new CultureInfo("es-AR")),
                        NombreCompleto = a.Socio.NombreCompleto,
                        Dni = a.Socio.Dni
                    })
                    .ToList();

                // 4. Actualizar la Interfaz
                TxtSociosActivos.Text = sociosActivos.ToString();
                TxtCuotasVencidas.Text = cuotasVencidas.ToString();
                TxtAsistenciasHoy.Text = asistenciasHoy.ToString();
                TxtAltasMes.Text = altasMes.ToString();

                GridUltimosAccesos.ItemsSource = ultimosAccesos;
                GridProximosVencimientos.ItemsSource = sociosEnTolerancia;
            }
        }
    }
}