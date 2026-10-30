using System.IO;
using System.IO.Ports;
using Microsoft.Win32;

namespace GymManager.Services;

public sealed record DoorControllerResult(bool Success, string Message, string? PortName = null);

public sealed class DoorControllerService : IDisposable
{
    private const int VendorId = 0x1A86;
    private const int ProductId = 0x7523;
    private const int BaudRate = 9600;
    private const string IdentityResponse = "GYM-DOOR/1";
    private readonly object _sync = new();
    private SerialPort? _serialPort;
    private bool _disposed;

    public string? PortName
    {
        get
        {
            lock (_sync)
            {
                return _serialPort?.IsOpen == true ? _serialPort.PortName : null;
            }
        }
    }

    public bool IsConnected
    {
        get
        {
            lock (_sync)
            {
                return _serialPort?.IsOpen == true;
            }
        }
    }

    public Task<DoorControllerResult> ConnectAsync()
    {
        return Task.Run(Connect);
    }

    public Task<DoorControllerResult> OpenDoorAsync()
    {
        return Task.Run(OpenDoor);
    }

    private DoorControllerResult Connect()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return new DoorControllerResult(false, "El controlador de puerta ya fue cerrado.");
            }

            if (_serialPort?.IsOpen == true)
            {
                try
                {
                    _serialPort.DiscardInBuffer();
                    _serialPort.WriteLine("IDENTIFY?");
                    if (_serialPort.ReadLine().Trim() == IdentityResponse)
                    {
                        return new DoorControllerResult(true, "Controlador conectado.", _serialPort.PortName);
                    }
                }
                catch (Exception ex) when (ex is IOException or TimeoutException or InvalidOperationException)
                {
                }
            }

            ClosePort();

            foreach (string portName in FindCh340Ports())
            {
                SerialPort candidate = new(portName, BaudRate, Parity.None, 8, StopBits.One)
                {
                    NewLine = "\n",
                    ReadTimeout = 700,
                    WriteTimeout = 700,
                    DtrEnable = false,
                    RtsEnable = false
                };

                try
                {
                    candidate.Open();
                    Thread.Sleep(1500);
                    candidate.DiscardInBuffer();

                    for (int attempt = 0; attempt < 3; attempt++)
                    {
                        candidate.WriteLine("IDENTIFY?");
                        try
                        {
                            if (candidate.ReadLine().Trim() == IdentityResponse)
                            {
                                _serialPort = candidate;
                                return new DoorControllerResult(true, "Controlador conectado.", portName);
                            }
                        }
                        catch (TimeoutException)
                        {
                        }
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
                {
                }

                candidate.Dispose();
            }

            return new DoorControllerResult(false, "No se encontró un Nano CH340 con el firmware GymManager.");
        }
    }

    private DoorControllerResult OpenDoor()
    {
        lock (_sync)
        {
            DoorControllerResult connection = Connect();
            if (!connection.Success || _serialPort is null)
            {
                return connection;
            }

            try
            {
                _serialPort.DiscardInBuffer();
                _serialPort.WriteLine("OPEN");
                string response = _serialPort.ReadLine().Trim();
                return response == "OPENING"
                    ? new DoorControllerResult(true, "Pulso de apertura enviado.", _serialPort.PortName)
                    : new DoorControllerResult(false, $"El controlador respondió: {response}", _serialPort.PortName);
            }
            catch (Exception ex) when (ex is IOException or TimeoutException or InvalidOperationException)
            {
                string? failedPort = _serialPort.PortName;
                ClosePort();
                return new DoorControllerResult(false, $"Se perdió la comunicación con {failedPort}.");
            }
        }
    }

    private static IReadOnlyList<string> FindCh340Ports()
    {
        HashSet<string> availablePorts = SerialPort.GetPortNames().ToHashSet(StringComparer.OrdinalIgnoreCase);
        List<string> matchingPorts = [];

        try
        {
            using RegistryKey? usbDevices = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\USB");
            if (usbDevices is null)
            {
                return matchingPorts;
            }

            string hardwareIdPrefix = $"VID_{VendorId:X4}&PID_{ProductId:X4}";
            foreach (string hardwareId in usbDevices.GetSubKeyNames())
            {
                if (!hardwareId.StartsWith(hardwareIdPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                using RegistryKey? instances = usbDevices.OpenSubKey(hardwareId);
                if (instances is null)
                {
                    continue;
                }

                foreach (string instance in instances.GetSubKeyNames())
                {
                    using RegistryKey? parameters = instances.OpenSubKey($@"{instance}\Device Parameters");
                    if (parameters?.GetValue("PortName") is string portName && availablePorts.Contains(portName))
                    {
                        matchingPorts.Add(portName);
                    }
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
        }

        return matchingPorts.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private void ClosePort()
    {
        _serialPort?.Dispose();
        _serialPort = null;
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            ClosePort();
            _disposed = true;
        }
    }
}
