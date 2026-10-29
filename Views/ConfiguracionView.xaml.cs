using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Microsoft.EntityFrameworkCore;
using GymManager.Data;
using GymManager.Licensing;
using GymManager.Models;

namespace GymManager.Views
{
    public partial class ConfiguracionView : UserControl
    {
        private string _claveGuardada = string.Empty;

        public ConfiguracionView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CargarConfiguracion();
            CargarEstadoLicencia();
        }

        private void CargarEstadoLicencia()
        {
            var license = LicenseRuntime.Current;
            TxtLicenciaGimnasio.Text = license is null
                ? "Licencia no cargada"
                : $"Gimnasio: {license.GymName}";
            TxtLicenciaNfc.Text = LicenseRuntime.HasFeature(LicenseFeatures.NfcAccess)
                ? "Control de acceso NFC: habilitado"
                : "Control de acceso NFC: no incluido";
            TxtLicenciaPantallaCliente.Text = LicenseRuntime.HasFeature(LicenseFeatures.CustomerDisplay)
                ? "Pantalla secundaria para clientes: habilitada"
                : "Pantalla secundaria para clientes: no incluida";
            TxtIdentificadorInstalacion.Text = InstallationIdentity.GetCurrent();
        }

        private void BtnCopiarIdInstalacion_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(TxtIdentificadorInstalacion.Text);
            MessageBox.Show("Identificador copiado.", "Licencia", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnImportarLicencia_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Seleccionar licencia de GymManager",
                Filter = "Licencia GymManager (*.gymlicense)|*.gymlicense|Todos los archivos (*.*)|*.*",
                Multiselect = false
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                var result = LicenseRuntime.Install(File.ReadAllText(dialog.FileName));
                if (!result.IsValid)
                {
                    MessageBox.Show(result.Message, "Licencia inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                CargarEstadoLicencia();
                MessageBox.Show("Licencia actualizada. Las próximas pantallas usarán los permisos nuevos.",
                    "Licencia", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (IOException)
            {
                MessageBox.Show("No se pudo leer el archivo de licencia.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("No hay permiso para leer el archivo de licencia.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CargarConfiguracion()
        {
            using (var db = new GymDbContext())
            {
                var config = db.Configuraciones.FirstOrDefault() ?? new Configuracion();

                TxtPrecioCuota.Text = config.PrecioCuotaMensual.ToString("0.##");
                TxtDiasTolerancia.Text = config.DiasToleranciaMora.ToString();
                TxtNombreGimnasio.Text = config.NombreGimnasio;
                TxtTelefono.Text = config.TelefonoContacto;
                TxtDireccion.Text = config.Direccion;

                // Almacenamos la clave actual en memoria
                _claveGuardada = config.ClaveAdmin ?? string.Empty;

                // Limpiamos los inputs por seguridad
                TxtClaveActual.Password = string.Empty;
                TxtClaveNueva.Password = string.Empty;

                // Si nunca se configuró clave, deshabilitamos el campo de clave actual
                PnlClaveActual.IsEnabled = !string.IsNullOrEmpty(_claveGuardada);
            }
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(TxtPrecioCuota.Text.Trim(), out decimal precio) || precio < 0)
            {
                MessageBox.Show("Ingrese un precio de cuota válido.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(TxtDiasTolerancia.Text.Trim(), out int tolerancia) || tolerancia < 0)
            {
                MessageBox.Show("Los días de tolerancia deben ser un entero no negativo.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string claveActualIngresada = TxtClaveActual.Password.Trim();
            string claveNuevaIngresada = TxtClaveNueva.Password.Trim();

            // 1. Verificación de seguridad si intentan alterar la clave
            if (!string.IsNullOrEmpty(claveNuevaIngresada) || !string.IsNullOrEmpty(claveActualIngresada))
            {
                // Si el sistema ya contaba con una clave previa, debe coincidir exactamente
                if (!string.IsNullOrEmpty(_claveGuardada))
                {
                    if (claveActualIngresada != _claveGuardada)
                    {
                        MessageBox.Show("La contraseña actual ingresada es incorrecta. No se aplicaron modificaciones de seguridad.", 
                                        "Acceso Denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                        TxtClaveActual.SelectAll();
                        TxtClaveActual.Focus();
                        return;
                    }
                }
            }

            // 2. Persistencia en la base de datos
            using (var db = new GymDbContext())
            {
                var config = db.Configuraciones.FirstOrDefault();
                if (config == null)
                {
                    config = new Configuracion();
                    db.Configuraciones.Add(config);
                }

                config.PrecioCuotaMensual = precio;
                config.DiasToleranciaMora = tolerancia;
                config.NombreGimnasio = TxtNombreGimnasio.Text.Trim();
                config.TelefonoContacto = TxtTelefono.Text.Trim();
                config.Direccion = TxtDireccion.Text.Trim();

                // Caso A: Ya existía clave y se validó la clave actual correctamente
                if (!string.IsNullOrEmpty(_claveGuardada) && claveActualIngresada == _claveGuardada)
                {
                    config.ClaveAdmin = claveNuevaIngresada; // Si la dejó vacía, quita la protección
                    _claveGuardada = claveNuevaIngresada;
                }
                // Caso B: No había clave registrada previamente y se establece por primera vez
                else if (string.IsNullOrEmpty(_claveGuardada) && !string.IsNullOrEmpty(claveNuevaIngresada))
                {
                    config.ClaveAdmin = claveNuevaIngresada;
                    _claveGuardada = claveNuevaIngresada;
                }

                db.SaveChanges();

                if (Application.Current.MainWindow is MainWindow mainWin)
                {
                    mainWin.CargarNombreEstablecimiento();
                }
            }

            // Limpieza y actualización visual de los campos de contraseña
            TxtClaveActual.Password = string.Empty;
            TxtClaveNueva.Password = string.Empty;
            PnlClaveActual.IsEnabled = !string.IsNullOrEmpty(_claveGuardada);

            MessageBox.Show("Configuración guardada correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnBackup_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog
            {
                FileName = $"GymManager_Backup_{DateTime.Now:yyyyMMdd_HHmm}.db",
                Filter = "Base de datos SQLite (*.db)|*.db"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    string dbPath = DatabasePaths.DatabasePath;
                    using (var db = new GymDbContext())
                    {
                        db.Database.ExecuteSqlRaw("PRAGMA wal_checkpoint(FULL);");
                    }

                    if (File.Exists(dbPath))
                    {
                        File.Copy(dbPath, sfd.FileName, overwrite: true);
                        MessageBox.Show("Copia de seguridad generada con éxito.", "Backup Completado", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("No se encontró el archivo de la base de datos para respaldar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al crear la copia de respaldo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnRestaurarBackup_Click(object sender, RoutedEventArgs e)
        {
            // 1. Detectar carpeta inicial (OneDrive > Documentos > Escritorio)
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string oneDrivePath = Environment.GetEnvironmentVariable("OneDriveConsumer") 
                                  ?? Environment.GetEnvironmentVariable("OneDrive") 
                                  ?? Path.Combine(userProfile, "OneDrive");

            string carpetaInicial = Path.Combine(oneDrivePath, "Backups_GymManager");
            if (!Directory.Exists(carpetaInicial))
            {
                carpetaInicial = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            var ofd = new OpenFileDialog
            {
                Title = "Seleccionar archivo de base de datos para restaurar",
                InitialDirectory = Directory.Exists(carpetaInicial) ? carpetaInicial : userProfile,
                Filter = "Base de datos GymManager (*.db)|*.db|Todos los archivos (*.*)|*.*",
                Multiselect = false
            };

            if (ofd.ShowDialog() != true) return;

            // 2. Advertencia de seguridad crítica
            var confirmacion = MessageBox.Show(
                $"¿Confirmás la restauración del archivo seleccionado?\n\nArchivo: {Path.GetFileName(ofd.FileName)}\n\n" +
                "⚠️ ADVERTENCIA: Esta acción reemplazará la base de datos actual con los datos del respaldo seleccionado. " +
                "La aplicación se reiniciará automáticamente al completar el proceso.",
                "Confirmar Restauración de Datos",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmacion != MessageBoxResult.Yes) return;

            try
            {
                // 3. Localizar el archivo principal de la app
                string dbPath = DatabasePaths.DatabasePath;

                // 4. Liberar todas las conexiones SQLite en memoria
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // 5. Sobrescribir el archivo .db y limpiar archivos WAL/SHM temporales si existen
                File.Copy(ofd.FileName, dbPath, overwrite: true);

                string walPath = dbPath + "-wal";
                string shmPath = dbPath + "-shm";
                if (File.Exists(walPath)) File.Delete(walPath);
                if (File.Exists(shmPath)) File.Delete(shmPath);

                MessageBox.Show(
                    "La base de datos fue restaurada con éxito.\nEl sistema se reiniciará ahora para aplicar los cambios.",
                    "Restauración Completada",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // 6. Reiniciar la aplicación limpiamente
                System.Diagnostics.Process.Start(Environment.ProcessPath!);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un error al restaurar la base de datos:\n{ex.Message}", "Error al restaurar", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}