using System;
using System.Windows;
using GymManager.Licensing;
using GymManager.Models;

namespace GymManager.Views
{
    public partial class NuevoSocioWindow : Window
    {
        public Socio? SocioCreado { get; private set; }
        private readonly Socio? _socioExistente;

        // Constructor para ALTA (sin parámetros)
        public NuevoSocioWindow() : this(null) { }

        // Constructor que admite un socio existente para EDICIÓN
        public NuevoSocioWindow(Socio? socioParaEditar)
        {
            InitializeComponent();
            _socioExistente = socioParaEditar;

            // Si el módulo NFC no está activo, se oculta la fila y se reduce la ventana
            if (!LicenseRuntime.HasFeature(LicenseFeatures.NfcAccess))
            {
                PnlCodigoAcceso.Visibility = Visibility.Collapsed;
                Height = 440;
            }

            if (_socioExistente != null)
            {
                Title = "Editar Ficha de Socio";
                TxtTituloModal.Text = "Editar Datos del Socio";
                TxtSubtituloModal.Text = $"Modificando la ficha de {_socioExistente.NombreCompleto}";
                TxtNombre.Text = _socioExistente.Nombre;
                TxtApellido.Text = _socioExistente.Apellido;
                TxtDni.Text = _socioExistente.Dni;
                TxtTelefono.Text = _socioExistente.Telefono;
                TxtCodigoAcceso.Text = _socioExistente.CodigoAcceso ?? string.Empty;
            }
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtNombre.Text) || string.IsNullOrWhiteSpace(TxtApellido.Text))
            {
                MessageBox.Show("Por favor, ingresá al menos Nombre y Apellido.", "Datos incompletos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string codigoTexto = TxtCodigoAcceso.Text.Trim();
            string? codigoFinal = string.IsNullOrEmpty(codigoTexto) ? null : codigoTexto;

            if (_socioExistente != null)
            {
                // MODO EDICIÓN: Modificamos las propiedades del objeto existente
                _socioExistente.Nombre = TxtNombre.Text.Trim();
                _socioExistente.Apellido = TxtApellido.Text.Trim();
                _socioExistente.Dni = TxtDni.Text.Trim();
                _socioExistente.Telefono = TxtTelefono.Text.Trim();
                _socioExistente.CodigoAcceso = codigoFinal;

                SocioCreado = _socioExistente;
            }
            else
            {
                // MODO ALTA: Creamos una nueva entidad
                SocioCreado = new Socio
                {
                    Nombre = TxtNombre.Text.Trim(),
                    Apellido = TxtApellido.Text.Trim(),
                    Dni = TxtDni.Text.Trim(),
                    Telefono = TxtTelefono.Text.Trim(),
                    CodigoAcceso = codigoFinal,
                    FechaAlta = DateTime.Now,
                    EstaActivo = true
                };
            }

            DialogResult = true;
        }

        private void BtnLimpiarCodigo_Click(object sender, RoutedEventArgs e)
        {
            TxtCodigoAcceso.Text = string.Empty;
            TxtCodigoAcceso.Focus();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}