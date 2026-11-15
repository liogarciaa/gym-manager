using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GymManager.Data;
using GymManager.Models;
using GymManager.Services;

namespace GymManager.Views;

public partial class RutinaEditorWindow : Window
{
    private readonly ObservableCollection<EjercicioEdicion> _ejercicios = new();
    private readonly string[] _ejerciciosGuardados;
    private bool _actualizandoSugerencias;

    public string NombreRutina => TxtNombre.Text.Trim();
    public string DescripcionRutina => TxtDescripcion.Text.Trim();
    public IReadOnlyList<EjercicioEdicion> Ejercicios => _ejercicios.ToList();

    public RutinaEditorWindow(Rutina? rutina = null)
    {
        InitializeComponent();

        using (var db = new GymDbContext())
        {
            _ejerciciosGuardados = db.EjerciciosRutina
                .Select(ejercicio => ejercicio.Nombre)
                .Distinct()
                .ToArray();
        }

        if (rutina is not null)
        {
            Title = "Editar rutina";
            TxtNombre.Text = rutina.Nombre;
            TxtDescripcion.Text = rutina.Descripcion;
            foreach (EjercicioRutina ejercicio in rutina.Ejercicios.OrderBy(ejercicio => ejercicio.Orden))
            {
                _ejercicios.Add(new EjercicioEdicion
                {
                    Nombre = ejercicio.Nombre,
                    Series = ejercicio.Series,
                    Repeticiones = ejercicio.Repeticiones,
                    DescansoSegundos = ejercicio.DescansoSegundos,
                    Notas = ejercicio.Notas
                });
            }
        }

        GridEjercicios.ItemsSource = _ejercicios;
        LstSugerencias.ItemsSource = new ObservableCollection<string>();
    }

    private void TxtBuscarEjercicio_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_actualizandoSugerencias) return;

        string busqueda = TxtBuscarEjercicio.Text.Trim();
        var sugerencias = CatalogoEjercicios.ObtenerSugerencias(busqueda, _ejerciciosGuardados);
        LstSugerencias.ItemsSource = sugerencias;
        LstSugerencias.SelectedIndex = -1;
        PopupSugerencias.IsOpen = sugerencias.Count > 0;
    }

    private void TxtBuscarEjercicio_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Down && LstSugerencias.Items.Count > 0)
        {
            PopupSugerencias.IsOpen = true;
            LstSugerencias.SelectedIndex = 0;
            LstSugerencias.Focus();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            AgregarEjercicio(LstSugerencias.SelectedItem as string ?? TxtBuscarEjercicio.Text);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            PopupSugerencias.IsOpen = false;
        }
    }

    private void LstSugerencias_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (LstSugerencias.SelectedItem is string ejercicio)
        {
            AgregarEjercicio(ejercicio);
        }
    }

    private void LstSugerencias_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && LstSugerencias.SelectedItem is string ejercicio)
        {
            AgregarEjercicio(ejercicio);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            PopupSugerencias.IsOpen = false;
            TxtBuscarEjercicio.Focus();
            e.Handled = true;
        }
    }

    private void BtnAgregarEjercicio_Click(object sender, RoutedEventArgs e) =>
        AgregarEjercicio(LstSugerencias.SelectedItem as string ?? TxtBuscarEjercicio.Text);

    private void AgregarEjercicio(string nombre)
    {
        nombre = nombre.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
        {
            MostrarError("Escribí o seleccioná un ejercicio para agregar.");
            TxtBuscarEjercicio.Focus();
            return;
        }

        var ejercicio = new EjercicioEdicion { Nombre = nombre };
        _ejercicios.Add(ejercicio);
        GridEjercicios.SelectedItem = ejercicio;
        GridEjercicios.ScrollIntoView(ejercicio);
        _actualizandoSugerencias = true;
        TxtBuscarEjercicio.Clear();
        LstSugerencias.ItemsSource = new ObservableCollection<string>();
        PopupSugerencias.IsOpen = false;
        _actualizandoSugerencias = false;
        TxtBuscarEjercicio.Focus();
    }

    private void BtnGuardar_Click(object sender, RoutedEventArgs e)
    {
        GridEjercicios.CommitEdit(DataGridEditingUnit.Cell, true);
        GridEjercicios.CommitEdit(DataGridEditingUnit.Row, true);

        if (string.IsNullOrWhiteSpace(NombreRutina))
        {
            MostrarError("Ingresá un nombre para la rutina.");
            TxtNombre.Focus();
            return;
        }

        EjercicioEdicion[] ejercicios = _ejercicios
            .Where(ejercicio => !string.IsNullOrWhiteSpace(ejercicio.Nombre))
            .ToArray();

        if (ejercicios.Length == 0)
        {
            MostrarError("Agregá al menos un ejercicio con nombre.");
            return;
        }

        if (ejercicios.Any(ejercicio =>
                ejercicio.Series < 1 ||
                string.IsNullOrWhiteSpace(ejercicio.Repeticiones) ||
                ejercicio.DescansoSegundos < 0))
        {
            MostrarError("Revisá los ejercicios: las series deben ser mayores a cero, las repeticiones no pueden quedar vacías y el descanso no puede ser negativo.");
            return;
        }

        DialogResult = true;
    }

    private void MostrarError(string mensaje) =>
        MessageBox.Show(this, mensaje, "Datos incompletos", MessageBoxButton.OK, MessageBoxImage.Warning);
}

public sealed class EjercicioEdicion
{
    public string Nombre { get; set; } = string.Empty;
    public int Series { get; set; } = 3;
    public string Repeticiones { get; set; } = "10";
    public int? DescansoSegundos { get; set; }
    public string Notas { get; set; } = string.Empty;
}
