using QRCoder;

namespace StarEventsTicketing.Services
{
    public class QRCodeService
    {
        private readonly IWebHostEnvironment _environment;

        public QRCodeService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string> GenerateQRCodeAsync(string data, string ticketNumber)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var fileName = $"QR_{ticketNumber}_{timestamp}.png";
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "qrcodes");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var filePath = Path.Combine(uploadsFolder, fileName);

            await Task.Run(() =>
            {
                using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
                {
                    QRCodeData qrData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
                    using (PngByteQRCode qrCode = new PngByteQRCode(qrData))
                    {
                        byte[] qrCodeBytes = qrCode.GetGraphic(20);
                        System.IO.File.WriteAllBytes(filePath, qrCodeBytes);
                    }
                }
            });

            return $"/qrcodes/{fileName}";
        }
    }
}

