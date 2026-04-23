using AutoMapper;
using Azure;
using Mango.Services.ShoppingCartAPI.Data;
using Mango.Services.ShoppingCartAPI.Models;
using Mango.Services.ShoppingCartAPI.Models.Dto;
using Mango.Services.ShoppingCartAPI.Service;
using Mango.Services.ShoppingCartAPI.Service.IService;
using Mango.Services.ShoppingCartAPI.UnitOfWork;
using MessageBus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System.Reflection.PortableExecutable;

namespace Mango.Services.ShoppingCartAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;
        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        [HttpPost("CartUpsert")]
        public async Task<ActionResult<ResponseDto<CartDto>>> CartUpsert(CartDto cartDto)
        {
            var response = new ResponseDto<CartDto>();
            try
            {
                response.Result = await _cartService.UpsertCartAsync(cartDto);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                return StatusCode(500, response);
            }
            return Ok(response);
        }
        [HttpDelete("RemoveCart/{cartDetailsId}")]
        public async Task<ActionResult<ResponseDto<bool>>> RemoveCart(int cartDetailsId)
        {
            var response = new ResponseDto<bool>();
            try
            {
                await _cartService.RemoveCartItemAsync(cartDetailsId);
                response.Result = true;
            }
            catch (KeyNotFoundException ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                return NotFound(response);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                return StatusCode(500, response);
            }
            return Ok(response);
        }
        [HttpGet("GetCart/{userId}")]
        public async Task<ActionResult<ResponseDto<CartDto>>> GetCart(string userId)
        {
            var response = new ResponseDto<CartDto>();
            try
            {
                response.Result = await _cartService.GetCartAsync(userId);
            }
            catch (KeyNotFoundException ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                return NotFound(response);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                return StatusCode(500, response);
            }
            return Ok(response);
        }
        [HttpPost("ApplyCoupon")]
        public async Task<ActionResult<ResponseDto<bool>>> ApplyCoupon([FromBody] CartDto cartDto)
        {
            var response = new ResponseDto<bool>();
            try
            {
                if (cartDto?.CartHeader == null || string.IsNullOrEmpty(cartDto.CartHeader.UserId))
                {
                    response.IsSuccess = false;
                    response.Message = "Invalid request";
                    return BadRequest(response);
                }
                await _cartService.ApplyCouponAsync(cartDto.CartHeader.UserId, cartDto.CartHeader.CouponCode);
                response.Result = true;
                response.Message = "Coupon applied successfully";
            }
            catch (KeyNotFoundException ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                return NotFound(response);
            }
            catch (ArgumentException ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                return BadRequest(response);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                return StatusCode(500, response);
            }
            return Ok(response);
        }
        [HttpPost("RemoveCoupon")]
        public async Task<ActionResult<ResponseDto<bool>>> RemoveCoupon([FromBody] CartDto cartDto)
        {
            var response = new ResponseDto<bool>();
            try
            {
                if (cartDto?.CartHeader == null || string.IsNullOrEmpty(cartDto.CartHeader.UserId))
                {
                    response.IsSuccess = false;
                    response.Message = "Invalid request";
                    return BadRequest(response);
                }
                await _cartService.RemoveCouponAsync(cartDto.CartHeader.UserId);
                response.Result = true;
                response.Message = "Coupon removed successfully";
            }
            catch (KeyNotFoundException ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                return NotFound(response);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                return StatusCode(500, response);
            }
            return Ok(response);
        }
        [HttpPost("EmailCartRequest")]
        public async Task<ActionResult<ResponseDto<bool>>> EmailCartRequest([FromBody] CartDto cartDto)
        {
            var response = new ResponseDto<bool>();
            try
            {
                await _cartService.EmailCartRequestAsync(cartDto);
                response.Result = true;
                response.Message = "Email request sent to queue successfully";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                return StatusCode(500, response);
            }
            return Ok(response);
        }

    }
}
