namespace VALUM.Application.Common.Pagination
{
    public class CursorPagedResult<T>
    {
        public IReadOnlyList<T> Items { get; init; } = new List<T>();
        public string? NextCursor { get; init; }
        public bool HasNext { get; init; }
    }
}
