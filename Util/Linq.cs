namespace System.Linq
{
    public static class LinqEx
    {
        public static T ElementAtOrDefault<T>(this IList<T> list, int index, T @default)
        {
            return index >= 0 && index < list.Count ? list[index] : @default;
        }
    }
}
