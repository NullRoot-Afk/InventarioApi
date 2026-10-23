using InventarioApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventarioApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly TokenService _tokenService;//Clase encargada de generar el token JWT

    public AuthController(TokenService tokenService)//Inyeccion de dependencias del servicio de token
    {
        _tokenService = tokenService;
    }

    public class LoginRequest
    {
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    [HttpPost("login")]
    public IActionResult Login(LoginRequest request)
    {
        // Credenciales estaticas
        if (request.Username == "admin" && request.Password == "password")
        {
            var token = _tokenService.GenerateToken(request.Username, new[] { "Admin" });//GenerateToken(username, role)
            return Ok(new { token });
        }
        if (request.Username == "user" && request.Password == "password")
        {
            
            var token = _tokenService.GenerateToken(request.Username, null);//GenerateToken(username, role)
            return Ok(new { token });
        }

        return Unauthorized();//Si el usuario no se encuentra almacenado retorna un 401 Unauthorized
    }
}
