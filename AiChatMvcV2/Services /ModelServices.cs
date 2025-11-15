///////////////////////////////////////////////
//    David Ferrell
//    Copyright (C) 2025, Xcodeguy Software
///////////////////////////////////////////////
using AiChatMvcV2.Contracts;
using System.Text;
using AiChatMvcV2.Objects;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using AiChatMvcV2.Models;
using System.Data;
using System.Reflection;
using System.ComponentModel.DataAnnotations;

namespace AiChatMvcV2.Services
{
    public class ModelServices : IModelServices
    {
        #region Declarations
        private readonly ILogger<ModelServices> _logger;
        private readonly ResponseServices _responseService;
        private readonly ApplicationSettings _settings;
        string _className = string.Empty;

        // Enable Mirostat sampling for controlling perplexity. 
        // (default: 0, 0 = disabled, 1 = Mirostat, 2 = Mirostat 2.0)
        private readonly double microstat = 0;
        // Influences how quickly the algorithm responds to feedback 
        // from the generated text. A lower learning rate will result 
        // in slower adjustments, while a higher learning rate will make 
        // the algorithm more responsive. (Default: 0.1)	
        private readonly double microstat_eta = 0.5;
        // Controls the balance between coherence and diversity of the output. 
        // A lower value will result in more focused and coherent text. (Default: 5.0)˝
        private readonly double microstat_tau = 5.0;
        // Sets the size of the context window used to generate the next token. (Default: 2048)	
        private readonly double num_ctx = 2048;
        // Sets how far back for the model to look back to prevent repetition. (Default: 64, 0 = disabled, -1 = num_ctx)	
        private readonly double repeat_last_n = 64;
        // Sets how strongly to penalize repetitions. A higher value (e.g., 1.5) 
        // will penalize repetitions more strongly, while a lower value (e.g., 0.9) 
        // will be more lenient. (Default: 1.1)
        private readonly double repeat_penalty = 1.1;
        // The temperature of the model. Increasing the temperature will make the 
        // model answer more creatively. (Default: 0.8)	
        private readonly double temperature = 0.8;
        // Sets the random number seed to use for generation. Setting this to a 
        // specific number will make the model generate the same text for the same 
        // prompt. (Default: 0)
        private readonly double seed = 0;
        // Maximum number of tokens to predict when generating text. (Default: -1, infinite generation)
        private readonly double num_predict = -1;
        // Reduces the probability of generating nonsense. A higher value (e.g. 100) 
        // will give more diverse answers, while a lower value (e.g. 10) will be more 
        // conservative. (Default: 40)	
        private readonly double top_k = 40;
        // Works together with top-k. A higher value (e.g., 0.95) will lead to more 
        // diverse text, while a lower value (e.g., 0.5) will generate more focused 
        // and conservative text. (Default: 0.9)	
        private readonly double top_p = 0.9;
        // Alternative to the topp, and aims to ensure a balance of quality and variety. 
        // The parameter _p represents the minimum probability for a token to be considered, 
        // relative to the probability of the most likely token. For example, with p=0.05 
        // and the most likely token having a probability of 0.9, logits with a value less 
        // than 0.045 are filtered out. (Default: 0.0)
        private readonly double min_p = 0.0;

        private const string sp_insert_table_response = "sp_insert_table_response";
        private static string _connectionString = "Server=localhost;Database=WakeNbake;Uid=root;Pwd=";
        string ExceptionMessageString = string.Empty;
        #endregion

        public ModelServices(IOptions<ApplicationSettings> settings, ILogger<ModelServices> logger, ResponseServices responseService)
        {
            _logger = logger;
            _responseService = responseService;
            _settings = settings.Value;
            _className = this.GetType().Name;
            Type declaringType = MethodBase.GetCurrentMethod()!.DeclaringType!;
            _className = declaringType.Name;
        }

        private MySqlConnection? GetConnection()
        {
            try
            {
                _logger.LogInformation("Returning new MySql connection.");

                return new MySqlConnection(_connectionString);
            }
            catch (Exception ex)
            {
                ExceptionMessageString = $"ModelServices.GetConnection: {ex.Message}";
                _logger.LogCritical(ExceptionMessageString);
                throw new Exception(ExceptionMessageString);
            }
        }

        public bool InsertResponse(ResponseItem TheResponse)
        {
            using (MySqlConnection connection = new(_connectionString))
            {
                using (MySqlCommand command = new(sp_insert_table_response, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("timestamp", TheResponse.TimeStamp);
                    command.Parameters.AddWithValue("response", TheResponse.Response);
                    command.Parameters.AddWithValue("model", TheResponse.Model);
                    command.Parameters.AddWithValue("topic", TheResponse.Topic);
                    command.Parameters.AddWithValue("prompt", TheResponse.Prompt);
                    command.Parameters.AddWithValue("negative_prompt", TheResponse.NegativePrompt);
                    command.Parameters.AddWithValue("active", TheResponse.Active);
                    command.Parameters.AddWithValue("last_updated", TheResponse.LastUpdated);
                    command.Parameters.AddWithValue("response_time", DateTime.Now.ToString("yyyy-MM-dd ") +
                                                    TheResponse.ResponseTime);
                    command.Parameters.AddWithValue("word_count", TheResponse.WordCount);

                    try
                    {
                        connection.Open();
                        command.ExecuteNonQuery(); // Use ExecuteReader if the SP returns data
                        connection.Close();
                        _logger.LogInformation($"Query ({sp_insert_table_response}) executed and connection closed.");
                    }
                    catch (MySqlException ex)
                    {
                        ExceptionMessageString = $"ModelServices.InsertResponse: {ex.Message}";
                        _logger.LogCritical(ExceptionMessageString);
                        throw new Exception(ExceptionMessageString);
                    }
                }
            }

            return true;
        }

        public async Task<string> GetModelResponseAsync(string Model, string Prompt)
        {
            string url = _settings.Url;
            string data;

            var options = $@"{{""microstat"" : {microstat},
                                    ""microstat_eta"" : {microstat_eta},
                                    ""microstat_tau"" : {microstat_tau},
                                    ""num_ctx"" : {num_ctx},
                                    ""repeat_last_n"" : {repeat_last_n},
                                    ""repeat_penalty"" : {repeat_penalty},
                                    ""temperature"" : {temperature},
                                    ""seed"" : {seed},
                                    ""num_predict"" : {num_predict},
                                    ""top_k"" : {top_k},
                                    ""top_p"" : {top_p},
                                    ""min_p"" : {min_p}
                                }}";
            /* ,
            ""options"" : {options} */
            data = $@"{{""model"" : ""{Model}"",
                    ""prompt"" : ""{Prompt}"",
                    ""stream"" : false,
                    ""format"" : ""json""
                }}";

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(_settings.HttpApiTimeout);

                    var content = new StringContent(data, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        using var stream = await response.Content.ReadAsStreamAsync();
                        using var reader = new StreamReader(stream);
                        var text = await reader.ReadToEndAsync();

                        text = _responseService.RemoveHtmlAndThinkTagsFromModelResponse(text).Result;
                        return text;
                    }
                    else
                    {
                        ExceptionMessageString = $"HTTP Request failed with status code: {response.StatusCode}\n\nModel->{Model}\n\nPromt->{Prompt}";
                        _logger.LogCritical(ExceptionMessageString);
                        throw new Exception(ExceptionMessageString);
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionMessageString = $"ModelServices.GetMethodResponseAsync: {ex.Message}";
                _logger.LogCritical(ExceptionMessageString);
                throw new Exception(ExceptionMessageString);
            }
        }
    }       //end class
}       //end namespace