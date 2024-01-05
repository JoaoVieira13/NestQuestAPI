using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NestQuest.Enum;
using NestQuest.Models;

namespace NestQuest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly NestQuesteContext _context;
        private readonly IConfiguration _configuration;
        private readonly string _jwtSecret;

        public UsersController(NestQuesteContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            _jwtSecret = GenerateRandomSecret();
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            var users = await _context.Users
                .ToListAsync();

            return users;
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            if (_context.Users == null)
            {
                return NotFound();
            }
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        // PUT: api/Users/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, User user)
        {
            if (id != user.Id)
            {
                return BadRequest();
            }

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Users
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<User>> PostUser(User user)
        {
            if (_context.Users == null)
            {
                return Problem("Entity set 'NestQuesteContext.Users' is null.");
            }

            // Hash da senha antes de salvar no banco de dados
            user.Password = HashPassword(user.Password);

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            return CreatedAtAction("GetUser", new { id = user.Id }, user);
        }

        // DELETE: api/Users/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            if (_context.Users == null)
            {
                return NotFound();
            }
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // UPDATE: api/Users/5/offer-count
        [HttpGet("{userId}/offer-count")]
        public async Task<ActionResult<int>> GetUserOfferCount(int userId)
        {
            var offerCount = await _context.Offers.CountAsync(o => o.CreatedBy.Id == userId);
            return Ok(offerCount);
        }

        // PUT: api/Users/5/role
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}/role")]
        public async Task<IActionResult> PutUserRole(int id, [FromBody] int userRole)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            if (userRole >= 0 && userRole <= 3)
            {
                user.UserRole = (UserRole)userRole;
            }
            else
            {
                return BadRequest($"Invalid UserRole: {userRole}");
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // GET: api/Users/5/Offers
        [HttpGet("{id}/Offers")]
        public async Task<ActionResult<IEnumerable<Offer>>> GetUserOffers(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            // Obter a contagem de ofertas
            var offerCount = await _context.Offers.CountAsync(o => o.CreatedBy.Id == id);

            // Agora, se a contagem for maior que zero, obtemos as ofertas
            if (offerCount > 0)
            {
                var userOffers = await _context.Offers
                    .Where(o => o.CreatedBy.Id == id)
                    .Include(o => o.Category)
                    .Include(o => o.CreatedBy)
                    .ToListAsync();

                return userOffers;
            }

            // Se não houver ofertas, retornar uma lista vazia
            return new List<Offer>();
        }

        [HttpPut("public/{id}")]
        public async Task<IActionResult> PutUser(int id, [FromBody] UserUpdateData updateData)
        {
            if (updateData == null)
            {
                return BadRequest();
            }

            var existingUser = await _context.Users.FindAsync(id);

            if (existingUser == null)
            {
                return NotFound();
            }

            existingUser.Birthdate = updateData.Birthdate ?? existingUser.Birthdate;
            existingUser.PhoneNumber = updateData.PhoneNumber ?? existingUser.PhoneNumber;
            existingUser.Address = updateData.Address ?? existingUser.Address;
            existingUser.PostalCode = updateData.PostalCode ?? existingUser.PostalCode;
            existingUser.City = updateData.City ?? existingUser.City;
            existingUser.Country = updateData.Country ?? existingUser.Country;
            existingUser.LongDescription = updateData.LongDescription ?? existingUser.LongDescription;
            existingUser.ShortDescription = updateData.ShortDescription ?? existingUser.ShortDescription;
            existingUser.Avatar = updateData.Avatar ?? existingUser.Avatar;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpPost("register")]
        public async Task<ActionResult<string>> Register([FromBody] User userModel)
        {
            try
            {
                // Verifique se o usuário já existe com o mesmo email
                if (_context.Users.Any(u => u.Email == userModel.Email))
                {
                    return BadRequest("Email is already registered");
                }

                // Hash da senha antes de salvar no banco de dados
                userModel.Password = HashPassword(userModel.Password);

                // Adicione o usuário ao banco de dados
                _context.Users.Add(userModel);
                await _context.SaveChangesAsync();

                // Crie um token JWT para o novo usuário
                var token = CreateToken(userModel);

                return Ok(token);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        [HttpPost("login")] // Rota /login
        public async Task<ActionResult<string>> Login([FromBody] User userModel)
        {
            try
            {
                var user = FindUser(userModel.Email, userModel.Password);

                if (user == null)
                {
                    return Unauthorized("Invalid email or password");
                }

                var token = CreateToken(user);

                return Ok(token);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        [HttpGet("me")]
        public ActionResult<object> GetUserInfo()
        {
            string token = HttpContext.Request.Headers["Authorization"];

            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized(new { auth = false, message = "No token provided." });
            }

            try
            {
                // Remova o prefixo "Bearer " do token
                var jwt = token.Replace("Bearer ", string.Empty);

                // Use o novo namespace para JwtSecurityToken
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(jwt) as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;

                if (jsonToken == null)
                {
                    return Unauthorized(new { auth = false, message = "Invalid token." });
                }

                // Simplifique a extração das reivindicações diretamente do token
                var userId = jsonToken.Claims.FirstOrDefault(claim => claim.Type == "id")?.Value;
                var userType = jsonToken.Claims.FirstOrDefault(claim => claim.Type == "userType")?.Value;
                var userEmail = jsonToken.Claims.FirstOrDefault(claim => claim.Type == "email")?.Value;
                var userFirstName = jsonToken.Claims.FirstOrDefault(claim => claim.Type == "firstName")?.Value;
                var userLastName = jsonToken.Claims.FirstOrDefault(claim => claim.Type == "lastName")?.Value;
                var userAvatar = jsonToken.Claims.FirstOrDefault(claim => claim.Type == "avatar")?.Value;

                // Verifique se as reivindicações necessárias estão presentes
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userType))
                {
                    return BadRequest(new { auth = false, message = "Invalid token claims." });
                }

                var userInfo = new
                {
                    Id = int.Parse(userId),
                    UserType = userType,
                    Email = userEmail,
                    FirsName = userFirstName,
                    LastName = userLastName,
                    Avatar = userAvatar,
                };

                return Ok(new { auth = true, userInfo });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetUserInfo: {ex.Message}");
                return StatusCode(500, new { auth = false, message = $"Internal Server Error: {ex.Message}" });
            }
        }

        [HttpPost("passwordReset")]
        public async Task<ActionResult> RequestPasswordReset([FromBody] EmailRequest emailModel)
        {
            try
            {
                // Verifique se o email fornecido é válido
                var emailValidator = new EmailAddressAttribute();
                if (!emailValidator.IsValid(emailModel.Email))
                {
                    return BadRequest("Invalid email format.");
                }

                // Encontre o usuário pelo email
                var user = _context.Users.FirstOrDefault(u => u.Email == emailModel.Email);

                if (user == null)
                {
                    // Usuário não encontrado pelo email
                    return BadRequest("User not found.");
                }

                // Crie ou obtenha um token para o usuário
                var token = _context.Tokens.FirstOrDefault(t => t.UserId == user.Id) ?? new Token
                {
                    UserId = user.Id,
                    TokenValue = Guid.NewGuid().ToString(), // Use um método seguro para gerar tokens
                    CreatedAt = DateTime.UtcNow
                };

                // Defina a data de expiração do token (por exemplo, 1 hora a partir de agora)
                token.ExpirationDate = token.CreatedAt.AddHours(1);

                // Salve o token no banco de dados
                if (token.Id == 0)
                {
                    _context.Tokens.Add(token);
                }

                await _context.SaveChangesAsync();

                // Envie o link de redefinição de senha por e-mail
                var resetLink = $"{_configuration["AppBaseUrl"]}/passwordReset/{user.Id}/{token.TokenValue}";
                await SendPasswordResetEmail(user.Email, resetLink);

                return Ok("Password reset link sent to your email account!");
            }
            catch (Exception ex)
            {
                // Log ou manipulação de erro adequada
                Console.WriteLine($"An error occurred: {ex.Message}");
                return StatusCode(500, "An error occurred.");
            }
        }

        [HttpPost("passwordReset/{userId}/{token}")]
        public async Task<ActionResult> ResetPassword(int userId, string token, [FromBody] PasswordReset resetModel)
        {
            try
            {
                // Encontre o usuário pelo ID
                var user = _context.Users.FirstOrDefault(u => u.Id == userId);

                if (user == null)
                {
                    return BadRequest("Invalid user ID.");
                }

                // Encontre o token correspondente ao usuário
                var tokenEntity = _context.Tokens.FirstOrDefault(t => t.UserId == userId && t.TokenValue == token);

                if (tokenEntity == null || DateTime.UtcNow > tokenEntity.ExpirationDate)
                {
                    return BadRequest("Invalid or expired token.");
                }

                // Defina a nova senha para o usuário
                user.Password = HashPassword(resetModel.Password);

                // Remova o token após a redefinição da senha
                _context.Tokens.Remove(tokenEntity);

                // Salve as alterações no banco de dados
                await _context.SaveChangesAsync();

                return Ok("Password reset successfully.");
            }
            catch (Exception ex)
            {
                // Log ou manipulação de erro adequada
                Console.WriteLine($"An error occurred: {ex.Message}");
                return StatusCode(500, "An error occurred.");
            }
        }

        private string HashPassword(string password)
        {
            // Gere um salt usando um algoritmo de geração de salt seguro
            byte[] salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Use o algoritmo PBKDF2 para derivar a senha
            string hashed = Convert.ToBase64String(
                KeyDerivation.Pbkdf2(
                    password: password,
                    salt: salt,
                    prf: KeyDerivationPrf.HMACSHA256,
                    iterationCount: 10000,
                    numBytesRequested: 256 / 8
                )
            );

            // Combine o salt e a senha derivada em uma única string
            string combinedHash = $"{Convert.ToBase64String(salt)}:{hashed}";

            return combinedHash;
        }

        private bool ComparePassword(string password, string hashedPassword)
        {
            // Extraia o salt e a senha derivada da string combinada
            string[] parts = hashedPassword.Split(':');
            if (parts.Length != 2)
            {
                // A string combinada não tem o formato esperado
                return false;
            }

            byte[] salt = Convert.FromBase64String(parts[0]);
            string storedHash = parts[1];

            // Use o algoritmo PBKDF2 para derivar a senha fornecida
            string enteredHash = Convert.ToBase64String(
                KeyDerivation.Pbkdf2(
                    password: password,
                    salt: salt,
                    prf: KeyDerivationPrf.HMACSHA256,
                    iterationCount: 10000,
                    numBytesRequested: 256 / 8
                )
            );

            // Compare os hashes
            return storedHash.Equals(enteredHash);
        }

        private User FindUser(string email, string password)
        {
            // Encontre o usuário pelo email
            var user = _context.Users.SingleOrDefault(u => u.Email == email);

            if (user == null)
            {
                // Usuário não encontrado pelo email
                return null;
            }

            // Compare a senha fornecida com a senha armazenada
            if (!ComparePassword(password, user.Password))
            {
                // Senha incorreta
                return null;
            }

            return user;
        }

        // Função para criar um token JWT
        private string CreateToken(User user)
        {
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
            new Claim("id", user.Id.ToString()),
            new Claim("userType", user.UserType.ToString()),
            new Claim("email", user.Email),
            new Claim("firstName", user.FirsName),
            new Claim("lastName", user.LastName),
            new Claim("avatar", user.Avatar),
        }),
                NotBefore = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtSecret)), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            var tokenString = tokenHandler.WriteToken(token);
            SaveTokenDetails(user.Id, tokenString, tokenDescriptor.Expires.GetValueOrDefault());

            return tokenHandler.WriteToken(token);
        }

        [NonAction]
        public ClaimsPrincipal VerifyToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_jwtSecret);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    RequireExpirationTime = true,
                    ValidateLifetime = true
                };

                Console.WriteLine($"Received Token: {token}");

                // A diferença está aqui: apenas valide o token, não precisa do token validado.
                var principal = tokenHandler.ValidateToken(token, validationParameters, out _);

                Console.WriteLine($"Token Validated. Subject: {principal.Identity.Name}");

                return principal;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error validating token: {ex.Message}");
                return null;
            }
        }

        private string GenerateRandomSecret()
        {
            // Gera uma chave secreta aleatória de 64 caracteres
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, 64)
                .Select(s => s[new Random().Next(s.Length)]).ToArray());
        }

        private async Task SendPasswordResetEmail(string userEmail, string resetLink)
        {
            try
            {
                using (var client = new SmtpClient())
                {
                    client.Host = "smtp.gmail.com";
                    client.Port = 587;
                    client.EnableSsl = true;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential("NestQuestLDS@gmail.com", "znuy ahog dnlw zuit");
                    client.Timeout = 10000;

                    Console.WriteLine($"SmtpClient configured: Port {client.Port}, EnableSsl {client.EnableSsl}");

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress("NestQuestLDS@gmail.com"),
                        Subject = "Password Reset",
                        Body = $"Click the following link to reset your password: http://localhost:3000{resetLink}",
                        IsBodyHtml = false
                    };

                    mailMessage.To.Add(userEmail);

                    Console.WriteLine($"Sending password reset email to {userEmail}");

                    await client.SendMailAsync(mailMessage);

                    Console.WriteLine($"Password reset email sent successfully to {userEmail}");
                }
            }
            catch (Exception ex)
            {
                // Log ou manipulação de erro adequada
                Console.WriteLine($"Error sending password reset email to {userEmail}: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
            }
        }


        private void SaveTokenDetails(int userId, string token, DateTime expirationDate)
        {
            var tokenDetails = new Token
            {
                UserId = userId,
                TokenValue = token,
                ExpirationDate = expirationDate,
                CreatedAt = DateTime.UtcNow,
            };

            _context.Tokens.Add(tokenDetails);
            _context.SaveChanges();
        }

        private bool UserExists(int id)
        {
            return (_context.Users?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
