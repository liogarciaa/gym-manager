using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GymManager.Data;
using GymManager.Licensing;
using GymManager.Models;
using GymManager.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

namespace GymManager.Views;

public partial class WorkoutRoutinesView : UserControl
{
    public WorkoutRoutinesView()
    {
        InitializeComponent();
        CargarRutinas();
    }

    private void CargarRutinas(int? rutinaIdSeleccionada = null)
    {
        using var db = new GymDbContext();
        var rutinas = db.Rutinas
            .Include(rutina => rutina.Ejercicios)
            .Include(rutina => rutina.Asignaciones)
            .OrderBy(rutina => rutina.Nombre)
            .ToList();

        GridRutinas.ItemsSource = rutinas;
        GridRutinas.SelectedItem = rutinaIdSeleccionada.HasValue
            ? rutinas.FirstOrDefault(rutina => rutina.Id == rutinaIdSeleccionada.Value)
            : rutinas.FirstOrDefault();

        if (GridRutinas.SelectedItem is null)
        {
            GridAsignaciones.ItemsSource = null;
            TxtAsignacionesDescripcion.Text = "Seleccioná una rutina para ver sus socios.";
        }
        else
        {
            CargarAsignaciones();
        }
    }

    private void CargarAsignaciones()
    {
        if (GridRutinas.SelectedItem is not Rutina rutina)
        {
            GridAsignaciones.ItemsSource = null;
            TxtAsignacionesDescripcion.Text = "Seleccioná una rutina para ver sus socios.";
            return;
        }

        using var db = new GymDbContext();
        var asignaciones = db.SociosRutinas
            .Include(asignacion => asignacion.Socio)
            .Include(asignacion => asignacion.Rutina)
                .ThenInclude(rutina => rutina!.Ejercicios)
            .Where(asignacion => asignacion.RutinaId == rutina.Id)
            .OrderBy(asignacion => asignacion.Socio!.Apellido)
            .ThenBy(asignacion => asignacion.Socio!.Nombre)
            .ToList();

        GridAsignaciones.ItemsSource = asignaciones;
        TxtAsignacionesDescripcion.Text = $"{rutina.Nombre} · {asignaciones.Count} socio(s)";
    }

