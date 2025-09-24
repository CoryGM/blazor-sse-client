using System.Text.Json;

namespace BlazorSseClient.Demo.Tests
{
    [TestClass]
    public sealed class WeatherTests
    {
        private JsonSerializerOptions _jsonOptions = JsonSerializerOptions.Web;

        [TestMethod]
        public void WeatherDeserialization()
        {
            // Arrange
            var json = """{ "city":"Philadelphia, USA","latitude":39.9526,"longitude":-75.1652,"temperature":"64.4 \u00B0F","relativeHumidity":"93 %","apparentTemperature":"66.4 \u00B0F","isDayTime":false,"windSpeed":"5.7 mp/h","windDirection":"21\u00B0 NNE","windGusts":"17.4 mp/h","precipitation":"0 inch"}""";

            // Act
            var weather = JsonSerializer.Deserialize<Components.Weather.WeatherSseModel?>(json, _jsonOptions);

            // Assert
            Assert.IsNotNull(weather);
            Assert.AreEqual("Philadelphia, USA", weather?.City);
            Assert.AreEqual(39.9526, weather?.Latitude);
            Assert.AreEqual(-75.1652, weather?.Longitude);
            Assert.AreEqual("64.4 °F", weather?.Temperature);
            Assert.AreEqual("93 %", weather?.RelativeHumidity);
            Assert.AreEqual("66.4 °F", weather?.ApparentTemperature);
            Assert.IsFalse(weather?.IsDayTime);
            Assert.AreEqual("5.7 mp/h", weather?.WindSpeed);
            Assert.AreEqual("21° NNE", weather?.WindDirection);
            Assert.AreEqual("17.4 mp/h", weather?.WindGusts);
            Assert.AreEqual("0 inch", weather?.Precipitation);
        }
    }
}
