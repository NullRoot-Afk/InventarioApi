using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace InventarioApi.Services;

public class TokenService
{
    private readonly IConfiguration _config; //variable privada que almacenara la configuracion de la aplicacion

    public TokenService(IConfiguration config)
    {
        _config = config; //Guarda una instancia de la configuracion de la aplicacion
    }

    public string GenerateToken(string username,IEnumerable<string> roles=null)//Los roles son opcionales, por eso se inicializa en null
    {
        var jwtSection = _config.GetSection("Jwt");
        var key = jwtSection.GetValue<string>("Key");
        var issuer = jwtSection.GetValue<string>("Issuer");
        var audience = jwtSection.GetValue<string>("Audience");
        var expiresIn = jwtSection.GetValue<int>("ExpiresInMinutes");

        var claims = new List<Claim>//Los claims son los datos incluidos en el token
        {
            new Claim(ClaimTypes.Name, username),//nombre del usuario
            new Claim(JwtRegisteredClaimNames.Sub, username),//sub es el subject o sujeto del token, que es el nombre del usuario
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())//Genera un identificador unico para el token y lo convierte a string
        };

        if (roles is not null)//Si roles fue especificado en la solicitud, se recorre la lista de roles y se agregan como claims al token del usuario
        {
            foreach (var role in roles)
            {
                if (!string.IsNullOrWhiteSpace(role))
                {
                    claims.Add(new Claim(ClaimTypes.Role, role.Trim()));//
                }
            }
        }

        var keyBytes = Encoding.UTF8.GetBytes(key);
        var signingKey = new SymmetricSecurityKey(keyBytes);
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresIn),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
