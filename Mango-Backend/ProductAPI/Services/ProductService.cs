using AutoMapper;
using Mango.Services.ProductAPI.Services.IServices;
using Mango.Services.ProductAPI.UnitOfWork;
using ProductAPI.Models;
using ProductAPI.Models.Dto;

namespace Mango.Services.ProductAPI.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper     _mapper;

    public ProductService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper     = mapper;
    }

    public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
    {
        var products = await _unitOfWork.Product.GetAllAsync();
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }

    public async Task<ProductDto?> GetProductByIdAsync(int id)
    {
        var product = await _unitOfWork.Product.GetAsync(p => p.ProductId == id);
        return product == null ? null : _mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> CreateProductAsync(ProductDto productDto, HttpRequest request)
    {
        var product = _mapper.Map<Product>(productDto);

        await _unitOfWork.Product.CreateAsync(product);
        await _unitOfWork.SaveAsync();

        if (productDto.Image != null)
        {
            product.ImageLocalPath = GetImageLocalPath(productDto, product.ProductId);
            DeleteImageIfExists(product.ImageLocalPath);
            product.ImageUrl = await HandleImageUploadAsync(productDto, product.ProductId, request);
        }
        else
        {
            product.ImageUrl = "https://placehold.co/600x400";
        }

        await _unitOfWork.Product.UpdateAsync(product);
        await _unitOfWork.SaveAsync();

        return _mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> UpdateProductAsync(ProductDto productDto, HttpRequest request)
    {
        var product = await _unitOfWork.Product.GetAsync(x => x.ProductId == productDto.ProductId)
            ?? throw new KeyNotFoundException("Product not found");

        _mapper.Map(productDto, product);

        if (productDto.Image != null)
        {
            DeleteImageIfExists(product.ImageLocalPath);
            product.ImageLocalPath = GetImageLocalPath(productDto, product.ProductId);
            product.ImageUrl = await HandleImageUploadAsync(productDto, product.ProductId, request);
        }

        await _unitOfWork.Product.UpdateAsync(product);
        await _unitOfWork.SaveAsync();

        return _mapper.Map<ProductDto>(product);
    }

    public async Task DeleteProductAsync(int id)
    {
        var product = await _unitOfWork.Product.GetAsync(p => p.ProductId == id)
            ?? throw new KeyNotFoundException("Product not found");

        DeleteImageIfExists(product.ImageLocalPath);

        await _unitOfWork.Product.RemoveAsync(product);
        await _unitOfWork.SaveAsync();
    }

    // ─── Private Helpers ────────────────────────────────────────────────────────

    private async Task<string> HandleImageUploadAsync(ProductDto dto, int productId, HttpRequest request)
    {
        string localPath = GetImageLocalPath(dto, productId)!;
        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), localPath);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        using var stream = new FileStream(fullPath, FileMode.Create);
        await dto.Image!.CopyToAsync(stream);

        var baseUrl = $"{request.Scheme}://{request.Host.Value}{request.PathBase.Value}";
        string fileName = productId + Path.GetExtension(dto.Image.FileName);
        return baseUrl + "/ProductImages/" + fileName;
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
        if (file.Exists) file.Delete();
    }
}
