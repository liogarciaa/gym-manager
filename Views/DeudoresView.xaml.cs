using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GymManager.Data;
using GymManager.Models;
using GymManager.Services;

namespace GymManager.Views
{
    public class DeudorItem
    {
        public int SocioId { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Dni { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public DateTime? FechaVencimiento { get; set; }
        public int DiasVencido { get; set; }
        public string EstadoCuota { get; set; } = string.Empty;
    }

    public partial class DeudoresView : UserControl
    {
        private List<DeudorItem> _todosLosDeudores = new();

        public DeudoresView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CargarDeudores();
        }

        private void CargarDeudores()
        {
            using (var db = new GymDbContext())
            {
                // Obtenemos todos los socios activos no eliminados que estén vencidos o sin pagos
                var socios = db.Socios
                               .Where(s => !s.Eliminado && s.EstaActivo)
                               .ToList();

                DateTime hoy = DateTime.Today;

                _todosLosDeudores = socios
                    .Where(s => !s.FechaVencimiento.HasValue || s.FechaVencimiento.Value.Date < hoy)
                    .Select(s => new DeudorItem
                    {
                        SocioId = s.Id,
                        NombreCompleto = s.NombreCompleto,
                        Dni = s.Dni,
                        Telefono = string.IsNullOrWhiteSpace(s.Telefono) ? "Sin registrar" : s.Telefono,
                        FechaVencimiento = s.FechaVencimiento,
                        DiasVencido = s.FechaVencimiento.HasValue ? (hoy - s.FechaVencimiento.Value.Date).Days : 0,
                        EstadoCuota = s.FechaVencimiento.HasValue ? "Cuota Vencida" : "Sin pagos iniciales"
                    })
                    .OrderByDescending(d => d.DiasVencido)
                    .ToList();

                GridDeudores.ItemsSource = _todosLosDeudores;
                TxtTotalMorosos.Text = $"{_todosLosDeudores.Count} socios";
            }
        }

        private void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filtro = TxtBuscar.Text.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(filtro))
            {
                GridDeudores.ItemsSource = _todosLosDeudores;
            }
            else
            {
                GridDeudores.ItemsSource = _todosLosDeudores
                    .Where(d => d.NombreCompleto.ToLower().Contains(filtro) || d.Dni.Contains(filtro))
                    .ToList();
            }
        }

        private void BtnRefrescar_Click(object sender, RoutedEventArgs e)
        {
            CargarDeudores();
        }

        private void BtnRegularizar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: DeudorItem item }) return;

            Socio? socio;
            using (var db = new GymDbContext())
            {
                socio = db.Socios.FirstOrDefault(s => s.Id == item.SocioId && !s.Eliminado);
            }

            if (socio == null)
            {
                CargarDeudores();
                return;
            }

            var ventanaPago = new RegistrarPagoWindow(socio)
            {
                Owner = Window.GetWindow(this)
            };

            if (ventanaPago.ShowDialog() != true || ventanaPago.PagoRealizado == null) return;

            using (var db = new GymDbContext())
            {
                var socioDb = db.Socios.Find(socio.Id);
                if (socioDb == null) return;

                db.Pagos.Add(ventanaPago.PagoRealizado);
                socioDb.FechaVencimiento = ventanaPago.PagoRealizado.FechaVencimiento;
                db.SaveChanges();
            }

            BackupService.NotificarCambioCritico();
            CargarDeudores();
            MessageBox.Show(
                $"Pago registrado con éxito. Válido hasta: {ventanaPago.PagoRealizado.FechaVencimiento:dd/MM/yyyy}",
                "Cobro exitoso",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}