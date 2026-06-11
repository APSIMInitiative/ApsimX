using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using APSIMNG.Utility;
using NUnit.Framework;

namespace UnitTests.UtilityTests
{
    [TestFixture]
    public class WeatherThirdPartyUtilityTests
    {
        [SetUp]
        public void SetUp()
        {
            WeatherThirdPartyUtility.ResetTestHooks();
        }

        [TearDown]
        public void TearDown()
        {
            WeatherThirdPartyUtility.ResetTestHooks();
        }

        [Test]
        public async Task GetNasaPower_WithoutWorldModellersRain_ReturnsApsimMetFileUsingNASARain()
        {
            string startDate = "2023-07-01";
            string endDate = "2023-07-03";

            string nasaResponse = CreateNasaPowerResponse(
                3,
                new Dictionary<string, double[]>
                {
                    ["T2M_MIN"] = new[] { 10.1, 10.2, 10.3 },
                    ["T2M_MAX"] = new[] { 20.1, 20.2, 20.3 },
                    ["ALLSKY_SFC_SW_DWN"] = new[] { 5.0, 5.1, 5.2 },
                    ["RH2M"] = new[] { 60.0, 61.0, 62.0 },
                    ["WS2M"] = new[] { 2.0, 2.1, 2.2 },
                    ["PRECTOTCORR"] = new[] { 0.5, 0.6, 0.7 },
                });

            WeatherThirdPartyUtility.ExtractDataFromURL = (url, cancellationToken) => Task.FromResult(new MemoryStream(Encoding.UTF8.GetBytes(nasaResponse)));

            string metContent = await WeatherThirdPartyUtility.GetNasaPower(0.0, 0.0, startDate, endDate, useWorldModellersRain: false);
            Assert.That(metContent, Does.Contain("[weather.met.weather]"));
            Assert.That(metContent, Does.Contain("date"));
            Assert.That(metContent, Does.Contain("rain"));

            string[] lines = metContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            Assert.That(lines.Length, Is.EqualTo(6 + 3)); // 6 header lines + 3 data lines

            DateTime currentDate = DateTime.Parse(startDate);
            for (int i = 6; i < lines.Length; i++)
            {
                string[] tokens = lines[i].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                Assert.That(tokens[0], Is.EqualTo(currentDate.ToShortDateString()));
                Assert.That(tokens[1], Is.EqualTo((10.1 + i - 6).ToString("0.0"))
                    .Or.EqualTo("10.1")
                    .Or.EqualTo("10.2")
                    .Or.EqualTo("10.3"));
                currentDate = currentDate.AddDays(1);
            }
        }

        [Test]
        public async Task GetNasaPower_WithWorldModellersRain_UsesWorldModellersRainInOutput()
        {
            string startDate = "2023-07-01";
            string endDate = "2023-07-03";
            string nasaResponse = CreateNasaPowerResponse(
                3,
                new Dictionary<string, double[]>
                {
                    ["T2M_MIN"] = new[] { 11.0, 11.0, 11.0 },
                    ["T2M_MAX"] = new[] { 21.0, 21.0, 21.0 },
                    ["ALLSKY_SFC_SW_DWN"] = new[] { 6.0, 6.0, 6.0 },
                    ["RH2M"] = new[] { 65.0, 65.0, 65.0 },
                    ["WS2M"] = new[] { 2.5, 2.5, 2.5 },
                    ["PRECTOTCORR"] = new[] { 0.9, 1.0, 1.1 },
                });

            string worldModellersMet = CreateApsimMetFile(startDate, endDate, new[] { 7.0, 8.0, 9.0 });

            WeatherThirdPartyUtility.ExtractDataFromURL = (url, cancellationToken) => Task.FromResult(new MemoryStream(Encoding.UTF8.GetBytes(nasaResponse)));
            WeatherThirdPartyUtility.GetStringFromURL = (url, cancellationToken) => Task.FromResult(worldModellersMet);

            string metContent = await WeatherThirdPartyUtility.GetNasaPower(0.0, 0.0, startDate, endDate, useWorldModellersRain: true);
            string[] lines = metContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            Assert.That(lines.Length, Is.EqualTo(6 + 3));
            Assert.That(lines[0], Does.Contain("[weather.met.weather]"));
            Assert.That(lines[4], Does.Contain("date"));
            Assert.That(lines[5], Does.Contain("()"));
            Assert.That(lines[6].Split(' ', StringSplitOptions.RemoveEmptyEntries).Last(), Is.EqualTo("7"));
            Assert.That(lines[7].Split(' ', StringSplitOptions.RemoveEmptyEntries).Last(), Is.EqualTo("8"));
            Assert.That(lines[8].Split(' ', StringSplitOptions.RemoveEmptyEntries).Last(), Is.EqualTo("9"));
        }

