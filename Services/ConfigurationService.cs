using System.ComponentModel.DataAnnotations;
using ExcelTableConverter.Configuration;
using ExcelTableConverter.Model;
using ExcelTableConverter.Util;
using Newtonsoft.Json;

namespace ExcelTableConverter.Services
{
    /// <summary>
    /// Configuration service implementation that manages application settings
    /// 
    /// Combines command-line arguments, configuration files, and environment variables
    /// into a unified configuration system following ASP.NET Core patterns.
    /// Command-line arguments override configuration file settings.
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        private readonly AppConfiguration _appConfig;
        private readonly FileConfiguration _fileConfig;

        /// <summary>
        /// Initializes a new instance of the ConfigurationService
        /// </summary>
        /// <param name="appConfig">Command-line and application configuration</param>
        public ConfigurationService(AppConfiguration appConfig)
        {
            _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
            _fileConfig = LoadConfigurationFile();

            ValidateConfiguration();
        }

        /// <summary>
        /// Gets the input directory path for Excel files
        /// </summary>
        public string InputDirectory => _appConfig.InputDirectory;

        /// <summary>
        /// Gets the target programming languages for code generation
        /// </summary>
        public IReadOnlySet<string> TargetLanguages => _appConfig.TargetLanguages;

        /// <summary>
        /// Gets the DSL configuration file path
        /// </summary>
        public string DslFilePath => _appConfig.DslFilePath;

        /// <summary>
        /// Gets the environment variable value
        /// </summary>
        public string Environment => _appConfig.Environment;

        /// <summary>
        /// Gets the namespace configuration
        /// </summary>
        public List<string> Namespace => _fileConfig.Namespace ?? new List<string>();

        /// <summary>
        /// Gets the enum namespace configuration
        /// </summary>
        public List<string> EnumNamespace => _fileConfig.EnumNamespace ?? new List<string>();

        /// <summary>
        /// Gets the const namespace configuration
        /// </summary>
        public List<string> ConstNamespace => _fileConfig.ConstNamespace ?? new List<string>();

        /// <summary>
        /// Gets the const file prefix
        /// </summary>
        public string ConstFilePrefix => _fileConfig.ConstFilePrefix ?? string.Empty;

        /// <summary>
        /// Gets the enum file prefix
        /// </summary>
        public string EnumFilePrefix => _fileConfig.EnumFilePrefix ?? string.Empty;

        /// <summary>
        /// Gets the JSON file path
        /// </summary>
        public string JsonFilePath => _fileConfig.JsonFilePath ?? string.Empty;

        /// <summary>
        /// Gets the diff file path
        /// </summary>
        public string DiffFilePath => _fileConfig.DiffFilePath ?? string.Empty;

        /// <summary>
        /// Gets the parent table format
        /// </summary>
        public string ParentTableFormat => _fileConfig.ParentTableFormat ?? "{0}Attribute";

        /// <summary>
        /// Gets the parent property name
        /// </summary>
        public string ParentPropName => _fileConfig.ParentPropName ?? "Parent";

        /// <summary>
        /// Gets the DSL type enum name
        /// </summary>
        public string DslTypeEnumName => _fileConfig.DslTypeEnumName ?? "DslFunctionType";

        /// <summary>
        /// Gets the additional header files
        /// </summary>
        public HashSet<string> AdditionalHeaderFiles => _fileConfig.AdditionalHeaderFiles ?? new HashSet<string>();

        /// <summary>
        /// Gets the legacy Config object for backward compatibility
        /// This will be removed in future versions
        /// </summary>
        [Obsolete("Use individual properties instead of Config object")]
        public FileConfiguration GetLegacyConfig() => _fileConfig;

        /// <summary>
        /// Loads configuration from the config.json file with environment variable support
        /// </summary>
        /// <returns>The loaded configuration</returns>
        /// <exception cref="FileNotFoundException">Thrown when config file is not found</exception>
        /// <exception cref="JsonException">Thrown when config file has invalid JSON</exception>
        private FileConfiguration LoadConfigurationFile()
        {
            var file = Path.GetFileNameWithoutExtension("config.json");
            var ext = Path.GetExtension("config.json");
            var configFileName = "config.json";

            // Check for environment-specific config file
            if (!string.IsNullOrEmpty(_appConfig.Environment))
            {
                var envFileName = $"{file}.{_appConfig.Environment}{ext}";
                if (File.Exists(envFileName))
                    configFileName = envFileName;
            }

            if (!File.Exists(configFileName))
                throw new FileNotFoundException($"Configuration file not found: {configFileName}");

            try
            {
                var contents = File.ReadAllText(configFileName);
                return JsonConvert.DeserializeObject<FileConfiguration>(contents) ?? new FileConfiguration();
            }
            catch (JsonException ex)
            {
                throw new JsonException($"Invalid JSON in configuration file '{configFileName}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validates the combined configuration
        /// </summary>
        /// <exception cref="ValidationException">Thrown when validation fails</exception>
        private void ValidateConfiguration()
        {
            var errors = new List<string>();

            // Validate namespace configuration
            if (Namespace == null || !Namespace.Any())
            {
                errors.Add("Namespace configuration is required in config.json");
            }

            // Validate enum namespace configuration
            if (EnumNamespace == null || !EnumNamespace.Any())
            {
                errors.Add("EnumNamespace configuration is required in config.json");
            }

            if (errors.Any())
            {
                throw new ValidationException($"Configuration validation failed:\n{string.Join("\n", errors)}");
            }
        }
    }
}