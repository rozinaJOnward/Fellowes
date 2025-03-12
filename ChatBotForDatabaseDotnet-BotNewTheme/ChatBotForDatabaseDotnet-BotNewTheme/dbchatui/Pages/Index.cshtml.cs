using Azure;
using Azure.AI.OpenAI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;
using System.Text.Json;
using YourOwnData;
using Azure.Identity;
//using OpenAI.Chat;
using static System.Environment;
//dotnet add package Azure.Search.Documents
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
namespace StoryCreator.Pages
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        public string UserPrompt { get; set; } = string.Empty;
        [BindProperty]
        public string SelectedBot { get; set; } = string.Empty;
        public List<List<string>> RowData { get; set; } = [];
        public string ChatBotResponse { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Query { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        //public string SelectedBot { get; set; } //= "GenAI"; // Default to GenAI
        public int attempt = 0;
        public async Task OnGet()
        {
            // Set a default message for the chat bot response
            ChatBotResponse = "Welcome to Fellowes GenAI ChatBot! How can I assist you today?";
        }

        public async Task OnPost()
        {
            string strpropmpt = UserPrompt;
            string bottype = SelectedBot;

            if (SelectedBot == "GenAI")
            {
                ChatBotResponse = await RunGenAIQuery(UserPrompt);
            }
            else if (SelectedBot == "NLPI")
            {
                RunNLPIQuery(UserPrompt);
            }
        }

        public void RunNLPIQuery(string userPrompt)
        {
 
            string openAIEndpoint = "https://otl-hr-oai.openai.azure.com/";
            string openAIKey = "f965a35a9c014662bfc09d6443f500bc";
            string openAIDeploymentName = "gpt-35-turbo";

            OpenAIClient client = new(new Uri(openAIEndpoint), new AzureKeyCredential(openAIKey));

            // execute SchemaLoader project to export your db schema and then paste the schema in the placeholder below, you can remove the object/table you dont want to include

            /*
                        //- public.organizations (filter_replace_notified_at, is_public, id, customer_id, device_offline_notified_at, updated_at, subscription_end_at, country, inserted_at, name, subscription, poor_aq_notified_at, subscription_start_at, type )
                       // - public.buildings (updated_at, name, multi_floors, id, inserted_at, address, timezone, organization_id )
                        //- public.floors (name, updated_at, building_id, inserted_at, id )
                       // - public.areas (latest_sensor_sharing_cmd, inserted_at, name, id, desired_fan_mode, prev_desired_fan_speed, description, bad_air_quality_triggered, updated_at, area_id, desired_fan_speed, sensor_sharing_enabled, floor_id )          
                        //- public.devices (organization_id, hardware_id, area_id, serial_number, updated_at, lookout_firmware_version, id, auth_token, last_reported_timestamp, publish_to_reset_cloud, name, thing_name, model_type, inserted_at )
                       // - public.favorite_areas (area_id, user_id, id, updated_at, inserted_at )
                        //- public.filter_change_events (filter_id, inserted_at, id, updated_at ) -- when filter changes
                       // - public.filters (updated_at, position, life_percentage, inserted_at, life_time, id, type, device_id ) -- Filter information
                        //- public.time_based_commands (stop_second, start_minute, stop_hour, start_days, stop_days, inserted_at, start_hour, start_time, stopped, command, name, stop_time, updated_at, repeating, start_second, id, device_id, area_id, stop_minute )

            */

            var systemMessage = @"Your are a helpful, cheerful database assistant. 
            Use the following database schema when creating your answers:

            - public.vw_Organizations (organization_id, organization_name, organization_type, total_subscriptions, subscription_starts, subscription_ends, organization_country, filter_replace_notified_time, poor_air_quality_notified_time, device_offline_notified )
            - public.vw_Buildings (building_id, building_name, address, has_multi_floor, organization_id, timezone ) 
            - public.vw_Floors (floor_id,floor_name,building_id )
            - public.vw_Areas (area_id, area_name, area_code, area_floor_id, area_desired_fan_speed, area_desired_fan_mode, is_bad_air_quality_triggered ) 
            - public.vw_Devices (device_id, device_name, things_name, organizatio_id, area_id, serial_number, model_type, last_reported_time, hardware_id, lookout_firmware_version )
            - public.vw_filter_change_events (filter_change_events_id, filter_id, filter_change_date )
            - public.vw_filters (filters_id, device_id, life_percentage, life_time, type )

            -- to know the device in a area have any scheduled actions  stop/start and   (e.g. runs on  Weekdays and stops in Weekend )

            Include column name headers in the query results. 

            Dont show the columns names on which joining is happening. 

            If you are showing Column names then give it like we have provided in bracket as it is having correct required Capitalisation and is preferd way to display

            Always provide your answer in the JSON format below:
            
            { ""summary"": ""your-summary"", ""query"":  ""your-query"" }
            
            Output ONLY JSON.
            In the preceding JSON response, substitute ""your-query"" with postgres SQL Query to retrieve the requested data.
            In the preceding JSON response, substitute ""your-summary"" with a summary of the query.
            Always include all columns in the table.
            If the resulting query is non-executable, replace ""your-query"" with NA, but still substitute ""your-query"" with a summary of the query.
            Follow the postgres Database Syntax only.";

            ChatCompletionsOptions chatCompletionsOptions = new ChatCompletionsOptions()
            {
                Messages = {
                    new ChatRequestSystemMessage(systemMessage),
                    new ChatRequestUserMessage(userPrompt)
                },
                DeploymentName = openAIDeploymentName
            };
            try
            {
                ChatCompletions chatCompletionsResponse = client.GetChatCompletions(chatCompletionsOptions);

                //var response = JsonSerializer.Deserialize<AIQuery>(chatCompletionsResponse.Choices[0].Message.Content.Replace("```json", "").Replace("```", ""));
                var jsonContent = chatCompletionsResponse.Choices[0].Message.Content.Replace("```json", "").Replace("```", "").Trim();
                jsonContent = jsonContent.Replace("\n", "").Replace("\r", "");

                AIQuery? response = null; // Initialize the response object

                try
                {
                    response = JsonSerializer.Deserialize<AIQuery>(jsonContent);
                }
                catch (JsonException ex)
                {
                    try
                    {
                        response = null;
                        response = JsonSerializer.Deserialize<AIQuery>(chatCompletionsResponse.Choices[0].Message.Content.Replace("```json", "").Replace("```", ""));
                    }
                    catch (Exception ex2)
                    {
                        Error = $"Deserialization error: {ex2.Message}";
                        return;
                    }
                }


                Summary = response.summary;
                Query = response.query;

                //RowData = DataService.GetDataTable(response.query);
                RowData = GetProcessedRowData(response.query);
            }
            catch (Exception e)
            {
                if (attempt < 2 )
                {
                    attempt=attempt+1;
                    RunNLPIQuery(userPrompt);
                }
                else    
                {
                Error = e.Message;
                }
            }
        }

        public async Task<string> RunGenAIQuery(string userPrompt)
        {
            string apiUrl = "https://genai-nlpi-gseqcwa6h0hwdfem.eastus-01.azurewebsites.net/api/chat";
            string apiResponsedtl = string.Empty;
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Create the request content
                    var requestData = new
                    {
                        prompt_input = userPrompt
                    };
                    string jsonRequest = JsonSerializer.Serialize(requestData);
                    StringContent content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                    // Send the POST request
                    HttpResponseMessage response = await client.PostAsync(apiUrl, content);

                    // Check if the request was successful
                    response.EnsureSuccessStatusCode();

                    // Read the response content
                    string responseContent = await response.Content.ReadAsStringAsync();

                    // Deserialize the response into the ApiResponse class
                    //var apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseContent);
                    //ChatBotResponse = apiResponse?.Response;
                    using (JsonDocument doc = JsonDocument.Parse(responseContent))
                    {
                        if (doc.RootElement.TryGetProperty("response", out JsonElement jsonResponse))
                        {
                            apiResponsedtl = jsonResponse.GetString();
                        }
                        else
                        {
                            apiResponsedtl = "The 'response' field was not found in the API response.";
                        }
                    }
                }
                catch (Exception e)
                {
                    apiResponsedtl = $"Error: {e.Message}";
                }
                return apiResponsedtl;
            }
        }


        public List<List<string>> GetProcessedRowData(string query)
        {
            var rawData = DataService.GetDataTable(query);

            if (rawData.Count > 0)
                rawData[0] = rawData[0].Select(header => ToPascalCase(header)).ToList();

            return rawData;
        }
        public string ToPascalCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var words = input.Split(new[] { ' ', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join("", words.Select(word => char.ToUpper(word[0]) + word.Substring(1).ToLower()));
        }


        public class AIQuery
        {
            public string summary { get; set; }
            public string query { get; set; }
        }
        public class ApiResponse
        {
            public string Response { get; set; }
        }
    }

}