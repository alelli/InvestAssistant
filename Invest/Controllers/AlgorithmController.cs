using Invest.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ML;
using Microsoft.ML.Data;
using System.Text;

namespace Invest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlgorithmController : ControllerBase
    {
        public static MLContext mlContext = new MLContext();

        public static float MAE { get; private set; }
        public static float RMSE { get; private set; }
        public static float MAPE { get; private set; }

        [HttpGet]
        public async Task<IActionResult> GetSecuritiesListAsync()
        {
            var securities = await RecomendController.GetAllMarketSecuritiesAsync(StockController.sharesMarket);
            return Ok(securities);
        }

        [HttpGet("{secid}")]
        public async Task<IActionResult> EvaluateAlgorithmAsync(string secid)
        {
            SecurityHistory yearData = await StockController.ParseSecurityDataAsync(secid, StockController.sharesMarket, DateTime.Now.AddDays(-366 - 30).Date, DateTime.Now.AddDays(-30).Date),
                lastMonthData = await StockController.ParseSecurityDataAsync(secid, StockController.sharesMarket, DateTime.Now.AddDays(-30).Date, DateTime.Now.Date);
            if (yearData.Prices.Count <= 0 || lastMonthData.Prices.Count <= 0)
            {
                return BadRequest("Stock is null");
            }
            List<Stock> stocksToForecast = ConvertSecurityDataToStockList(yearData);
            List<Stock> stocksToCompare = ConvertSecurityDataToStockList(lastMonthData);

            IEnumerable<float> forecast, actual;

            CalculateErrorExponents(stocksToCompare, stocksToForecast, out forecast, out actual);
            string result = ConvertEvaluationToString(forecast, actual);

            return Ok(result);
        }

        private static void CalculateErrorExponents(List<Stock> stocksToCompare, List<Stock> stocksToForecast, out IEnumerable<float> forecast, out IEnumerable<float> actual)
        { 
            IDataView dataToTest = mlContext.Data.LoadFromEnumerable(stocksToCompare);
            IDataView dataToForecast = mlContext.Data.LoadFromEnumerable(stocksToForecast);

            var model = mlContext.Forecasting.ForecastBySsa(
                outputColumnName: "ForecastedPrices",
                inputColumnName: "Value",
                windowSize: 7,
                seriesLength: 30,
                trainSize: stocksToForecast.Count,
                horizon: stocksToCompare.Count,
                confidenceLevel: 0.95f);
            var forecaster = model.Fit(dataToForecast);

            var predictions = forecaster.Transform(dataToTest);
            forecast = mlContext.Data.CreateEnumerable<StockForecast>(predictions, true)
                .Select(observed => observed.ForecastedPrices[0]);
            actual = mlContext.Data.CreateEnumerable<Stock>(dataToTest, true)
                .Select(observed => observed.Value);

            MAE = CalculateMAE(actual, forecast);
            RMSE = CalculateRMSE(actual, forecast);
            MAPE = CalculateMAPE(actual, forecast);
        }

        private static float CalculateMAE(IEnumerable<float> actual, IEnumerable<float> forecast)
        {
            var errors = actual.Zip(forecast, (actualValue, forecastValue) => actualValue - forecastValue);
            return errors.Average(errorValue => Math.Abs(errorValue));
        }

        private static float CalculateRMSE(IEnumerable<float> actual, IEnumerable<float> forecast)
        {
            var errors = actual.Zip(forecast, (actualValue, forecastValue) => actualValue - forecastValue);
            return (float)Math.Sqrt(errors.Average(errorValue => Math.Pow(errorValue, 2)));
        }

        private static float CalculateMAPE(IEnumerable<float> actual, IEnumerable<float> forecast)
        {
            var errors = actual.Zip(forecast, (actualValue, forecastValue) => (actualValue - forecastValue) / actualValue);
            return errors.Average(errorValue => Math.Abs(errorValue)) * 100;
        }

        private static string ConvertEvaluationToString(IEnumerable<float> forecast, IEnumerable<float> actual)//, IEnumerable<float> error)   float MAE, float RMSE, float MAPE, 
        {
            int length = forecast.First().ToString().Length + 5;

            var builder = new StringBuilder("Actual:");
            foreach (var item in actual)
                builder.Append($"{item, 10:0.#}");

            builder.Append("\nForecast:");
            foreach (var item in forecast)
                builder.Append($"{item,10:0.#}");

            builder.Append("\nDiffer:");
            var errors = actual.Zip(forecast, (actualValue, forecastValue) => actualValue - forecastValue);
            foreach (var error in errors)
                builder.Append($"{error,10:F0}");

            builder.Append($"\nMAE: {MAE}\nRMSE: {RMSE}\nMAPE: {MAPE}");
            return builder.ToString();
        }

        [NonAction]
        public static List<Stock> ConvertSecurityDataToStockList(SecurityHistory securityData)
        {
            var stocks = new List<Stock>();
            for (int i = 0; i < securityData.Dates.Count && i < securityData.Prices.Count; i++)
            {
                stocks.Add(new Stock(securityData.Dates[i], (float)securityData.Prices[i]));
            }
            return stocks;
        }
    }
}