    private void GridRutinas_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded)
        {
            CargarAsignaciones();
        }
    }

    private void GridRutinas_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source ||
            ItemsControl.ContainerFromElement(GridRutinas, source) is not DataGridRow)
        {
            return;
        }

        BtnEditarRutina_Click(sender, new RoutedEventArgs());
        e.Handled = true;
    }

    private void BtnNuevaRutina_Click(object sender, RoutedEventArgs e)
    {
        var editor = new RutinaEditorWindow { Owner = Window.GetWindow(this) };
        if (editor.ShowDialog() != true) return;

        try
        {
            using var db = new GymDbContext();
            var rutina = new Rutina
            {
                Nombre = editor.NombreRutina,
                Descripcion = editor.DescripcionRutina,
                FechaCreacion = DateTime.Now,
                Ejercicios = CrearEjercicios(editor.Ejercicios)
            };
            db.Rutinas.Add(rutina);
            db.SaveChanges();
            CargarRutinas(rutina.Id);
        }
        catch (Exception ex)
        {
            MostrarError("No se pudo guardar la rutina.", ex);
        }
    }

    private void BtnEditarRutina_Click(object sender, RoutedEventArgs e)
    {
        if (GridRutinas.SelectedItem is not Rutina rutinaSeleccionada)
        {
            MostrarAviso("Seleccioná una rutina para editar.");
            return;
        }

        Rutina? rutina;
        using (var db = new GymDbContext())
        {
            rutina = db.Rutinas.Include(item => item.Ejercicios)
                .FirstOrDefault(item => item.Id == rutinaSeleccionada.Id);
        }

        if (rutina is null)
        {
            MostrarAviso("La rutina seleccionada ya no existe.");
            CargarRutinas();
            return;
        }

        var editor = new RutinaEditorWindow(rutina) { Owner = Window.GetWindow(this) };
        if (editor.ShowDialog() != true) return;

        try
        {
            using var db = new GymDbContext();
            Rutina? rutinaActual = db.Rutinas.Include(item => item.Ejercicios)
                .FirstOrDefault(item => item.Id == rutina.Id);
            if (rutinaActual is null)
            {
                MostrarAviso("La rutina ya no existe.");
                CargarRutinas();
                return;
            }

            db.EjerciciosRutina.RemoveRange(rutinaActual.Ejercicios);
            rutinaActual.Nombre = editor.NombreRutina;
            rutinaActual.Descripcion = editor.DescripcionRutina;
            rutinaActual.Ejercicios = CrearEjercicios(editor.Ejercicios);
            db.SaveChanges();
            CargarRutinas(rutinaActual.Id);
        }
        catch (Exception ex)
        {
            MostrarError("No se pudo actualizar la rutina.", ex);
        }
    }

    private void BtnEliminarRutina_Click(object sender, RoutedEventArgs e)
    {
        if (GridRutinas.SelectedItem is not Rutina rutina)
        {
            MostrarAviso("Seleccioná una rutina para eliminar.");
            return;
        }

        MessageBoxResult confirmacion = MessageBox.Show(
            Window.GetWindow(this),
            $"¿Eliminar la rutina \"{rutina.Nombre}\" y sus asignaciones a socios?",
            "Eliminar rutina",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (confirmacion != MessageBoxResult.Yes) return;

        try
        {
            using var db = new GymDbContext();
            Rutina? rutinaActual = db.Rutinas.FirstOrDefault(item => item.Id == rutina.Id);
            if (rutinaActual is not null)
            {
                db.Rutinas.Remove(rutinaActual);
                db.SaveChanges();
            }
            CargarRutinas();
        }
        catch (Exception ex)
        {
            MostrarError("No se pudo eliminar la rutina.", ex);
        }
    }

    private void BtnAsignar_Click(object sender, RoutedEventArgs e)
    {
        if (GridRutinas.SelectedItem is not Rutina rutina)
        {
            MostrarAviso("Seleccioná una rutina para asignar.");
            return;
        }

        List<Socio> socios;
        HashSet<int> asignados;
        using (var db = new GymDbContext())
        {
            socios = db.Socios
                .Where(socio => !socio.Eliminado && socio.EstaActivo)
                .OrderBy(socio => socio.Apellido)
                .ThenBy(socio => socio.Nombre)
                .ToList();
            asignados = db.SociosRutinas
                .Where(asignacion => asignacion.RutinaId == rutina.Id)
                .Select(asignacion => asignacion.SocioId)
                .ToHashSet();
        }

        if (socios.Count == 0)
        {
            MostrarAviso("No hay socios activos para asignar.");
            return;
        }

        socios = socios.Where(socio => !asignados.Contains(socio.Id)).ToList();
        if (socios.Count == 0)
        {
            MostrarAviso("Todos los socios activos ya tienen esta rutina asignada.");
            return;
        }

        var dialogo = new SociosRutinaWindow(socios) { Owner = Window.GetWindow(this) };
        dialogo.AsignacionSolicitada += (_, _) =>
        {
            int[] nuevosIds = dialogo.SocioIdsSeleccionados.Except(asignados).ToArray();
            if (nuevosIds.Length == 0) return;

            try
            {
                using var db = new GymDbContext();
                db.SociosRutinas.AddRange(nuevosIds.Select(socioId => new SocioRutina
                {
                    SocioId = socioId,
                    RutinaId = rutina.Id,
                    FechaAsignacion = DateTime.Now
                }));
                db.SaveChanges();
                asignados.UnionWith(nuevosIds);
                dialogo.ConfirmarAsignacion(nuevosIds);
                CargarRutinas(rutina.Id);
            }
            catch (Exception ex)
            {
                MessageBox.Show(dialogo, $"No se pudo asignar la rutina.\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };
        dialogo.ShowDialog();
    }

    private void BtnQuitarAsignacion_Click(object sender, RoutedEventArgs e)
    {
        if (GridAsignaciones.SelectedItem is not SocioRutina asignacion)
        {
            MostrarAviso("Seleccioná una asignación para quitar.");
            return;
        }

        if (MessageBox.Show(
                Window.GetWindow(this),
                $"¿Quitar la rutina a {asignacion.Socio?.NombreCompleto}?",
                "Quitar asignación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            using var db = new GymDbContext();
            SocioRutina? actual = db.SociosRutinas.Find(asignacion.Id);
            if (actual is not null)
            {
                db.SociosRutinas.Remove(actual);
                db.SaveChanges();
            }
            CargarRutinas(asignacion.RutinaId);
        }
        catch (Exception ex)
        {
            MostrarError("No se pudo quitar la asignación.", ex);
        }
    }

    private void BtnPdf_Click(object sender, RoutedEventArgs e)
    {
        if (GridAsignaciones.SelectedItem is not SocioRutina asignacion ||
            asignacion.Rutina is null ||
            asignacion.Socio is null)
        {
            MostrarAviso("Seleccioná una asignación para generar el PDF.");
            return;
        }

        var dialogo = new SaveFileDialog
        {
            FileName = $"Rutina_{NombreArchivo(asignacion.Socio.NombreCompleto)}_{NombreArchivo(asignacion.Rutina.Nombre)}.pdf",
            Filter = "Documento PDF (*.pdf)|*.pdf",
            DefaultExt = ".pdf",
            AddExtension = true
        };
        if (dialogo.ShowDialog(Window.GetWindow(this)) != true) return;

        try
        {
            using var db = new GymDbContext();
            string nombreGym = db.Configuraciones.Select(configuracion => configuracion.NombreGimnasio).FirstOrDefault() ?? string.Empty;
            RutinaPdfService.Generar(
                dialogo.FileName,
                asignacion.Rutina,
                asignacion.Socio.NombreCompleto,
                asignacion.FechaAsignacion,
                nombreGym);
            MessageBox.Show(Window.GetWindow(this), "El PDF de la rutina se generó correctamente.", "PDF creado", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MostrarError("No se pudo generar el PDF.", ex);
        }
    }

    private void BtnQr_Click(object sender, RoutedEventArgs e)
    {
        if (GridAsignaciones.SelectedItem is not SocioRutina asignacion || asignacion.Rutina is null)
        {
            MostrarAviso("Seleccioná una asignación para mostrar su QR.");
            return;
        }

        try
        {
            string texto = RutinaTextoService.Formatear(asignacion.Rutina);
            byte[] imagenQr = RutinaQrService.CrearImagen(texto);
            new RutinaQrWindow(texto, imagenQr) { Owner = Window.GetWindow(this) }.ShowDialog();
        }
        catch (Exception ex)
        {
            MostrarError("No se pudo generar el QR de la rutina.", ex);
        }
    }

    private static List<EjercicioRutina> CrearEjercicios(IEnumerable<EjercicioEdicion> ejercicios) =>
        ejercicios.Select((ejercicio, indice) => new EjercicioRutina
        {
            Orden = indice + 1,
            Nombre = ejercicio.Nombre.Trim(),
            Series = ejercicio.Series,
            Repeticiones = ejercicio.Repeticiones.Trim(),
            DescansoSegundos = ejercicio.DescansoSegundos,
            Notas = ejercicio.Notas.Trim()
        }).ToList();

    private static string NombreArchivo(string nombre)
    {
        foreach (char caracter in Path.GetInvalidFileNameChars())
        {
            nombre = nombre.Replace(caracter, '_');
        }
        return string.Join("_", nombre.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
    }

    private void MostrarAviso(string mensaje) =>
        MessageBox.Show(Window.GetWindow(this), mensaje, "Rutinas", MessageBoxButton.OK, MessageBoxImage.Information);

    private void MostrarError(string mensaje, Exception exception) =>
        MessageBox.Show(Window.GetWindow(this), $"{mensaje}\n{exception.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
}
