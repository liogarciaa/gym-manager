using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace GymManager.Views;

public partial class AccessDisplayWindow : Window
{
    private const uint MonitorInfoPrimary = 1;
    private readonly DispatcherTimer _clockTimer;

    private AccessDisplayWindow(MonitorBounds? bounds, bool preview)
    {
        InitializeComponent();

        if (preview)
        {
            Title = "Vista previa - pantalla del cliente";
            WindowStyle = WindowStyle.SingleBorderWindow;
            ResizeMode = ResizeMode.CanResize;
            ShowInTaskbar = true;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Width = 960;
            Height = 600;
        }
        else if (bounds is not null)
        {
            Left = bounds.Left;
            Top = bounds.Top;
            Width = bounds.Right - bounds.Left;
            Height = bounds.Bottom - bounds.Top;
            WindowState = WindowState.Maximized;
        }

        _clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _clockTimer.Tick += (_, _) => UpdateClock();
        UpdateClock();
    }

    public static AccessDisplayWindow? CreateForSecondaryMonitor()
    {
        List<MonitorBounds> monitors = GetMonitorBounds();
        MonitorBounds? secondary = monitors.FirstOrDefault(monitor => !monitor.IsPrimary);
        return secondary is null ? null : new AccessDisplayWindow(secondary, false);
    }

    public static AccessDisplayWindow CreatePreview() => new(null, true);

    public void ShowResult(string title, string details, Color background, Color border, Color titleColor, Color detailsColor)
    {
        BrdResultado.Background = new SolidColorBrush(background);
        BrdResultado.BorderBrush = new SolidColorBrush(border);
        TxtMensajeEstado.Text = title;
        TxtMensajeEstado.Foreground = new SolidColorBrush(titleColor);
        TxtDetalleSocio.Text = details;
        TxtDetalleSocio.Foreground = new SolidColorBrush(detailsColor);
        PnlRelojEnEspera.Visibility = Visibility.Collapsed;
        PnlMensajeResultado.Visibility = Visibility.Visible;
    }

    public void ShowWaiting()
    {
        BrdResultado.Background = new SolidColorBrush(Color.FromRgb(248, 250, 252));
        BrdResultado.BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225));
        PnlMensajeResultado.Visibility = Visibility.Collapsed;
        PnlRelojEnEspera.Visibility = Visibility.Visible;
        UpdateClock();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _clockTimer.Start();
        ShowWaiting();
    }

    private void UpdateClock()
    {
        DateTime now = DateTime.Now;
        TxtRelojDigital.Text = now.ToString("HH:mm:ss");

        string dateText = now.ToString("dddd, d 'de' MMMM", new CultureInfo("es-ES"));
        TxtFechaEnEspera.Text = char.ToUpper(dateText[0]) + dateText[1..];
    }

    private static List<MonitorBounds> GetMonitorBounds()
    {
        List<MonitorBounds> monitors = [];
        MonitorEnumCallback callback = (IntPtr monitor, IntPtr _, ref NativeRect _, IntPtr _) =>
        {
            MonitorInfo info = new() { Size = (uint)Marshal.SizeOf<MonitorInfo>() };
            if (GetMonitorInfo(monitor, ref info))
            {
                monitors.Add(new MonitorBounds(
                    info.Monitor.Left,
                    info.Monitor.Top,
                    info.Monitor.Right,
                    info.Monitor.Bottom,
                    (info.Flags & MonitorInfoPrimary) != 0));
            }

            return true;
        };

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);
        return monitors;
    }

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumCallback callback, IntPtr data);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

    private delegate bool MonitorEnumCallback(IntPtr monitor, IntPtr hdc, ref NativeRect monitorRect, IntPtr data);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public uint Size;
        public NativeRect Monitor;
        public NativeRect WorkArea;
        public uint Flags;
    }

    private sealed record MonitorBounds(int Left, int Top, int Right, int Bottom, bool IsPrimary);
}
