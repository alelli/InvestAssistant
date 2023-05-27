﻿using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;
using Microsoft.ML.Data;

namespace Invest.Controllers
{
    public class StockController : Controller
    {
        private readonly ILogger<StockController> _logger;
        private static HttpClient _httpClient = new HttpClient();

        public StockController(ILogger<StockController> logger)
        {
            _logger = logger;
        }

        public async Task<IActionResult> Shares(string id) 
        {
            if (id != null)
            {
                DateTime currentDate = DateTime.Now.Date,
                    parsingDate = currentDate.AddDays(-365);
                string stringParsingDate = $"{parsingDate.Year}-{parsingDate.Month}-{parsingDate.Day}";
                //ViewBag.Date = parsingDate.ToString();
                string urlDate = $"https://iss.moex.com/iss/history/engines/stock/markets/shares/boards/TQBR/securities/{id}.json?iss.meta=off&history.columns=TRADEDATE&from={stringParsingDate}";
                var urlPrice = $"https://iss.moex.com/iss/history/engines/stock/markets/shares/boards/TQBR/securities/{id}.json?iss.meta=off&history.columns=OPEN&from={stringParsingDate}";
                var url = $"https://iss.moex.com/iss/history/engines/stock/markets/shares/boards/TQBR/securities/{id}.json?iss.meta=off&iss.json=extended&history.columns=TRADEDATE,OPEN&from={stringParsingDate}";
                // new method
                ChartData chartData = await GetDataFromUrlsAsync(urlDate, urlPrice);
                if (chartData != null)
                {
                    // method ForecastData
                    var stocks = new List<Stock>(); // даты и цены вместе в одном классе - для прогнозирования
                    for (int i = 0; i < chartData.Dates.Count; i++)
                    {
                        stocks.Add(new Stock(chartData.Dates[i], (float)chartData.Prices[i]));
                    }
                    float[] prediction = ForecastStocks(stocks).ForecastedPrices; 
                    // end of method

                    // method AddPredictionToChartData
                    DateTime lastDate = chartData.Dates[chartData.Dates.Count - 1];
                    foreach (float item in prediction)
                    {
                        lastDate = lastDate.AddDays(1);
                        chartData.Dates.Add(lastDate);
                        chartData.Prices.Add(item);
                    }
                    // end of method

                    chartData.Secid = id;
                    return View("Chart", chartData);
                }
                // end of method
            }
            return View("StockList", 1);
        }

        public async Task<ChartData> GetDataFromUrlsAsync(string urlDate, string urlPrice)
        {
            var jsonDates = await _httpClient.GetFromJsonAsync<Root1>(urlDate);
            var jsonPrices = await _httpClient.GetFromJsonAsync<Root2>(urlPrice);

            if (jsonDates != null && jsonPrices != null)
            {
                var length = jsonDates.history.data.Count; // delete

                var dates = new List<DateTime>(); // отдельно даты и цены - для построения графика
                var prices = new List<double>();
                for (int i = 0; i < length; i++)
                {
                    dates.Add(DateTime.ParseExact(jsonDates.history.data[i][0], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
                    prices.Add(jsonPrices.history.data[i][0]);
                }
                return new ChartData { Dates = dates, Prices = prices };
            }
            return new ChartData();
        }

        public async Task<ChartData> GetDataFromUrlAsync(string url)
        {
            var jsonData = await _httpClient.GetFromJsonAsync<Root3>(url);
            if (jsonData != null)
            {
                var length = jsonData.history.Count;
                var dates = new List<DateTime>(); // отдельно даты и цены - для построения графика
                var prices = new List<double>();
                for (int i = 0; i < length; i++)
                {
                    dates.Add(DateTime.ParseExact(jsonData.history[i].TRADEDATE, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
                    prices.Add(jsonData.history[i].OPEN);
                }
                return new ChartData { Dates = dates, Prices = prices };
            }
            return new ChartData();
        }

        private static StockPrediction? ForecastStocks(List<Stock> stocks)
        {
            var mlContext = new MLContext();
            List<Stock> actual = new List<Stock>();
            for (int i = stocks.Count - 30; i < stocks.Count; i++)
            {
                actual.Add(stocks[i]);
                stocks.RemoveAt(i);
            }
            IDataView dataToPredict = mlContext.Data.LoadFromEnumerable(stocks);
            IDataView dataToTest = mlContext.Data.LoadFromEnumerable(actual);

            var model = mlContext.Forecasting.ForecastBySsa(
                outputColumnName: "ForecastedPrices",
                inputColumnName: "Value",
                windowSize: 7,
                seriesLength: 15,
                trainSize: stocks.Count,// - 30, //с начала ряда или с конца?
                horizon: 7,
                confidenceLevel: 0.95f,
                confidenceUpperBoundColumn: "UpperBoundPrices",
                confidenceLowerBoundColumn: "LowerBoundPrices");

            var forecaster = model.Fit(dataToPredict);

            float[] errors = EvaluateAlgorithmResult(dataToTest, forecaster, mlContext);
          

            var forecastEngine = forecaster.CreateTimeSeriesEngine<Stock, StockPrediction>(mlContext);
            StockPrediction forecast = forecastEngine.Predict();

            return forecast;
        }

        private static float[] EvaluateAlgorithmResult(IDataView testData, ITransformer model, MLContext mlContext)//StockPrediction forecast)
        {
            var predictions = model.Transform(testData);
            IEnumerable<float> forecast = mlContext.Data.CreateEnumerable<StockPrediction>(predictions, true)
                .Select(observed => observed.ForecastedPrices[0]); 
            IEnumerable<float> actual = mlContext.Data.CreateEnumerable<Stock>(testData, true)
                .Select(observed => observed.Value);

            var metrics = actual.Zip(forecast, (actualValue, forecastValue) => actualValue - forecastValue);
            float MAE = metrics.Average(error => Math.Abs(error));
            float RMSE = (float)Math.Sqrt(metrics.Average(error => Math.Pow(error, 2)));

            return new float[] { MAE, RMSE };
        }
    }
}