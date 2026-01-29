namespace MV.DomainLayer.DTOs.ResponseModels
{
    public class PagedResponse<T>
    {
        public List<T> Items { get; set; }
        public PaginationMetadata Pagination { get; set; }

        public PagedResponse(List<T> items, int pageNumber, int pageSize, int totalRecords)
        {
            Items = items;
            Pagination = new PaginationMetadata
            {
                CurrentPage = pageNumber,
                PageSize = pageSize,
                TotalItems = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize)
            };
        }
    }

    public class PaginationMetadata
    {
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public bool HasNext => CurrentPage < TotalPages;
        public bool HasPrevious => CurrentPage > 1;
    }
}