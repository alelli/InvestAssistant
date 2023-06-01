using Microsoft.AspNetCore.Mvc;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;
using Microsoft.ML.Data;

namespace Invest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlgorithmController : ControllerBase
    {

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
                horizon: 50,
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
