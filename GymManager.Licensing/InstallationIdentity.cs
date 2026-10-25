using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace GymManager.Licensing;

public static class InstallationIdentity
{
    public static string GetCurrent()
    {
        using var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
            .OpenSubKey(@"SOFTWARE\Microsoft\Cryptography", writable: false);
        string? machineGuid = key?.GetValue("MachineGuid") as string;
        if (string.IsNullOrWhiteSpace(machineGuid))
            throw new InvalidOperationException("No se pudo obtener el identificador de esta PC.");

        byte[] input = Encoding.UTF8.GetBytes($"GymManager.Installation.v1|{machineGuid.Trim()}");
        return Convert.ToHexString(SHA256.HashData(input));
    }
}