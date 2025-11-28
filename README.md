# Excel Table Converter

A comprehensive tool that converts Excel files containing game data into strongly-typed code and JSON files for multiple programming languages. The converter supports C++, C#, Node.js, and Go, providing seamless integration between game data design and code implementation.

## Features

- **Multi-language Support**: Generate code for C++, C#, Node.js, and Go
- **Intelligent Caching**: Only processes changed files for optimal performance
- **Data Validation**: Comprehensive validation including schema, keys, enums, and relationships
- **Inheritance Support**: Table inheritance with polymorphic data loading
- **Type Safety**: Strong typing with nullable type support
- **Scope Management**: Separate data for server and client builds
- **Real-time Processing**: Incremental compilation with error tracking

## Installation & Usage

### Basic Usage
```bash
ExcelTableConverter --dir=<path-to-excel-files> 
                    --lang="c++|c#|node|go"
                    --dsl=<path-to-dsl>
```

### Command Line Options

#### Required Options
- `--dir=<path>` or `-d=<path>`: Input directory containing Excel files
- `--lang=<languages>` or `-l=<languages>`: Target languages (pipe-separated)
- `--dsl=<path>`: DSL configuration file path

#### Optional Configuration Options
- `--namespace=<value>` or `--ns=<value>`: Namespace (dot separated, default: unnamed)
- `--const-namespace=<value>`: Const namespace (dot separated, default: const_value)
- `--enum-namespace=<value>`: Enum namespace (dot separated, default: enum_value)
- `--const-prefix=<value>`: Const file prefix (default: const)
- `--enum-prefix=<value>`: Enum file prefix (default: enum)
- `--json-path=<value>`: JSON file path (default: json)
- `--diff-path=<value>`: Diff file path (default: diff)
- `--parent-format=<value>`: Parent table format (default: {0}_attribute)
- `--parent-prop=<value>`: Parent property name (default: parent)
- `--dsl-enum=<value>`: DSL enum name (default: DSL)
- `--additional-headers=<value>`: Additional header files (pipe separated, default: none)
- `--help` or `-h`: Show help information

### Examples
```bash
# Generate C++ code only
ExcelTableConverter --dir="../data" --lang="c++" --dsl="dsl.json"

# Generate multiple languages
ExcelTableConverter --dir="../data" --lang="c++|c#|go" --dsl="dsl.json"

# Specify custom namespace
ExcelTableConverter --dir="../data" --lang="c++" --dsl="dsl.json" --ns="my.fb.model"

# Use short options
ExcelTableConverter -d "../data" -l "c++" --dsl="dsl.json" --ns="fb.model"

# Custom configuration
ExcelTableConverter --dir="../data" --lang="c++" --dsl="dsl.json" \
  --ns="fb.model" \
  --const-namespace="const_value" \
  --enum-namespace="enum_value" \
  --additional-headers="my.header.h|another.header.h"
```

## Rules of File name
If the prefix is 'const', it is treated as a constant table; if it is 'enum', it is treated as an enum table; otherwise, it is treated as a normal data file.
## File Organization & Naming Rules

### File Categories
The converter categorizes Excel files based on filename prefixes:

- **Constant Files**: `const_*.xlsx` - Contains constant definitions
- **Enum Files**: `enum_*.xlsx` - Contains enumeration definitions  
- **Data Files**: `*.xlsx` (no prefix) - Contains game data tables

### Important Note on File Names
**File names are not significant for data generation.** The actual data structure is determined by **sheet names** within the Excel files. You can organize files however you prefer - the converter will merge data from multiple files based on sheet names during processing.

## Rules of Data Files
For example:
- `items_weapons.xlsx` with sheet "Item"
- `items_armor.xlsx` with sheet "Item" 
- `items_consumables.xlsx` with sheet "Item"

All three will be merged into a single "Item" data structure.

## Excel Sheet Structure

### Basic Data Sheet Layout
![screenshot](image/1.png)
If the schema to be defined inherits another table, you must define 'based' and 'json' in the first and second lines. 'based' is the name of the parent table and 'json' is the name of the json file to be stored. If you do not inherit, skip this.

### Schema
Three values are required for each column to define the default schema: the first is the name of the field, the second is the type, and the third is the scope for which the field will be used.
The second field, Type, supports the following formats:
- byte, uint8, uint8_t
- sbyte, int8, int8_t
- short, int16, int16_t
- ushort, uint16, uint16_t
- bool
- int, int32, int32_t
- uint, uint32, uint32_t
- long, int64, int64_t
- ulong, uint64, uint64_t
- double
- float
- string
- dsl
- TimeSpan
- DateTime
- DateRange
- point<T>
- size<T>
- range<T>
- [T] (array)
- {K:V} (map)

