using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using QR_Code.Models;
using QRCoder;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

public class QRCodeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Generate(QRCodeModel qrModel)
    {
        if (string.IsNullOrWhiteSpace(qrModel.URL))
        {
            return BadRequest("URL cannot be empty.");
        }

        try
        {
            string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "my_gibhli.png");

            // Generate QR code data
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(qrModel.URL, QRCodeGenerator.ECCLevel.H);
            var qrCode = new BitmapByteQRCode(qrCodeData);

            // Generate QR code byte array with dark red foreground & white background
            byte[] qrBytes = qrCode.GetGraphic(
                pixelsPerModule: 20,
                darkColorRgb: new byte[] { 222, 31, 82 },  // dark red
                lightColorRgb: new byte[] { 255, 255, 255 } // white
            );

            using var msQr = new MemoryStream(qrBytes);
            using var qrImage = new Bitmap(msQr);

            using var centerImage = new Bitmap(imagePath);

            using (var g = Graphics.FromImage(qrImage))
            {
                int iconSize = qrImage.Width * 25 / 100; // 25% of QR size
                int margin = iconSize / 3;                // margin thickness

                int iconX = (qrImage.Width - iconSize) / 2;
                int iconY = (qrImage.Height - iconSize) / 2;

                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Draw white circular margin behind the icon
                using (var whiteBrush = new SolidBrush(Color.White))
                {
                    g.FillEllipse(whiteBrush, iconX - margin / 2, iconY - margin / 2, iconSize + margin, iconSize + margin);
                }

                // Draw rounded clipped center image
                using var path = new System.Drawing.Drawing2D.GraphicsPath();
                path.AddEllipse(iconX, iconY, iconSize, iconSize);
                g.SetClip(path);
                g.DrawImage(centerImage, new System.Drawing.Rectangle(iconX, iconY, iconSize, iconSize));
                g.ResetClip();
            }

            // Create final image with space below for text (currently commented out)
            int finalWidth = qrImage.Width;
            int finalHeight = qrImage.Height;

            using var finalImage = new Bitmap(finalWidth, finalHeight);
            using (var g = Graphics.FromImage(finalImage))
            {
                g.Clear(Color.White);
                g.DrawImage(qrImage, 0, 0);
            }

            using var msFinal = new MemoryStream();
            finalImage.Save(msFinal, ImageFormat.Png);
            string base64Image = Convert.ToBase64String(msFinal.ToArray());

            string qrFinalImagePath = "data:image/png;base64," + base64Image;
            qrModel.QrCodeImage = qrFinalImagePath;

            return PartialView("_QRCodeResult", qrModel);
        }
        catch (Exception ex)
        {
            // Log the exception as needed, then return error message or view
            // For now, return a simple error message
            return BadRequest("An error occurred while generating the QR code: " + ex.Message);
        }
    }

    [HttpPost]
    public IActionResult DownloadQRCode(QRCodeModel qrModel)
    {
        try
        {
            using var ms = new MemoryStream();
            using (var doc = new iTextSharp.text.Document(PageSize.A4, 50, 50, 100, 100)) // margins: left, right, top, bottom
            {
                iTextSharp.text.pdf.PdfWriter.GetInstance(doc, ms);
                doc.Open();

                // Add QR Image
                var qrImageBytes = Convert.FromBase64String(qrModel.QrCodeImage.Replace("data:image/png;base64,", ""));
                iTextSharp.text.Image qrImage = iTextSharp.text.Image.GetInstance(qrImageBytes);
                qrImage.ScaleAbsolute(200, 200);              // fixed size 200x200
                qrImage.Alignment = iTextSharp.text.Image.ALIGN_CENTER;
                doc.Add(qrImage);

                // Add some space between image and URL
                doc.Add(new iTextSharp.text.Paragraph("\n"));

                // Prepare URL in a table cell to mimic border, background color and padding
                var font = iTextSharp.text.FontFactory.GetFont(
                    iTextSharp.text.FontFactory.HELVETICA_BOLD,
                    14,
                    new iTextSharp.text.BaseColor(222, 31, 82));

                PdfPTable table = new PdfPTable(1);
                table.WidthPercentage = 50; // 50% width to center nicely
                table.HorizontalAlignment = Element.ALIGN_CENTER;

                PdfPCell cell = new PdfPCell(new Phrase(qrModel.URL, font))
                {
                    BackgroundColor = BaseColor.WHITE,
                    Padding = 10f,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    BorderColor = new BaseColor(222, 31, 82),
                    BorderWidth = 1f,
                    Border = iTextSharp.text.Rectangle.BOX,
                    UseBorderPadding = true
                };

                table.AddCell(cell);
                doc.Add(table);

                doc.Close();
            }

            return File(ms.ToArray(), "application/pdf", "QRCode.pdf");
        }
        catch
        {
            return BadRequest("Error generating PDF");
        }
    }

}
