using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Effects;
using GymManager.Views;
using GymManager.Data;
using GymManager.Services;

namespace GymManager
{
    public partial class MainWindow : Window
    {
        private readonly Stack<UserControl> _backHistory = new();
        private readonly Stack<UserControl> _forwardHistory = new();

        public MainWindow()
        {
            InitializeComponent();
            CargarNombreEstablecimiento();

            // Vista predeterminada al abrir la aplicación
            ShowView(new DashboardView());
        }

        private void ShowView(UserControl view)
        {
            MainContent.Content = view;
            SyncNavigationSelection(view);
            UpdateHistoryButtons();
        }

        private void NavigateTo(UserControl view)
        {
            if (MainContent.Content is UserControl current)
            {
                if (current.GetType() == view.GetType()) return;
                _backHistory.Push(current);
            }

            _forwardHistory.Clear();
            ShowView(view);
        }

        private void NavigateDirectly(UserControl view)
        {
            _backHistory.Clear();
            _forwardHistory.Clear();
            ShowView(view);
        }

        private void SyncNavigationSelection(UserControl view)
        {
            NavInicio.IsChecked = view is DashboardView;
            NavSocios.IsChecked = view is SociosView;
            NavControlAcceso.IsChecked = view is CheckInView;
            NavDeudores.IsChecked = view is DeudoresView;
            NavPagos.IsChecked = view is PagosView;
            NavReportes.IsChecked = view is ReportesView;
            NavConfiguracion.IsChecked = view is ConfiguracionView;
        }

        private void UpdateHistoryButtons()
        {
            BtnBack.IsEnabled = _backHistory.Count > 0;
            BtnForward.IsEnabled = _forwardHistory.Count > 0;
        }

        private void GoBack()
        {
            if (!_backHistory.TryPop(out var previous)) return;
            if (MainContent.Content is UserControl current) _forwardHistory.Push(current);
            ShowView(previous);
        }

        private void GoForward()
        {
            if (!_forwardHistory.TryPop(out var next)) return;
            if (MainContent.Content is UserControl current) _backHistory.Push(current);
            ShowView(next);
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e) => GoBack();

        private void BtnForward_Click(object sender, RoutedEventArgs e) => GoForward();

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Alt && e.Key == Key.Left)
            {
                GoBack();
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Alt && e.Key == Key.Right)
            {
                GoForward();
                e.Handled = true;
            }

            base.OnPreviewKeyDown(e);
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.XButton1)
            {
                GoBack();
                e.Handled = true;
            }
            else if (e.ChangedButton == MouseButton.XButton2)
            {
                GoForward();
                e.Handled = true;
            }

            base.OnPreviewMouseDown(e);
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
            NavigateDirectly(new DashboardView());
        }

        private void BtnNavSocios_Click(object sender, RoutedEventArgs e)
        {
            NavigateDirectly(new SociosView());
        }

        private void BtnNavAsistencia_Click(object sender, RoutedEventArgs e)
        {
            NavigateDirectly(new CheckInView());
        }

        private void BtnNavPagos_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarAccesoSeguro())
            {
                SyncNavigationSelection((UserControl)MainContent.Content);
                return;
            }

            NavigateDirectly(new PagosView());
        }

        private void BtnNavDeudores_Click(object sender, RoutedEventArgs e)
        {
            NavigateDirectly(new DeudoresView());
        }

        private void BtnNavReportes_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarAccesoSeguro())
            {
                SyncNavigationSelection((UserControl)MainContent.Content);
                return;
            }

            NavigateDirectly(new ReportesView());
        }

        private void BtnNavConfiguracion_Click(object sender, RoutedEventArgs e)
        {
            NavigateDirectly(new ConfiguracionView());
        }

        public void NavegarAControlAcceso()
        {
            NavigateTo(new CheckInView());
        }

        public void NavegarASocios()
        {
            NavigateTo(new SociosView());
        }

        public void NavegarADeudores()
        {
            NavigateTo(new DeudoresView());
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