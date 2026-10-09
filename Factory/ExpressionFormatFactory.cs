using ExcelTableConverter.Model;

namespace ExcelTableConverter.Factory
{
    // Formats an expression that reads or converts a named value (a field, a json member, a parameter).
    public abstract class ExpressionFormatFactory<T>
    {
        protected Context Context { get; }

        protected ExpressionFormatFactory(Context ctx)
        {
            Context = ctx;
        }

        public T Build(string type, string expression, IExcelFileTrackable tracker = null)
        {
            return Build(Context.Type(type, tracker), expression);
        }

        public T Build(DataType type, string expression)
        {
            return type.Kind switch
            {
                DataKind.Byte => ByteType(type, expression),
                DataKind.Sbyte => SbyteType(type, expression),
                DataKind.Short => ShortType(type, expression),
                DataKind.Ushort => UshortType(type, expression),
                DataKind.Bool => BoolType(type, expression),
                DataKind.Int => IntType(type, expression),
                DataKind.Uint => UintType(type, expression),
                DataKind.Long => LongType(type, expression),
                DataKind.Ulong => UlongType(type, expression),
                DataKind.Double => DoubleType(type, expression),
                DataKind.Float => FloatType(type, expression),
                DataKind.String => StringType(type, expression),
                DataKind.Dsl => DslType(type, expression),
                DataKind.TimeSpan => TimeSpanType(type, expression),
                DataKind.DateTime => DateTimeType(type, expression),
                DataKind.DateRange => DateRangeType(type, expression),
                DataKind.Array => ArrayType((ArrayDataType)type, expression),
                DataKind.Map => MapType((MapDataType)type, expression),
                DataKind.Point => PointType((GeometryDataType)type, expression),
                DataKind.Size => SizeType((GeometryDataType)type, expression),
                DataKind.Range => RangeType((GeometryDataType)type, expression),
                DataKind.Area => AreaType((GeometryDataType)type, expression),
                DataKind.Enum => EnumType((EnumDataType)type, expression),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type.Kind, null),
            };
        }

        protected abstract T ByteType(DataType type, string expression);
        protected abstract T SbyteType(DataType type, string expression);
        protected abstract T ShortType(DataType type, string expression);
        protected abstract T UshortType(DataType type, string expression);
        protected abstract T BoolType(DataType type, string expression);
        protected abstract T IntType(DataType type, string expression);
        protected abstract T UintType(DataType type, string expression);
        protected abstract T LongType(DataType type, string expression);
        protected abstract T UlongType(DataType type, string expression);
        protected abstract T DoubleType(DataType type, string expression);
        protected abstract T FloatType(DataType type, string expression);
        protected abstract T StringType(DataType type, string expression);
        protected abstract T DslType(DataType type, string expression);
        protected abstract T TimeSpanType(DataType type, string expression);
        protected abstract T DateTimeType(DataType type, string expression);
        protected abstract T DateRangeType(DataType type, string expression);
        protected abstract T ArrayType(ArrayDataType type, string expression);
        protected abstract T MapType(MapDataType type, string expression);
        protected abstract T PointType(GeometryDataType type, string expression);
        protected abstract T SizeType(GeometryDataType type, string expression);
        protected abstract T RangeType(GeometryDataType type, string expression);
        protected abstract T AreaType(GeometryDataType type, string expression);
        protected abstract T EnumType(EnumDataType type, string expression);
    }
}
