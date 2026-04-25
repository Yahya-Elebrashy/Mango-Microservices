using AutoMapper;
using Mango.Services.ProductAPI.Services.IServices;
using Mango.Services.ProductAPI.UnitOfWork;
using ProductAPI.Models;
using ProductAPI.Models.Dto;

namespace Mango.Services.ProductAPI.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<ProductService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
    {
        _logger.LogInformation("Fetching all products");

        var products = await _unitOfWork.Product.GetAllAsync();
        var result = _mapper.Map<IEnumerable<ProductDto>>(products);

        _logger.LogInformation("Successfully retrieved {Count} products", result.Count());
        return result;
    }

    public async Task<ProductDto?> GetProductByIdAsync(int id)
    {
        _logger.LogInformation("Fetching product with ID {ProductId}", id);

        var product = await _unitOfWork.Product.GetAsync(p => p.ProductId == id);

        if (product == null)
        {
            _logger.LogWarning("Product with ID {ProductId} was not found", id);
            return null;
        }

        _logger.LogInformation("Successfully retrieved product with ID {ProductId}", id);
        return _mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> CreateProductAsync(ProductDto productDto, HttpRequest request)
    {
        _logger.LogInformation("Creating new product with Name={Name}, Price={Price}, CategoryName={Category}",
            productDto.Name, productDto.Price, productDto.CategoryName);

        var product = _mapper.Map<Product>(productDto);

        await _unitOfWork.Product.CreateAsync(product);
        await _unitOfWork.SaveAsync();

        _logger.LogInformation("Product saved to DB with assigned ID {ProductId}", product.ProductId);

        if (productDto.Image != null)
        {
            _logger.LogInformation("Image detected for product {ProductId}. Processing image upload", product.ProductId);

            product.ImageLocalPath = GetImageLocalPath(productDto, product.ProductId);
            DeleteImageIfExists(product.ImageLocalPath);
            product.ImageUrl = await HandleImageUploadAsync(productDto, product.ProductId, request);

            _logger.LogInformation("Image uploaded successfully for product {ProductId}. ImageUrl={ImageUrl}",
                product.ProductId, product.ImageUrl);
        }
        else
        {
            _logger.LogInformation("No image provided for product {ProductId}. Using placeholder URL", product.ProductId);
            product.ImageUrl = "https://placehold.co/600x400";
        }

        await _unitOfWork.Product.UpdateAsync(product);
        await _unitOfWork.SaveAsync();

        _logger.LogInformation("Product {ProductId} created successfully", product.ProductId);
        return _mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> UpdateProductAsync(ProductDto productDto, HttpRequest request)
    {
        _logger.LogInformation("Updating product with ID {ProductId}", productDto.ProductId);

        var product = await _unitOfWork.Product.GetAsync(x => x.ProductId == productDto.ProductId);
        if (product == null)
        {
            _logger.LogWarning("Update failed — product with ID {ProductId} not found", productDto.ProductId);
            throw new KeyNotFoundException("Product not found");
        }

        _mapper.Map(productDto, product);
        _logger.LogInformation("Mapped updated fields onto product {ProductId}", product.ProductId);

        if (productDto.Image != null)
        {
            _logger.LogInformation("New image provided for product {ProductId}. Replacing existing image", product.ProductId);

            DeleteImageIfExists(product.ImageLocalPath);
            product.ImageLocalPath = GetImageLocalPath(productDto, product.ProductId);
            product.ImageUrl = await HandleImageUploadAsync(productDto, product.ProductId, request);

            _logger.LogInformation("Image replaced successfully for product {ProductId}. New ImageUrl={ImageUrl}",
                product.ProductId, product.ImageUrl);
        }
        else
        {
            _logger.LogInformation("No new image provided for product {ProductId}. Keeping existing image", product.ProductId);
        }

        await _unitOfWork.Product.UpdateAsync(product);
        await _unitOfWork.SaveAsync();

        _logger.LogInformation("Product {ProductId} updated successfully", product.ProductId);
        return _mapper.Map<ProductDto>(product);
    }

    public async Task DeleteProductAsync(int id)
    {
        _logger.LogInformation("Deleting product with ID {ProductId}", id);

        var product = await _unitOfWork.Product.GetAsync(p => p.ProductId == id);
        if (product == null)
        {
            _logger.LogWarning("Delete failed — product with ID {ProductId} not found", id);
            throw new KeyNotFoundException("Product not found");
        }

        if (!string.IsNullOrEmpty(product.ImageLocalPath))
        {
            _logger.LogInformation("Deleting image for product {ProductId} at path {ImageLocalPath}",
                id, product.ImageLocalPath);
            DeleteImageIfExists(product.ImageLocalPath);
        }

        await _unitOfWork.Product.RemoveAsync(product);
        await _unitOfWork.SaveAsync();

        _logger.LogInformation("Product {ProductId} deleted successfully", id);
    }

    // ─── Private Helpers ────────────────────────────────────────────────────────

    private async Task<string> HandleImageUploadAsync(ProductDto dto, int productId, HttpRequest request)
    {
        string localPath = GetImageLocalPath(dto, productId)!;
        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), localPath);

        _logger.LogDebug("Saving image for product {ProductId} to path {FullPath}", productId, fullPath);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        using var stream = new FileStream(fullPath, FileMode.Create);
        await dto.Image!.CopyToAsync(stream);

        var baseUrl = $"{request.Scheme}://{request.Host.Value}{request.PathBase.Value}";
        string fileName = productId + Path.GetExtension(dto.Image.FileName);
        var imageUrl = baseUrl + "/ProductImages/" + fileName;

        _logger.LogDebug("Image for product {ProductId} saved. Resolved URL: {ImageUrl}", productId, imageUrl);
        return imageUrl;
    }

    private string? GetImageLocalPath(ProductDto dto, int productId)
    {
        if (dto.Image == null) return null;
        string fileName = productId + Path.GetExtension(dto.Image.FileName);
        return @"wwwroot\ProductImages\" + fileName;
    }

    private void DeleteImageIfExists(string? localPath)
    {
        if (string.IsNullOrEmpty(localPath)) return;

        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), localPath);
        var file = new FileInfo(fullPath);

        if (file.Exists)
        {
            file.Delete();
            _logger.LogInformation("Deleted image file at {FullPath}", fullPath);
        }
        else
        {
            _logger.LogDebug("No image file found at {FullPath} — nothing to delete", fullPath);
        }
    }
}