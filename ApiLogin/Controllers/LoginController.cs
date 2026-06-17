using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ApiLogin.Controllers
{
  [ApiController]
  [Route("api/[controller]")]
  public class LoginController : ControllerBase
  {
    private static readonly List<Usuario> usuarios = new()
        {
            new Usuario
            {
                UserName = "admin",
                Password = "1234",
                Permisos = new Permisos
                {
                    Get = true,
                    Add = true,
                    Update = true,
                    Delete = true
                }
            },
            new Usuario
            {
                UserName = "visor",
                Password = "1234",
                Permisos = new Permisos
                {
                    Get = true,
                    Add = false,
                    Update = false,
                    Delete = false
                }
            }
        };

    [HttpPost]
    public IActionResult Login([FromBody] LoginRequest request)
    {
      var usuario = usuarios.FirstOrDefault(u =>
          u.UserName == request.Usuario && u.Password == request.Password);

      if (usuario is null)
        return Unauthorized(new { message = "Credenciales incorrectas" });

      var token = GenerarJWT(usuario.UserName, usuario.Permisos);

      Response.Cookies.Append("auth_token", token, new CookieOptions
      {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.None,
        Expires = DateTimeOffset.UtcNow.AddMinutes(30)
      });

      return Ok(new { message = "Login exitoso", permisos = usuario.Permisos });
    }

    private string GenerarJWT(string userName, Permisos permisos)
    {
      var key = new SymmetricSecurityKey(
          Encoding.UTF8.GetBytes("clave-secreta-minimo-32-caracteres!!")
      );
      var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

      var claims = new[]
      {
                new Claim(ClaimTypes.Name, userName),
                new Claim("permisos", System.Text.Json.JsonSerializer.Serialize(permisos))
            };

      var token = new JwtSecurityToken(
          issuer: "ApiLogin",
          audience: "ReactApp",
          claims: claims,
          expires: DateTime.UtcNow.AddMinutes(5),
          signingCredentials: creds
      );

      return new JwtSecurityTokenHandler().WriteToken(token);
    }
  }

  public class LoginRequest
  {
    public string Usuario { get; set; }
    public string Password { get; set; }
  }

  public class Permisos
  {
    public bool Get { get; set; }
    public bool Add { get; set; }
    public bool Update { get; set; }
    public bool Delete { get; set; }
  }

  public class Usuario
  {
    public string UserName { get; set; }
    public string Password { get; set; }
    public Permisos Permisos { get; set; }
  }
}
