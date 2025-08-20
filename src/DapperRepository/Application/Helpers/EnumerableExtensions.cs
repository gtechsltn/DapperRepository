using DapperRepository.Application.Dtos;

namespace DapperRepository.Application.Helpers;

public static class EnumerableExtensions
{
    public static IReadOnlyList<T> ToReadOnlyList<T>(this IEnumerable<T> source)
    {
        return source as IReadOnlyList<T> ?? source.ToList();
    }

    public static PagedResult<T> ToPagedResult<T>(this IEnumerable<T> source, int page = 1, int pageSize = 20, int totalCount = 0)
    {
        return new PagedResult<T>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = source.ToReadOnlyList()
        };
    }
}