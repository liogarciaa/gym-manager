using QRCoder;

namespace GymManager.Services;

public static class RutinaQrService
{
    public static byte[] CrearImagen(string texto)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(texto);
        RutinaTextoService.ValidarTamanoQr(texto);

        using var generador = new QRCodeGenerator();
        using QRCodeData datosQr = generador.CreateQrCode(texto, QRCodeGenerator.ECCLevel.L, forceUtf8: true);
        return new PngByteQRCode(datosQr).GetGraphic(8);
    }
}
