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
using AiChatMvcV2.Services;

namespace AiChatMvcV2.Services
{
    public class ModelServices : IModelServices
    {
        private readonly ILogger<ModelServices> _logger;
        private readonly ResponseServices _responseController;
        private readonly ApplicationSettings _settings;
        private const float temperature = 0.8f;     //0.8
        private const int num_ctx = 2048;           //2048
        private const int num_predict = -1;         //-1
        private const string sp_insert_table_response = "sp_insert_table_response";
        private static string _connectionString = "Server=localhost;Database=WakeNbake;Uid=root;Pwd=";
        string ExceptionMessageString = string.Empty;

        public ModelServices(IOptions<ApplicationSettings> settings, ILogger<ModelServices> logger, ResponseServices responseController)
        {
            _logger = logger;
            _responseController = responseController;
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
            using (MySqlConnection connection = new(_connectionString))
            {
                using (MySqlCommand command = new(sp_insert_table_response, connection))
                {
                    _logger.LogInformation("Executing: {sp_insert_table_response}.", sp_insert_table_response);
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
                        _logger.LogInformation("Executed stored procedure sp_insert_table_response with a response object of {0}", TheResponse);
                        connection.Close();
                        _logger.LogInformation("Query executed and connection closed.");
                    }
                    catch (MySqlException ex)
                    {
                        _logger.LogCritical("Exception: CallController->InsertResponse: {exMessage}, {TheResponse}", ex.Message, TheResponse);
                        throw new Exception(ex.Message);
                    }
                }
            }

            return true;
        }

        public async Task<string> CallApiAsync(string Model, string SystemContent, string UserContent, string NegativePrompt)
        {
            string? url = _settings.Url;
            string data;
            UserContent = SystemContent + " " + UserContent + " " + NegativePrompt;
            var options = "\"options\" : {{\"temperature\" : " + temperature + ", \"num_ctx\" : " + num_ctx + ", \"num_predict\" : " + num_predict + "}}";

            data = String.Format("{{\"model\": \"{0}\", \"prompt\": \"{1}\", \"stream\": false, " + options + "}}", Model, UserContent);
            _logger.LogInformation("Built data string");

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(300); //5 min timeout

                    _logger.LogInformation("Calling model API");
                    var content = new StringContent(data, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Model response success");
                        using var stream = await response.Content.ReadAsStreamAsync();
                        using var reader = new StreamReader(stream);
                        var text = await reader.ReadToEndAsync();
                        _logger.LogInformation("Returning contents of {model}:{prompt}:{text}", Model, UserContent, text);
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
                _logger.LogCritical("Exception in CallController::CallApiAsync()->{0}\n{1}",
                            ExceptionMessageString != string.Empty ? ExceptionMessageString : "Unknown error",
                            ex.Message);
            }
            
            return string.Empty;
        }
        
    }       //end class

}       //end namespace