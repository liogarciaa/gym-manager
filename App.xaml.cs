using System.Windows;
using GymManager.Data;
using GymManager.Licensing;
using GymManager.Views;

namespace GymManager
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

#if DEBUG
            LicenseRuntime.SetCurrent(new GymLicensePayload
            {
                LicenseId = "development",
                GymName = "GymManager (desarrollo)",
                InstallationId = InstallationIdentity.GetCurrent(),
                Features = [LicenseFeatures.NfcAccess],
                IssuedUtc = DateTimeOffset.UtcNow
            });
#else
            var licenseResult = LicenseRuntime.LoadCurrent();
            if (!licenseResult.IsValid || licenseResult.License is null)
            {
                var activationWindow = new LicenseActivationWindow(licenseResult.Message);
                if (activationWindow.ShowDialog() != true)
                {
                    Shutdown();
                    return;
                }

                licenseResult = LicenseRuntime.LoadCurrent();
            }

            if (!licenseResult.IsValid || licenseResult.License is null)
            {
                MessageBox.Show(licenseResult.Message, "Licencia inválida", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            LicenseRuntime.SetCurrent(licenseResult.License);
#endif

            // Crea la base de datos y sus tablas si aún no existen
            using (var db = new GymDbContext())
            {
                db.Database.EnsureCreated();
            }

            MainWindow = new MainWindow();
            MainWindow.Show();
        }
    }
}