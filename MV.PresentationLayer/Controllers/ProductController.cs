using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.Product.Request;
using MV.DomainLayer.DTOs.ProductBundle.Request;
using MV.DomainLayer.DTOs.RequestModels;
using Swashbuckle.AspNetCore.Annotations;

namespace MV.PresentationLayer.Controllers
{
    /// <summary>
    /// Quản lý sản phẩm STEM (Arduino, Raspberry Pi, sensors, modules...)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly IProductBundleService _bundleService;

        public ProductController(IProductService productService, IProductBundleService bundleService)
        {
            _productService = productService;
            _bundleService = bundleService;
        }

        /// <summary>
        /// Lấy danh sách sản phẩm với filter và phân trang
        /// </summary>
        /// <param name="filter">Bộ lọc: SearchTerm, BrandId, CategoryId, MinPrice, MaxPrice, PageNumber, PageSize</param>
        /// <returns>Danh sách sản phẩm với thông tin Brand, Categories, PrimaryImage</returns>
        /// <remarks>
        /// Sample request:
        /// 
        ///     GET /api/product?searchTerm=arduino&amp;brandId=1&amp;pageNumber=1&amp;pageSize=10
        ///     
        /// Public endpoint - Không cần authentication
        /// </remarks>
        [HttpGet]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Get products with filters and pagination")]
        public async Task<IActionResult> GetProducts([FromQuery] ProductFilter filter)
        {
            var result = await _productService.GetProductsAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết đầy đủ 1 sản phẩm
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <returns>Thông tin chi tiết: Brand, Categories, Images, WarrantyPolicy, Reviews, Stock</returns>
        /// <remarks>
        /// Sample request:
        /// 
        ///     GET /api/product/5
        ///     
        /// Trả về:
        /// - Thông tin sản phẩm đầy đủ
        /// - Tất cả ảnh sản phẩm
        /// - Danh sách categories
        /// - Chính sách bảo hành
        /// - Tổng kết reviews (rating, số lượng)
        /// </remarks>
        [HttpGet("{id}")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Get product details by ID")]
        public async Task<IActionResult> GetProductById(int id)
        {
            var result = await _productService.GetByIdAsync(id);

            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Tạo sản phẩm mới (Admin only)
        /// </summary>
        /// <param name="request">Thông tin sản phẩm: Name, SKU, Price, BrandId, CategoryIds...</param>
        /// <returns>Sản phẩm vừa tạo với đầy đủ thông tin</returns>
        /// <remarks>
        /// Sample request:
        /// 
        ///     POST /api/product
        ///     {
        ///       "name": "Arduino Uno R3",
        ///       "sku": "ARD-UNO-001",
        ///       "description": "Vi điều khiển Arduino phổ biến nhất",
        ///       "price": 250000,
        ///       "stockQuantity": 50,
        ///       "brandId": 1,
        ///       "warrantyPolicyId": 1,
        ///       "hasSerialTracking": true,
        ///       "categoryIds": [1, 5]
        ///     }
        ///     
        /// Validation:
        /// - SKU phải unique
        /// - Brand phải tồn tại
        /// - Categories phải tồn tại
        /// - Price &gt; 0
        /// - HasSerialTracking = true → StockQuantity = 0 (quản lý bằng serial)
        /// </remarks>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Create new product")]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _productService.CreateAsync(request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return CreatedAtAction(nameof(GetProductById), new { id = result.Data!.ProductId }, result);
        }

        /// <summary>
        /// Tạo sản phẩm KIT kèm components trong 1 transaction duy nhất (Admin only)
        /// </summary>
        /// <param name="request">Thông tin KIT + danh sách components</param>
        /// <returns>KIT product đã tạo với thông tin bundle</returns>
        /// <remarks>
        /// Sample request:
        /// 
        ///     POST /api/product/kit
        ///     {
        ///       "name": "Arduino Starter Kit - Complete",
        ///       "sku": "KIT-ARD-001",
        ///       "description": "Complete Arduino kit with sensors and components",
        ///       "price": 850000,
        ///       "brandId": 1,
        ///       "categoryIds": [1, 2],
        ///       "components": [
        ///         { "productId": 10, "quantity": 1 },
        ///         { "productId": 15, "quantity": 3 },
        ///         { "productId": 20, "quantity": 5 }
        ///       ]
        ///     }
        ///     
        /// Validation:
        /// - Brand phải tồn tại
        /// - Categories phải tồn tại
        /// - Tất cả components phải tồn tại
        /// - Components KHÔNG thể là KIT (chỉ MODULE hoặc COMPONENT)
        /// - Không được duplicate component trong danh sách
        /// 
        /// Behavior:
        /// - ProductType tự động set = KIT
        /// - StockQuantity tự động = 0 (dùng /bundle/available-stock để tính stock thực tế)
        /// - Tạo Product + ProductBundles trong cùng 1 transaction (atomic)
        /// - Nếu có lỗi bất kỳ → rollback toàn bộ
        /// </remarks>
        [HttpPost("kit")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Create KIT product with components in single atomic transaction")]
        public async Task<IActionResult> CreateKit([FromBody] CreateKitRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _productService.CreateKitAsync(request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return CreatedAtAction(nameof(GetProductById), new { id = result.Data!.ProductId }, result);
        }

        /// <summary>
        /// Cập nhật thông tin sản phẩm (Admin only)
        /// </summary>
        /// <param name="id">Product ID cần update</param>
        /// <param name="request">Thông tin mới (giống CreateProduct)</param>
        /// <returns>Sản phẩm sau khi update</returns>
        /// <remarks>
        /// Sample request:
        /// 
        ///     PUT /api/product/5
        ///     {
        ///       "name": "Arduino Uno R3 - Updated",
        ///       "sku": "ARD-UNO-001",
        ///       "price": 280000,
        ///       "stockQuantity": 45,
        ///       "brandId": 1,
        ///       "hasSerialTracking": true,
        ///       "categoryIds": [1, 5, 8]
        ///     }
        ///     
        /// Lưu ý:
        /// - SKU phải unique (trừ chính sản phẩm này)
        /// - Categories cũ sẽ bị xóa và thay bằng CategoryIds mới
        /// </remarks>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Update product")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _productService.UpdateAsync(id, request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Xóa sản phẩm (Admin only)
        /// </summary>
        /// <param name="id">Product ID cần xóa</param>
        /// <returns>Success hoặc Error message</returns>
        /// <remarks>
        /// Sample request:
        /// 
        ///     DELETE /api/product/5
        ///     
        /// Bảo vệ dữ liệu:
        /// - KHÔNG cho phép xóa nếu sản phẩm đã có trong Orders
        /// - Tự động xóa ảnh, categories liên quan
        /// </remarks>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Delete product")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var result = await _productService.DeleteAsync(id);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Thêm categories cho sản phẩm (Admin only)
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <param name="categoryIds">Mảng Category IDs cần thêm</param>
        /// <returns>Success message</returns>
        /// <remarks>
        /// Sample request:
        /// 
        ///     POST /api/product/5/categories
        ///     [1, 3, 5]
        ///     
        /// Lưu ý:
        /// - Chỉ thêm categories chưa có
        /// - Không trùng lặp
        /// </remarks>
        [HttpPost("{id}/categories")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Add categories to product")]
        public async Task<IActionResult> AddCategoriesToProduct(int id, [FromBody] List<int> categoryIds)
        {
            var result = await _productService.AddCategoriesToProductAsync(id, categoryIds);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Xóa 1 category khỏi sản phẩm (Admin only)
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <param name="categoryId">Category ID cần xóa</param>
        /// <returns>Success message</returns>
        /// <remarks>
        /// Sample request:
        /// 
        ///     DELETE /api/product/5/categories/3
        /// </remarks>
        [HttpDelete("{id}/categories/{categoryId}")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Remove category from product")]
        public async Task<IActionResult> RemoveCategoryFromProduct(int id, int categoryId)
        {
            var result = await _productService.RemoveCategoryFromProductAsync(id, categoryId);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Lấy tất cả categories với số lượng sản phẩm
        /// </summary>
        /// <returns>Danh sách categories + ProductCount</returns>
        /// <remarks>
        /// Sample request:
        /// 
        ///     GET /api/product/categories
        ///     
        /// Dùng để hiển thị filter categories trên trang shop
        /// </remarks>
        [HttpGet("categories")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Get all categories with product count")]
        public async Task<IActionResult> GetAllCategories()
        {
            var result = await _productService.GetAllCategoriesAsync();
            return Ok(result);
        }

        /// <summary>
        /// Lấy tất cả brands với số lượng sản phẩm
        /// </summary>
        /// <returns>Danh sách brands + ProductCount</returns>
        /// <remarks>
        /// Sample request:
        /// 
        ///     GET /api/product/brands
        ///     
        /// Dùng để hiển thị filter brands trên trang shop
        /// </remarks>
        [HttpGet("brands")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Get all brands with product count")]
        public async Task<IActionResult> GetAllBrands()
        {
            var result = await _productService.GetAllBrandsAsync();
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách components trong KIT (chỉ cho ProductType = KIT)
        /// </summary>
        /// <param name="kitId">KIT Product ID</param>
        /// <returns>Danh sách sản phẩm con và số lượng trong KIT</returns>
        /// <remarks>
        /// Sample request:
        /// 
        ///     GET /api/product/4/bundle
        ///     
        /// Trả về danh sách components của Arduino Starter Kit
        /// </remarks>
        [HttpGet("{kitId}/bundle")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Get KIT components")]
        public async Task<IActionResult> GetKitComponents(int kitId)
        {
            var result = await _bundleService.GetBundleComponentsAsync(kitId);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Thêm sản phẩm vào KIT (Admin only)
        /// </summary>
        /// <param name="kitId">KIT Product ID</param>
        /// <param name="request">Child product ID và quantity</param>
        /// <returns>Bundle item vừa tạo</returns>
        /// <remarks>
        /// Sample request:
        /// 
        ///     POST /api/product/4/bundle
        ///     {
        ///       "childProductId": 1,
        ///       "quantity": 1
        ///     }
        ///     
        /// Ví dụ: Thêm 1 Arduino Uno vào Arduino Starter Kit
        /// 
        /// Validation:
        /// - Parent product phải là KIT
        /// - Child product không được là KIT (không cho KIT lồng KIT)
        /// - Child product chưa có trong bundle
        /// </remarks>
        [HttpPost("{kitId}/bundle")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Add product to KIT")]
        public async Task<IActionResult> AddProductToKit(int kitId, [FromBody] AddProductToBundleRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _bundleService.AddProductToBundleAsync(kitId, request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Xóa sản phẩm khỏi KIT (Admin only)
        /// </summary>
        /// <param name="kitId">KIT Product ID</param>
        /// <param name="childProductId">Child Product ID cần xóa</param>
        /// <returns>Success message</returns>
        /// <remarks>
        /// Sample request:
        /// 
        ///     DELETE /api/product/4/bundle/1
        ///     
        /// Xóa Arduino Uno khỏi Arduino Starter Kit
        /// </remarks>
        [HttpDelete("{kitId}/bundle/{childProductId}")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Remove product from KIT")]
        public async Task<IActionResult> RemoveProductFromKit(int kitId, int childProductId)
        {
            var result = await _bundleService.RemoveProductFromBundleAsync(kitId, childProductId);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Tính số lượng KIT có thể làm từ stock hiện tại
        /// </summary>
        /// <param name="kitId">KIT Product ID</param>
        /// <returns>Số lượng KIT tối đa có thể làm</returns>
        /// <remarks>
        /// Sample request:
        /// 
        ///     GET /api/product/4/bundle/available-stock
        ///     
        /// Logic: Tính dựa trên component có stock ít nhất
        /// 
        /// Ví dụ KIT gồm:
        /// - 1x Arduino Uno (stock: 50)
        /// - 1x Breadboard (stock: 100)
        /// - 10x LED (stock: 500)
        /// 
        /// → Có thể làm 50 KITs (bị giới hạn bởi Arduino Uno)
        /// </remarks>
        [HttpGet("{kitId}/bundle/available-stock")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Get available KIT stock")]
        public async Task<IActionResult> GetAvailableKitStock(int kitId)
        {
            var result = await _bundleService.GetAvailableKitStockAsync(kitId);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}