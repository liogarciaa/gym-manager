using System;
using System.Linq;
using System.Windows;
using System.Windows.Media.Effects;
using GymManager.Views;
using GymManager.Data;
using GymManager.Services;

namespace GymManager
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            CargarNombreEstablecimiento();

            // Vista predeterminada al abrir la aplicación
            MainContent.Content = new DashboardView();
        }

        public void CargarNombreEstablecimiento()
        {
            try
            {
                using (var db = new GymDbContext())
                {
                    var config = db.Configuraciones.FirstOrDefault();
                    string nombre = config?.NombreGimnasio?.Trim() ?? string.Empty;

                    TxtNombreGym.Text = string.IsNullOrWhiteSpace(nombre) 
                        ? "Sucursal Central" 
                        : nombre;
                }
            }
            catch
            {
                TxtNombreGym.Text = "Sucursal Principal";
            }
        }

        // Handlers que coinciden con MainWindow.xaml
        private void BtnNavInicio_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new DashboardView();
        }

        private void BtnNavSocios_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new SociosView();
        }

        private void BtnNavAsistencia_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new CheckInView();
        }

        private void BtnNavPagos_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarAccesoSeguro()) return;

            MainContent.Content = new PagosView();
        }

        private void BtnNavDeudores_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new DeudoresView();
        }

        private void BtnNavReportes_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarAccesoSeguro()) return;

            MainContent.Content = new ReportesView();
        }

        private void BtnNavConfiguracion_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new ConfiguracionView();
        }

        public void NavegarAControlAcceso()
        {
            NavControlAcceso.IsChecked = true;
            MainContent.Content = new CheckInView();
        }

        public void NavegarASocios()
        {
            NavSocios.IsChecked = true;
            MainContent.Content = new SociosView();
        }

        public void NavegarADeudores()
        {
            NavDeudores.IsChecked = true;
            MainContent.Content = new DeudoresView();
        }

        private bool ValidarAccesoSeguro()
        {
            string claveConfigurada;

            using (var db = new GymDbContext())
            {
                claveConfigurada = db.Configuraciones
                                     .Select(c => c.ClaveAdmin)
                                     .FirstOrDefault() ?? string.Empty;
            }

            // Si no se configuró clave en Ajustes, el paso es libre
            if (string.IsNullOrWhiteSpace(claveConfigurada))
            {
                return true;
            }

            // 1. Aplicamos el desenfoque a la ventana principal
            this.Effect = new BlurEffect { Radius = 15, RenderingBias = RenderingBias.Quality };

            try
            {
                // 2. Mostramos el diálogo centrado sobre la ventana
                var prompt = new PasswordPromptDialog(claveConfigurada)
                {
                    Owner = this
                };

                bool? resultado = prompt.ShowDialog();
                return resultado == true && prompt.Autenticado;
            }
            finally
            {
                // 3. Restauramos la nitidez del fondo al cerrar el diálogo
                this.Effect = null;
            }
        }

            protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
            {
                BackupService.RealizarBackupAutomatico();
                base.OnClosing(e);
            }
    }
}