using NDesk.Options;
using System.ComponentModel.DataAnnotations;

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
        /// Gets or sets the namespace configuration (dot separated)
        /// </summary>
        public List<string> Namespace { get; set; } = new List<string> { "unnamed" };

        /// <summary>
        /// Gets or sets the const namespace configuration (dot separated)
        /// </summary>
        public List<string> ConstNamespace { get; set; } = new List<string> { "const_value" };

        /// <summary>
        /// Gets or sets the enum namespace configuration (dot separated)
        /// </summary>
        public List<string> EnumNamespace { get; set; } = new List<string> { "enum_value" };

        /// <summary>
        /// Gets or sets the const file prefix
        /// </summary>
        public string ConstFilePrefix { get; set; } = "const";

        /// <summary>
        /// Gets or sets the enum file prefix
        /// </summary>
        public string EnumFilePrefix { get; set; } = "enum";

        /// <summary>
        /// Gets or sets the JSON file path
        /// </summary>
        public string JsonFilePath { get; set; } = "json";

        /// <summary>
        /// Gets or sets the diff file path
        /// </summary>
        public string DiffFilePath { get; set; } = "diff";

        /// <summary>
        /// Gets or sets the parent table format
        /// </summary>
        public string ParentTableFormat { get; set; } = "{0}_attribute";

        /// <summary>
        /// Gets or sets the parent property name
        /// </summary>
        public string ParentPropName { get; set; } = "parent";

        /// <summary>
        /// Gets or sets the DSL type enum name
        /// </summary>
        public string DslTypeEnumName { get; set; } = "DSL";

        /// <summary>
        /// Gets or sets the additional header files (pipe separated)
        /// </summary>
        public HashSet<string> AdditionalHeaderFiles { get; set; } = new HashSet<string>();

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
                { "dsl=", "dsl file path", v => config.DslFilePath = v },
                
                // Configuration options with default values shown in descriptions
                { "namespace=|ns=", "namespace (dot separated, default: unnamed)", v => config.Namespace = v.Split('.').ToList() },
                { "const-namespace=", "const namespace (dot separated, default: const_value)", v => config.ConstNamespace = v.Split('.').ToList() },
                { "enum-namespace=", "enum namespace (dot separated, default: enum_value)", v => config.EnumNamespace = v.Split('.').ToList() },
                { "const-prefix=", "const file prefix (default: const)", v => config.ConstFilePrefix = v },
                { "enum-prefix=", "enum file prefix (default: enum)", v => config.EnumFilePrefix = v },
                { "json-path=", "json file path (default: json)", v => config.JsonFilePath = v },
                { "diff-path=", "diff file path (default: diff)", v => config.DiffFilePath = v },
                { "parent-format=", "parent table format (default: {0}_attribute)", v => config.ParentTableFormat = v },
                { "parent-prop=", "parent property name (default: parent)", v => config.ParentPropName = v },
                { "dsl-enum=", "dsl enum name (default: DSL)", v => config.DslTypeEnumName = v },
                { "additional-headers=", "additional header files (pipe separated, default: none)", v => config.AdditionalHeaderFiles = v.Split('|').ToHashSet() },

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

            // Validate namespace configuration
            if (string.IsNullOrWhiteSpace(Namespace.FirstOrDefault()))
            {
                errors.Add("Namespace configuration is required");
            }

            // Validate enum namespace configuration
            if (string.IsNullOrWhiteSpace(EnumNamespace.FirstOrDefault()))
            {
                errors.Add("EnumNamespace configuration is required");
            }

            if (errors.Any())
            {
                throw new ValidationException($"Configuration validation failed:\n{string.Join("\n", errors)}");
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
            Console.WriteLine("  -d, --dir=VALUE            input directory");
            Console.WriteLine("  -l, --lang=VALUE           code language");
            Console.WriteLine("      --dsl=VALUE            dsl file path");
            Console.WriteLine("      --namespace=VALUE      namespace (dot separated, default: unnamed)");
            Console.WriteLine("      --ns=VALUE             namespace (dot separated, default: unnamed)");
            Console.WriteLine("      --const-namespace=VALUE const namespace (dot separated, default: const_value)");
            Console.WriteLine("      --enum-namespace=VALUE  enum namespace (dot separated, default: enum_value)");
            Console.WriteLine("      --const-prefix=VALUE   const file prefix (default: const)");
            Console.WriteLine("      --enum-prefix=VALUE    enum file prefix (default: enum)");
            Console.WriteLine("      --json-path=VALUE      json file path (default: json)");
            Console.WriteLine("      --diff-path=VALUE      diff file path (default: diff)");
            Console.WriteLine("      --parent-format=VALUE  parent table format (default: {0}_attribute)");
            Console.WriteLine("      --parent-prop=VALUE    parent property name (default: parent)");
            Console.WriteLine("      --dsl-enum=VALUE       dsl enum name (default: DSL)");
            Console.WriteLine("      --additional-headers=VALUE additional header files (pipe separated, default: none)");
            Console.WriteLine("  -h, --help                 show help");
            Console.WriteLine();
            Console.WriteLine("Supported Languages:");
            Console.WriteLine("  c++   - Generate C++ code");
            Console.WriteLine("  c#    - Generate C# code");
            Console.WriteLine("  node  - Generate Node.js code");
            Console.WriteLine("  go    - Generate Go code");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  ExcelTableConverter -d ./tables -l \"c++|c#\" --dsl dsl.json");
            Console.WriteLine("  ExcelTableConverter --dir ./data --lang c++ --namespace \"fb.model\" --const-namespace \"const_value\"");

            System.Environment.Exit(0);
        }
    }
}