If you add a '?' after the type, it becomes a nullable type.

Each data sheet follows this structure:
1. **Row 1**: Inheritance declaration (optional)
2. **Row 2**: JSON output filename (optional)
3. **Row 3**: Field names
4. **Row 4**: Data types
5. **Row 5**: Scope definitions
6. **Row 6+**: Actual data

### Inheritance Support
For tables that inherit from other tables:
```
Row 1: based=ParentTableName
Row 2: json=output_filename.json
Row 3: field1    field2    field3    ...
Row 4: int       string    float     ...
Row 5: server    common    client    ...
```

If no inheritance is needed, skip rows 1-2 and start directly with field definitions.

## Supported Data Types

### Primitive Types
- **Integers**: `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`
- **Floating Point**: `float`, `double`
- **Boolean**: `bool`
- **Text**: `string`
- **Special**: `dsl` (Domain Specific Language references)

### Date/Time Types
- `DateTime` - Specific date and time
- `TimeSpan` - Duration or time interval
- `DateRange` - Range between two dates

### Generic Types
- `point<T>` - 2D coordinate (x, y)
- `size<T>` - Dimensions (width, height)  
- `range<T>` - Value range (min, max)

### Collection Types
- `[T]` - Array of type T
- `{K:V}` - Dictionary/Map with key type K and value type V

### Nullable Types
Add `?` after any type to make it nullable:
- `int?` - Nullable integer
- `string?` - Nullable string
- `[int]?` - Nullable array of integers

## Scope Definitions

Define data visibility for different build targets:

- **`server`**: Data only available in server builds
- **`client`**: Data only available in client builds  
- **`common`**: Data available in common server and client builds

## Data Processing Pipeline

### 1. File Discovery & Categorization
- Scans directory for `*.xlsx` files
- Categorizes by filename prefix (const, enum, data)
- Calculates CRC checksums for change detection

### 2. Incremental Processing
- Only processes files that have changed since last run
- Maintains cache for unchanged files
- Tracks error files for reprocessing

### 3. Data Loading & Merging
- Loads Excel workbooks and sheets
- **Merges sheets with identical names across multiple files**
- Processes constants, enums, and data tables separately

### 4. Validation Pipeline
- **Name Validation**: Ensures consistent naming conventions
- **Schema Validation**: Verifies column definitions and types
- **Key Validation**: Checks primary keys and uniqueness
- **Enum Validation**: Validates enumeration references
- **DSL Validation**: Verifies domain-specific language usage
- **Relation Validation**: Checks foreign key relationships
- **Type Validation**: Ensures data conforms to defined types

### 5. Code Generation
- Generates JSON data files for runtime loading
- Creates strongly-typed classes for each target language
- Produces CRC files for data integrity verification

## Inheritance & Polymorphism

### Defining Inheritance
In your Excel sheet, specify inheritance in the first two rows:
```
Row 1: based=BaseItem
Row 2: json=items.json
```

### Global Static Table Access
All generated code uses a global static `Table` class that provides direct access to all data tables. This eliminates the need for dependency injection and allows tables to be accessed from anywhere in your codebase.

### Code Integration Examples

#### C++ Example
In your C++ application, handle polymorphic loading with hooks:

```cpp
using table = fb::model::table;

// From main.cpp - Item inheritance example
table::item.hook.build = [](const Json::Value& json) -> fb::model::item* {
    auto type = fb::model::build<ITEM_TYPE>(json["type"]);
    switch (type)
    {
    case ITEM_TYPE::WEAPON:
        return fb::model::build<fb::model::weapon*>(json);
    case ITEM_TYPE::ARMOR:
        return fb::model::build<fb::model::armor*>(json);
    case ITEM_TYPE::CONSUME:
        return fb::model::build<fb::model::consume*>(json);
    case ITEM_TYPE::HELMET:
        return fb::model::build<fb::model::helmet*>(json);
    case ITEM_TYPE::RING:
        return fb::model::build<fb::model::ring*>(json);
    case ITEM_TYPE::SHIELD:
        return fb::model::build<fb::model::shield*>(json);
    default:
        return fb::model::build<fb::model::item*>(json);
    }
};

// Load all tables
table::foreach([](fb::model::container& container) {
    container.load();
});

// Access data
auto item = table::item[123];
```

#### C# Example
In your C# application, use the static `Table` class:

