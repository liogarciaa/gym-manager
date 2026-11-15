using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace GymManager.Views;

public partial class RutinaQrWindow : Window
{
    public RutinaQrWindow(string texto, byte[] imagenQr)
    {
        InitializeComponent();
        TxtContenido.Text = texto;

        using var stream = new MemoryStream(imagenQr);
        var imagen = new BitmapImage();
        imagen.BeginInit();
        imagen.CacheOption = BitmapCacheOption.OnLoad;
        imagen.StreamSource = stream;
        imagen.EndInit();
        imagen.Freeze();
        ImgQr.Source = imagen;
    }
}
