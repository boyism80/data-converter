using ExcelTableConverter.Model;

namespace ExcelTableConverter.Factory
{
    public abstract class TypeFormatFactory<T>
    {
        protected Context Context { get; }

        protected TypeFormatFactory(Context ctx)
        {
            Context = ctx;
        }

        public T Build(string type, IExcelFileTrackable tracker = null)
        {
            return Build(Context.Type(type, tracker));
        }

        public T Build(DataType type)
        {
            return type.Kind switch
            {
                DataKind.Byte => ByteType(type),
                DataKind.Sbyte => SbyteType(type),
                DataKind.Short => ShortType(type),
                DataKind.Ushort => UshortType(type),
                DataKind.Bool => BoolType(type),
                DataKind.Int => IntType(type),
                DataKind.Uint => UintType(type),
                DataKind.Long => LongType(type),
                DataKind.Ulong => UlongType(type),
                DataKind.Double => DoubleType(type),
                DataKind.Float => FloatType(type),
                DataKind.String => StringType(type),
                DataKind.Dsl => DslType(type),
                DataKind.TimeSpan => TimeSpanType(type),
                DataKind.DateTime => DateTimeType(type),
                DataKind.DateRange => DateRangeType(type),
                DataKind.Array => ArrayType((ArrayDataType)type),
                DataKind.Map => MapType((MapDataType)type),
                DataKind.Point => PointType((GeometryDataType)type),
                DataKind.Size => SizeType((GeometryDataType)type),
                DataKind.Range => RangeType((GeometryDataType)type),
                DataKind.Area => AreaType((GeometryDataType)type),
                DataKind.Enum => EnumType((EnumDataType)type),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type.Kind, null),
            };
        }

        protected abstract T ByteType(DataType type);
        protected abstract T SbyteType(DataType type);
        protected abstract T ShortType(DataType type);
        protected abstract T UshortType(DataType type);
        protected abstract T BoolType(DataType type);
        protected abstract T IntType(DataType type);
        protected abstract T UintType(DataType type);
        protected abstract T LongType(DataType type);
        protected abstract T UlongType(DataType type);
        protected abstract T DoubleType(DataType type);
        protected abstract T FloatType(DataType type);
        protected abstract T StringType(DataType type);
        protected abstract T DslType(DataType type);
        protected abstract T TimeSpanType(DataType type);
        protected abstract T DateTimeType(DataType type);
        protected abstract T DateRangeType(DataType type);
        protected abstract T ArrayType(ArrayDataType type);
        protected abstract T MapType(MapDataType type);
        protected abstract T PointType(GeometryDataType type);
        protected abstract T SizeType(GeometryDataType type);
        protected abstract T RangeType(GeometryDataType type);
        protected abstract T AreaType(GeometryDataType type);
        protected abstract T EnumType(EnumDataType type);
    }
}
