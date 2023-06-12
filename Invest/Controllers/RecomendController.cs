using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net.Http;

namespace Invest.Controllers
{
    public class RecomendController : Controller
    {
        private static HttpClient _httpClient = new HttpClient();
        private readonly DataContext _context;

        public RecomendController(DataContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Create(int investSum, int months, int sharesPercent, int bondsPercent, string addStocks)
        {
            if (investSum == 0)
            {
                return View(new RecomendModel()
                {
                    InvestSum = 1000,
                    Months = 12,
                    SharesPercent = 50,
                    BondsPercent = 50,
                    SharesList = new List<RecomendData>(),
                    BondsList = new List<RecomendData>()
                });
            }

            const int DaysInYear = 366;
            DateTime currentDate = DateTime.Now.Date,
                startDate = currentDate.AddDays(-DaysInYear);

            var shares = new List<RecomendData>();
            var bonds = new List<RecomendData>();
            float sharesSum = 0, bondsSum;

            if (sharesPercent > 0)
            {
                sharesSum = (float)sharesPercent * investSum / 100;
                ViewData["Error"] = sharesPercent.ToString();
                ViewData["Error1"] = (investSum).ToString();
                var market = "shares/boards/TQBR";
                var securities = await GetAllSecurities(market);
                if (securities.Count > 0)
                {
                    shares = await GetRecomendTableAsync(securities, market, sharesSum, startDate, DaysInYear, months * 30);
                }
            }
            if (bondsPercent > 0)
            {
                bondsSum = investSum - sharesSum;
                var market = "bonds/boards/TQCB";
                var securities = await GetAllSecurities(market);
                if (securities.Count > 0)
                {
                    bonds = await GetRecomendTableAsync(securities, market, bondsSum, startDate, DaysInYear, months * 30);
                }
            }
            if (addStocks != null)
            {
                foreach (var share in shares)
                {
                    var stockInfo = new StockInfo()
                    {
                        SecId = share.SecId,
                        LastPrice = share.LastPrice,
                        Amount = share.Amount
                    };
                    //AddUserStock(stockInfo);
                }
                foreach (var bond in bonds)
                {

                }
            }

            return View(new RecomendModel() { InvestSum = investSum, Months = months, SharesPercent = sharesPercent,
                BondsPercent = bondsPercent, SharesList = shares, BondsList = bonds });
        }

        [NonAction]
        public async void AddUserStock(StockInfo info)
        {
            var dbUser = await _context.Users.FirstOrDefaultAsync(x => x.Email == User.Identity.Name);
            if (dbUser != null)
            {
                var dbStock = await _context.UserStocks.FindAsync(dbUser.Id, info.SecId);
                if (dbStock != null)
                {
                    var prevSum = dbStock.PurchasePrice * dbStock.Quantity;
                    var curSum = info.LastPrice * info.Amount;
                    dbStock.Quantity += info.Amount;
                    dbStock.PurchasePrice = (float)Math.Round((prevSum + curSum) / dbStock.Quantity, 2);
                }
                else
                {
                    UserStock newStock = new UserStock()
                    {
                        User = dbUser,
                        SecName = info.SecName,
                        SecId = info.SecId,
                        Quantity = info.Amount,
                        PurchasePrice = info.LastPrice
                    };
                    _context.UserStocks.Add(newStock);
                }
                await _context.SaveChangesAsync();
            }
        }

        private static async Task<List<RecomendData>> GetRecomendTableAsync(List<string> securities, string market, float recomendSum, DateTime startDate, int daysToParse, int horizon)
        {
            var data = new List<RecomendData>();
            for (int i = 0; i < 15; i++) //50   each (var sec in securities)   securities.Count
            {
                var stocks = await GetSecurityData(market, securities[i], startDate, daysToParse);
                if (stocks.Count > 0)
                {
                    var lastPrice = stocks.Last().Value;
                    //var annualChange = lastPrice - stocks[0].Value;

                    var forecastedPrices = StockController.ForecastTimeSeries(stocks, horizon).ForecastedPrices;

                    var forecastedChange = forecastedPrices[horizon - 1] - lastPrice;
                    data.Add(new RecomendData()
                    {
                        SecId = securities[i],
                        LastPrice = lastPrice,
                        ForecastedPrice = forecastedPrices[horizon - 1],
                        Income = forecastedChange,
                    });
                }
                else
                    continue;
            }
            data.Sort((x, y) => y.Income.CompareTo(x.Income));

            var result = new List<RecomendData>();
            float totalSum = 0;
            float minRecomendSum = 0.9f * recomendSum,
                maxRecomendSum = 1.1f * recomendSum;
            foreach (var row in data)
            {
                if (row.Income > 0)// && row.AnnualChange > 0)
                {
                    int amount = (int)Math.Ceiling(recomendSum / row.LastPrice);

                    if (amount == 1)
                    {
                        if (minRecomendSum <= totalSum + row.LastPrice && totalSum + row.LastPrice <= maxRecomendSum)
                            amount = 1;
                        else
                            continue;
                    }
                    else if (row.LastPrice * amount > maxRecomendSum)
                    {
                        amount--;
                    }
                    
                    float buySum = row.LastPrice * amount;
                    row.Amount = amount;
                    row.Buy = (float)Math.Round(buySum, 2);
                    row.Sale = (float)Math.Round(amount * row.ForecastedPrice, 2);
                    row.TotalIncome = (float)Math.Round(row.Sale - row.Buy, 2);
                    result.Add(row);

                    totalSum += buySum;
                    if (totalSum > minRecomendSum)
                    {
                        break;
                    }
                    recomendSum -= totalSum;
                }
                else
                    continue;
            }
            return result;
        }

        private static async Task<List<Stock>> GetSecurityData(string market, string secid, DateTime startDate, int daysToParse)
        {
            var dates = new List<DateTime>();
            var prices = new List<double>();
            DateTime endDate;

            for (int k = 0; k < 3; k++)
            {
                endDate = startDate.AddDays(daysToParse / 3 - 1);
                string stringStartDate = $"{startDate.Year}-{startDate.Month}-{startDate.Day}",
                    stringEndDate = $"{endDate.Year}-{endDate.Month}-{endDate.Day}";

                var newPrices = await StockController.GetPricesAsync(secid, market, stringStartDate, stringEndDate);
                if (newPrices.Count > 0)
                {
                    var newDates = await StockController.GetDatesAsync(secid, market, stringStartDate, stringEndDate);

                    dates.AddRange(newDates);
                    prices.AddRange(newPrices);
                    startDate = endDate.AddDays(1);
                }
                else
                    break;
            }
            return AlgorithmController.DataToStock(dates, prices);
        }

        private static async Task<List<string>> GetAllSecurities(string market) // без ошибок
        {
            string url = $"https://iss.moex.com/iss/engines/stock/markets/{market}/securities.json?iss.meta=off&iss.only=securities&securities.columns=SECID";
            var jsonSec = await _httpClient.GetFromJsonAsync<StringStaticData>(url);
            var securities = new List<string>();
            if (jsonSec != null)
            {
                for (int i = 0; i < jsonSec.securities.data.Count; i++)
                {
                    securities.Add(jsonSec.securities.data[i][0]);
                }
            }
            return securities;
        }
    }
}
