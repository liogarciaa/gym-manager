using QRCoder;

namespace GymManager.Services;

public static class RutinaQrService
{
    public static byte[] CrearImagen(string texto)
    {
        string textoQr = RutinaTextoService.PrepararParaQr(texto);
        RutinaTextoService.ValidarTamanoQr(textoQr);

        using var generador = new QRCodeGenerator();
        using QRCodeData datosQr = generador.CreateQrCode(
            textoQr,
            QRCodeGenerator.ECCLevel.L,
            forceUtf8: false);
        return new PngByteQRCode(datosQr).GetGraphic(4);
    }
}
