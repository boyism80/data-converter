using NDesk.Options;
using System.ComponentModel.DataAnnotations;

namespace ExcelTableConverter.Configuration
{
    public class AppConfiguration
    {
        public string InputDirectory { get; set; } = Path.Combine("..", "..", "..", "..");

        public string Languages { get; set; } = "c++";

        public string DslFilePath { get; set; } = "dsl.json";

        public List<string> Namespace { get; set; } = new List<string> { "unnamed" };

        public List<string> ConstNamespace { get; set; } = new List<string> { "const_value" };

        public List<string> EnumNamespace { get; set; } = new List<string> { "enum_value" };

        public string ConstFilePrefix { get; set; } = "const";

        public string EnumFilePrefix { get; set; } = "enum";

        public string JsonFilePath { get; set; } = "json";

        public string DiffFilePath { get; set; } = "diff";

        public string ParentTableFormat { get; set; } = "{0}_attribute";

        public string ParentPropName { get; set; } = "parent";

        public string DslTypeEnumName { get; set; } = "DSL";

        public HashSet<string> AdditionalHeaderFiles { get; set; } = new HashSet<string>();

        public List<string> Scopes { get; set; } = new List<string> { "server", "client" };

        public IReadOnlySet<string> TargetLanguages => Languages
            .Split('|')
            .Select(x => x.Trim().ToLower())
            .Where(x => !string.IsNullOrEmpty(x))
            .ToHashSet();

        public IReadOnlyList<(uint Flag, string Name)> DefinedScopes =>
            Scopes.Select((name, index) => (1u << index, name)).ToList();

        public static bool ContainsScope(uint columnScope, uint target) =>
            (columnScope & target) == target;

        public uint ParseScope(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("scope value is empty");

            uint result = 0;
            foreach (var part in value.Split('|'))
            {
                var name = part.Trim().ToLowerInvariant();
                if (string.IsNullOrEmpty(name))
                    throw new ArgumentException($"invalid scope value: {value}");

                var index = Scopes.FindIndex(x => x == name);
                if (index < 0)
                    throw new ArgumentException($"unknown scope '{name}' (configured: {string.Join("|", Scopes)})");

                result |= 1u << index;
            }

            if (result == 0)
                throw new ArgumentException($"invalid scope value: {value}");

            return result;
        }

        public string FormatScope(uint scope)
        {
            var names = DefinedScopes
                .Where(x => ContainsScope(scope, x.Flag))
                .Select(x => x.Name)
                .ToList();

            return names.Count > 0 ? string.Join("|", names) : scope.ToString();
        }

        public string GetScopeName(uint flag)
        {
            var match = DefinedScopes.FirstOrDefault(x => x.Flag == flag);
            if (match.Name == null)
                throw new ArgumentException($"scope flag is not a single configured scope: {flag}");

            return match.Name;
        }

        public static AppConfiguration Parse(string[] args)
        {
            var config = new AppConfiguration();
            OptionSet options = null!;

            options = new OptionSet
            {
                { "d|dir=", "input directory", v => config.InputDirectory = v },
                { "l|lang=", "code language", v => config.Languages = v },
                { "dsl=", "dsl file path", v => config.DslFilePath = v },
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
                { "scopes=", "scope names (pipe separated, default: server|client)", v => config.Scopes = ParseScopeNames(v) },
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

        public void Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(InputDirectory))
            {
                errors.Add("Input directory is required");
            }
            else if (!Directory.Exists(InputDirectory))
            {
                errors.Add($"Input directory does not exist: {InputDirectory}");
            }

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

            if (string.IsNullOrWhiteSpace(DslFilePath))
            {
                errors.Add("DSL file path is required");
            }
            else if (!File.Exists(DslFilePath))
            {
                errors.Add($"DSL file does not exist: {DslFilePath}");
            }

            if (string.IsNullOrWhiteSpace(Namespace.FirstOrDefault()))
            {
                errors.Add("Namespace configuration is required");
            }

            if (string.IsNullOrWhiteSpace(EnumNamespace.FirstOrDefault()))
            {
                errors.Add("EnumNamespace configuration is required");
            }

            if (Scopes == null || Scopes.Count == 0)
            {
                errors.Add("At least one scope name is required");
            }
            else if (Scopes.Count > 32)
            {
                errors.Add("Scope count cannot exceed 32");
            }
            else if (Scopes.Any(string.IsNullOrWhiteSpace))
            {
                errors.Add("Scope names cannot be empty");
            }
            else if (Scopes.Count != Scopes.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            {
                errors.Add("Scope names must be unique");
            }

            if (errors.Any())
            {
                throw new ValidationException($"Configuration validation failed:\n{string.Join("\n", errors)}");
            }

            Scopes = Scopes.Select(x => x.Trim().ToLowerInvariant()).ToList();
        }

        private static List<string> ParseScopeNames(string value)
        {
            return value
                .Split('|')
                .Select(x => x.Trim().ToLowerInvariant())
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList();
        }

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
            Console.WriteLine("  ExcelTableConverter -d ./tables -l \"c++|c#\" --dsl dsl.json");
            Console.WriteLine("  ExcelTableConverter --dir ./data --lang c++ --namespace \"fb.model\" --const-namespace \"const_value\"");

            System.Environment.Exit(0);
        }
    }
}