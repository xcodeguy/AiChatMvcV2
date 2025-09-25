# Copilot Instructions for AiChatMvcV2

## Project Overview
- **AiChatMvcV2** is an ASP.NET Core MVC application for AI-driven chat and text-to-speech (TTS) workflows.
- The architecture is layered: `Controllers/` (MVC endpoints), `Classes/` (business logic), `Contracts/` (interfaces), `Models/` (view/data models), and `Objects/` (configuration/data objects).
- Data persistence and AI integration are handled via custom SQL scripts in `MySql/` and external API calls (e.g., TTS).

## Key Patterns & Conventions
- **Dependency Injection**: Controllers and classes use constructor injection for settings (`IOptions<ApplicationSettings>`) and logging (`ILogger<T>`).
- **Configuration**: App settings are in `appsettings.json` and `ApplicationSettings.cs` (see `Objects/`).
- **Logging**: Uses NLog, configured via `nlog.config`.
- **Async**: Most I/O and API calls are async (e.g., `GenerateTextToSpeechResourceFile`).
- **Error Handling**: Log errors with `_logger.LogCritical` or `_logger.LogInformation` as appropriate.
- **File Management**: TTS audio files are managed in `wwwroot/assets/` via helper methods (see `CopySpeechFileToAssets`).

## Developer Workflows
- **Build**: Use `dotnet build AiChatMvcV2.csproj` from the project root.
- **Run**: Use `dotnet run --project AiChatMvcV2.csproj`.
- **Debug**: Launch via Visual Studio or VS Code C# extension; settings in `Properties/launchSettings.json`.
- **Test**: No standard test project detected; add tests in a `/Tests` folder if needed.

## Integration Points
- **TTS API**: Endpoint and model name configured in `appsettings.json`; called from `ResponseController`.
- **MySQL**: SQL scripts for schema and stored procedures in `MySql/`.
- **Static Assets**: Place user-facing files in `wwwroot/assets/`.

## Examples
- To add a new AI feature, create an interface in `Contracts/`, implement in `Classes/`, and register via DI in `Program.cs`.
- To update TTS, modify `ApplicationSettings.cs` and `appsettings.json`, then update logic in `ResponseController`.

## File References
- `Classes/ResponseController.cs`: TTS and response parsing logic
- `Contracts/`: Service interfaces
- `MySql/`: Database scripts
- `Objects/ApplicationSettings.cs`: Strongly-typed config
- `wwwroot/assets/`: Static files for web UI

---

**For AI agents:**
- Follow the established DI, logging, and async patterns.
- Reference and update configuration via `ApplicationSettings`.
- Use the provided SQL scripts for DB changes.
- Place new static assets in `wwwroot/assets/`.
- Log all exceptions and critical events.
