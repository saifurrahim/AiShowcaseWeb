using AiShowcaseWeb.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AiShowcaseWeb.Controllers
{
    public class ChatController : Controller
    {
        private readonly IAiService _aiService;
        private static CancellationTokenSource _cancellationTokenSource;

        public ChatController(IAiService aiService)
        {
            _aiService = aiService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GenerateResponse(string userMessage)
        {
            if(string.IsNullOrWhiteSpace(userMessage))
            {
                return View("Index", "Please enter a message");
            }

            var response = await _aiService.GetChatbotResponseAsync(userMessage);
            return View("Index", response);
        }

        // Action to stream the response to the client
        public async Task<IActionResult> StreamResponse(string message, CancellationToken cancellationToken)
        {
            Response.ContentType = "text/event-stream";
            Response.StatusCode = 200;

            _cancellationTokenSource = new CancellationTokenSource();

            // Stream the response using the AiService
            await _aiService.StreamChatbotResponseAsync(message, async (chunk) =>
            {
                // Send each chunk of data to the browser as a Server-Sent Event
                await Response.WriteAsync($"data: {chunk}\n\n");
                await Response.Body.FlushAsync(); // Flush immediately to push data to the client
            }, _cancellationTokenSource.Token);

            return new EmptyResult(); // Return an empty result since we're streaming
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
