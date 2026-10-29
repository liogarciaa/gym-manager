using System;
using System.Globalization;
using System.Linq;
using System.Media;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using GymManager.Data;
using GymManager.Licensing;
using GymManager.Models;
using GymManager.Services;

namespace GymManager.Views
{
    public partial class CheckInView : UserControl
    {
        private const int MinutosBloqueoReingreso = 120; // 2 horas de espera anti-passback
        private readonly DispatcherTimer _timerReset;
        private readonly DispatcherTimer _timerReloj;
        private bool _procesandoAcceso;

        public CheckInView()
        {
            InitializeComponent();

            _timerReset = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _timerReset.Tick += TimerReset_Tick;

            _timerReloj = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timerReloj.Tick += TimerReloj_Tick;
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            DoorControllerService doorController = ((App)Application.Current).DoorController;
            ((App)Application.Current).ShowAccessWaiting();
#if DEBUG
            BtnVistaPreviaPantalla.Visibility = Visibility.Visible;
#endif
            _ = Dispatcher.BeginInvoke(() =>
            {
                if (IsLoaded)
                {
                    TxtIdentificador.Focus();
                    Keyboard.Focus(TxtIdentificador);
                }
            }, DispatcherPriority.Input);
            ActualizarReloj();
            _timerReloj.Start();
            CargarAsistenciasHoy();

            // Adaptar textos si el módulo no está contratado
            if (!LicenseRuntime.HasFeature(LicenseFeatures.NfcAccess))
            {
                TxtSubtituloTerminal.Text = "Digite su N° de Socio o DNI para registrar su ingreso";
                TxtEtiquetaInput.Text = "N° de Socio o DNI:";
            }

            DoorControllerResult resultado = await doorController.ConnectAsync();
            if (!IsLoaded)
            {
                return;
            }

            TxtEstadoControladorPuerta.Text = resultado.Success
                ? $"Puerta: controlador conectado ({resultado.PortName})"
                : "Puerta: controlador no detectado; se buscará al autorizar un ingreso";
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            _timerReloj.Stop();
            _timerReset.Stop();
            ((App)Application.Current).ShowAccessWaiting();
        }

        private void TimerReloj_Tick(object? sender, EventArgs e)
        {
            ActualizarReloj();
        }

        private void ActualizarReloj()
        {
            DateTime ahora = DateTime.Now;
            TxtRelojDigital.Text = ahora.ToString("HH:mm:ss");

            string fechaTexto = ahora.ToString("dddd, d 'de' MMMM", new CultureInfo("es-ES"));
            TxtFechaEnEspera.Text = char.ToUpper(fechaTexto[0]) + fechaTexto.Substring(1);
        }

        private void CargarAsistenciasHoy()
        {
            DateTime hoyInicio = DateTime.Today;
            DateTime hoyFin = DateTime.Today.AddDays(1).AddTicks(-1);

            using (var db = new GymDbContext())
            {
                var asistenciasHoy = db.Asistencias
                                       .Include(a => a.Socio)
                                       .Where(a => a.FechaHora >= hoyInicio && a.FechaHora <= hoyFin)
                                       .OrderByDescending(a => a.FechaHora)
                                       .ToList()
                                       .Select(a => new
                                       {
                                           Hora = a.FechaHora.ToString("hh:mm tt", new CultureInfo("es-AR")),
                                           SocioId = a.Socio.Id,
                                           SocioNombre = a.Socio.NombreCompleto,
                                           Dni = a.Socio.Dni,
                                           FechaVencimiento = a.Socio.FechaVencimiento
                                       })
                                       .ToList();

                GridAsistencias.ItemsSource = asistenciasHoy;
                TxtContadorHoy.Text = $"{asistenciasHoy.Count} ingresos registrados hoy";
            }
        }

        private async void TxtIdentificador_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                await ProcesarAccesoAsync();
            }
        }

        private async void BtnIngresar_Click(object sender, RoutedEventArgs e)
        {
            await ProcesarAccesoAsync();
        }

        private void BtnVistaPreviaPantalla_Click(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).ToggleAccessDisplayPreview();
        }

