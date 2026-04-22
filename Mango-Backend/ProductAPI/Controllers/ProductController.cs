using AutoMapper;
using Azure;
using Mango.Services.ProductAPI.Constants;
using Mango.Services.ProductAPI.Data;
using Mango.Services.ProductAPI.Models.Dto;
using Mango.Services.ProductAPI.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductAPI.Models;
using ProductAPI.Models.Dto;

namespace Mango.Services.ProductAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public ProductController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ResponseDto<IEnumerable<ProductDto>>> Get()
        {
            var response = new ResponseDto<IEnumerable<ProductDto>>();
            try
            {
                var products = await _unitOfWork.Product.GetAllAsync();
                response.Result = _mapper.Map<IEnumerable<ProductDto>>(products);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }
       
        [HttpGet("{id:int}")]
        public async Task<ResponseDto<ProductDto>> Get(int id)
        {
            var response = new ResponseDto<ProductDto>();
            try
            {
                var product = await _unitOfWork.Product.GetAsync(p => p.ProductId == id);

                if (product == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Product not found";
                    return response;
                }

                response.Result = _mapper.Map<ProductDto>(product);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }
        
        [Authorize(Roles = SD.RoleAdmin)]
        [HttpPost]
        public async Task<ResponseDto<ProductDto>> Post(ProductDto productDto)
        {
            var response = new ResponseDto<ProductDto>();
            try
            {
                Product product = _mapper.Map<Product>(productDto);

                await _unitOfWork.Product.CreateAsync(product);
                await _unitOfWork.SaveAsync();

                if (productDto.Image != null)
                {

                    string fileName = product.ProductId + Path.GetExtension(productDto.Image.FileName);
                    string filePath = @"wwwroot\ProductImages\" + fileName;

                    //I have added the if condition to remove the any image with same name if that exist in the folder by any change
                    var directoryLocation = Path.Combine(Directory.GetCurrentDirectory(), filePath);
                    FileInfo file = new FileInfo(directoryLocation);
                    if (file.Exists)
                    {
                        file.Delete();
                    }

                    var filePathDirectory = Path.Combine(Directory.GetCurrentDirectory(), filePath);
                    using (var fileStream = new FileStream(filePathDirectory, FileMode.Create))
                    {
                        productDto.Image.CopyTo(fileStream);
                    }
                    var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}{HttpContext.Request.PathBase.Value}";
                    product.ImageUrl = baseUrl + "/ProductImages/" + fileName;
                    product.ImageLocalPath = filePath;
                }
                else
                {
                    product.ImageUrl = "https://placehold.co/600x400";
                }
                await _unitOfWork.Product.UpdateAsync(product);
                await _unitOfWork.SaveAsync();

                response.Result = _mapper.Map<ProductDto>(product);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }
        
        [Authorize(Roles = SD.RoleAdmin)]
        [HttpPut]
        public async Task<ResponseDto<ProductDto>> Update(ProductDto productDto)
        {
            var response = new ResponseDto<ProductDto>();
            try
            {
                var product = await _unitOfWork.Product.GetAsync(x => x.ProductId == productDto.ProductId);

                if (product == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Product not found";
                    return response;
                }
                _mapper.Map(productDto, product);
                if (productDto.Image != null)
                {
                    if (!string.IsNullOrEmpty(product.ImageLocalPath))
                    {
                        var oldFilePathDirectory = Path.Combine(Directory.GetCurrentDirectory(), product.ImageLocalPath);
                        FileInfo file = new FileInfo(oldFilePathDirectory);
                        if (file.Exists)
                        {
                            file.Delete();
                        }
                    }

                    string fileName = product.ProductId + Path.GetExtension(productDto.Image.FileName);
                    string filePath = @"wwwroot\ProductImages\" + fileName;
                    var filePathDirectory = Path.Combine(Directory.GetCurrentDirectory(), filePath);
                    using (var fileStream = new FileStream(filePathDirectory, FileMode.Create))
                    {
                        productDto.Image.CopyTo(fileStream);
                    }
                    var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}{HttpContext.Request.PathBase.Value}";
                    product.ImageUrl = baseUrl + "/ProductImages/" + fileName;
                    product.ImageLocalPath = filePath;
                }
                await _unitOfWork.Product.UpdateAsync(product);
                await _unitOfWork.SaveAsync();

                response.Result = _mapper.Map<ProductDto>(product);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = SD.RoleAdmin)]
        public async Task<ResponseDto<string>> Delete(int id)
        {
            var response = new ResponseDto<string>();
            try
            {
                var product = await _unitOfWork.Product.GetAsync(p => p.ProductId == id);
                if (product == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Product not found";
                    return response;
                }
                if (!string.IsNullOrEmpty(product.ImageLocalPath))
                {
                    var oldFilePathDirectory = Path.Combine(Directory.GetCurrentDirectory(), product.ImageLocalPath);
                    FileInfo file = new FileInfo(oldFilePathDirectory);
                    if (file.Exists)
                    {
                        file.Delete();
                    }
                }

                await _unitOfWork.Product.RemoveAsync(product);
                await _unitOfWork.SaveAsync();

                response.Result = "Deleted successfully";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }
    }
}
