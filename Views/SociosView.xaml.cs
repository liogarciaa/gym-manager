using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GymManager.Data;
using GymManager.Models;
using System.Windows.Media;
using GymManager.Services;

namespace GymManager.Views
{
    public partial class SociosView : UserControl
    {
        private ObservableCollection<Socio> _socios = new();

        public SociosView()
        {
            InitializeComponent();
            CargarSociosDesdeBaseDeDatos();
        }

        private void CargarSociosDesdeBaseDeDatos()
        {
            using (var db = new GymDbContext())
            {
                var listaDb = db.Socios.Where(s => !s.Eliminado).OrderBy(s => s.Id).ToList();
                _socios = new ObservableCollection<Socio>(listaDb);
                SociosDataGrid.ItemsSource = _socios;
            }
        }

        private void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filtro = TxtBuscar.Text.Trim();

            if (string.IsNullOrWhiteSpace(filtro))
            {
                SociosDataGrid.ItemsSource = _socios;
                return;
            }

            string filtroLower = filtro.ToLower();
            bool esNumero = int.TryParse(filtro, out int idBuscado);

            var resultados = _socios.Where(s =>
            {
                if (esNumero)
                {
                    return s.Id == idBuscado;
                }

                return s.Nombre.ToLower().Contains(filtroLower) ||
                       s.Apellido.ToLower().Contains(filtroLower);
            }).ToList();

            SociosDataGrid.ItemsSource = resultados;
        }

        private void BtnNuevoSocio_Click(object sender, RoutedEventArgs e)
        {
            var ventanaAlta = new NuevoSocioWindow();
            ventanaAlta.Owner = Window.GetWindow(this);

            if (ventanaAlta.ShowDialog() == true && ventanaAlta.SocioCreado != null)
            {
                var nuevo = ventanaAlta.SocioCreado;

                // VALIDACIÓN DE LLAVERO DUPLICADO
                if (!string.IsNullOrEmpty(nuevo.CodigoAcceso))
                {
                    using (var db = new GymDbContext())
                    {
                        bool yaExiste = db.Socios.Any(s => s.CodigoAcceso == nuevo.CodigoAcceso && !s.Eliminado) ||
                                        db.LlaverosMaestros.Any(llavero => llavero.CodigoAcceso == nuevo.CodigoAcceso);
                        if (yaExiste)
                        {
                            MessageBox.Show("Ese llavero ya está asignado a otro socio activo.", "Credencial Duplicada", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }
                }

                using (var db = new GymDbContext())
                {
                    db.Socios.Add(nuevo);
                    db.SaveChanges();
                }

                BackupService.NotificarCambioCritico();

                _socios.Add(nuevo);
                TxtBuscar_TextChanged(TxtBuscar, null!);
            }
        }
        private void AbrirEdicionSocio(Socio socioSeleccionado)
        {
            var ventanaEdicion = new NuevoSocioWindow(socioSeleccionado);
            ventanaEdicion.Owner = Window.GetWindow(this);

            if (ventanaEdicion.ShowDialog() == true)
            {
                // VALIDACIÓN DE LLAVERO DUPLICADO (excluyendo al socio actual con s.Id != socioSeleccionado.Id)
                if (!string.IsNullOrEmpty(socioSeleccionado.CodigoAcceso))
                {
                    using (var db = new GymDbContext())
                    {
                        bool yaExiste = db.Socios.Any(s => s.CodigoAcceso == socioSeleccionado.CodigoAcceso 
                                                        && !s.Eliminado 
                                                        && s.Id != socioSeleccionado.Id) ||
                                        db.LlaverosMaestros.Any(llavero => llavero.CodigoAcceso == socioSeleccionado.CodigoAcceso);
                        if (yaExiste)
                        {
                            MessageBox.Show("Ese llavero ya está asignado a otro socio activo.", "Credencial Duplicada", MessageBoxButton.OK, MessageBoxImage.Warning);
                            CargarSociosDesdeBaseDeDatos(); // Revertimos los cambios en memoria
                            return;
                        }
                    }
                }

                using (var db = new GymDbContext())
                {
                    db.Socios.Update(socioSeleccionado);
                    db.SaveChanges();
                }

                BackupService.NotificarCambioCritico();

                // Refrescamos la grilla para mostrar los cambios
                SociosDataGrid.Items.Refresh();
            }
        }
        private void SociosDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SociosDataGrid.SelectedItem is Socio socioSeleccionado)
            {
                AbrirEdicionSocio(socioSeleccionado);
            }
        }

        private void MenuEditar_Click(object sender, RoutedEventArgs e)
        {
            if (SociosDataGrid.SelectedItem is Socio socioSeleccionado)
            {
                AbrirEdicionSocio(socioSeleccionado);
            }
        }

        private void MenuCambiarEstado_Click(object sender, RoutedEventArgs e)
        {
            if (SociosDataGrid.SelectedItem is Socio socioSeleccionado)
            {
                // Invertimos el estado (Activo <-> Inactivo)
                socioSeleccionado.EstaActivo = !socioSeleccionado.EstaActivo;

                using (var db = new GymDbContext())
                {
                    db.Socios.Update(socioSeleccionado);
                    db.SaveChanges();
                }

                SociosDataGrid.Items.Refresh();
            }
        }

