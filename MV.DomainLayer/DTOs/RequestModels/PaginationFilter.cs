namespace MV.DomainLayer.DTOs.RequestModels
{
    public class PaginationFilter
    {
        private const int MaxPageSize = 50; // Giới hạn tối đa 50 item/trang
        private int _pageSize = 10; // Mặc định 10 item/trang

        public int PageNumber { get; set; } = 1;

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
        }

        public PaginationFilter()
        {
            PageNumber = 1;
            PageSize = 10;
        }

        public PaginationFilter(int pageNumber, int pageSize)
        {
            PageNumber = pageNumber < 1 ? 1 : pageNumber;
            PageSize = pageSize > MaxPageSize ? MaxPageSize : pageSize;
        }
    }
}