using Microsoft.AspNetCore.Mvc;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;
using Microsoft.ML.Data;
using System.Text;

namespace Invest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlgorithmController : ControllerBase
    {
        [HttpGet("{secid}")]
        public async Task<IActionResult> AlgorithmEvaluation(string secid)
        {
            const int DaysInYear = 366;
            var market = "shares/boards/TQBR";

            var dates = new List<DateTime>();
            var prices = new List<double>();

            DateTime currentDate = DateTime.Now.Date,
                startDate = currentDate.AddDays(-DaysInYear-30),
                endDate;

            for (int k = 0; k < 3; k++)
            {
                endDate = startDate.AddDays(DaysInYear / 3 - 1);
                string stringStartDate = $"{startDate.Year}-{startDate.Month}-{startDate.Day}",
                    stringEndDate = $"{endDate.Year}-{endDate.Month}-{endDate.Day}";

                var newDates = await StockController.GetDatesAsync(secid, market, stringStartDate, stringEndDate);
                var newPrices = await StockController.GetPricesAsync(secid, market, stringStartDate, stringEndDate);
                
                dates.AddRange(newDates);
                prices.AddRange(newPrices);
                startDate = endDate.AddDays(1);
            }

            var stocksToPredict = new List<Stock>();
            for (int i = 0; i < dates.Count && i < prices.Count; i++)
            {
                stocksToPredict.Add(new Stock(dates[i], (float)prices[i]));
            }

            string newStartDate = $"{startDate.Year}-{startDate.Month}-{startDate.Day}",
                stringCurrentDate = $"{currentDate.Year}-{currentDate.Month}-{currentDate.Day}";
            var actualPrices = await StockController
                .GetPricesAsync(secid, market, newStartDate, stringCurrentDate);
            var actualDates = await StockController
                .GetDatesAsync(secid, market, newStartDate, stringCurrentDate);

            var actualStocks = new List<Stock>();
            for (int i = 0; i < actualDates.Count && i < actualPrices.Count; i++)
            {
                actualStocks.Add(new Stock(actualDates[i], (float)actualPrices[i]));
            }
            //float[] errors = 

            return Ok(ForecastStocks(stocksToPredict, actualStocks)); //$"MAE: {errors[0]}\nRMSE: {errors[1]}"
        }

        private static string ForecastStocks(List<Stock> stocksToPredict, List<Stock> actualStocks) //StockPrediction?
        {
            var mlContext = new MLContext();
            IDataView dataToPredict = mlContext.Data.LoadFromEnumerable(stocksToPredict);
            IDataView dataToTest = mlContext.Data.LoadFromEnumerable(actualStocks);

            var model = mlContext.Forecasting.ForecastBySsa(
                outputColumnName: "ForecastedPrices",
                inputColumnName: "Value",
                windowSize: 7,
                seriesLength: 30,
                trainSize: stocksToPredict.Count,
                horizon: actualStocks.Count,
                confidenceLevel: 0.95f,
                confidenceUpperBoundColumn: "UpperBoundPrices",
                confidenceLowerBoundColumn: "LowerBoundPrices");

            var forecaster = model.Fit(dataToPredict);

            return EvaluateAlgorithmResult(dataToTest, forecaster, mlContext);

            //var forecastEngine = forecaster.CreateTimeSeriesEngine<Stock, StockPrediction>(mlContext);
            //StockPrediction forecast = forecastEngine.Predict();
            //return forecast;
        }

        private static string EvaluateAlgorithmResult(IDataView testData, ITransformer model, MLContext mlContext)//StockPrediction forecast)
        {
            var predictions = model.Transform(testData);
            IEnumerable<float> forecast = mlContext.Data.CreateEnumerable<StockPrediction>(predictions, true)
                .Select(observed => observed.ForecastedPrices[0]);
            IEnumerable<float> actual = mlContext.Data.CreateEnumerable<Stock>(testData, true)
                .Select(observed => observed.Value);

            var error = actual.Zip(forecast, (actualValue, forecastValue) => actualValue - forecastValue);
            float MAE = error.Average(errorValue => Math.Abs(errorValue));
            float RMSE = (float)Math.Sqrt(error.Average(errorValue => Math.Pow(errorValue, 2)));

            var metrics2 = actual.Zip(forecast, (actualValue, forecastValue) => (actualValue - forecastValue) / actualValue);
            float MAPE = metrics2.Average(errorValue => Math.Abs(errorValue)) * 100;
            //var result = actual.Zip(forecast, (actualValue, forecastValue) => actualValue +"\t" + forecastValue + "\n");

            //string strResult = "Actual:\tForecast:\n";
            //foreach(var item in result)
            //{
            //    strResult += item.ToString();
            //}
            //strResult += "\nMAE: " + MAE + "\nRMSE: " + RMSE;

            var builder = new StringBuilder("Actual:\t\t");
            foreach (var item in actual)
            {
                builder.Append(item.ToString());
                builder.Append("\t");
            }
            builder.Append("\nForecast:\t");
            foreach (var item in forecast)
            {
                builder.Append($"{item:F0}");
                builder.Append("\t");
            }
            builder.Append("\nDiffer:\t\t");
            foreach (var item in error)
            {
                builder.Append($"{item:F0}");
                builder.Append("\t");
            }
            builder.Append($"\nMAE: {MAE}\nRMSE: {RMSE}");//\nMAPE: {MAPE}%");

            return builder.ToString();//strResult;// new float[] { MAE, RMSE };
        }

        [NonAction]
        public void GetAllSecurities()
        {

        }

        [NonAction]
        public static List<Stock> DataToStock(List<DateTime> dates, List<double> prices)
        {
            var stocks = new List<Stock>();
            for (int i = 0; i < dates.Count && i < prices.Count; i++)
            {
                stocks.Add(new Stock(dates[i], (float)prices[i]));
            }
            return stocks;
        }
    }
}
