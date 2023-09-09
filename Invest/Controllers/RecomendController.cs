using Invest.JsonClasses;
using Invest.Models;
using Microsoft.AspNetCore.Mvc;

namespace Invest.Controllers
{
    public class RecomendController : Controller
    {
        private static HttpClient _httpClient = new HttpClient();

        public async Task<IActionResult> Generate(int investSum, int months, int sharesPercent, int bondsPercent)
        {
            if (investSum == 0)
            {
                return View(new RecomendView()
                {
                    InvestSum = 1000,
                    Months = 12,
                    SharesPercent = 50,
                    BondsPercent = 50,
                    SharesList = new List<RecomendTableData>(),
                    BondsList = new List<RecomendTableData>()
                });
            }

            var shares = await FormPortfolio(investSum, months, sharesPercent, StockController.sharesMarket);
            var bonds = await FormPortfolio(investSum, months, bondsPercent, StockController.bondsMarket);
            return View(new RecomendView() { InvestSum = investSum, Months = months, SharesPercent = sharesPercent,
                BondsPercent = bondsPercent, SharesList = shares, BondsList = bonds });
        }

        private static async Task<List<RecomendTableData>> FormPortfolio(int investSum, int months, int percent, string market)
        {
            var portfolio = new List<RecomendTableData>();
            if (percent > 0)
            {
                float stockSum = (float)percent * investSum / 100;
                var securities = await GetAllMarketSecuritiesAsync(market);
                if (securities.Count > 0)
                {
                    portfolio = await GetRecomendsAsync(securities, market, stockSum, DateTime.Now.AddDays(-366).Date, DateTime.Now.Date, months * 30);
                }
            }
            return portfolio;
        }

        public static async Task<List<string>> GetAllMarketSecuritiesAsync(string market)
        {
            string url = $"https://iss.moex.com/iss/engines/stock/markets/{market}/securities.json?iss.meta=off&iss.only=securities&securities.columns=SECID";
            var jsonSec = await _httpClient.GetFromJsonAsync<StaticData>(url);
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

        private static async Task<List<RecomendTableData>> GetRecomendsAsync(List<string> securities, string market, float recomendSum, DateTime startDate, DateTime endDate, int horizon)
        {
            var data = await GetSecuritiesSortedByIncome(securities, market, startDate, endDate, horizon);
            var result = FormRecomendTableAsync(data, recomendSum);
            return result;
        }

        private static async Task<List<RecomendTableData>> GetSecuritiesSortedByIncome(List<string> securities, string market, DateTime startDate, DateTime endDate, int forecastHorizon)
        {
            var data = new List<RecomendTableData>();
            for (int i = 0; i < securities.Count && i < 60; i++)
            {
                SecurityHistory securityData = await StockController.ParseSecurityDataAsync(securities[i], market, startDate, endDate);

                if (securityData.Prices.Count > 14) // training size is greater than twice the window size(=7)
                {
                    var stocks = AlgorithmController.ConvertSecurityDataToStockList(securityData);
                    var lastPrice = stocks.Last().Value;
                    var forecastedPrices = StockController.ForecastStocks(stocks, forecastHorizon).ForecastedPrices;

                    var forecastedChange = forecastedPrices[forecastHorizon - 1] - lastPrice;
                    data.Add(new RecomendTableData()
                    {
                        SecId = securities[i],
                        LastPrice = lastPrice,
                        ForecastedPrice = forecastedPrices[forecastHorizon - 1],
                        Income = forecastedChange,
                    });
                }
                else
                    continue;
            }
            data.Sort((x, y) => y.Income.CompareTo(x.Income));

            return data;
        }

        private static List<RecomendTableData> FormRecomendTableAsync(List<RecomendTableData> data, float recomendSum)
        {
            var result = new List<RecomendTableData>();
            float totalSum = 0,
                minRecomendSum = 0.9f * recomendSum,
                maxRecomendSum = 1.1f * recomendSum;
            foreach (var row in data)
            {
                if (row.Income > 1)
                {
                    int amount = (int)Math.Ceiling(recomendSum / row.LastPrice);

                    if (amount == 1)
                    {
                        if (totalSum + row.LastPrice <= maxRecomendSum)
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
                    if (totalSum >= minRecomendSum)
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
    }
}
