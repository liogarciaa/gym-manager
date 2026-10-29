using System.Windows;
using System.Windows.Media;
using GymManager.Data;
using GymManager.Licensing;
using GymManager.Services;
using GymManager.Views;
using Microsoft.EntityFrameworkCore;

namespace GymManager
{
    public partial class App : Application
    {
        public DoorControllerService DoorController { get; } = new();
        public AccessDisplayWindow? AccessDisplay { get; private set; }
        public AccessDisplayWindow? AccessDisplayPreview { get; private set; }
        private AccessDisplayState _accessDisplayState = AccessDisplayState.Waiting;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

#if DEBUG
            LicenseRuntime.SetCurrent(new GymLicensePayload
            {
                LicenseId = "development",
                GymName = "GymManager (desarrollo)",
                InstallationId = InstallationIdentity.GetCurrent(),
                Features = [LicenseFeatures.NfcAccess, LicenseFeatures.CustomerDisplay],
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
            DatabasePaths.PrepareApplicationDatabase();
            using (var db = new GymDbContext())
            {
                db.Database.EnsureCreated();
                DatabaseSchemaUpdater.EnsureCurrentSchema(db.Database.GetDbConnection());
            }

            _ = DoorController.ConnectAsync();

            MainWindow = new MainWindow();
            MainWindow.Show();

            if (LicenseRuntime.HasFeature(LicenseFeatures.CustomerDisplay))
            {
                AccessDisplay = AccessDisplayWindow.CreateForSecondaryMonitor();
            }
            AccessDisplay?.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            AccessDisplayPreview?.Close();
            AccessDisplay?.Close();
            DoorController.Dispose();
            base.OnExit(e);
        }

        public void ShowAccessResult(string title, string details, Color background, Color border, Color titleColor, Color detailsColor)
        {
            _accessDisplayState = new AccessDisplayState(false, title, details, background, border, titleColor, detailsColor);
            ApplyAccessDisplayState(AccessDisplay);
            ApplyAccessDisplayState(AccessDisplayPreview);
        }

        public void ShowAccessWaiting()
        {
            _accessDisplayState = AccessDisplayState.Waiting;
            ApplyAccessDisplayState(AccessDisplay);
            ApplyAccessDisplayState(AccessDisplayPreview);
        }

        public void ToggleAccessDisplayPreview()
        {
            if (AccessDisplayPreview?.IsVisible == true)
            {
                AccessDisplayPreview.Close();
                AccessDisplayPreview = null;
                return;
            }

            AccessDisplayPreview = AccessDisplayWindow.CreatePreview();
            AccessDisplayPreview.Closed += (_, _) => AccessDisplayPreview = null;
            ApplyAccessDisplayState(AccessDisplayPreview);
            AccessDisplayPreview.Show();
        }

        private void ApplyAccessDisplayState(AccessDisplayWindow? window)
        {
            if (window is null)
            {
                return;
            }

            if (_accessDisplayState.IsWaiting)
            {
                window.ShowWaiting();
            }
            else
            {
                window.ShowResult(
                    _accessDisplayState.Title,
                    _accessDisplayState.Details,
                    _accessDisplayState.Background,
                    _accessDisplayState.Border,
                    _accessDisplayState.TitleColor,
                    _accessDisplayState.DetailsColor);
            }
        }

        private sealed record AccessDisplayState(
            bool IsWaiting,
            string Title,
            string Details,
            Color Background,
            Color Border,
            Color TitleColor,
            Color DetailsColor)
        {
            public static AccessDisplayState Waiting { get; } = new(
                true,
                string.Empty,
                string.Empty,
                Colors.White,
                Colors.Transparent,
                Colors.Black,
                Colors.Black);
        }
    }
}