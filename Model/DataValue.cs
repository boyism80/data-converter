using Newtonsoft.Json;
using System.Globalization;

namespace ExcelTableConverter.Model
{
    // A cast cell value. Write emits the JSON of the output files; ToString is the text used for
    // dictionary keys and for comparing values with each other.
    public abstract record DataValue
    {
        public abstract void Write(JsonWriter writer);

        // Output files only; the cache serializes values with type names instead.
        public sealed class Converter : JsonConverter<DataValue>
        {
            public override bool CanRead => false;

            public override void WriteJson(JsonWriter writer, DataValue value, JsonSerializer serializer) => value.Write(writer);

            public override DataValue ReadJson(JsonReader reader, Type objectType, DataValue existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                throw new NotSupportedException();
            }
        }
    }

    public sealed record NullValue : DataValue
    {
        public static readonly NullValue Instance = new();

        public override void Write(JsonWriter writer) => writer.WriteNull();
        public override string ToString() => string.Empty;
    }

    public sealed record IntegerValue(long Value) : DataValue
    {
        public override void Write(JsonWriter writer) => writer.WriteValue(Value);
        public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed record UnsignedValue(ulong Value) : DataValue
    {
        public override void Write(JsonWriter writer) => writer.WriteValue(Value);
        public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed record RealValue(double Value) : DataValue
    {
        public override void Write(JsonWriter writer) => writer.WriteValue(Value);
        public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed record BoolValue(bool Value) : DataValue
    {
        public override void Write(JsonWriter writer) => writer.WriteValue(Value);
        public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
    }

    public record StringValue(string Value) : DataValue
    {
        public override void Write(JsonWriter writer) => writer.WriteValue(Value);
        public override string ToString() => Value;
    }

    public sealed record TimeSpanValue(TimeSpan Value) : DataValue
    {
        public override void Write(JsonWriter writer) => writer.WriteValue(Value);
        public override string ToString() => Value.ToString();
    }

    public sealed record DateTimeValue(DateTime Value) : DataValue
    {
        public override void Write(JsonWriter writer) => writer.WriteValue(Value);
        public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
    }

    // Begin and End are DateTimeValue or NullValue.
    public sealed record DateRangeValue(DataValue Begin, DataValue End) : DataValue
    {
        public override void Write(JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("Begin");
            Begin.Write(writer);
            writer.WritePropertyName("End");
            End.Write(writer);
            writer.WriteEndObject();
        }

        public override string ToString() => $"{Begin}~{End}";
    }

    public sealed record ArrayValue(IReadOnlyList<DataValue> Items) : DataValue
    {
        public override void Write(JsonWriter writer)
        {
            writer.WriteStartArray();
            foreach (var item in Items)
                item.Write(writer);
            writer.WriteEndArray();
        }

        public override string ToString() => string.Join(", ", Items);
    }

    public sealed record MapEntry(DataValue Key, DataValue Value);

    public sealed record MapValue(IReadOnlyList<MapEntry> Entries) : DataValue
    {
        public override void Write(JsonWriter writer)
        {
            writer.WriteStartObject();
            foreach (var entry in Entries)
            {
                writer.WritePropertyName(entry.Key.ToString());
                entry.Value.Write(writer);
            }
            writer.WriteEndObject();
        }

        public override string ToString() => string.Join(", ", Entries.Select(x => $"{x.Key}:{x.Value}"));
    }

    // A base table row nested into its derived row by the flattened output.
    public sealed record ObjectValue(IReadOnlyDictionary<string, DataValue> Fields) : DataValue
    {
        public override void Write(JsonWriter writer)
        {
            writer.WriteStartObject();
            foreach (var (name, value) in Fields)
            {
                writer.WritePropertyName(name);
                value.Write(writer);
            }
            writer.WriteEndObject();
        }

        public override string ToString() => string.Join(", ", Fields.Select(x => $"{x.Key}:{x.Value}"));
    }

    public sealed record PointValue(ulong X, ulong Y) : DataValue
    {
        public override void Write(JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(X);
            writer.WritePropertyName("y");
            writer.WriteValue(Y);
            writer.WriteEndObject();
        }

        public override string ToString() => $"{X}, {Y}";
    }

    public sealed record SizeValue(ulong Width, ulong Height) : DataValue
    {
        public override void Write(JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("width");
            writer.WriteValue(Width);
            writer.WritePropertyName("height");
            writer.WriteValue(Height);
            writer.WriteEndObject();
        }

        public override string ToString() => $"{Width}, {Height}";
    }

    public sealed record RangeValue(ulong Min, ulong Max) : DataValue
    {
        public override void Write(JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("min");
            writer.WriteValue(Min);
            writer.WritePropertyName("max");
            writer.WriteValue(Max);
            writer.WriteEndObject();
        }

        public override string ToString() => $"{Min}~{Max}";
    }

    public sealed record AreaValue(ulong Left, ulong Top, ulong Right, ulong Bottom) : DataValue
    {
        public override void Write(JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("left");
            writer.WriteValue(Left);
            writer.WritePropertyName("top");
            writer.WriteValue(Top);
            writer.WritePropertyName("right");
            writer.WriteValue(Right);
            writer.WritePropertyName("bottom");
            writer.WriteValue(Bottom);
            writer.WriteEndObject();
        }

        public override string ToString() => $"{Left}, {Top}, {Right}, {Bottom}";
    }

    // A single member keeps its name; an expression of several members is stored as the computed number.
    public sealed record EnumValue(string Name, int? Number) : DataValue
    {
        public override void Write(JsonWriter writer)
        {
            if (Name != null)
                writer.WriteValue(Name);
            else
                writer.WriteValue(Number.Value);
        }

        public override string ToString() => Name ?? Number.Value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed record DslValue(string Header, IReadOnlyList<DataValue> Params) : DataValue
    {
        public override void Write(JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("Header");
            writer.WriteValue(Header);
            writer.WritePropertyName("Params");
            writer.WriteStartArray();
            foreach (var param in Params)
                param.Write(writer);
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        public override string ToString() => $"{Header}({string.Join(", ", Params)})";
    }
}
