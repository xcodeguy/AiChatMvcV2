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
                        _logger.LogCritical("Exception: CallController->InsertResponse: {exMessage}, {TheResponse}", ex.Message, TheResponse);
                        throw new Exception(ex.Message);
                    }
                }
            }

            return true;
        }

        public async Task<string> GetModelResponseAsync(string Model, string SystemContent, string UserContent, string NegativePrompt)
        {
            string url = _settings.Url;
            string data;
            UserContent = SystemContent + " " + UserContent + " " + NegativePrompt;
            var options = "\"options\" : {{\"temperature\" : " + temperature + ", \"num_ctx\" : " + num_ctx + ", \"num_predict\" : " + num_predict + "}}";

            data = String.Format("{{\"model\": \"{0}\", \"prompt\": \"{1}\", \"stream\": false, " + options + "}}", Model, UserContent);

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
                _logger.LogCritical("Exception in CallController::CallApiAsync()->{0}\n{1}",
                                    ExceptionMessageString,
                                    ex.Message);
            }

            return string.Empty;
        }

    }       //end class

}       //end namespace