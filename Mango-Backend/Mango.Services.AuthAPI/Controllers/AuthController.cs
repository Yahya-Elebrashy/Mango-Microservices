using Mango.Services.AuthAPI.Models.Dto;
using Mango.Services.AuthAPI.Service.IService;
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
        public async Task<IActionResult> Register([FromBody] RegisterationRequestDto request)
        {
            var errorMessage = await _authService.Register(request);
            if (!string.IsNullOrEmpty(errorMessage))
                return BadRequest(new ResponseDto<string> { IsSuccess = false, Message = errorMessage });

            return Ok(new ResponseDto<string> { Result = "User registered successfully" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var loginResponse = await _authService.Login(request);
            if (loginResponse == null || loginResponse.User == null)
                return Unauthorized(new ResponseDto<string> { IsSuccess = false, Message = "Username or Password is incorrect" });

            return Ok(new ResponseDto<LoginResponseDto> { Result = loginResponse });
        }

        [HttpPost("AssignRole")]
        public async Task<IActionResult> AssignRole([FromBody] RegisterationRequestDto request)
        {
            var success = await _authService.AssignRole(request.Email, request.Role.ToUpper());
            if (!success)
                return BadRequest(new ResponseDto<string> { IsSuccess = false, Message = "Error assigning role" });

            return Ok(new ResponseDto<string> { Result = "Role assigned successfully" });
        }
    }
}