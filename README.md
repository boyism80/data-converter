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
                    --dsl=<path-to-dsl-config>
                    --env=<environment>
```

### Examples
```bash
# Generate C++ code only
ExcelTableConverter --dir="../data" --lang="c++"

# Generate multiple languages
ExcelTableConverter --dir="../data" --lang="c++|c#|go"

# Specify custom DSL configuration
ExcelTableConverter --dir="../data" --lang="c++" --dsl="custom-dsl.json"

# Set environment for conditional processing
ExcelTableConverter --dir="../data" --lang="c++" --env="production"
```

## File Organization & Naming Rules

### File Categories
The converter categorizes Excel files based on filename prefixes:

- **Constant Files**: `const_*.xlsx` - Contains constant definitions
- **Enum Files**: `enum_*.xlsx` - Contains enumeration definitions  
- **Data Files**: `*.xlsx` (no prefix) - Contains game data tables

### Important Note on File Names
**File names are not significant for data generation.** The actual data structure is determined by **sheet names** within the Excel files. You can organize files however you prefer - the converter will merge data from multiple files based on sheet names during processing.

For example:
- `items_weapons.xlsx` with sheet "Item"
- `items_armor.xlsx` with sheet "Item" 
- `items_consumables.xlsx` with sheet "Item"

All three will be merged into a single "Item" data structure.

## Excel Sheet Structure

### Basic Data Sheet Layout
![screenshot](image/1.png)

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
Row 5: server    both      client    ...
```

If no inheritance is needed, skip rows 1-2 and start directly with field definitions.

## Key System & Data Format

The converter determines the output data format based on how keys are defined in your schema. The key configuration affects whether data is generated as arrays, objects, or nested structures.

### Key Types & Formatting Rules

#### 1. **Primary Keys (*type)**
Fields with `*type` format in the type row (Row 4) are treated as primary keys.

```
Row 3: id        name     description
Row 4: *int      string   string
Row 5: both      both     both
```
- **Single Primary Key**: Generates object format `{ "1": {...}, "2": {...} }`

#### 2. **Multiple Primary Keys (Bold + Regular)**
**Special Case**: You can have multiple primary keys by combining one **bold field** with one regular primary key.

```
Row 3: category  id        name     description
Row 4: *string   *int      string   string
Row 5: both      both      both     both
```
Where `category` field name is **bold** in Excel (Row 3).

- **Bold Primary Key**: Acts as parent/group identifier
- **Regular Primary Key**: Acts as child identifier within the group
- Creates nested structure: `{ "weapons": { "1": {...}, "2": {...} }, "armor": { "1": {...} } }`
- **Important**: Bold field creates a separate parent table for hierarchical data organization

#### 3. **Group Keys ((type))**
Fields with `(type)` format in the type row (Row 4) are treated as group keys.

```
Row 3: level     id       name     description
Row 4: (int)     *int     string   string
Row 5: both      both     both     both
```
- Groups data by the group field value
- Generates: `{ "1": [{...}, {...}], "2": [{...}] }`

#### 4. **Reference Types ($Table or $Table.field)**
Fields with `$` prefix reference other tables or specific fields.

```
Row 3: item_id   category_id    name     description
Row 4: *int      $Category      string   string
Row 5: both      both           both     both
```
- `$Table`: References the primary key of another table
- `$Table.field`: References a specific field of another table
- Used for foreign key relationships and data validation

#### 5. **Regular Fields**
Fields without special prefixes are treated as data fields only.

```
Row 3: id        name     description  
Row 4: int       string   string
Row 5: both      both     both
```
- **No Keys**: Generates array format `[{...}, {...}, {...}]`

### Data Format Examples

#### Array Format (No Keys)
```excel
Row 3: id        name        type
Row 4: int       string      string
Row 5: both      both        both
```
**Output JSON:**
```json
[
  { "id": 1, "name": "Sword", "type": "weapon" },
  { "id": 2, "name": "Shield", "type": "armor" }
]
```

#### Object Format (Single Primary Key)
```excel
Row 3: id        name        type
Row 4: *int      string      string  
Row 5: both      both        both
```
**Output JSON:**
```json
{
  "1": { "id": 1, "name": "Sword", "type": "weapon" },
  "2": { "id": 2, "name": "Shield", "type": "armor" }
}
```

#### Nested Object Format (Bold + Regular Primary Keys)
```excel
Row 3: **category**  id        name        description
Row 4: *string       *int      string      string
Row 5: both          both      both        both
```
Where `**category**` indicates the field name is **bold** in Excel.

**Output JSON (Two separate tables):**

**Parent Table (Category):**
```json
{
  "weapon": { "category": "weapon" },
  "armor": { "category": "armor" }
}
```

**Child Table (Items):**
```json
{
  "1": { "parent": "weapon", "id": 1, "name": "Sword", "description": "Sharp blade" },
  "2": { "parent": "weapon", "id": 2, "name": "Bow", "description": "Ranged weapon" },
  "3": { "parent": "armor", "id": 3, "name": "Shield", "description": "Protective gear" }
}
```
- Bold fields create hierarchical parent-child relationships
- Parent table contains only the bold field data
- Child table references parent via the `parent` property

#### Group Key Format
```excel
Row 3: level     id        name        exp_required
Row 4: (int)     *int      string      int
Row 5: both      both      both        both
```
**Output JSON:**
```json
{
  "1": [
    { "level": 1, "id": 1, "name": "Beginner Sword", "exp_required": 0 },
    { "level": 1, "id": 2, "name": "Beginner Shield", "exp_required": 0 }
  ],
  "2": [
    { "level": 2, "id": 3, "name": "Iron Sword", "exp_required": 100 }
  ]
}
```

