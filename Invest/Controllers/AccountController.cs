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
        public IActionResult SignUp() => View(new RegisterViewModel());

        [HttpPost]
        public async Task<IActionResult> SignUp(RegisterViewModel model)
        {
            if (model.Email == null || model.Name == null || model.Password == null || model.PasswordConfirm == null)
            {
                ViewData["Error"] = "Не все поля заполнены!";
                return View(model);
            }
            if (model.Password != model.PasswordConfirm)
            {
                ViewData["Error"] = "Пароли не совпадают!";
                return View(model);
            }

            var foundUser = GetUserByEmail(model.Email);//await _context.Users.FindAsync(model.Login);// (u => u.Login == user.Login);
            if (foundUser != null)
            {
                ViewData["Error"] = "Пользователь с данной почтой уже существует!";
                return View(model);
            }

            User newUser = new User()
            {
                Email = model.Email, // добавлено
                Name = model.Name,
                Role = Role.User,
                Password = HashPassword(model.Password)
            };
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            var claimsIdentity = AuthenticateByEmail(newUser.Email);
            await HttpContext.SignInAsync("Cookies", new ClaimsPrincipal(claimsIdentity));

            return RedirectToAction("Settings");
        }

        [NonAction]
        private ClaimsIdentity AuthenticateByEmail(string email)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimsIdentity.DefaultNameClaimType, email),
                //new Claim(ClaimsIdentity.DefaultRoleClaimType, user.Role.ToString())
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
        public async Task<IActionResult> Login(string email, string password)
        {
            if (email == null || password == null)
            {
                ViewData["Error"] = "Не все поля заполнены!";
                return View(new object[] { email, password });
            }
            User user = GetUserByEmail(email);
            if (user == null)
            {
                ViewData["Error"] = "Пользователя с данной почтой не существует!";
                return View(new object[] { email, password });
            }
            if (HashPassword(password) != user.Password)
            {
                ViewData["Error"] = "Неверный пароль!";
                return View(new object[] { email, password });
            }
            var claimsIdentity = AuthenticateByEmail(user.Email);
            await Request.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
            return RedirectToAction("Settings");
        }

        [NonAction]
        private User GetUserById(int id)
        {
            //User user = users.Find(x => x.Id == id);

            User user = _context.Users.Find(id);
            return user;
        }

        [NonAction]
        private User GetUserByEmail(string email)
        {
            //User user = users.Find(x => x.Login == login);
            //User user = await _context.Users.FindAsync(id);

            User user = _context.Users.FirstOrDefault(x => x.Email == email);
            return user;
        }

        [Authorize]
        public async Task<IActionResult> Settings(RegisterViewModel model)
        {
            var identityEmail = User.Identity.Name;
            User dbUser = await _context.Users.FirstOrDefaultAsync(x => x.Email == identityEmail);
            //var userId = dbUser.Id;

            if (model.Email == null)
            {
                return View(new RegisterViewModel() { Email = dbUser.Email, Name = dbUser.Name });
            }
            if (model.Email != dbUser.Email) 
            {
                dbUser.Email = model.Email;

                await HttpContext.SignOutAsync("Cookies");
                var claimsIdentity = AuthenticateByEmail(model.Email);
                await HttpContext.SignInAsync("Cookies", new ClaimsPrincipal(claimsIdentity));
            }
            if (model.Name != dbUser.Name)
            {
                dbUser.Name = model.Name;
            }
            if (model.Password != null)
            {
                ViewData["Color"] = "red";
                if (model.CurrentPassword == null || model.PasswordConfirm == null)
                {
                    ViewData["Error"] = "Не все поля для изменения пароля заполнены";
                    return View(model);
                }
                if (model.CurrentPassword != dbUser.Password)
                {
                    ViewData["Error"] = "Текущий пароль введен неверно";
                    return View(model);
                }
                if (model.Password != model.PasswordConfirm)
                {
                    ViewData["Error"] = "Пароли не совпадают";
                    return View(model);
                }
                dbUser.Password = model.Password;
            }
            await _context.SaveChangesAsync();
            ViewData["Error"] = "Данные обновлены";
            ViewData["Color"] = "black";

            return View(model);
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("Cookies");
            return RedirectToAction("Shares", "Stock");
        }


        [Authorize]
        public async Task<IActionResult> Portfolio()
        {
            var identityEmail = User.Identity.Name;
            var dbUser = await _context.Users.FirstOrDefaultAsync(x => x.Email == identityEmail);
            var stocks = await _context.UserStocks
                .Where(u => u.UserId == dbUser.Id)
                .ToListAsync();
            return View(stocks);
        }
    }
}
