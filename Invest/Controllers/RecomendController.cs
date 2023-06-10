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

        public async Task<IActionResult> Create(int sum, int months, int sharesPercent, int bondsPercent)
        {
            List<StockInfo> shares, bonds;

            DateTime currentDate = DateTime.Now.Date,
                startDate = currentDate.AddDays(-366);
            const int DaysInYear = 366;

            var predictions = new Dictionary<string, float>();
            Dictionary<string, float> sortedPredictions;
            List<string> securities;

            if (sharesPercent > 0) //АСКО - последняя цена null !!!
            {
                var market = "shares/boards/TQBR";
                securities = await GetAllSecurities(market);
                if (securities.Count > 0)
                {
                    for (int i = 0; i < 1; i++) //each (var sec in securities)   securities.Count
                    {
                        var stocks = await GetSecurityData(market, securities[i], startDate, DaysInYear);//StockController.GetChartDataAsync(securities[i], market);
                        var lastPrice = stocks.Last().Value;
                        var forecastedPrices = StockController.ForecastTimeSeries(stocks, months * 30).ForecastedPrices;
                        var length = forecastedPrices.Length;
                        var percentChange = forecastedPrices[length - 1] / lastPrice - 100;
                        predictions.Add(securities[i], percentChange);
                    }
                    sortedPredictions = predictions.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
                }
                else
                {
                    ViewData["Error"] = $"https://iss.moex.com/iss/engines/stock/markets/{market}/securities.json?iss.meta=off&iss.only=securities&securities.columns=SECID";
                }
            }
            if (bondsPercent > 0)
            {
                var market = "bonds/boards/TQCB";
                securities = await GetAllSecurities(market);

            }

            return View(new RecomendModel() { Prediction = predictions});
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

                var newDates = await StockController.GetDatesAsync(secid, market, stringStartDate, stringEndDate);
                var newPrices = await StockController.GetPricesAsync(secid, market, stringStartDate, stringEndDate);

                dates.AddRange(newDates);
                prices.AddRange(newPrices);
                startDate = endDate.AddDays(1);
            }
            return AlgorithmController.DataToStock(dates, prices);
        }

        private static async Task<List<string>> GetAllSecurities(string market) // без ошибок
        {
            string url = $"https://iss.moex.com/iss/engines/stock/markets/{market}/securities.json?iss.meta=off&iss.only=securities&securities.columns=SECID";
            var jsonSec = await _httpClient.GetFromJsonAsync<Root>(url);
            //List<string> securities = null;
            var securities = new List<string>();
            if (jsonSec != null)
            {
                for (int i = 0; i < jsonSec.securities.data.Count; i++)
                {
                    securities.Add(jsonSec.securities.data[i][0]);
                    //try
                    //{
                        
                    //}
                    //catch
                    //{
                    //    return null;
                    //}
                }
            }

            return securities;
        }
    }
}