        [Test]
        public void GetNasaPower_InvalidNasaPowerResponse_ThrowsException()
        {
            string startDate = "2023-07-01";
            string endDate = "2023-07-03";
            string badResponse = "{ \"properties\": { \"parameter\": { \"T2M_MAX\": { \"20230701\": 1 } } } }";
            WeatherThirdPartyUtility.ExtractDataFromURL = (url, cancellationToken) => Task.FromResult(new MemoryStream(Encoding.UTF8.GetBytes(badResponse)));

            var ex = Assert.ThrowsAsync<Exception>(async () => await WeatherThirdPartyUtility.GetNasaPower(0.0, 0.0, startDate, endDate, useWorldModellersRain: false));
            Assert.That(ex.Message, Does.Contain("Error retrieving NASA POWER data"));
            Assert.That(ex.InnerException, Is.Not.Null);
        }

        private static string CreateNasaPowerResponse(int numDays, Dictionary<string, double[]> values)
        {
            var startDate = new DateTime(2023, 7, 1);
            var response = new StringBuilder();
            response.Append("{\"properties\":{\"parameter\":{");
            bool firstParam = true;
            foreach (var parameterName in values.Keys)
            {
                if (!firstParam)
                    response.Append(",");
                firstParam = false;
                response.AppendFormat("\"{0}\":{{", parameterName);
                for (int i = 0; i < numDays; i++)
                {
                    if (i > 0)
                        response.Append(",");
                    string dateString = startDate.AddDays(i).ToString("yyyyMMdd");
                    response.AppendFormat("\"{0}\":{1}", dateString, values[parameterName][i].ToString("G", System.Globalization.CultureInfo.InvariantCulture));
                }
                response.Append("}");
            }
            response.Append("}}}");
            return response.ToString();
        }

        private static string CreateApsimMetFile(string startDate, string endDate, double[] rainValues)
        {
            DateTime start = DateTime.Parse(startDate);
            DateTime end = DateTime.Parse(endDate);
            var sb = new StringBuilder();
            sb.AppendLine("[weather.met.weather]");
            sb.AppendLine("latitude = 0");
            sb.AppendLine("longitude = 0");
            sb.AppendLine("!Mock APSIM met file");
            sb.AppendLine("date       mint      maxt      radn      rh      wind      rain");
            sb.AppendLine("()         (oC)      (oC)      (MJ/m^2)  (%)      (m/s)     (mm)");

            int index = 0;
            for (DateTime date = start; date <= end; date = date.AddDays(1))
            {
                sb.AppendFormat(
                    "{0,-12}{1,-10}{2,-10}{3,-10}{4,-10}{5,-10}{6,-10}\n",
                    date.ToShortDateString(),
                    "1.0",
                    "2.0",
                    "3.0",
                    "4.0",
                    "5.0",
                    rainValues[index].ToString("G", System.Globalization.CultureInfo.InvariantCulture));
                index++;
            }

            return sb.ToString();
        }
    }
}
