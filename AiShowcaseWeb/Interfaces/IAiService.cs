namespace AiShowcaseWeb.Interfaces
{
    public interface IAiService
    {
        Task<string> GetChatbotResponseAsync(string message);

        Task StreamChatbotResponseAsync(string message, Func<string, Task> streamHandler, CancellationToken cancellationToken);
    }
}
