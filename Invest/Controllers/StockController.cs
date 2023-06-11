using Microsoft.AspNetCore.Mvc;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;
using Microsoft.ML.Data;

namespace Invest.Controllers
{
    public class StockController : Controller
    {
        private readonly ILogger<StockController> _logger;
        private static HttpClient _httpClient = new HttpClient();
        private readonly DataContext _context;

        public StockController(ILogger<StockController> logger, DataContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Shares(string id)
        {
            string market = "shares/boards/TQBR";

            if (id != null)
            {
                ChartData chartData = await GetChartDataAsync(id, market);
                return View("Chart", chartData);
            }
            return View("StockList", "shares/boards/TQBR");
        }

        [HttpPost]
        public async Task<IActionResult> Shares(string secId, string secName, float lastPrice, int amount)  // UserStock
        {
            var identityEmail = User.Identity.Name;
            var dbUser = await _context.Users.FirstOrDefaultAsync(x => x.Email == identityEmail);
            if (dbUser != null)
            {
                var dbStock = await _context.UserStocks.FindAsync(dbUser.Id, secId);
                if (dbStock != null)
                {
                    var prevSum = dbStock.PurchasePrice * dbStock.Quantity;
                    var curSum = lastPrice * amount;
                    dbStock.Quantity += amount;
                    dbStock.PurchasePrice = (float)Math.Round((prevSum + curSum) / dbStock.Quantity, 2);
                }
                else
                {
                    //ViewBag.Info = secId;
                    UserStock newStock = new UserStock()
                    {
                        User = dbUser,
                        SecName = secName,
                        SecId = secId,
                        Quantity = amount,
                        PurchasePrice = lastPrice
                    };
                    _context.UserStocks.Add(newStock);
                }
                await _context.SaveChangesAsync();
            }
            return await Shares(secId);
        }

        public async Task<IActionResult> Bonds(string id)
        {
            string market = "bonds/boards/TQCB";

            if (id != null)
            {
                ChartData chartData = await GetChartDataAsync(id, market);
                return View("Chart", chartData);
            }
            return View("StockList", market);
        }

        public static async Task<ChartData> GetChartDataAsync(string id, string market)
        {
            var dates = new List<DateTime>();
            var prices = new List<double>();
            ChartData chartData = new ChartData();

            DateTime currentDate = DateTime.Now.Date,
                startDate = currentDate.AddDays(-366),
                endDate;

            for (int k = 0; k < 3; k++)
            {
                endDate = startDate.AddDays(366 / 3 - 1);
                string stringStartDate = $"{startDate.Year}-{startDate.Month}-{startDate.Day}",
                    stringEndDate = $"{endDate.Year}-{endDate.Month}-{endDate.Day}";

                var newPrices = await GetPricesAsync(id, market, stringStartDate, stringEndDate);
                if (newPrices.Count > 0)
                {
                    var newDates = await GetDatesAsync(id, market, stringStartDate, stringEndDate);
                    if (k == 2)
                    {
                        var stockInfo = await GetSecurityDataAsync(market);
                        string secName = "";
                        float lastPrice = 0, lastChange = 0;
                        foreach (var info in stockInfo)
                        {
                            if (info.SecId == id)
                            {
                                secName = info.SecName;
                                lastPrice = info.LastPrice;
                                lastChange = info.LastChange;
                                break;
                            }
                        }
                        chartData = new ChartData()
                        {
                            SecId = id,
                            SecName = secName,
                            LastPrice = lastPrice,
                            LastChange = lastChange,
                            Amount = 1,
                            Dates = newDates,
                            Prices = newPrices,
                            ForecastedPrices = new List<double>(newPrices)
                        };
                    }
                    dates.AddRange(newDates);
                    prices.AddRange(newPrices);
                    startDate = endDate.AddDays(1);

                }
                else
                    return chartData;
            }
            StockPrediction? prediction = ForecastTimeSeries(AlgorithmController.DataToStock(dates, prices), 30);
            AddPredictionToChartData(prediction, ref chartData, dates[dates.Count - 1]);
            return chartData;
        }

        [NonAction]
        public static async Task<List<double>> GetPricesAsync(string id, string market, string startDate, string endDate)
        {
            var url = $"https://iss.moex.com/iss/history/engines/stock/markets/{market}/securities/{id}.json?iss.meta=off&history.columns=OPEN&from={startDate}&till={endDate}";
            var jsonPrices = await _httpClient.GetFromJsonAsync<DoubleHistoryData>(url);
            var prices = new List<double>();
            if (jsonPrices != null)
            {
                for (int i = 0; i < jsonPrices.history.data.Count; i++)
                {
                    if (jsonPrices.history.data[i][0].HasValue)
                    {
                        prices.Add(jsonPrices.history.data[i][0].Value);
                    }
                    else
                    {
                        return new List<double>();
                    }
                }
            }
            return prices;
        }

        [NonAction]
        public static async Task<List<DateTime>> GetDatesAsync(string id, string market, string startDate, string endDate)
        {
            var url = $"https://iss.moex.com/iss/history/engines/stock/markets/{market}/securities/{id}.json?iss.meta=off&history.columns=TRADEDATE&from={startDate}&till={endDate}";
            var jsonDates = await _httpClient.GetFromJsonAsync<StringHistoryData>(url);
            List<DateTime> dates = new List<DateTime>();
            if (jsonDates != null)
            {
                for (int i = 0; i < jsonDates.history.data.Count; i++)
                {
                    dates.Add(DateTime.ParseExact(jsonDates.history.data[i][0], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
                }
            }
            return dates;
        }

        [NonAction]
        public static async Task<List<StockInfo>> GetSecurityDataAsync(string market)
        {
            var urlStatic = $"https://iss.moex.com/iss/engines/stock/markets/{market}/securities.json?&iss.meta=off&iss.only=securities&securities.columns=SECID,SHORTNAME";
            var jsonStatic = await _httpClient.GetFromJsonAsync<StringStaticData>(urlStatic);

            var urlDynamic = $"https://iss.moex.com/iss/engines/stock/markets/{market}/securities.json?&iss.meta=off&iss.only=marketdata&marketdata.columns=LAST,LASTCHANGEPRCNT";
            var jsonDynamic = await _httpClient.GetFromJsonAsync<MarketData>(urlDynamic);

            var dynamicData = new List<StockInfo>();
            if (jsonStatic != null && jsonDynamic != null)
            {
                var staticData = jsonStatic.securities.data;
                var marketData = jsonDynamic.marketdata.data;
                for (int i = 0; i < staticData.Count && i < marketData.Count; i++)
                {
                    if (marketData[i][0].HasValue)
                    {
                        dynamicData.Add(new StockInfo()
                        {
                            SecId = staticData[i][0],
                            SecName = staticData[i][1],
                            LastPrice = (float)marketData[i][0].Value,
                            LastChange = (float)marketData[i][1].Value,
                        });
                    }
                }
            }
            return dynamicData;
        }

        private static void AddPredictionToChartData(StockPrediction? prediction, ref ChartData chartData, DateTime lastDate)
        {
            if (prediction != null)
            {
                foreach (float item in prediction.ForecastedPrices)
                {
                    lastDate = lastDate.AddDays(1);
                    chartData.Dates.Add(lastDate);
                    chartData.ForecastedPrices.Add(Math.Round(item, 2));
                }
            }
        }

        [NonAction]
        public static StockPrediction? ForecastTimeSeries(List<Stock> stocks, int horizon)
        {
            var mlContext = new MLContext();
            IDataView data = mlContext.Data.LoadFromEnumerable(stocks);

            var model = mlContext.Forecasting.ForecastBySsa(
                outputColumnName: "ForecastedPrices",
                inputColumnName: "Value",
                windowSize: 7,
                seriesLength: 30,
                trainSize: stocks.Count,
                horizon: horizon,
                confidenceLevel: 0.95f);

            var forecaster = model.Fit(data);
            var forecastEngine = forecaster.CreateTimeSeriesEngine<Stock, StockPrediction>(mlContext);
            var forecast = forecastEngine.Predict();
            return forecast;
        }
    }
}