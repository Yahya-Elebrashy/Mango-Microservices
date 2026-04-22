using Azure;
using Azure.Core;
using Mango.Services.AuthAPI.Models.Dto;
using Mango.Services.AuthAPI.Service.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Mango.Services.AuthAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<ResponseDto<string>>> Register([FromBody] RegisterationRequestDto request)
        {
            var response = new ResponseDto<string>();

            try
            {
                var errorMessage = await _authService.Register(request);

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    response.IsSuccess = false;
                    response.Message = errorMessage;
                    return BadRequest(response);
                }

                response.Result = "User registered successfully";
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                return StatusCode(500, response);
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<ResponseDto<LoginResponseDto>>> Login([FromBody] LoginRequestDto request)
        {
            var response = new ResponseDto<LoginResponseDto>();
            try
            {
                var loginResponse = await _authService.Login(request);

                if (loginResponse == null || loginResponse.User == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Username or Password is incorrect";
                    return BadRequest(response);
                }

                response.Result = loginResponse;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                return StatusCode(500, response);
            }
        }
        [HttpPost("AssignRole")]
        public async Task<ActionResult<ResponseDto<string>>> AssignRole([FromBody] RegisterationRequestDto request)
        {
            var response = new ResponseDto<string>();

            try
            {
                var success = await _authService.AssignRole(request.Email, request.Role.ToUpper());

                if (!success)
                {
                    response.IsSuccess = false;
                    response.Message = "Error assigning role";
                    return BadRequest(response);
                }

                response.Result = "Role assigned successfully";
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                return StatusCode(500, response);
            }
        }
    }
}
