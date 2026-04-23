using AutoMapper;
using Azure;
using Mango.Services.ProductAPI.Constants;
using Mango.Services.ProductAPI.Data;
using Mango.Services.ProductAPI.Models.Dto;
using Mango.Services.ProductAPI.Services;
using Mango.Services.ProductAPI.Services.IServices;
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
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<ResponseDto<IEnumerable<ProductDto>>> Get()
        {
            var response = new ResponseDto<IEnumerable<ProductDto>>();
            try
            {
                response.Result = await _productService.GetAllProductsAsync();

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
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Product not found";
                    return response;
                }
                response.Result = product;
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
                response.Result = await _productService.CreateProductAsync(productDto, Request);
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
                response.Result = await _productService.UpdateProductAsync(productDto, Request);
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
                await _productService.DeleteProductAsync(id);
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
