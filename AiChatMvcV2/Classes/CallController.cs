///////////////////////////////////////////////
//    David Ferrell
//    Copyright (C) 2025, Xcodeguy Software
//    Class for calling LLM API's in OLlama
///////////////////////////////////////////////
using AiChatMvcV2.Contracts;
using System.Text;
using AiChatMvcV2.Objects;
using Microsoft.Extensions.Options;
using MySql.Data;
using MySql.Data.MySqlClient;
using AiChatMvcV2.Models;
using System.Data;


namespace AiChatMvcV2.Classes
{

    public class CallController : ICallController
    {
        private readonly ILogger<CallController> _logger;
        private readonly ApplicationSettings _settings;
        private const float temperature = 0.8f;     //0.8
        private const int num_ctx = 2048;           //2048
        private const int num_predict = -1;         //-1

        private const string sp_insert_table_response = "sp_insert_table_response";

        private static string _connectionString = "Server=localhost;Database=WakeNbake;Uid=root;Pwd=amputee2025!";


        public CallController(IOptions<ApplicationSettings> settings, ILogger<CallController> logger)
        {
            _logger = logger;
            _settings = settings.Value;
            _logger.LogDebug(1, "CallApi class initialized. Injections are happy");
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
            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                using (MySqlCommand command = new MySqlCommand(sp_insert_table_response, connection))
                {
                    _logger.LogInformation("Executing: {sp_insert_table_response}.", sp_insert_table_response);
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("timestamp", TheResponse.TimeStamp);
                    command.Parameters.AddWithValue("response", TheResponse.Response);
                    command.Parameters.AddWithValue("model", TheResponse.Model);
                    command.Parameters.AddWithValue("topic", TheResponse.Prompt);
                    command.Parameters.AddWithValue("prompt", TheResponse.Prompt);
                    command.Parameters.AddWithValue("negative_prompt", TheResponse.NegativePrompt);
                    command.Parameters.AddWithValue("active", 1);
                    command.Parameters.AddWithValue("last_updated", new DateTime());
                    command.Parameters.AddWithValue("exceptions", TheResponse.Exceptions);
                    command.Parameters.AddWithValue("response_time", TheResponse.ResponseTime);
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
                    }
                }
            }

            return true;
        }

        public async Task<string> CallApiAsync(string Model, string SystemContent, string UserContent, string NegativePrompt)
        {
            MySqlConnection? conn = GetConnection();
            if (conn != null)
            {
                conn.Close();
            }


            string? url = _settings.Url;
            string data;
            UserContent = SystemContent + " " + UserContent + " " + NegativePrompt;
            var options = "\"options\" : {{\"temperature\" : " + temperature + ", \"num_ctx\" : " + num_ctx + ", \"num_predict\" : " + num_predict + "}}";
            //data = "{\"model\": \"" + Model + "\",\"messages\":[{\"role\":\"system\",\"content\":\"" + SystemContent + "\"},{\"role\": \"user\", \"content\": \"" + UserContent + "\"}],\"stream\": false}";
            /*
                EXAMPLE:
                {
                    "model": "gemma3",
                    "prompt": "Simulate a Model UN session regarding global nutrition.",
                    "stream": false,
                    "options": {
                        "temperature": 2,
                        "num_ctx": 2048,
                        "num_predict": -1
                    }
                }
            */
            data = String.Format("{{\"model\": \"{0}\", \"prompt\": \"{1}\", \"stream\": false, " + options + "}}", Model, UserContent);
            _logger.LogInformation("Built data string");

            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(300); //5 min timeout

                _logger.LogInformation("Calling AI model API");
                var content = new StringContent(data, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("AI model response success");
                    var text = response.Content.ReadAsStreamAsync();
                    _logger.LogInformation("Returning contents of {model}:{prompt}:{text}", Model, UserContent, text);
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    String ExceptionMessageString = String.Format("Exception in CallController::CallApiAsync() {0} {1}, {2}\nException: {3}", Model, UserContent, NegativePrompt, response.RequestMessage);
                    _logger.LogCritical(ExceptionMessageString);
                    throw new Exception(ExceptionMessageString);
                }
            }
        }
    }       //end class

}       //end namespace