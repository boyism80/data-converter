# data-converter
```data-converter``` convert excel files to json files and source files for declareation and load json files.


## Get started

| Argument | Description |
| --- | --- |
| `--dir` | directory contains excel files |
| `--lang` | language of source files. `c++` and `c#` support.<br>If you can use both, try : `c++\|c#` |


## Schema
The 3 rows used to declare schema. 1st line you set field name, and 2nd line is type. and 3rd is scope that support `common`, `server`, `client`. 
| value1 | value2 | value3 |
| --- | --- | --- |
| uint32_t | string | [uint8_t]
| server | client | common |

### Key
You can set a key field using `*` keyword.
| value1 | value2 | value3 |
| --- | --- | --- |
| *uint32_t | string | [uint8_t]
| server | client | common |

And this data structure is key-value format.

### Reference
You can reference other table key.

sheet name : sample1
| value1 | value2 | value3 |
| --- | --- | --- |
| *uint32_t | string | [uint8_t]
| common | common | common |
| 1| hello | 0 |
| 2| world | 255 |

sheet name : sample2
| value1 | value2 |
| --- | --- |
| *$sample1 | [$sample1.value3]
| common | common |
| 1 | 0 & 255 |
| 2 | 255 |

### DSL
Declare dsl in dsl.json and use it.
```json
{
    "sample_dsl": [
        {
            "name": "id",
            "type": "$sample1",
            "desc": "id"
        },
        {
            "name": "count",
            "type": "uint",
            "desc": "count",
            "default": 1
        }
    ]
}
```
sheet name: sample3
| id | dsl_value |
| --- | --- |
| *uint32_t | dsl |
| common | common |
| 1| sample_dsl(1, 100) |
| 2| sample_dsl(2, 500) |

in source code : 
```c++
auto params = dsl::sample_dsl(model.sample3[1].dsl_value);
std::cout << params.count << std::endl;
```


## Excel file format
You can declare 4 ways format `Linear`, `PK`, `GK`, `PK-PK`.

### Linear format

Sheet name : `sample`
| value1 | value2 |
| --- | --- |
| uint32_t | string |
| common | common |
| 1 | hello |
| 2 | world |

```c++
for(auto& row : model.sample)
{
    std::cout << row.value1 << std::endl;
    std::cout << row.value2 << std::endl;
}
```

### PK format

Sheet name : `sample`
| value1 | value2 |
| --- | --- |
| *uint32_t | string |
| common | common |
| 1 | hello |
| 2 | world |
```C++
std::cout << model.sample[1].value2 << std::endl;
```


### GK format
Sheet name : `sample`

| value1 | value2 |
| --- | --- |
| (uint32_t) | string |
| common | common |
| 1 | hello-1 |
| 1 | world-1 |
| 2 | hello-2 |
| 2 | world-2 |
```C++
for(auto& [id, rows] : model.sample)
{
    std::cout << id << std::endl;
    for(auto& row : rows)
    {
        std::cout << row.value2 << std::endl;
    }
}
```


### PK-PK format
Use **bold schema** to set parent group. Here is a sample table file.
| **id** | level | dexteritry | intelligence | strength|
| --- | --- | --- | --- | --- |
| **\*CLASS** | *uint8_t | uint8_t | uint8_t | uint8_t|
| **server** | server | server | server | server|
| NONE |  |  |  | |
|  | 1  | 0  | 0  | 0 |
|  | 2  | 0  | 0  | 0 |
| WARRIOR |  |  |  | |
|  | 5  | 0  | 0  | 0 |
|  | 6  | 0  | 0  | 0 |

This file will convert to `Dictionary<CLASS, <Dictionary<uint8_t, Ability>>` and you can access this data in C#
```C#
var level = model.Ability[CLASS.NONE][1].level;
```
