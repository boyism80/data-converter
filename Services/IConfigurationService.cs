using ExcelTableConverter.Model;

namespace ExcelTableConverter.Services
{
    /// <summary>
    /// Configuration service interface for managing application settings
    /// 
    /// Provides unified access to configuration settings from multiple sources
    /// including config files, command-line arguments, and environment variables.
    /// Follows the ASP.NET Core IConfiguration pattern for dependency injection.
    /// </summary>
    public interface IConfigurationService
    {
        /// <summary>
        /// Gets the input directory path for Excel files
        /// </summary>
        string InputDirectory { get; }

        /// <summary>
        /// Gets the target programming languages for code generation
        /// </summary>
        IReadOnlySet<string> TargetLanguages { get; }

        /// <summary>
        /// Gets the DSL configuration file path
        /// </summary>
        string DslFilePath { get; }

        /// <summary>
        /// Gets the environment variable value
        /// </summary>
        string Environment { get; }

        /// <summary>
        /// Gets the namespace configuration
        /// </summary>
        List<string> Namespace { get; }

        /// <summary>
        /// Gets the enum namespace configuration
        /// </summary>
        List<string> EnumNamespace { get; }

        /// <summary>
        /// Gets the const namespace configuration
        /// </summary>
        List<string> ConstNamespace { get; }

        /// <summary>
        /// Gets the const file prefix
        /// </summary>
        string ConstFilePrefix { get; }

        /// <summary>
        /// Gets the enum file prefix
        /// </summary>
        string EnumFilePrefix { get; }

        /// <summary>
        /// Gets the JSON file path
        /// </summary>
        string JsonFilePath { get; }

        /// <summary>
        /// Gets the diff file path
        /// </summary>
        string DiffFilePath { get; }

        /// <summary>
        /// Gets the parent table format
        /// </summary>
        string ParentTableFormat { get; }

        /// <summary>
        /// Gets the parent property name
        /// </summary>
        string ParentPropName { get; }

        /// <summary>
        /// Gets the DSL type enum name
        /// </summary>
        string DslTypeEnumName { get; }

        /// <summary>
        /// Gets the additional header files
        /// </summary>
        HashSet<string> AdditionalHeaderFiles { get; }

        /// <summary>
        /// Gets the legacy Config object for backward compatibility
        /// This will be removed in future versions
        /// </summary>
        [Obsolete("Use individual properties instead of Config object")]
        FileConfiguration GetLegacyConfig();
    }
}