```csharp
using Fb.Model;

// Set up Item inheritance hook
Table.Item.Hook.Build = token => {
    var type = token["type"].ToObject<ItemType>();
    switch (type)
    {
        case ItemType.Weapon:
            return token.ToObject<Weapon>();
        case ItemType.Armor:
            return token.ToObject<Armor>();
        case ItemType.Consume:
            return token.ToObject<Consume>();
        // ... other types
        default:
            return token.ToObject<Item>();
    }
};

// Load all tables
foreach (var container in Table.Containers)
{
    container.Load();
}

// Access data
var item = Table.Item[123];
```

#### Go Example
In your Go application, use global table variables:

```go
// Set up Item inheritance hook
model.ItemHook = func(item *model.Item, data json.RawMessage) (model.ItemInterface, error) {
    switch item.Type {
    case model.ITEM_TYPE_WEAPON:
        weapon, err := model.NewWeaponBuilder(nil).Build(data)
        return &weapon, err
    case model.ITEM_TYPE_ARMOR:
        armor, err := model.NewArmorBuilder(nil).Build(data)
        return &armor, err
    // ... other types
    default:
        return item, nil
    }
}

// Load all tables
model.LoadAll(func(percentage float64) {
    log.Printf("Loading: %.2f%%", percentage*100)
})

// Access data
item := model.Item[123]
```

#### Node.js Example
In your Node.js application, use the global `Table` object:

```javascript
const model = require('./model');

// Set up Item inheritance hook
model.Table.itemHook = (item, data) => {
    switch (item.type) {
        case model.enum.ITEM_TYPE.WEAPON:
            return model.WeaponBuilder().build(data);
        case model.enum.ITEM_TYPE.ARMOR:
            return model.ArmorBuilder().build(data);
        // ... other types
        default:
            return item;
    }
};

// Load all tables
await model.Table.load('./json');

// Access data
const item = model.Table.item[123];
```

These hooks allow the model loader to instantiate the correct derived class based on the item type field. All tables are accessible globally through the `Table` class/object without requiring dependency injection.

## Configuration

### DSL Configuration (`dsl.json`)
Defines domain-specific language rules and custom type mappings.

### Command Line Configuration
All configuration options are now available via command line arguments. The tool no longer uses a separate `config.json` file. All settings can be specified using the command line options listed above.

## Output Structure

### Generated Files
```
output/
├── json/
│   ├── server/
│   │   ├── items.json
│   │   ├── npcs.json
│   │   └── Crc.txt
│   └── client/
│       ├── items.json
│       ├── npcs.json
│       └── Crc.txt
├── cpp/
│   ├── item.h
│   ├── npc.h
│   └── ...
├── cs/
│   ├── Item.cs
│   ├── Npc.cs
│   └── ...
└── diff/
    └── changes.txt
```

### CRC Integrity Files
Each scope generates a `Crc.txt` file containing checksums of all JSON files for data integrity verification at runtime.

## Error Handling & Debugging

### Error Tracking
- Failed files are tracked and automatically reprocessed on next run
- Detailed error messages with file and sheet context
- Performance metrics saved to `ElapsedTime.txt`

### Common Issues
1. **Missing Parent Table**: Ensure base tables are defined before derived tables
2. **Type Mismatch**: Verify data conforms to declared column types
3. **Invalid References**: Check that enum and foreign key references exist
4. **Scope Conflicts**: Ensure scope definitions are consistent across related tables

## Best Practices

### File Organization
- Group related data logically (e.g., `items_weapons.xlsx`, `items_armor.xlsx`)
- Use consistent naming conventions for sheets across files
- Keep enum and constant definitions in separate files

### Schema Design
- Define base classes for common properties
- Use appropriate scopes to minimize client data size
- Leverage nullable types for optional fields
- Document complex relationships in sheet comments

### Performance Optimization
- The converter uses intelligent caching - only changed files are reprocessed
- Large datasets are processed in parallel where possible
- Use incremental builds during development

## Troubleshooting

### Build Failures
1. Check console output for specific validation errors
2. Review `ElapsedTime.txt` for performance bottlenecks
3. Verify Excel files are not locked by other applications
4. Ensure all referenced parent tables and enums exist

### Data Inconsistencies
1. Verify CRC files match between builds
2. Check that inheritance hierarchies are properly defined
3. Ensure scope definitions are consistent across related data
4. Validate that all required fields are populated

This tool provides a robust foundation for managing game data with strong typing, validation, and multi-language support, enabling efficient development workflows and reliable data integrity.