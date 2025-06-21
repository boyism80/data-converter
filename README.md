# Excel Table Converter

A powerful tool for converting Excel files to various programming language formats including C++, C#, Node.js, and Go. This application processes Excel files containing game data tables and generates strongly-typed code representations with comprehensive validation and caching capabilities.

## Features

- **Multi-Language Support**: Generate code for C++, C#, Node.js, and Go
- **Intelligent Caching**: CRC32-based change detection to process only modified files
- **Comprehensive Validation**: Multiple validation stages ensure data integrity
- **Service-Based Architecture**: Clean separation of concerns with dependency injection
- **Error Recovery**: Robust error handling with detailed logging and recovery mechanisms
- **Performance Monitoring**: Built-in elapsed time measurement and reporting

## Architecture Overview

The application follows a service-based architecture with clear separation of concerns:

### Core Services

#### Configuration Management
- **`AppConfiguration`**: Centralized configuration with validation and command-line parsing
- Supports multiple target languages and environment-specific settings
- Built-in help system and argument validation

#### File Processing Service
- **`IFileProcessingService`** / **`FileProcessingService`**: Handles Excel file operations
- CRC32-based change detection for incremental processing
- Cache management and cleanup operations
- Error file tracking and recovery

#### Processing Pipeline Service
- **`IProcessingPipelineService`** / **`ProcessingPipelineService`**: Coordinates processing stages
- Data loading and transformation pipeline
- Validation pipeline with multiple validators
- Code generation pipeline for target languages

#### Service Container
- **`ServiceContainer`**: Simple dependency injection container
- Service registration and resolution
- Singleton and factory pattern support

### Processing Pipeline

1. **File Discovery & Categorization**
   - Scans input directory for Excel files
   - Categorizes files by type (constants, enums, data)
   - Performs CRC32 checksums for change detection

2. **Data Loading**
   - Loads Excel workbooks and sheets
   - Processes constants, enums, and data tables
   - Merges with cached context

3. **Validation Pipeline**
   - Name validation
   - Schema validation
   - Key validation
   - Enum validation
   - DSL validation
   - Relation type validation
   - Strong type validation

4. **Code Generation**
   - JSON file generation
   - Language-specific code generation
   - CRC file generation for integrity checking

5. **Cache Management**
   - Updates data cache for processed files
   - Maintains performance metrics
   - Tracks error files for recovery

## Usage

### Command Line Options

```bash
ExcelTableConverter [options]

Options:
  -d, --dir=VALUE        Input directory containing Excel files
  -l, --lang=VALUE       Target programming languages (pipe-separated)
  -e, --env=VALUE        Environment variable value
  --dsl=VALUE           DSL configuration file path
  -h, --help            Show help information
```

### Examples

```bash
# Generate C++ code from Excel files
ExcelTableConverter -d ./tables -l c++

# Generate multiple language outputs
ExcelTableConverter -d ./data -l "c++|c#|node" --dsl config.json

# Production environment with specific settings
ExcelTableConverter --dir ./production-data --lang "c#|go" --env production
```

### Supported Languages

- **c++**: Generate C++ header and implementation files
- **c#**: Generate C# class files with proper namespacing
- **node**: Generate Node.js modules with TypeScript definitions
- **go**: Generate Go structs and JSON marshaling

## Configuration

### DSL Configuration

The DSL (Domain Specific Language) configuration file defines how data should be processed and validated. It supports:

- Custom data type definitions
- Validation rules and constraints
- Output formatting options
- Language-specific generation settings

### Environment Variables

Set the `env` environment variable to control environment-specific behavior:

```bash
# Set environment for configuration selection
ExcelTableConverter --env production
```

## File Structure

```
ExcelTableConverter/
├── Configuration/
│   └── AppConfiguration.cs          # Centralized configuration management
├── Services/
│   ├── IFileProcessingService.cs    # File processing interface
│   ├── FileProcessingService.cs     # File processing implementation
│   ├── IProcessingPipelineService.cs # Pipeline coordination interface
│   ├── ProcessingPipelineService.cs  # Pipeline coordination implementation
│   └── ServiceContainer.cs          # Dependency injection container
├── Worker/
│   ├── Cache/                       # Caching workers
│   ├── Generator/                   # Code generation workers
│   ├── Loader/                      # Data loading workers
│   └── Validator/                   # Validation workers
├── Model/                           # Data models and DTOs
├── Util/                           # Utility classes
├── Template/                       # Code generation templates
└── Program.cs                      # Main application entry point
```

## Caching System

The application uses an intelligent caching system to improve performance:

- **CRC32 Checksums**: Detects file changes without full processing
- **Incremental Processing**: Only processes modified files
- **Error Recovery**: Tracks and retries files that previously failed
- **Cache Invalidation**: Automatically clears cache when build version changes

## Error Handling

Comprehensive error handling includes:

- **Validation Errors**: Detailed validation messages with file and location information
- **Processing Errors**: Graceful handling of Excel file processing issues
- **Recovery Mechanisms**: Automatic retry of previously failed files
- **Error Tracking**: Persistent error file tracking across runs

## Performance Monitoring

Built-in performance monitoring provides:

- **Elapsed Time Tracking**: Detailed timing for each processing stage
- **Progress Reporting**: Real-time progress updates during processing
- **Performance Metrics**: Comprehensive performance reports
- **Bottleneck Identification**: Helps identify performance issues

## Dependencies

- **.NET 8.0**: Modern .NET runtime with performance improvements
- **NPOI**: Excel file reading and processing
- **Newtonsoft.Json**: JSON serialization and deserialization
- **Scriban**: Template engine for code generation
- **Crc32.NET**: CRC32 checksum calculation
- **NDesk.Options**: Command-line argument parsing
- **System.ComponentModel.Annotations**: Validation attributes

## Development

### Building

```bash
dotnet build
```

### Running Tests

```bash
dotnet test
```

### Code Standards

- Follow Microsoft C# coding conventions
- Use XML documentation for all public APIs
- Implement proper error handling and validation
- Use nullable reference types for better null safety
- Follow SOLID principles and clean architecture patterns

## Contributing

1. Fork the repository
2. Create a feature branch
3. Follow the coding standards
4. Add comprehensive tests
5. Update documentation
6. Submit a pull request

## License

This project is licensed under the MIT License. See the LICENSE file for details.

## Changelog

### Version 1.2.0
- **NEW**: Service-based architecture with dependency injection
- **NEW**: Centralized configuration management with validation
- **NEW**: Improved error handling and recovery mechanisms
- **NEW**: Enhanced logging and performance monitoring
- **IMPROVED**: Code organization and maintainability
- **IMPROVED**: Documentation and XML comments
- **FIXED**: Nullable reference type warnings
- **FIXED**: Memory leaks in file processing

### Version 1.1.0
- Initial stable release
- Multi-language code generation support
- CRC32-based caching system
- Comprehensive validation pipeline

