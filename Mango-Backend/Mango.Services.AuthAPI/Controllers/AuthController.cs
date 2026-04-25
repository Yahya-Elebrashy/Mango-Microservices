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
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterationRequestDto request)
        {
            _logger.LogInformation("POST /api/auth/register — Registration request for email {Email}", request.Email);

            var errorMessage = await _authService.Register(request);

            if (!string.IsNullOrEmpty(errorMessage))
            {
                _logger.LogWarning("POST /api/auth/register — Registration failed for email {Email}. Reason: {Error}",
                    request.Email, errorMessage);
                return BadRequest(new ResponseDto<string> { IsSuccess = false, Message = errorMessage });
            }

            _logger.LogInformation("POST /api/auth/register — User {Email} registered successfully", request.Email);
            return Ok(new ResponseDto<string> { Result = "User registered successfully" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            _logger.LogInformation("POST /api/auth/login — Login request for username {Username}", request.UserName);

            var loginResponse = await _authService.Login(request);

            if (loginResponse?.User == null)
            {
                _logger.LogWarning("POST /api/auth/login — Login failed for username {Username} — invalid credentials",
                    request.UserName);
                return Unauthorized(new ResponseDto<string> { IsSuccess = false, Message = "Username or Password is incorrect" });
            }

            _logger.LogInformation("POST /api/auth/login — User {Username} (ID={UserId}) logged in successfully",
                request.UserName, loginResponse.User.Id);
            return Ok(new ResponseDto<LoginResponseDto> { Result = loginResponse });
        }

        [HttpPost("AssignRole")]
        public async Task<IActionResult> AssignRole([FromBody] RegisterationRequestDto request)
        {
            _logger.LogInformation("POST /api/auth/AssignRole — Assigning role {Role} to email {Email}",
                request.Role, request.Email);

            var success = await _authService.AssignRole(request.Email, request.Role.ToUpper());

            if (!success)
            {
                _logger.LogWarning("POST /api/auth/AssignRole — Failed to assign role {Role} to email {Email}",
                    request.Role, request.Email);
                return BadRequest(new ResponseDto<string> { IsSuccess = false, Message = "Error assigning role" });
            }

            _logger.LogInformation("POST /api/auth/AssignRole — Role {Role} assigned successfully to email {Email}",
                request.Role, request.Email);
            return Ok(new ResponseDto<string> { Result = "Role assigned successfully" });
        }
    }
}