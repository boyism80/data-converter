namespace ExcelTableConverter.Model
{
    // Local holds the segments this namespace declares inside Parent.
    public sealed class Namespace
    {
        public Namespace Parent { get; }
        public IReadOnlyList<string> Local { get; }
        public IEnumerable<string> Segments => (Parent?.Segments ?? []).Concat(Local);

        public Namespace(IEnumerable<string> local, Namespace parent = null)
        {
            Local = local.ToList();
            Parent = parent;
        }
    }
}
