namespace AiShowcaseWeb.Interfaces
{
    public interface IAiService
    {
        Task<string> GetChatbotResponseAsync(string message);

        Task StreamChatbotResponseAsync(string message, Func<string, Task> streamHandler, CancellationToken cancellationToken);
        Task StreamOCRResponseAsync(string pdfText, string message, Func<string, Task> streamHandler, CancellationToken cancellationToken);
    }
}
