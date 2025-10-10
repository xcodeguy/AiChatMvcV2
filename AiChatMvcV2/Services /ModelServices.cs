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

namespace AiChatMvcV2.Services
{
    public class ModelServices : IModelServices
    {
        private readonly ILogger<ModelServices> _logger;
        private readonly ResponseServices _responseService;
        private readonly ApplicationSettings _settings;
        private const float temperature = 0.8f;     //0.8
        private const int num_ctx = 2048;           //2048
        private const int num_predict = -1;         //-1
        private const string sp_insert_table_response = "sp_insert_table_response";
        private static string _connectionString = "Server=localhost;Database=WakeNbake;Uid=root;Pwd=";
        string ExceptionMessageString = string.Empty;

        public ModelServices(IOptions<ApplicationSettings> settings, ILogger<ModelServices> logger, ResponseServices responseService)
        {
            _logger = logger;
            _responseService = responseService;
            _settings = settings.Value;
        }

        private MySqlConnection? GetConnection()
        {
            try
            {
                _logger.LogInformation("Returning new MySql connection.");

                return new MySqlConnection(_connectionString);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e.Message);
            }

            return null;
        }

        public bool InsertResponse(ResponseItem TheResponse)
        {
            //////////////////////////////////////////
            // TEST EXCEPTION THROW
            //////////////////////////////////////////
            if (_settings.MySqlTestException == true)
            {
                Type classType = this.GetType();
                if (MethodBase.GetCurrentMethod() != null)
                {
                    string className = classType.Name.ToString();
                    string methodName = MethodBase.GetCurrentMethod()?.Name ?? "UnknownMethod";
                    ExceptionMessageString = $"Test exception from: {className}.{methodName}";
                    _logger.LogInformation("ModelServicesTestException is true, testing exception throw.");
                }

                throw new Exception(ExceptionMessageString);
            }

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
                        _logger.LogInformation("Query ({}) executed and connection closed.", sp_insert_table_response);
                    }
                    catch (MySqlException ex)
                    {
                        Type classType = this.GetType();
                        string className = classType.Name.ToString();
                        string methodName = MethodBase.GetCurrentMethod()?.Name ?? "Unknown Method";
                        _logger.LogCritical($"{className}.{methodName}: {ex.Message}");
                        throw;
                    }
                }
            }

            return true;
        }

        public async Task<string> GetModelResponseAsync(string Model, string SystemContent, string UserContent, string NegativePrompt)
        {
            string url = _settings.Url;
            string data;
            string PromptTextDelimiter = _settings.PromptTextDelimiter;
            string FinalPrompt = SystemContent
                + " "
                + NegativePrompt
                + " "
                + PromptTextDelimiter
                + UserContent
                + PromptTextDelimiter;
                
            var options = "\"options\" : {{\"temperature\" : " + temperature + ", \"num_ctx\" : " + num_ctx + ", \"num_predict\" : " + num_predict + "}}";

            data = String.Format("{{\"model\": \"{0}\", \"prompt\": \"{1}\", \"stream\": false, " + options + "}}", Model, FinalPrompt);

            try
            {
                //////////////////////////////////////////
                // TEST EXCEPTION THROW
                //////////////////////////////////////////
                if (_settings.ModelServicesTestException == true)
                {
                    Type classType = this.GetType();
                    if (MethodBase.GetCurrentMethod() != null)
                    {
                        string className = classType.Name.ToString();
                        string methodName = MethodBase.GetCurrentMethod()?.Name ?? "UnknownMethod";
                        ExceptionMessageString = $"Test exception from: {className}.{methodName}";
                        _logger.LogInformation("ModelServicesTestException is true, testing exception throw.");
                    }

                    throw new Exception(ExceptionMessageString);
                }

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

                        text = _responseService.SanitizeResponseFromJson(text).Result;
                        return text;
                    }
                    else
                    {
                        ExceptionMessageString = String.Format("{0} {1}, {2}\nException: {3}", Model, UserContent, NegativePrompt, response.RequestMessage);
                        throw new Exception(ExceptionMessageString);
                    }
                }
            }
            catch (Exception ex)
            {
                Type classType = this.GetType();
                string className = classType.Name.ToString();
                string methodName = MethodBase.GetCurrentMethod()?.Name ?? "Unknown Method";
                _logger.LogCritical($"{className}.{methodName}: {ex.Message}");
                throw;
            }
        }

    }       //end class

}       //end namespace