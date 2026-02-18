using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.ProductBundle.Request;
using MV.DomainLayer.DTOs.ProductBundle.Response;
using MV.DomainLayer.DTOs.ResponseModels;
using MV.DomainLayer.Entities;
using MV.DomainLayer.Enums;
using MV.InfrastructureLayer.Interfaces;

namespace MV.ApplicationLayer.Services;

public class ProductBundleService : IProductBundleService
{
    private readonly IProductBundleRepository _bundleRepository;
    private readonly IProductRepository _productRepository;

    public ProductBundleService(IProductBundleRepository bundleRepository, IProductRepository productRepository)
    {
        _bundleRepository = bundleRepository;
        _productRepository = productRepository;
    }

    public async Task<ApiResponse<IEnumerable<ProductBundleResponse>>> GetBundleComponentsAsync(int kitProductId)
    {
        // Validate parent product is KIT
        var parentProduct = await _productRepository.GetByIdAsync(kitProductId);
        if (parentProduct == null)
        {
            return ApiResponse<IEnumerable<ProductBundleResponse>>.ErrorResponse($"Product with ID {kitProductId} not found.");
        }

        if (parentProduct.ProductType != ProductTypeEnum.KIT.ToString())
        {
            return ApiResponse<IEnumerable<ProductBundleResponse>>.ErrorResponse($"Product {kitProductId} is not a KIT.");
        }

        var bundles = await _bundleRepository.GetBundleComponentsAsync(kitProductId);
        var response = bundles.Select(MapToResponse);

        return ApiResponse<IEnumerable<ProductBundleResponse>>.SuccessResponse(response);
    }

    public async Task<ApiResponse<ProductBundleResponse>> AddProductToBundleAsync(int kitProductId, AddProductToBundleRequest request)
    {
        // Validate parent product is KIT
        var parentProduct = await _productRepository.GetByIdAsync(kitProductId);
        if (parentProduct == null)
        {
            return ApiResponse<ProductBundleResponse>.ErrorResponse($"Product with ID {kitProductId} not found.");
        }

        if (parentProduct.ProductType != ProductTypeEnum.KIT.ToString())
        {
            return ApiResponse<ProductBundleResponse>.ErrorResponse($"Product {kitProductId} is not a KIT. Only KIT products can have components.");
        }

        // Validate child product exists
        var childProduct = await _productRepository.GetByIdAsync(request.ChildProductId);
        if (childProduct == null)
        {
            return ApiResponse<ProductBundleResponse>.ErrorResponse($"Child product with ID {request.ChildProductId} not found.");
        }

        // Validate child product is not a KIT (không cho phép KIT trong KIT)
        if (childProduct.ProductType == ProductTypeEnum.KIT.ToString())
        {
            return ApiResponse<ProductBundleResponse>.ErrorResponse("Cannot add a KIT as a component of another KIT.");
        }

        // Validate not already in bundle
        if (await _bundleRepository.ExistsInBundleAsync(kitProductId, request.ChildProductId))
        {
            return ApiResponse<ProductBundleResponse>.ErrorResponse($"Product {request.ChildProductId} is already in this bundle.");
        }

        // Validate not adding itself
        if (kitProductId == request.ChildProductId)
        {
            return ApiResponse<ProductBundleResponse>.ErrorResponse("Cannot add a product to itself.");
        }

        var bundle = new ProductBundle
        {
            ParentProductId = kitProductId,
            ChildProductId = request.ChildProductId,
            Quantity = request.Quantity
        };

        var created = await _bundleRepository.AddToBundleAsync(bundle);
        var result = await _bundleRepository.GetBundleItemAsync(kitProductId, request.ChildProductId);
        var response = MapToResponse(result!);

        return ApiResponse<ProductBundleResponse>.SuccessResponse(response, "Product added to bundle successfully.");
    }

    public async Task<ApiResponse<bool>> RemoveProductFromBundleAsync(int kitProductId, int childProductId)
    {
        var bundle = await _bundleRepository.GetBundleItemAsync(kitProductId, childProductId);

        if (bundle == null)
        {
            return ApiResponse<bool>.ErrorResponse($"Product {childProductId} not found in bundle {kitProductId}.");
        }

        await _bundleRepository.RemoveFromBundleAsync(bundle.BundleId);
        return ApiResponse<bool>.SuccessResponse(true, "Product removed from bundle successfully.");
    }

    public async Task<ApiResponse<int>> GetAvailableKitStockAsync(int kitProductId)
    {
        // Validate parent product is KIT
        var parentProduct = await _productRepository.GetByIdAsync(kitProductId);
        if (parentProduct == null)
        {
            return ApiResponse<int>.ErrorResponse($"Product with ID {kitProductId} not found.");
        }

        if (parentProduct.ProductType != ProductTypeEnum.KIT.ToString())
        {
            return ApiResponse<int>.ErrorResponse($"Product {kitProductId} is not a KIT.");
        }

        // Get all components
        var components = await _bundleRepository.GetBundleComponentsAsync(kitProductId);

        if (!components.Any())
        {
            return ApiResponse<int>.SuccessResponse(0, "KIT has no components defined.");
        }

        // Calculate max kits can be made from available stock
        var maxKits = int.MaxValue;
        foreach (var component in components)
        {
            var childStock = component.ChildProduct.StockQuantity ?? 0;
            var requiredQty = component.Quantity ?? 1;
            var possibleKits = childStock / requiredQty;

            if (possibleKits < maxKits)
            {
                maxKits = possibleKits;
            }
        }

        return ApiResponse<int>.SuccessResponse(maxKits, $"Can make {maxKits} kits from current stock.");
    }

    private ProductBundleResponse MapToResponse(ProductBundle bundle)
    {
        return new ProductBundleResponse
        {
            BundleId = bundle.BundleId,
            ParentProductId = bundle.ParentProductId,
            ParentProductName = bundle.ParentProduct.Name,
            ChildProductId = bundle.ChildProductId,
            ChildProductName = bundle.ChildProduct.Name,
            ChildProductSku = bundle.ChildProduct.Sku,
            ChildProductPrice = bundle.ChildProduct.Price,
            Quantity = bundle.Quantity ?? 1,
            CreatedAt = bundle.CreatedAt
        };
    }
}
