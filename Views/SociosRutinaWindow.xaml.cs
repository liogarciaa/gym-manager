using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using GymManager.Models;

namespace GymManager.Views;

public partial class SociosRutinaWindow : Window
{
    private readonly List<SocioSeleccionable> _socios;
    private readonly ICollectionView _vistaSocios;

    public event EventHandler? AsignacionSolicitada;

    public IReadOnlyList<int> SocioIdsSeleccionados =>
        _socios.Where(socio => socio.Seleccionado).Select(socio => socio.Id).ToArray();

    public SociosRutinaWindow(IEnumerable<Socio> socios)
    {
        InitializeComponent();
        _socios = socios.Select(socio => new SocioSeleccionable
        {
            Id = socio.Id,
            NombreCompleto = socio.NombreCompleto,
            Dni = socio.Dni
        }).ToList();

        _vistaSocios = CollectionViewSource.GetDefaultView(_socios);
        _vistaSocios.Filter = FiltrarSocio;
        GridSocios.ItemsSource = _vistaSocios;
    }

    public void ConfirmarAsignacion(IEnumerable<int> socioIds)
    {
        int[] idsAsignados = socioIds.ToArray();
        foreach (SocioSeleccionable socio in _socios.Where(socio => idsAsignados.Contains(socio.Id)))
        {
            socio.Seleccionado = false;
            socio.PuedeAsignar = false;
        }

        GridSocios.UnselectAll();
        TxtEstado.Text = idsAsignados.Length == 1
            ? "Rutina asignada. La selección se desmarcó."
            : $"Rutina asignada a {idsAsignados.Length} socios. Las selecciones se desmarcaron.";
    }

    private bool FiltrarSocio(object elemento)
    {
        if (elemento is not SocioSeleccionable socio || string.IsNullOrWhiteSpace(TxtBuscar?.Text))
        {
            return true;
        }

        string busqueda = TxtBuscar.Text.Trim();
        return socio.NombreCompleto.Contains(busqueda, StringComparison.CurrentCultureIgnoreCase) ||
               socio.Dni.Contains(busqueda, StringComparison.CurrentCultureIgnoreCase);
    }

    private void TxtBuscar_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) =>
        _vistaSocios.Refresh();

    private void BtnAsignar_Click(object sender, RoutedEventArgs e)
    {
        if (SocioIdsSeleccionados.Count == 0)
        {
            MessageBox.Show(this, "Seleccioná al menos un socio.", "Sin selección", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        AsignacionSolicitada?.Invoke(this, EventArgs.Empty);
    }
}

public sealed class SocioSeleccionable : INotifyPropertyChanged
{
    private bool _seleccionado;
    private bool _puedeAsignar = true;

    public int Id { get; init; }
    public string NombreCompleto { get; init; } = string.Empty;
    public string Dni { get; init; } = string.Empty;
    public string Estado => PuedeAsignar ? "Disponible" : "Asignada";

    public bool Seleccionado
    {
        get => _seleccionado;
        set
        {
            if (_seleccionado == value) return;
            _seleccionado = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Seleccionado)));
        }
    }

    public bool PuedeAsignar
    {
        get => _puedeAsignar;
        set
        {
            if (_puedeAsignar == value) return;
            _puedeAsignar = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PuedeAsignar)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Estado)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
