using DapperRepository.Application.Constants;

namespace DapperRepository.Application.Helpers;

public static class PagingHelper
{
    public static (int Page, int PageSize) Sanitize(int page, int pageSize, int defaultPage = 1, int defaultPageSize = 20)
    {
        if (page <= 0) page = PaginationDefaults.DefaultPage;
        if (pageSize <= 0) pageSize = PaginationDefaults.DefaultPageSize;

        if (pageSize > PaginationDefaults.MaxPageSize) pageSize = PaginationDefaults.MaxPageSize;

        return (page, pageSize);
    }
}