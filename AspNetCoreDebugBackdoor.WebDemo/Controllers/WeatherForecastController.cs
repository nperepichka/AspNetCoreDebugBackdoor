using AspNetCoreDebugBackdoor.WebDemo.Models;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCoreDebugBackdoor.WebDemo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries =
        [
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        ];

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            Counter++;
            Cache ??= [.. Enumerable.Range(1, 5).Select(index => new WeatherForecast
                {
                    Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    TemperatureC = Random.Shared.Next(-20, 55),
                    Summary = Summaries[Random.Shared.Next(Summaries.Length)]
                })];
            return Cache;
        }

        [HttpGet("counter")]
        public int GetCounter()
        {
            return Counter;
        }

        private static WeatherForecast[]? Cache = null;

        private static int Counter = 0;
    }
}
