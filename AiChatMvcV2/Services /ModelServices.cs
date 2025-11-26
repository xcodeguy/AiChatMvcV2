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
using System.Text.Json;

namespace AiChatMvcV2.Services
{
    public class ModelServices : IModelServices
    {
        #region Declarations
        private readonly ILogger<ModelServices> _logger;
        private readonly ResponseServices _responseService;
        private readonly ApplicationSettings _settings;
        string _className = string.Empty;

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
                    command.Parameters.AddWithValue("response", TheResponse.Response);
                    command.Parameters.AddWithValue("model", TheResponse.Model);
                    command.Parameters.AddWithValue("topic", TheResponse.Topic);
                    command.Parameters.AddWithValue("prompt", TheResponse.Prompt);
                    command.Parameters.AddWithValue("negative_prompt", TheResponse.NegativePrompt);
                    command.Parameters.AddWithValue("active", TheResponse.Active);
                    command.Parameters.AddWithValue("audio_file_name", TheResponse.AudioFilename);
                    command.Parameters.AddWithValue("audio_file_size", TheResponse.AudioFileSize);
                    command.Parameters.AddWithValue("exceptions", TheResponse.Exceptions);
                    command.Parameters.AddWithValue("response_time", DateTime.Now.ToString("yyyy-MM-dd ") + TheResponse.ResponseTime);
                    command.Parameters.AddWithValue("word_count", TheResponse.WordCount);
                    command.Parameters.AddWithValue("tts_voice", TheResponse.TtsVoice);
                    command.Parameters.AddWithValue("score", TheResponse.Score);
                    command.Parameters.AddWithValue("grade", TheResponse.Grade);
                    command.Parameters.AddWithValue("score_reasons", JsonSerializer.Serialize(TheResponse.ScoreReasons));

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

            var options = ReadParametersFile();
            data = $@"{{""model"" : ""{Model}"",
                    ""prompt"" : ""{Prompt}"",
                    ""stream"" : false,
                    ""format"" : ""json"",
                    ""options"" : {options}
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
                        ExceptionMessageString = $"HTTP Request failed with status code: {response.StatusCode}";
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

        private string ReadParametersFile()
        {
            try
            {
                ModelParameters ParametersObject = new ModelParameters();
                string filePath = Directory.GetCurrentDirectory() + "/wwwroot/assets/parameters.json";
                string parameters = System.IO.File.ReadAllText(filePath);

                if (parameters == null || parameters == String.Empty)
                {
                    ExceptionMessageString = $"The prompt file parameters.json is missing or empty.";
                    _logger.LogCritical(ExceptionMessageString);
                    throw new Exception(ExceptionMessageString);
                }
                parameters = _responseService.RemoveFormatStrings(parameters, true);
                return parameters;
            }
            catch (Exception ex)
            {
                ExceptionMessageString = $"{_className}.{MethodBase.GetCurrentMethod()}: {ex.Message}";
                _logger.LogCritical(ExceptionMessageString);
                throw new Exception(ExceptionMessageString);
            }
        }

    }       //end class
}       //end namespace