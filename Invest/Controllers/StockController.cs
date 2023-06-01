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
            if (id != null)
            {
                DateTime currentDate = DateTime.Now.Date,
                    startParsingDate = currentDate.AddDays(-366),
                    endParsingDate;
                
                var dates = new List<DateTime>(); // отдельно даты и цены - для построения графика
                var prices = new List<double>();
                ChartData chartData = new ChartData();

                // method ParseData
                for (int k = 0; k < 3; k++)
                {
                    endParsingDate = startParsingDate.AddDays(366 / 3 - 1);
                    string start = $"{startParsingDate.Year}-{startParsingDate.Month}-{startParsingDate.Day}",
                        end = $"{endParsingDate.Year}-{endParsingDate.Month}-{endParsingDate.Day}";

                    string urlDate = $"https://iss.moex.com/iss/history/engines/stock/markets/shares/boards/TQBR/securities/{id}.json?iss.meta=off&history.columns=TRADEDATE&from={start}&till={end}";
                    var urlPrice = $"https://iss.moex.com/iss/history/engines/stock/markets/shares/boards/TQBR/securities/{id}.json?iss.meta=off&history.columns=OPEN&from={start}&till={end}";
                    //ViewBag.Info = urlDate + "\n" + urlPrice;
                    startParsingDate = endParsingDate.AddDays(1);

                    var newDates = await GetDatesFromUrlsAsync(urlDate);
                    var newPrices = await GetPricesFromUrlsAsync(urlPrice);

                    if (k == 2)
                    {
                        chartData = new ChartData() { Dates = newDates, Prices = newPrices, ForecastedPrices = new List<double>(newPrices) };
                    }

                    dates.AddRange(newDates);
                    prices.AddRange(newPrices);
                }

                // method ForecastData
                var stocks = new List<Stock>(); // даты и цены вместе в одном классе - для прогнозирования
                for (int i = 0; i < dates.Count; i++)
                {
                    stocks.Add(new Stock(dates[i], (float)prices[i]));
                }
                float[] prediction = ForecastStocks(stocks).ForecastedPrices;
                // end of method

                // method AddPredictionToChartData
                DateTime lastDate = dates[dates.Count - 1];
                foreach (float item in prediction)
                {
                    lastDate = lastDate.AddDays(1);
                    chartData.Dates.Add(lastDate);
                    chartData.ForecastedPrices.Add(Math.Round(item, 2));
                }
                // end of method

                chartData.Secid = id;
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
            if (id != null)
            {
                string urlDate = $"https://iss.moex.com/iss/history/engines/stock/markets/bonds/boards/TQCB/securities/{id}.json?iss.meta=off&history.columns=TRADEDATE&from=2023-02-01";
                var urlPrice = $"https://iss.moex.com/iss/history/engines/stock/markets/bonds/boards/TQCB/securities/{id}.json?iss.meta=off&history.columns=OPEN&from=2023-02-01";

                var jsonDates = await _httpClient.GetFromJsonAsync<Root1>(urlDate);
                var jsonPrices = await _httpClient.GetFromJsonAsync<Root2>(urlPrice);
                int length = jsonDates.history.data.Count;
                var dates = new List<DateTime>();
                var prices = new List<double>();
                var stocks = new List<Stock>();
                for (int i = 0; i < length; i++)
                {
                    dates.Add(DateTime.ParseExact(jsonDates.history.data[i][0], "yyyy-MM-dd",
                                       System.Globalization.CultureInfo.InvariantCulture));
                    prices.Add(jsonPrices.history.data[i][0]);
                    stocks.Add(new Stock(dates[i], (float)prices[i]));
                }
                DateTime lastDate = dates[length - 1];


                //var prediction = ForecastStocks(stocks).ForecastedPrices;

                //foreach (double item in prediction)
                //{
                //    prices.Add((double)item);
                //    lastDate = lastDate.AddDays(1);
                //    dates.Add(lastDate);
                //}

                ChartData chartData = new ChartData() { Secid = id, Dates = dates, Prices = prices };
                return View("Chart", chartData);
            }
            return View("StockList", "bonds/boards/TQCB");
        }

        [NonAction]
        public async Task<List<DateTime>> GetDatesFromUrlsAsync(string urlDate)
        {
            var jsonDates = await _httpClient.GetFromJsonAsync<Root1>(urlDate);
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
        public async Task<List<double>> GetPricesFromUrlsAsync(string urlPrice)
        {
            var jsonPrices = await _httpClient.GetFromJsonAsync<Root2>(urlPrice);
            List<double> prices = new List<double>();
            if (jsonPrices != null)
            {
                for (int i = 0; i < jsonPrices.history.data.Count; i++)
                {
                    prices.Add(jsonPrices.history.data[i][0]);
                }
            }
            return prices;
        }

        private static StockPrediction? ForecastStocks(List<Stock> stocks)
        {
            var mlContext = new MLContext();
            IDataView data = mlContext.Data.LoadFromEnumerable(stocks);

            var model = mlContext.Forecasting.ForecastBySsa(
                outputColumnName: "ForecastedPrices",
                inputColumnName: "Value",
                windowSize: 7,
                seriesLength: 30,
                trainSize: stocks.Count,
                horizon: 14,
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