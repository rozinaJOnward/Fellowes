using Azure;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using System;
using System.Threading.Tasks;

public class ChatBot
{
    // Azure OpenAI settings
    private readonly string openAIEndpoint = "https://otl-oai-fellow.openai.azure.com/";
    private readonly string openAIKey = "7267893b62584c07a099d10362266b36";
    private readonly string openAIDeploymentName = "otlgpt40model";

    // Azure Cognitive Search settings
    private readonly string searchEndpoint = "https://fellowesoai-search.search.windows.net";
    private readonly string searchKey = "C8ATyXlnYcRAlG8asRSEQ0PCZVQqxzTDl8vFqnybjRAzSeAzolXE";
    private readonly string searchIndexName = "fellowesoai-index";

    private readonly OpenAIClient openAIClient;
    private readonly SearchClient searchClient;

    public string ChatBotResponse { get; private set; }
    public string Error { get; private set; }


    public async Task<string> RunGenAIQuery(string userPrompt)
    {
        // Azure OpenAI settings
        string endpoint = "https://otl-oai-fellow.openai.azure.com/";
        string key = "7267893b62584c07a099d10362266b36";
        string deploymentName = "otlgpt40model";

        // Azure Cognitive Search settings
        string searchEndpoint = "https://fellowesoai-search.search.windows.net";
        string searchKey = "C8ATyXlnYcRAlG8asRSEQ0PCZVQqxzTDl8vFqnybjRAzSeAzolXE";
        string indexName = "fellowesoai-index";

        // Initialize Azure OpenAI client
        OpenAIClient openAIClient = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));

        // Initialize Azure Cognitive Search client
        SearchClient searchClient = new SearchClient(new Uri(searchEndpoint), indexName, new AzureKeyCredential(searchKey));

        // Example query
        string userQuery = userPrompt;

        // Perform search
        SearchResults<SearchDocument> results = await searchClient.SearchAsync<SearchDocument>(userQuery);

        // Prepare context from search results
        string searchContext = "";
        await foreach (SearchResult<SearchDocument> result in results.GetResultsAsync())
        {
            searchContext += result.Document["content"] + "\n";
        }

        // Prepare prompt
        string prompt = $"Based on the following information:\n{searchContext}\n\nAnswer the question: {userQuery}";

        // Call Azure OpenAI
        // Create CompletionsOptions
        /*
        var completionsOptions = new CompletionsOptions
        {
            Prompts = { prompt },
            MaxTokens = 100,
            Temperature = 0.7f

        };

        // Correct usage of the method
        Response<Completions> response = await openAIClient.GetCompletionsAsync(openAIDeploymentName, completionsOptions);
        //Response<Completions> response = await openAIClient.GetCompletionsAsync(completionsOptions);
*/
        ChatCompletionsOptions chatCompletionsOptions = new ChatCompletionsOptions()
        {
            Messages = {
                    new ChatRequestSystemMessage("Your are a helpful, cheerful database assistant"),
                    new ChatRequestUserMessage(userPrompt)
                },
            DeploymentName = openAIDeploymentName
        };
        // Display the response
    
        try
        {
            ChatCompletions chatCompletionsResponse = openAIClient.GetChatCompletions(chatCompletionsOptions);

            ChatBotResponse = chatCompletionsResponse.Choices[0].Message.Content;
        }
        catch (Exception e)
        {
            Error = e.Message;
        }

 
       // Console.WriteLine($"Response: {response.Value.Choices[0].Text.Trim()}");
        return ChatBotResponse;
    }

}