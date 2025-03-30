using AiShowcaseWeb.Interfaces;
using Microsoft.AspNetCore.Mvc;
using iText.Kernel.Pdf;
using System.IO;
using Microsoft.AspNetCore.Identity;
using iText.Kernel.Pdf.Canvas.Parser;
using System.Text;
using System.Text.RegularExpressions;

namespace AiShowcaseWeb.Controllers
{
    public class OCRController : Controller
    {
        private readonly IAiService _aiService;
        private static CancellationTokenSource _cancellationTokenSource;

        public OCRController(IAiService aiService)
        {
            _aiService = aiService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult SetPdf(string pdf64Encoded)
        {
            HttpContext.Session.SetString("pdf64Encoded", pdf64Encoded.Split(",")[1]);

            return Ok();
        }

        // Action to stream the response to the client
        public async Task<IActionResult> StreamResponse(string message, CancellationToken cancellationToken)
        {
            Response.ContentType = "text/event-stream";
            Response.StatusCode = 200;

            _cancellationTokenSource = new CancellationTokenSource();

            string pdf64Encoded = HttpContext.Session.GetString("pdf64Encoded") ?? "";

            string pdfText = Regex.Replace(GetTextFromPDF(pdf64Encoded), @"\t|\n|\r", " ");

            // Stream the response using the AiService
            await _aiService.StreamOCRResponseAsync(pdfText, message, async (chunk) =>
            {
                // Send each chunk of data to the browser as a Server-Sent Event
                await Response.WriteAsync($"data: {chunk}\n\n");
                await Response.Body.FlushAsync(); // Flush immediately to push data to the client
            }, _cancellationTokenSource.Token);

            return new EmptyResult(); // Return an empty result since we're streaming
        }

        private string GetTextFromPDF(string pdf64Encoded)
        {
            try
            {
                byte[] pdfBytes = Convert.FromBase64String(pdf64Encoded);

                var text = new StringBuilder();

                using (var memoryStream = new MemoryStream(pdfBytes))
                {
                    using (var pdfReader = new PdfReader(memoryStream))
                    {
                        var pdf = new PdfDocument(pdfReader);
                        for (int i = 1; i <= pdf.GetNumberOfPages(); i++)
                        {
                            text.Append(PdfTextExtractor.GetTextFromPage(pdf.GetPage(i)));
                        }
                    }
                }

                return text.ToString();
            }
            catch(Exception ex)
            {
                return ex.Message;
            }
            
        }

        [HttpPost]
        public IActionResult CancelStreaming()
        {
            // Cancel the ongoing streaming process if it exists
            _cancellationTokenSource?.Cancel();

            return Ok(); // Indicate that cancellation was successful
        }
    }
}