    // Permite deseleccionar si hacés clic de nuevo en la misma fila
private void SociosDataGrid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
{
    if (e.OriginalSource is DependencyObject depObj)
    {
        // 1. Si el clic cayó sobre un CheckBox o adentro de su celda, salimos y dejamos actuar al CheckBox
        if (EsDescendienteDe<CheckBox>(depObj))
        {
            return;
        }

        // 2. Buscamos si el clic fue dentro de una fila de datos
        var fila = ItemsControl.ContainerFromElement(SociosDataGrid, depObj) as DataGridRow;
        
        // 3. Si la fila ya estaba seleccionada y le hacemos clic izquierdo normal, deseleccionamos
        if (fila != null && fila.IsSelected && e.LeftButton == MouseButtonState.Pressed && e.ClickCount == 1)
        {
            SociosDataGrid.SelectedItem = null;
            e.Handled = true;
        }
    }
}

// Método auxiliar para detectar si el control clickeado está dentro de un tipo específico (ej: CheckBox)
private static bool EsDescendienteDe<T>(DependencyObject elemento) where T : DependencyObject
{
    DependencyObject? actual = elemento;
    while (actual != null)
    {
        if (actual is T) return true;
        actual = VisualTreeHelper.GetParent(actual);
    }
    return false;
}

// Si hacés clic en el área blanca fuera de las filas, deselecciona
private void Fondo_MouseDown(object sender, MouseButtonEventArgs e)
{
    SociosDataGrid.SelectedItem = null;
}     

private void ChkActivo_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
{
    if (sender is CheckBox chk && chk.DataContext is Socio socio)
    {
        // Invertimos el valor en un solo paso
        socio.EstaActivo = !socio.EstaActivo;
        chk.IsChecked = socio.EstaActivo;

        using (var db = new GymDbContext())
        {
            db.Socios.Update(socio);
            db.SaveChanges();
        }

        SociosDataGrid.Items.Refresh();

        // Marcamos el evento como manejado para evitar ciclos de foco innecesarios
        e.Handled = true;
    }
}

    private void MenuRegistrarPago_Click(object sender, RoutedEventArgs e)
{
    if (SociosDataGrid.SelectedItem is Socio socioSeleccionado)
    {
        var ventanaPago = new RegistrarPagoWindow(socioSeleccionado);
        ventanaPago.Owner = Window.GetWindow(this);

        if (ventanaPago.ShowDialog() == true && ventanaPago.PagoRealizado != null)
        {
            var nuevoPago = ventanaPago.PagoRealizado;

            using (var db = new GymDbContext())
            {
                // 1. Guardamos el registro del pago en la tabla Pagos
                db.Pagos.Add(nuevoPago);

                // 2. Actualizamos el vencimiento del socio en la base de datos
                var socioDb = db.Socios.Find(socioSeleccionado.Id);
                if (socioDb != null)
                {
                    socioDb.FechaVencimiento = nuevoPago.FechaVencimiento;
                    db.SaveChanges();
                }
            }

            BackupService.NotificarCambioCritico();

            // 3. Actualizamos el objeto en memoria para que la grilla refleje el cambio al instante
            socioSeleccionado.FechaVencimiento = nuevoPago.FechaVencimiento;
            SociosDataGrid.Items.Refresh();

            MessageBox.Show($"Pago registrado con éxito. Válido hasta: {nuevoPago.FechaVencimiento:dd/MM/yyyy}", "Cobro exitoso", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}

   // 1. Método centralizado que contiene toda la lógica de borrado
private void EliminarSocio(Socio socioAEliminar)
{
    var respuesta = MessageBox.Show(
        $"¿Estás seguro de que deseás eliminar al socio {socioAEliminar.NombreCompleto}?\n\nEsta acción no se puede deshacer.",
        "Confirmar eliminación",
        MessageBoxButton.YesNo,
        MessageBoxImage.Warning);

    if (respuesta == MessageBoxResult.Yes)
    {
        // Borramos de SQLite
        using (var db = new GymDbContext())
        {
            var socioDb = db.Socios.Find(socioAEliminar.Id);
            if (socioDb != null)
            {
                socioDb.Eliminado = true;
                socioDb.EstaActivo = false;
                db.SaveChanges(); // Persiste en el archivo GymManager.db
            }
        }

        BackupService.NotificarCambioCritico();

        // Borramos de la colección observable en memoria
        _socios.Remove(socioAEliminar);

        // Actualizamos el filtro de búsqueda si había algo escrito
        TxtBuscar_TextChanged(TxtBuscar, null!);
    }
}

// 2. Manejo de la tecla Suprimir
private void SociosDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
{
    if (e.Key == Key.Delete && SociosDataGrid.SelectedItem is Socio socioSeleccionado)
    {
        EliminarSocio(socioSeleccionado);
        e.Handled = true;
    }
}

// 3. Manejo de la opción en el menú contextual (Clic derecho)
private void MenuEliminarSocio_Click(object sender, RoutedEventArgs e)
{
    if (SociosDataGrid.SelectedItem is Socio socioSeleccionado)
    {
        EliminarSocio(socioSeleccionado);
    }
}

    private void MenuHistorialPagos_Click(object sender, RoutedEventArgs e)
{
    if (SociosDataGrid.SelectedItem is Socio socioSeleccionado)
    {
        var ventanaHistorial = new HistorialPagosWindow(socioSeleccionado);
        ventanaHistorial.Owner = Window.GetWindow(this);
        ventanaHistorial.ShowDialog();
    }

    // Al cerrarse el diálogo, refrescamos la grilla para mostrar el nuevo vencimiento y estado
        SociosDataGrid.Items.Refresh();
}

    } //agregar métodos arriba de este corchete

}