#### Reference Type Format
```excel
Row 3: id        category_id    name        damage
Row 4: *int      $Category      string      int
Row 5: both      both           both        both
```
**Output JSON:**
```json
{
  "1": { "id": 1, "category_id": "weapon", "name": "Sword", "damage": 50 },
  "2": { "id": 2, "category_id": "armor", "name": "Shield", "damage": 0 }
}
```
- The `category_id` field references the primary key of the `Category` table
- Validation ensures all referenced values exist in the target table

### Key Configuration Best Practices

1. **Use Single Primary Key (`*type`)** for simple lookups (items, NPCs, skills)
2. **Use Multiple Primary Keys** for hierarchical data (items by category and ID)
3. **Use Group Keys (`(type)`)** for level-based or time-based data organization
4. **Use Reference Types (`$Table`)** for foreign key relationships
5. **Use Array Format** for sequential data or when order matters
6. **Combine with Inheritance** for polymorphic data structures

### Key Validation Rules

- Primary keys (`*type`) must be unique within their scope
- Group keys (`(type)`) can have duplicate values (used for grouping)
- Reference types (`$Table`) must point to existing tables and valid keys
- Empty key values are not allowed for primary keys
- Key fields cannot be nullable types unless explicitly marked (`*int?`)
- Strong types (`!type`) enforce stricter validation rules
- Sequence types (`~type`) maintain insertion order

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

### Reference Types
- `$TableName` - References the primary key of another table
- `$TableName.fieldName` - References a specific field of another table
- Used for foreign key relationships and cross-table validation

### DSL (Domain Specific Language) Types
DSL types allow you to define complex, structured data with predefined schemas and validation rules.

#### Syntax
```
dsl_name(param1, param2, ...)
```

#### Usage in Game Logic
DSL types are commonly used for:
- **Conditions**: Game logic conditions (e.g., level requirements, item possession)
- **Actions**: Game actions (e.g., teleportation, script execution)
- **Complex Data**: Multi-parameter structured data

#### Example from Game Code
```cpp
// From context.handler.cpp - Warp condition checking
if (ch->condition(warp->condition) == false)
{
    ch->message("감히 접근할 수 없습니다.");
    return;
}

// DSL destination handling
switch (warp->dest.header)
{
case DSL::map:
    auto params = fb::model::dsl::map(warp->dest.params);
    auto& map = this->maps[params.id];
    ch->map(&map, fb::model::point16_t(params.x, params.y));
    break;
    
case DSL::script:
    auto params = fb::model::dsl::script(warp->dest.params);
    lua->load(params.path);
    lua->func(params.function);
    break;
}
```

#### DSL Definition (dsl.json)
First, define your DSL types in `dsl.json`:
```json
{
  "level": [
    {
      "name": "min",
      "type": "uint8_t?",
      "desc": "minimum level"
    },
    {
      "name": "max", 
      "type": "uint8_t?",
      "desc": "maximum level",
      "default": null
    }
  ],
  "map": [
    {
      "name": "id",
      "type": "$map",
      "desc": "map id"
    },
    {
      "name": "x",
      "type": "uint16_t",
      "desc": "x coordinate",
      "default": 0
    },
    {
      "name": "y", 
      "type": "uint16_t",
      "desc": "y coordinate",
      "default": 0
    }
  ]
}
```

#### Excel Usage Example
```excel
Row 3: id        condition        destination
Row 4: *int      dsl              dsl
Row 5: both      both             both

Data:
632      level(min:5)             map(id:2096,x:12,y:18)
633      level(min:10,max:50)     map(id:1002,x:100,y:200)
```

#### JSON Output
```json
{
  "632": {
    "id": 632,
    "condition": [
      {
        "Type": "level",
        "Parameters": [5, null]
      }
    ],
    "destination": {
      "Type": "map", 
      "Parameters": [2096, 12, 18, 0, 0]
    }
  }
}
```

#### DSL Parameter Mapping
- **Named Parameters**: `level(min:5,max:10)` → Uses parameter names from dsl.json
- **Positional Parameters**: `map(2096,12,18)` → Maps to parameters in definition order
- **Default Values**: Missing parameters use defaults from dsl.json definition
- **Output Format**: `Type` (DSL name) + `Parameters` (array of values in definition order)


### Special Type Modifiers
- `*type` - Primary key (e.g., `*int`, `*string`)
- `(type)` - Group key (e.g., `(int)`, `(string)`)
- `!type` - Strong type validation
- `~type` - Sequence type for ordered data

### Nullable Types
Add `?` after any type to make it nullable:
- `int?` - Nullable integer
- `string?` - Nullable string
- `[int]?` - Nullable array of integers
- `$Item?` - Nullable reference to Item table

## Scope Definitions

Define data visibility for different build targets:

- **`server`**: Data only available in server builds
- **`client`**: Data only available in client builds  
- **`both`**: Data available in both server and client builds

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

### Code Integration Example
In your C++ application, handle polymorphic loading with hooks:

```cpp
// From main.cpp - Item inheritance example
context->model.item.hook.build = [](const Json::Value& json) -> fb::model::item* {
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
    case ITEM_TYPE::BOW:
        return fb::model::build<fb::model::bow*>(json);
    default:
        return fb::model::build<fb::model::item*>(json);
    }
};
```

This hook allows the model loader to instantiate the correct derived class based on the item type field.

## Configuration Files

### DSL Configuration (`dsl.json`)
Defines domain-specific language rules and custom type mappings.

### Environment Configuration (`config.json`)
Contains environment-specific settings and build options.

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

