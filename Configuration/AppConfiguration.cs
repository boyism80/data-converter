using System.ComponentModel.DataAnnotations;
using NDesk.Options;

namespace ExcelTableConverter.Configuration
{
    /// <summary>
    /// Centralized application configuration with validation and command-line argument parsing
    /// 
    /// Manages all configuration settings for the Excel table converter including
    /// input directories, target languages, DSL files, and environment variables.
    /// </summary>
    public class AppConfiguration
    {
        /// <summary>
        /// Gets or sets the input directory path for Excel files
        /// </summary>
        [Required]
        public string InputDirectory { get; set; } = Path.Combine("..", "..", "..", "..");

        /// <summary>
        /// Gets or sets the target programming languages for code generation
        /// </summary>
        [Required]
        public string Languages { get; set; } = "c++";

        /// <summary>
        /// Gets or sets the DSL configuration file path
        /// </summary>
        [Required]
        public string DslFilePath { get; set; } = "dsl.json";

        /// <summary>
        /// Gets or sets the environment variable value
        /// </summary>
        public string Environment { get; set; } = string.Empty;

        /// <summary>
        /// Gets the parsed target languages as a collection
        /// </summary>
        public IReadOnlySet<string> TargetLanguages => Languages
            .Split('|')
            .Select(x => x.Trim().ToLower())
            .Where(x => !string.IsNullOrEmpty(x))
            .ToHashSet();

        /// <summary>
        /// Parses command-line arguments and populates configuration properties
        /// </summary>
        /// <param name="args">Command-line arguments array</param>
        /// <returns>The configured AppConfiguration instance</returns>
        /// <exception cref="ArgumentException">Thrown when invalid arguments are provided</exception>
        public static AppConfiguration Parse(string[] args)
        {
            var config = new AppConfiguration();
            OptionSet options = null!;
            
            options = new OptionSet
            {
                { "d|dir=", "input directory", v => config.InputDirectory = v },
                { "l|lang=", "code language", v => config.Languages = v },
                { "e|env=", "environment variable", v => config.Environment = v },
                { "dsl=", "dsl file path", v => config.DslFilePath = v },
                { "h|help", "show help", v => { if (v != null) ShowHelp(options); } }
            };

            try
            {
                options.Parse(args);
            }
            catch (OptionException ex)
            {
                throw new ArgumentException($"Invalid command line arguments: {ex.Message}", ex);
            }

            config.Validate();
            return config;
        }

        /// <summary>
        /// Validates the configuration settings
        /// </summary>
        /// <exception cref="ValidationException">Thrown when validation fails</exception>
        public void Validate()
        {
            var errors = new List<string>();

            // Validate input directory
            if (string.IsNullOrWhiteSpace(InputDirectory))
            {
                errors.Add("Input directory is required");
            }
            else if (!Directory.Exists(InputDirectory))
            {
                errors.Add($"Input directory does not exist: {InputDirectory}");
            }

            // Validate languages
            if (string.IsNullOrWhiteSpace(Languages))
            {
                errors.Add("Languages parameter is required");
            }
            else
            {
                var supportedLanguages = new HashSet<string> { "c++", "c#", "node", "go" };
                var invalidLanguages = TargetLanguages.Where(lang => !supportedLanguages.Contains(lang)).ToList();
                
                if (invalidLanguages.Any())
                {
                    errors.Add($"Unsupported languages: {string.Join(", ", invalidLanguages)}");
                }
            }

            // Validate DSL file
            if (string.IsNullOrWhiteSpace(DslFilePath))
            {
                errors.Add("DSL file path is required");
            }
            else if (!File.Exists(DslFilePath))
            {
                errors.Add($"DSL file does not exist: {DslFilePath}");
            }

            if (errors.Any())
            {
                throw new ValidationException($"Configuration validation failed:\n{string.Join("\n", errors)}");
            }
        }

        /// <summary>
        /// Applies the configuration settings to the environment
        /// </summary>
        public void ApplyToEnvironment()
        {
            if (!string.IsNullOrWhiteSpace(Environment))
            {
                System.Environment.SetEnvironmentVariable("env", Environment);
            }
        }

        /// <summary>
        /// Shows help information for command-line usage
        /// </summary>
        /// <param name="options">The option set to display help for</param>
        private static void ShowHelp(OptionSet options)
        {
            Console.WriteLine("Excel Table Converter - Converts Excel files to various programming language formats");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  ExcelTableConverter [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            options.WriteOptionDescriptions(Console.Out);
            Console.WriteLine();
            Console.WriteLine("Supported Languages:");
            Console.WriteLine("  c++   - Generate C++ code");
            Console.WriteLine("  c#    - Generate C# code");
            Console.WriteLine("  node  - Generate Node.js code");
            Console.WriteLine("  go    - Generate Go code");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  ExcelTableConverter -d ./tables -l \"c++|c#\" -dsl config.json");
            Console.WriteLine("  ExcelTableConverter --dir ./data --lang c++ --env production");
            
            System.Environment.Exit(0);
        }
    }
} 