private async Task ProcesarAccesoAsync()
{
    if (_procesandoAcceso)
    {
        return;
    }

    string entrada = TxtIdentificador.Text.Trim();
    if (string.IsNullOrWhiteSpace(entrada)) return;

    _procesandoAcceso = true;
    _timerReset.Stop();
    Socio? socioAutorizado = null;
    bool socioEnTolerancia = false;
    bool credencialNfc = false;
    Asistencia? asistenciaPendiente = null;

    try
    {
        using (var db = new GymDbContext())
        {
            int diasTolerancia = db.Configuraciones.Select(c => c.DiasToleranciaMora).FirstOrDefault();
            Socio? socio = null;

            // Buscar primero la credencial para que un UID numérico no se confunda con un ID.
            if (LicenseRuntime.HasFeature(LicenseFeatures.NfcAccess))
            {
                socio = db.Socios.FirstOrDefault(s => s.CodigoAcceso == entrada && !s.Eliminado);
                credencialNfc = socio != null;
            }

            // 1. Búsqueda por ID numérico de socio
            if (socio == null)
            {
                if (int.TryParse(entrada, out int socioId))
                {
                    socio = db.Socios.FirstOrDefault(s => s.Id == socioId && !s.Eliminado);
                }
            }

            // 2. Búsqueda por DNI (siempre disponible)
            if (socio == null)
            {
                socio = db.Socios.FirstOrDefault(s => s.Dni == entrada && !s.Eliminado);
            }

            DateTime hoy = DateTime.Today;
            DateTime ahora = DateTime.Now;

            if (socio == null)
            {
                MostrarAccesoRechazado("SOCIO NO ENCONTRADO", $"No existe ningún socio activo con ID/DNI '{entrada}'.");
            }
            else if (!socio.EstaActivo)
            {
                MostrarAccesoRechazado("ACCESO DENEGADO", $"[ID #{socio.Id}] {socio.NombreCompleto}\nLa cuenta se encuentra inactiva.");
            }
            else if (!socio.FechaVencimiento.HasValue)
            {
                MostrarAccesoRechazado("SIN CUOTA REGISTRADA", $"[ID #{socio.Id}] {socio.NombreCompleto}\nNo posee pagos de cuota registrados.");
            }
            else
            {
                if (((App)Application.Current).DoorController.IsConnected && !credencialNfc)
                {
                    MostrarAccesoRechazado(
                        "PASE EL LLAVERO NFC",
                        $"[ID #{socio.Id}] {socio.NombreCompleto}\nLa apertura física requiere la credencial NFC asignada."
                    );
                    return;
                }

                DateTime fechaVencimiento = socio.FechaVencimiento.Value.Date;
                DateTime fechaLimiteConTolerancia = fechaVencimiento.AddDays(diasTolerancia);

                if (hoy > fechaLimiteConTolerancia)
                {
                    MostrarAccesoRechazado("CUOTA VENCIDA", $"[ID #{socio.Id}] {socio.NombreCompleto}\nVenció el {fechaVencimiento:dd/MM/yyyy} (Tolerancia agotada).");
                }
                else
                {
                    // Control Anti-Passback (verificar si ya ingresó en las últimas 2 horas)
                    DateTime umbralReingreso = ahora.AddMinutes(-MinutosBloqueoReingreso);
                    var ultimoIngreso = db.Asistencias
                        .Where(a => a.SocioId == socio.Id && a.FechaHora >= umbralReingreso)
                        .OrderByDescending(a => a.FechaHora)
                        .FirstOrDefault();

                    if (ultimoIngreso != null)
                    {
                        var minutosTranscurridos = (int)(ahora - ultimoIngreso.FechaHora).TotalMinutes;
                        int minutosRestantes = MinutosBloqueoReingreso - minutosTranscurridos;

                        MostrarAccesoRechazado(
                            "INGRESO YA REGISTRADO",
                            $"[ID #{socio.Id}] {socio.NombreCompleto}\nÚltimo acceso: {ultimoIngreso.FechaHora:HH:mm} ({minutosTranscurridos} min atrás).\nReingreso en: {minutosRestantes} min."
                        );
                    }
                    else
                    {
                        asistenciaPendiente = new Asistencia
                        {
                            SocioId = socio.Id,
                            FechaHora = ahora
                        };

                        if (credencialNfc)
                        {
                            socioAutorizado = socio;
                            socioEnTolerancia = hoy > fechaVencimiento;
                        }
                        else
                        {
                            db.Asistencias.Add(asistenciaPendiente);
                            db.SaveChanges();
                            CargarAsistenciasHoy();
                            if (hoy > fechaVencimiento)
                            {
                                MostrarAccesoTolerancia(socio, fechaVencimiento);
                            }
                            else
                            {
                                MostrarAccesoPermitido(socio);
                            }
                        }
                    }
                }
            }
        }

        if (socioAutorizado != null && asistenciaPendiente != null)
        {
            DoorControllerResult resultadoPuerta = await ((App)Application.Current).DoorController.OpenDoorAsync();
            TxtEstadoControladorPuerta.Text = resultadoPuerta.Success
                ? $"Puerta: controlador conectado ({resultadoPuerta.PortName})"
                : "Puerta: controlador no detectado";

            if (!resultadoPuerta.Success)
            {
                MostrarAccesoPuertaNoDisponible(socioAutorizado, resultadoPuerta.Message);
            }
            else
            {
                try
                {
                    using var db = new GymDbContext();
                    db.Asistencias.Add(asistenciaPendiente);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    MostrarRegistroAsistenciaFallido(socioAutorizado, ex.Message);
                    return;
                }

                CargarAsistenciasHoy();
                if (socioEnTolerancia)
                {
                    MostrarAccesoTolerancia(socioAutorizado, socioAutorizado.FechaVencimiento!.Value.Date);
                }
                else
                {
                    MostrarAccesoPermitido(socioAutorizado);
                }
            }
        }
    }
    finally
    {
        TxtIdentificador.Clear();
        _timerReset.Start();
        _procesandoAcceso = false;
    }
}

        private void MostrarAccesoPermitido(Socio socio)
        {
            ActivarModoResultado();

            BrdResultado.Background = new SolidColorBrush(Color.FromRgb(220, 252, 231));
            BrdResultado.BorderBrush = new SolidColorBrush(Color.FromRgb(34, 197, 94));

            TxtMensajeEstado.Text = "ACCESO PERMITIDO";
            TxtMensajeEstado.Foreground = new SolidColorBrush(Color.FromRgb(22, 101, 52));

            TxtDetalleSocio.Text = $"[ID #{socio.Id}] {socio.NombreCompleto}\nCuota al día hasta el {socio.FechaVencimiento:dd/MM/yyyy}";
            TxtDetalleSocio.Foreground = new SolidColorBrush(Color.FromRgb(21, 128, 61));
            SincronizarPantallaCliente();

            SystemSounds.Asterisk.Play();
        }

        private void MostrarAccesoTolerancia(Socio socio, DateTime fechaVencimiento)
        {
            ActivarModoResultado();

            BrdResultado.Background = new SolidColorBrush(Color.FromRgb(254, 243, 199));
            BrdResultado.BorderBrush = new SolidColorBrush(Color.FromRgb(245, 158, 11));

            TxtMensajeEstado.Text = "ACCESO EN TOLERANCIA";
            TxtMensajeEstado.Foreground = new SolidColorBrush(Color.FromRgb(146, 64, 14));

            TxtDetalleSocio.Text = $"[ID #{socio.Id}] {socio.NombreCompleto}\nCuota vencida el {fechaVencimiento:dd/MM/yyyy}. Avisar cobro.";
            TxtDetalleSocio.Foreground = new SolidColorBrush(Color.FromRgb(180, 83, 9));
            SincronizarPantallaCliente();

            SystemSounds.Asterisk.Play();
        }

        private void MostrarAccesoPuertaNoDisponible(Socio socio, string detalle)
        {
            ActivarModoResultado();

            BrdResultado.Background = new SolidColorBrush(Color.FromRgb(254, 243, 199));
            BrdResultado.BorderBrush = new SolidColorBrush(Color.FromRgb(245, 158, 11));

            TxtMensajeEstado.Text = "SOCIO AUTORIZADO, PUERTA SIN RESPUESTA";
            TxtMensajeEstado.Foreground = new SolidColorBrush(Color.FromRgb(146, 64, 14));

            TxtDetalleSocio.Text = $"[ID #{socio.Id}] {socio.NombreCompleto}\nNo se registró el ingreso ni se envió el pulso.\n{detalle}";
            TxtDetalleSocio.Foreground = new SolidColorBrush(Color.FromRgb(180, 83, 9));
            SincronizarPantallaCliente();

            SystemSounds.Hand.Play();
        }

        private void MostrarRegistroAsistenciaFallido(Socio socio, string detalle)
        {
            ActivarModoResultado();

            BrdResultado.Background = new SolidColorBrush(Color.FromRgb(254, 226, 226));
            BrdResultado.BorderBrush = new SolidColorBrush(Color.FromRgb(239, 68, 68));

            TxtMensajeEstado.Text = "PUERTA ACTIVADA, ERROR DE REGISTRO";
            TxtMensajeEstado.Foreground = new SolidColorBrush(Color.FromRgb(153, 27, 27));

            TxtDetalleSocio.Text = $"[ID #{socio.Id}] {socio.NombreCompleto}\nEl relé aceptó el pulso, pero no se pudo guardar la asistencia.\n{detalle}";
            TxtDetalleSocio.Foreground = new SolidColorBrush(Color.FromRgb(185, 28, 28));
            SincronizarPantallaCliente();

            SystemSounds.Hand.Play();
        }

        private void MostrarAccesoRechazado(string titulo, string detalle)
        {
            ActivarModoResultado();

            BrdResultado.Background = new SolidColorBrush(Color.FromRgb(254, 226, 226));
            BrdResultado.BorderBrush = new SolidColorBrush(Color.FromRgb(239, 68, 68));

            TxtMensajeEstado.Text = titulo;
            TxtMensajeEstado.Foreground = new SolidColorBrush(Color.FromRgb(153, 27, 27));

            TxtDetalleSocio.Text = detalle;
            TxtDetalleSocio.Foreground = new SolidColorBrush(Color.FromRgb(185, 28, 28));
            SincronizarPantallaCliente();

            SystemSounds.Hand.Play();
        }

        private void ActivarModoResultado()
        {
            PnlRelojEnEspera.Visibility = Visibility.Collapsed;
            PnlMensajeResultado.Visibility = Visibility.Visible;
        }

        private void SincronizarPantallaCliente()
        {
            ((App)Application.Current).ShowAccessResult(
                TxtMensajeEstado.Text,
                TxtDetalleSocio.Text,
                ((SolidColorBrush)BrdResultado.Background).Color,
                ((SolidColorBrush)BrdResultado.BorderBrush).Color,
                ((SolidColorBrush)TxtMensajeEstado.Foreground).Color,
                ((SolidColorBrush)TxtDetalleSocio.Foreground).Color);
        }

        private void TimerReset_Tick(object? sender, EventArgs e)
        {
            _timerReset.Stop();

            BrdResultado.Background = new SolidColorBrush(Color.FromRgb(248, 250, 252));
            BrdResultado.BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225));

            PnlMensajeResultado.Visibility = Visibility.Collapsed;
            PnlRelojEnEspera.Visibility = Visibility.Visible;
            ((App)Application.Current).ShowAccessWaiting();

            TxtIdentificador.Focus();
        }
    }
}