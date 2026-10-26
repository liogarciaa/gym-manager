using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using GymManager.Data;

namespace GymManager.Services
{
    public static class BackupService
    {
        private static Timer? _debounceTimer;
        private static readonly object _lockObj = new object();

        /// <summary>
        /// Llama a este método tras registrar, modificar o cobrar (ej: después de SaveChangesAsync).
        /// Espera 45 segundos de inactividad antes de refrescar GymManager_Ultimo.db en OneDrive.
        /// </summary>
        public static void NotificarCambioCritico()
        {
            lock (_lockObj)
            {
                _debounceTimer?.Dispose();
                _debounceTimer = new Timer(_ =>
                {
                    Task.Run(() => SincronizarCopiaRapida());
                }, null, TimeSpan.FromSeconds(120), Timeout.InfiniteTimeSpan);
            }
        }

        /// <summary>
        /// Copia silenciosa en segundo plano que actualiza la última versión en OneDrive sin generar snapshots históricos.
        /// </summary>
        private static void SincronizarCopiaRapida()
        {
            try
            {
                string destinoCarpeta = ObtenerCarpetaDestino();
                Directory.CreateDirectory(destinoCarpeta);

                string dbPath = ObtenerRutaBaseDatos();
                if (string.IsNullOrEmpty(dbPath) || !File.Exists(dbPath))
                    return;

                // Checkpoint PASSIVE: vuelca páginas de WAL a disco sin bloquear operaciones concurrentes
                using (var db = new GymDbContext())
                {
                    db.Database.ExecuteSqlRaw("PRAGMA wal_checkpoint(PASSIVE);");
                }

                string archivoUltimo = Path.Combine(destinoCarpeta, "GymManager_Ultimo.db");
                File.Copy(dbPath, archivoUltimo, overwrite: true);
            }
            catch
            {
                // Falla silenciosa en segundo plano para no interrumpir la operativa
            }
        }

        /// <summary>
        /// Respaldo formal ejecutado en el OnClosing de MainWindow.
        /// Realiza checkpoint FULL, genera snapshot con fecha/hora y limpia copias con más de 10 días.
        /// </summary>
        public static void RealizarBackupAutomatico()
        {
            try
            {
                // Cancela cualquier timer pendiente de debounce al cerrar
                lock (_lockObj)
                {
                    _debounceTimer?.Dispose();
                    _debounceTimer = null;
                }

                string destinoCarpeta = ObtenerCarpetaDestino();
                Directory.CreateDirectory(destinoCarpeta);

                string dbPath = ObtenerRutaBaseDatos();
                if (string.IsNullOrEmpty(dbPath) || !File.Exists(dbPath))
                    return;

                // Checkpoint FULL: asegura que todo el WAL quede volcado al archivo .db principal
                using (var db = new GymDbContext())
                {
                    db.Database.ExecuteSqlRaw("PRAGMA wal_checkpoint(FULL);");
                }

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmm");
                string archivoHistorico = Path.Combine(destinoCarpeta, $"GymManager_{timestamp}.db");
                string archivoUltimo = Path.Combine(destinoCarpeta, "GymManager_Ultimo.db");

                File.Copy(dbPath, archivoHistorico, overwrite: true);
                File.Copy(dbPath, archivoUltimo, overwrite: true);

                LimpiarBackupsAntiguos(destinoCarpeta, diasRetencion: 10);
            }
            catch
            {
                // Evita que un error de I/O al cerrar bloquee la salida de la aplicación
            }
        }

        private static string ObtenerCarpetaDestino()
        {
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string oneDrivePath = Environment.GetEnvironmentVariable("OneDriveConsumer") 
                                  ?? Environment.GetEnvironmentVariable("OneDrive") 
                                  ?? Path.Combine(userProfile, "OneDrive");

            if (Directory.Exists(oneDrivePath))
            {
                return Path.Combine(oneDrivePath, "Backups_GymManager");
            }

            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "GymManager_Backups");
        }

        private static string ObtenerRutaBaseDatos()
        {
            return DatabasePaths.DatabasePath;
        }

        private static void LimpiarBackupsAntiguos(string carpeta, int diasRetencion)
        {
            try
            {
                var limite = DateTime.Now.AddDays(-diasRetencion);
                var dir = new DirectoryInfo(carpeta);
                foreach (var file in dir.GetFiles("GymManager_*.db"))
                {
                    if (file.Name != "GymManager_Ultimo.db" && file.CreationTime < limite)
                    {
                        file.Delete();
                    }
                }
            }
            catch { }
        }
    }
}