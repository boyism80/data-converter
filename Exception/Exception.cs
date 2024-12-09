using ExcelTableConverter.Model;

namespace ExcelTableConverter
{
    public class LogicException : Exception
    {
        public IExcelFileTrackable Tracker { get; private set; }

        public LogicException(string message, IExcelFileTrackable tracker = null) : base(message)
        {
            Tracker = tracker;
        }
    }

    public class TypeCastException : LogicException
    {
        public TypeCastException(object value, string type) : base($"{value}는 {type} 형식으로 변환할 수 없습니다.")
        {

        }
    }

    public class NullValueException : LogicException
    {
        public NullValueException(string type) : base($"{type} 타입은 빈 값을 가질 수 없습니다.")
        {

        }
    }
}
