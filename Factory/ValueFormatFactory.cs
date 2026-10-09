using ExcelTableConverter.Model;

namespace ExcelTableConverter.Factory
{
    // Formats a cast value; each kind receives the value class its DataType casts to.
    public abstract class ValueFormatFactory<T>
    {
        protected Context Context { get; }

        protected ValueFormatFactory(Context ctx)
        {
            Context = ctx;
        }

        public T Build(string type, DataValue value, IExcelFileTrackable tracker = null)
        {
            return Build(Context.Type(type, tracker), value, tracker);
        }

        public T Build(DataType dataType, DataValue value, IExcelFileTrackable tracker = null)
        {
            if (value is NullValue)
                return Null(dataType, tracker);

            return dataType.Kind switch
            {
                DataKind.Byte => ByteType(dataType, (UnsignedValue)value, tracker),
                DataKind.Sbyte => SbyteType(dataType, (IntegerValue)value, tracker),
                DataKind.Short => ShortType(dataType, (IntegerValue)value, tracker),
                DataKind.Ushort => UshortType(dataType, (UnsignedValue)value, tracker),
                DataKind.Bool => BoolType(dataType, (BoolValue)value, tracker),
                DataKind.Int => IntType(dataType, (IntegerValue)value, tracker),
                DataKind.Uint => UintType(dataType, (UnsignedValue)value, tracker),
                DataKind.Long => LongType(dataType, (IntegerValue)value, tracker),
                DataKind.Ulong => UlongType(dataType, (UnsignedValue)value, tracker),
                DataKind.Double => DoubleType(dataType, (RealValue)value, tracker),
                DataKind.Float => FloatType(dataType, (RealValue)value, tracker),
                DataKind.String => StringType(dataType, (StringValue)value, tracker),
                DataKind.Dsl => DslType(dataType, (DslValue)value, tracker),
                DataKind.TimeSpan => TimeSpanType(dataType, (TimeSpanValue)value, tracker),
                DataKind.DateTime => DateTimeType(dataType, (DateTimeValue)value, tracker),
                DataKind.DateRange => DateRangeType(dataType, (DateRangeValue)value, tracker),
                DataKind.Array => ArrayType((ArrayDataType)dataType, (ArrayValue)value, tracker),
                DataKind.Map => MapType((MapDataType)dataType, (MapValue)value, tracker),
                DataKind.Point => PointType((GeometryDataType)dataType, (PointValue)value, tracker),
                DataKind.Size => SizeType((GeometryDataType)dataType, (SizeValue)value, tracker),
                DataKind.Range => RangeType((GeometryDataType)dataType, (RangeValue)value, tracker),
                DataKind.Area => AreaType((GeometryDataType)dataType, (AreaValue)value, tracker),
                DataKind.Enum => EnumType((EnumDataType)dataType, (EnumValue)value, tracker),
                _ => throw new ArgumentOutOfRangeException(nameof(dataType), dataType.Kind, null),
            };
        }

        protected abstract T Null(DataType type, IExcelFileTrackable tracker);
        protected abstract T ByteType(DataType type, UnsignedValue value, IExcelFileTrackable tracker);
        protected abstract T SbyteType(DataType type, IntegerValue value, IExcelFileTrackable tracker);
        protected abstract T ShortType(DataType type, IntegerValue value, IExcelFileTrackable tracker);
        protected abstract T UshortType(DataType type, UnsignedValue value, IExcelFileTrackable tracker);
        protected abstract T BoolType(DataType type, BoolValue value, IExcelFileTrackable tracker);
        protected abstract T IntType(DataType type, IntegerValue value, IExcelFileTrackable tracker);
        protected abstract T UintType(DataType type, UnsignedValue value, IExcelFileTrackable tracker);
        protected abstract T LongType(DataType type, IntegerValue value, IExcelFileTrackable tracker);
        protected abstract T UlongType(DataType type, UnsignedValue value, IExcelFileTrackable tracker);
        protected abstract T DoubleType(DataType type, RealValue value, IExcelFileTrackable tracker);
        protected abstract T FloatType(DataType type, RealValue value, IExcelFileTrackable tracker);
        protected abstract T StringType(DataType type, StringValue value, IExcelFileTrackable tracker);
        protected abstract T DslType(DataType type, DslValue value, IExcelFileTrackable tracker);
        protected abstract T TimeSpanType(DataType type, TimeSpanValue value, IExcelFileTrackable tracker);
        protected abstract T DateTimeType(DataType type, DateTimeValue value, IExcelFileTrackable tracker);
        protected abstract T DateRangeType(DataType type, DateRangeValue value, IExcelFileTrackable tracker);
        protected abstract T ArrayType(ArrayDataType type, ArrayValue value, IExcelFileTrackable tracker);
        protected abstract T MapType(MapDataType type, MapValue value, IExcelFileTrackable tracker);
        protected abstract T PointType(GeometryDataType type, PointValue value, IExcelFileTrackable tracker);
        protected abstract T SizeType(GeometryDataType type, SizeValue value, IExcelFileTrackable tracker);
        protected abstract T RangeType(GeometryDataType type, RangeValue value, IExcelFileTrackable tracker);
        protected abstract T AreaType(GeometryDataType type, AreaValue value, IExcelFileTrackable tracker);
        protected abstract T EnumType(EnumDataType type, EnumValue value, IExcelFileTrackable tracker);
    }
}
