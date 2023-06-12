using Azure.Core;
using Microsoft.AspNetCore.Mvc;

namespace Invest.Controllers
{
    //[Route("api/[controller]")]
    //[ApiController]
    public class UserController : ControllerBase
    {
        private readonly DataContext _context;
        public UserController(DataContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<User>>> GetAllUser()
        {
            return Ok(await _context.Users.ToListAsync());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<List<User>>> GetUserById(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return BadRequest("User not found");
            }
            return Ok(user);
        }

        [HttpPost]
        public async Task<ActionResult<List<User>>> AddUser(User user)
        {
            user.Password = AccountController.HashPassword(user.Password);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Ok(await _context.Users.ToListAsync());
        }

        [HttpPut]
        public async Task<ActionResult<User>> UpdateUser(User request)
        {
            var dbUser = await _context.Users.FindAsync(request.Id);
            if (dbUser == null)
            {
                return BadRequest("User not found");
            }
            dbUser.Name = request.Name;
            dbUser.Email = request.Email;
            dbUser.Password = AccountController.HashPassword(request.Password);
            await _context.SaveChangesAsync();
            return Ok(await _context.Users.FindAsync(request.Id));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<List<User>>> DeleteUser(int id)
        {
            var dbUser = await _context.Users.FindAsync(id);
            if (dbUser == null)
            {
                return BadRequest("User not found");
            }
            _context.Users.Remove(dbUser);
            await _context.SaveChangesAsync();
            return Ok(await _context.Users.ToListAsync());
        }
    }
}
