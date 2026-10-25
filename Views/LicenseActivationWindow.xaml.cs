using System.IO;
using System.Windows;
using GymManager.Licensing;
using Microsoft.Win32;

namespace GymManager.Views;

public partial class LicenseActivationWindow : Window
{
    public LicenseActivationWindow(string statusMessage = "")
    {
        InitializeComponent();
        TxtInstallationId.Text = InstallationIdentity.GetCurrent();
        TxtStatus.Text = statusMessage;
    }

    private void BtnCopiarId_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(TxtInstallationId.Text);
        TxtStatus.Foreground = System.Windows.Media.Brushes.DarkGreen;
        TxtStatus.Text = "Identificador copiado. Enviáselo al proveedor para recibir la licencia.";
    }

    private void BtnSeleccionarLicencia_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Seleccionar licencia de GymManager",
            Filter = "Licencia GymManager (*.gymlicense)|*.gymlicense|Todos los archivos (*.*)|*.*",
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true) return;

        try
        {
            string licenseJson = File.ReadAllText(dialog.FileName);
            var result = LicenseRuntime.Install(licenseJson);
            if (!result.IsValid)
            {
                TxtStatus.Foreground = System.Windows.Media.Brushes.DarkRed;
                TxtStatus.Text = result.Message;
                return;
            }

            DialogResult = true;
        }
        catch (IOException)
        {
            TxtStatus.Foreground = System.Windows.Media.Brushes.DarkRed;
            TxtStatus.Text = "No se pudo leer el archivo seleccionado.";
        }
        catch (UnauthorizedAccessException)
        {
            TxtStatus.Foreground = System.Windows.Media.Brushes.DarkRed;
            TxtStatus.Text = "No hay permiso para leer el archivo seleccionado.";
        }
    }
}