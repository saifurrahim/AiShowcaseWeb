using AiShowcaseWeb.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace AiShowcaseWeb.Services
{
    public class AiService : IAiService
    {
        private readonly HttpClient _httpClient;
        private const string OllamaApiUrl = "http://127.0.0.1:11434/api/chat";

        public AiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // without streaming which is taking longer time (not good for UX)
        public async Task<string> GetChatbotResponseAsync(string message)
        {
            var requestData = new
            {
                model = "llama2",
                temperature = 0.7,
                max_tokens = 150,
                stream = false,
                messages = new[] {
                    new {
                        role = "system",
                        content = "you are a friendly assistant"
                    },
                    new {
                        role = "user",
                        content = message
                    }   
                }
            };

            var content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(OllamaApiUrl, content);

            if(response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                dynamic jsonResponse = JsonConvert.DeserializeObject(responseString);
                return jsonResponse?.response ?? "No response from model";
            }

            return "Error occurred while processing the request";
        }

        // Method to retrieve response with streaming enabled
        public async Task StreamChatbotResponseAsync(string message, Func<string, Task> streamHandler, CancellationToken cancellationToken = default)
        {
            var requestData = new
            {
                model = "llama2",
                temperature = 0.7,
                max_tokens = 150,
                stream = true,  // Set stream to true
                messages = new[] {
                    new {
                        role = "system",
                        content = "you are a friendly assistant"
                    },
                    new {
                        role = "user",
                        content = message
                    }
                }
            };

            var content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");

            // Create a request message
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, OllamaApiUrl)
            {
                Content = content
            };

            // Send the request using SendAsync with ResponseHeadersRead
            var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);


            if (response.IsSuccessStatusCode)
            {
                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var reader = new StreamReader(stream))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync();
                        if (!string.IsNullOrEmpty(line))
                        {
                            try
                            {
                                // Parse the JSON response to extract content and done status
                                dynamic jsonResponse = JsonConvert.DeserializeObject(line);

                                // Check if the 'message' object exists and extract the 'content'
                                string responseContent = jsonResponse?.message?.content;

                                // If there's content, send it to the stream handler
                                if (!string.IsNullOrEmpty(responseContent))
                                {
                                    await streamHandler(JsonConvert.SerializeObject(jsonResponse));
                                }

                            }
                            catch (JsonException ex)
                            {
                                // Handle any errors in deserialization, for example, if the line isn't valid JSON
                                Console.WriteLine($"Error deserializing JSON: {ex.Message}");
                            }
                        }
                    }
                }
            }
            else
            {
                await streamHandler("Error occurred while processing the request");
            }
        }
    }
}
