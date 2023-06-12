using Microsoft.AspNetCore.Mvc;

namespace Invest.Controllers
{
    //[Route("api/[controller]")]
    //[ApiController]
    public class UserStockController : ControllerBase
    {
        private readonly DataContext _context;
        public UserStockController(DataContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<UserStock>>> GetAllUserStocks()
        {
            return Ok(await _context.UserStocks.ToListAsync());
        }

        [HttpGet("{userId}, {secId}")]
        public async Task<ActionResult<List<UserStock>>> GetStock(int userId, string secId)
        {
            return Ok(await _context.UserStocks.FindAsync(userId, secId));
        }

        [HttpGet("{userId}")]
        public async Task<ActionResult<List<UserStock>>> GetStockByUserId(int userId)
        {
            var dbUser = await _context.Users.FindAsync(userId);
            if (dbUser == null)
            {
                return BadRequest("User not found");
            }
            var stocks = await _context.UserStocks
                .Where(u => u.UserId == userId)
                .ToListAsync();
            return Ok(stocks);
        }

        [HttpPost]
        public async Task<ActionResult<List<UserStock>>> AddUserStock(UserStock userStock)
        {
            var dbUser = await _context.Users.FindAsync(userStock.UserId);
            if (dbUser == null)
            {
                return BadRequest("User not found");
            }
            _context.UserStocks.Add(userStock);
            await _context.SaveChangesAsync();
            var stocks = await _context.UserStocks
                .Where(u => u.UserId == userStock.UserId)
                .ToListAsync();
            return Ok(stocks);
        }

        [HttpPut]
        public async Task<ActionResult<UserStock>> UpdateUserStock(UserStock userStock)
        {
            var dbStock = await _context.UserStocks.FindAsync(userStock.UserId, userStock.SecId);
            if (dbStock == null)
            {
                return BadRequest("Stock not found");
            }
            dbStock.Quantity = userStock.Quantity;
            dbStock.PurchasePrice = userStock.PurchasePrice;
            await _context.SaveChangesAsync();
            return Ok(await _context.UserStocks.FindAsync(userStock.UserId, userStock.SecId));
        }

        [HttpDelete("{userId}, {secId}")]
        public async Task<ActionResult<List<UserStock>>> DeleteUserStock(int userId, string secId)
        {
            var dbStock = await _context.UserStocks.FindAsync(userId, secId);
            if (dbStock == null)
            {
                return BadRequest("Stock not found");
            }
            _context.UserStocks.Remove(dbStock);
            await _context.SaveChangesAsync();
            return Ok(await _context.UserStocks.ToListAsync());
        }
    }
}
