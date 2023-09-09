using Invest.JsonClasses;
using Invest.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;

namespace Invest.Controllers
{
    public class StockController : Controller
    {
        public readonly static string sharesMarket = "shares/boards/TQBR",
            bondsMarket = "bonds/boards/TQCB";

        private static HttpClient _httpClient = new HttpClient();

        public async Task<IActionResult> Shares(string id)
        {
            if (id != null)
            {
                ChartData chartData = await GetChartDataAsync(id, sharesMarket, DateTime.Now.AddDays(-366).Date, DateTime.Now.Date);
                return View("Chart", chartData);
            }
            return View("StockList", sharesMarket);
        }

        public async Task<IActionResult> Bonds(string id)
        {
            if (id != null)
            {
                ChartData chartData = await GetChartDataAsync(id, bondsMarket, DateTime.Now.AddDays(-366).Date, DateTime.Now.Date);
                return View("Chart", chartData);
            }
            return View("StockList", bondsMarket);
        }

        public static async Task<ChartData> GetChartDataAsync(string id, string market, DateTime startDate, DateTime endDate)
        {
            ChartData chartData = await GetSecurityInfoAsync(id, market);
            SecurityHistory securityData = await ParseSecurityDataAsync(id, market, startDate, endDate);

            int length = securityData.Dates.Count;
            chartData.Dates = new List<DateTime>(securityData.Dates.GetRange(length - 100, 100));
            chartData.Prices = new List<double>(securityData.Prices.GetRange(length - 100, 100));
            chartData.ForecastedPrices = new List<double>(securityData.Prices.GetRange(length - 100, 100));

            if (securityData.Prices.Count > 14)
            {
                StockForecast? forecast = ForecastStocks(AlgorithmController.ConvertSecurityDataToStockList(securityData), 14);
                AddPredictionToChartData(forecast, ref chartData, securityData.Dates[securityData.Dates.Count - 1]);
            }
            return chartData;
        }

        public static async Task<SecurityHistory> ParseSecurityDataAsync(string id, string market, DateTime startDate, DateTime endDate)
        {
            const int parsingDays = 100;

            var dates = new List<DateTime>();
            var prices = new List<double>();

            var daysInterval = Math.Abs((startDate - endDate).Days);
            var iterations = Math.Ceiling((double)daysInterval / parsingDays);
            DateTime currentEndDate;

            for (int i = 0; i < iterations; i++)
            {
                currentEndDate = startDate.AddDays(parsingDays);
                if (currentEndDate > endDate)
                    currentEndDate = endDate;
                
                var history = await GetHistoryByRequestAsync(id, market, startDate, currentEndDate);
                var newPrices = history.Prices;
                var newDates = history.Dates;

                dates.AddRange(newDates);
                prices.AddRange(newPrices);

                startDate = currentEndDate.AddDays(1);
            }
            return new SecurityHistory { Dates = dates, Prices = prices };
        }

        [NonAction]
        public static async Task<SecurityHistory> GetHistoryByRequestAsync(string id, string market, DateTime startDate, DateTime endDate)
        {
            string stringStartDate = startDate.ToString("yyyy-MM-dd"),
                stringEndDate = endDate.ToString("yyyy-MM-dd");

            var urlPrice = $"https://iss.moex.com/iss/history/engines/stock/markets/{market}/securities/{id}.json?iss.meta=off&history.columns=OPEN&from={stringStartDate}&till={stringEndDate}";
            var urlDate = $"https://iss.moex.com/iss/history/engines/stock/markets/{market}/securities/{id}.json?iss.meta=off&history.columns=TRADEDATE&from={stringStartDate}&till={stringEndDate}";
            var jsonPrices = await _httpClient.GetFromJsonAsync<DoubleHistoryData>(urlPrice);
            var jsonDates = await _httpClient.GetFromJsonAsync<StringHistoryData>(urlDate);

            var prices = new List<double>();
            var dates = new List<DateTime>();

            if (jsonPrices != null && jsonDates != null)
            {
                for (int i = 0; i < jsonPrices.history.data.Count; i++)
                {
                    if (jsonPrices.history.data[i][0].HasValue)
                    {
                        prices.Add(jsonPrices.history.data[i][0].Value);
                        dates.Add(DateTime.ParseExact(jsonDates.history.data[i][0], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
                    }
                }

            }
            return new SecurityHistory() { Dates = dates, Prices = prices };
        }

        [NonAction]
        public static async Task<ChartData> GetSecurityInfoAsync(string id, string market)
        {
            var urlStatic = $"https://iss.moex.com/iss/engines/stock/markets/{market}/securities.json?&iss.meta=off&iss.only=securities&securities.columns=SECID,SHORTNAME";
            var jsonStatic = await _httpClient.GetFromJsonAsync<StaticData>(urlStatic);

            var urlDynamic = $"https://iss.moex.com/iss/engines/stock/markets/{market}/securities.json?&iss.meta=off&iss.only=marketdata&marketdata.columns=LAST,LASTTOPREVPRICE";//LASTCHANGEPRCNT
            var jsonDynamic = await _httpClient.GetFromJsonAsync<DynamicData>(urlDynamic);

            ChartData data = new ChartData();

            if (jsonStatic != null && jsonDynamic != null)
            {
                var staticData = jsonStatic.securities.data;
                var dynamicData = jsonDynamic.marketdata.data;

                for (int i = 0; i < staticData.Count && i < dynamicData.Count; i++)
                {
                    if (dynamicData[i][0].HasValue && staticData[i][0] == id)
                    {
                        data = new ChartData()
                        {
                            SecName = staticData[i][1],
                            LastPrice = (float)dynamicData[i][0].Value,
                            LastChange = (float)dynamicData[i][1].Value
                        };
                    }
                }
            }
            return data;
        }

        private static void AddPredictionToChartData(StockForecast? forecast, ref ChartData chartData, DateTime lastDate)
        {
            if (forecast != null)
            {
                foreach (float item in forecast.ForecastedPrices)
                {
                    lastDate = lastDate.AddDays(1);
                    chartData.Dates.Add(lastDate);
                    chartData.ForecastedPrices.Add(Math.Round(item, 2));
                }
            }
        }

        [NonAction]
        public static StockForecast? ForecastStocks(List<Stock> stocks, int forecastHorizon)
        {
            IDataView data = AlgorithmController.mlContext.Data.LoadFromEnumerable(stocks);
            var model = AlgorithmController.mlContext.Forecasting.ForecastBySsa(
                outputColumnName: "ForecastedPrices",
                inputColumnName: "Value",
                windowSize: 7,
                seriesLength: 30,
                trainSize: stocks.Count,
                horizon: forecastHorizon,
                confidenceLevel: 0.95f);

            var forecaster = model.Fit(data);
            var forecastEngine = forecaster.CreateTimeSeriesEngine<Stock, StockForecast>(AlgorithmController.mlContext);
            var forecast = forecastEngine.Predict();
            return forecast;
        }
    }
}