using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using System.Security.Cryptography;

namespace Invest.Controllers
{
    public class AccountController : Controller
    {
        private readonly DataContext _context;
        public AccountController(DataContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Register() => View(new RegisterViewModel());

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (model.Login == null || model.Name == null || model.Password == null || model.PasswordConfirm == null)
            {
                ViewData["Error"] = "Не все обязательные поля заполнены";
                return View(model);
            }

            var foundUser = GetUserByLogin(model.Login);//await _context.Users.FindAsync(model.Login);// (u => u.Login == user.Login);
            if (foundUser != null)
            {
                ViewData["Error"] = "Пользователь с этим логином уже существует";
                return View(model);
            }

            User newUser = new User()
            {
                Name = model.Name,
                Role = Role.User,
                Password = HashPassword(model.Password)
            };
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            var claim = Authenticate(newUser); //BaseResponse<ClaimsIdentity>
            await HttpContext.SignInAsync("Cookies", new ClaimsPrincipal(claim));

            return RedirectToAction("Profile");
        }

        private ClaimsIdentity Authenticate(User user)
        {
            var claims = new List<Claim>
            {
                //new Claim(ClaimsIdentity.DefaultNameClaimType, user.Login),
                new Claim(ClaimsIdentity.DefaultRoleClaimType, user.Role.ToString())
            };
            return new ClaimsIdentity(claims, "ApplicationCookie",
                ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType); //?
        }

        [NonAction]
        public static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var hash = BitConverter.ToString(hashedBytes);//.Replace("-", "").ToLower();
                return hash;
            }
        }


        [HttpGet]
        public IActionResult Login()
        {
            return View(new object[2]);
        }

        [HttpPost]
        public async Task<IActionResult> Login(string login, string password)
        {
            if (login == null || password == null)
            {
                ViewData["Error"] = "fill empty fields";
            }
            else
            {
                User user = GetUserByLogin(login);
                if (user == null)
                {
                    ViewData["Error"] = "not found";
                }
                else if (HashPassword(password) != user.Password)
                {
                    ViewData["Error"] = "wrong pass";
                }
                else
                {
                    var claim = Authenticate(user);
                    await Request.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claim));

                    return RedirectToAction("Profile");
                }
            }
            return View(new object[] { login, password });
        }

        private User GetUserById(int id)
        {
            //User user = users.Find(x => x.Id == id);

            User user = _context.Users.Find(id);
            return user;
        }

        private User GetUserByLogin(string email)
        {
            //User user = users.Find(x => x.Login == login);
            //User user = await _context.Users.FindAsync(id);

            User user = _context.Users.FirstOrDefault(x => x.Email == email);
            return user;
        }

        [HttpGet]
        public IActionResult Profile() => View();

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("Cookies");
            return RedirectToAction("Index", "Home");
        }
    }
}
