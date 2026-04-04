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
        public readonly ResponseDto _response;
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _response = new ResponseDto();
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterationRequestDto registerationRequestDto)
        {
            var errorMessage = await _authService.Register(registerationRequestDto);
            if(!string.IsNullOrEmpty(errorMessage))
            {
                _response.Message = errorMessage;
                _response.IsSuccess = false;
                return BadRequest(_response);
            }
            return Ok(_response);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto loginRequestDto)
        {
            var loginResponse = await _authService.Login(loginRequestDto);
            if (loginResponse.User == null)
            {
                _response.Message = "Username or Password is incorrect";
                _response.IsSuccess = false;
                return BadRequest(_response);
            }
            _response.Result = loginResponse;
            return Ok(_response);
        }
        [HttpPost("AssignRole")]
        public async Task<IActionResult> AssignRole([FromBody] RegisterationRequestDto registerationRequestDto)
        {
            var assignRoleSuccessful = await _authService.AssignRole(registerationRequestDto.Email, registerationRequestDto.Role.ToUpper());
            if (!assignRoleSuccessful)
            {
                _response.Message = "Error encountered.";
                _response.IsSuccess = false;
                return BadRequest(_response);
            }
            return Ok(_response);
        }
    }
}
