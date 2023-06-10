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

        //[HttpGet]
        //public IActionResult Shares() => View("StockList", 1); 

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
        public async Task<IActionResult> Shares(StockInfo info)  // UserStock
        {
            var identityEmail = User.Identity.Name;
            var dbUser = await _context.Users.FirstOrDefaultAsync(x => x.Email == identityEmail);
            if (dbUser != null)
            {
                var dbStock = await _context.UserStocks.FindAsync(dbUser.Id, info.SecId);
                if (dbStock != null)
                {
                    var prevSum = dbStock.PurchasePrice * dbStock.Quantity;
                    var curSum = info.PurchasePrice * info.Quantity;
                    dbStock.Quantity += info.Quantity;
                    dbStock.PurchasePrice = (float)Math.Round((prevSum + curSum) / dbStock.Quantity, 2);
                }
                else
                {
                    ViewBag.Info = info.PurchasePrice;
                    UserStock newStock = new UserStock()
                    {
                        User = dbUser,
                        SecName = info.SecName,
                        SecId = info.SecId,
                        Quantity = info.Quantity,
                        PurchasePrice = info.PurchasePrice
                    };
                    _context.UserStocks.Add(newStock);
                }
                await _context.SaveChangesAsync();
            }
            return await Shares(info.SecId);
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

                var newDates = await GetDatesAsync(id, market, stringStartDate, stringEndDate);
                var newPrices = await GetPricesAsync(id, market, stringStartDate, stringEndDate);
                if (k == 2)
                {
                    chartData = new ChartData() { Secid = id, Dates = newDates, Prices = newPrices, 
                        ForecastedPrices = new List<double>(newPrices) };
                }
                dates.AddRange(newDates);
                prices.AddRange(newPrices);
                startDate = endDate.AddDays(1);
            }
            StockPrediction? prediction = ForecastTimeSeries(AlgorithmController.DataToStock(dates, prices), 14);
            AddPredictionToChartData(prediction, ref chartData, dates[dates.Count - 1]);
            return chartData;
        }

        [NonAction]
        public static async Task<List<DateTime>> GetDatesAsync(string id, string market, string startDate, string endDate)
        {
            var url = $"https://iss.moex.com/iss/history/engines/stock/markets/{market}/securities/{id}.json?iss.meta=off&history.columns=TRADEDATE&from={startDate}&till={endDate}";
            var jsonDates = await _httpClient.GetFromJsonAsync<Root1>(url);
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
        public static async Task<List<double>> GetPricesAsync(string id, string market, string startDate, string endDate)
        {
            var url = $"https://iss.moex.com/iss/history/engines/stock/markets/{market}/securities/{id}.json?iss.meta=off&history.columns=OPEN&from={startDate}&till={endDate}";
            var jsonPrices = await _httpClient.GetFromJsonAsync<Root2>(url);
            var prices = new List<double>();
            if (jsonPrices != null)
            {
                for (int i = 0; i < jsonPrices.history.data.Count && jsonPrices.history.data[i][0] != null; i++)
                {
                    prices.Add(jsonPrices.history.data[i][0]);
                }
            }
            return prices;
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
                confidenceLevel: 0.95f,
                confidenceLowerBoundColumn: "LowerBoundPrices",
                confidenceUpperBoundColumn: "UpperBoundPrices");

            var forecaster = model.Fit(data);
            var forecastEngine = forecaster.CreateTimeSeriesEngine<Stock, StockPrediction>(mlContext);
            var forecast = forecastEngine.Predict();
            return forecast;
        }
    }
}