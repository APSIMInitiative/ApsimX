using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using APSIM.Workflow;
using NUnit.Framework;

namespace UnitTests.APSIMWorkflow
{
    /// <summary>Tests for <see cref="AvaUtilities"/>.</summary>
    [TestFixture]
    public class AvaUtilitiesTests
    {
        private class CapturingHandler : HttpMessageHandler
        {
            public HttpRequestMessage Request { get; private set; }
            public string Body { get; private set; }
            public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Request = request;
                Body = await request.Content.ReadAsStringAsync(cancellationToken);
                return new HttpResponseMessage(Status);
            }
        }

        [Test]
        public void BuildPayload_ContainsExpectedFieldsAndStripsMountPrefix()
        {
            string json = AvaUtilities.BuildPayload("11745", "e027fb7", "/wd/Validation/Wheat.apsimx", "151.0", new[] { "Example log" });

            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            Assert.That(root.GetProperty("pullRequest").GetString(), Is.EqualTo("11745"));
            Assert.That(root.GetProperty("commit").GetString(), Is.EqualTo("e027fb7"));
            Assert.That(root.GetProperty("path").GetString(), Is.EqualTo("Validation/Wheat.apsimx"));
            Assert.That(root.GetProperty("time").GetString(), Is.EqualTo("151.0"));
            Assert.That(root.GetProperty("data").EnumerateArray().Select(e => e.GetString()), Is.EqualTo(new[] { "Example log" }));
        }

        [Test]
        public void ReadLogLines_MissingFile_ReturnsEmpty()
        {
            Assert.That(AvaUtilities.ReadLogLines(Path.Combine(Path.GetTempPath(), "does-not-exist.txt")), Is.Empty);
        }

        [Test]
        public async Task PostAsync_SendsJsonPost()
        {
            var handler = new CapturingHandler();
            using var client = new HttpClient(handler);

            await AvaUtilities.PostAsync(client, "{\"a\":1}", AvaUtilities.JobCompleteRoute);

            Assert.That(handler.Request.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(handler.Request.RequestUri.ToString(), Is.EqualTo("https://ava.apsim.info/api/pr/jobcomplete"));
            Assert.That(handler.Body, Is.EqualTo("{\"a\":1}"));
        }

        [Test]
        public void PostAsync_ServerError_Throws()
        {
            var handler = new CapturingHandler { Status = HttpStatusCode.InternalServerError };
            using var client = new HttpClient(handler);

            Assert.ThrowsAsync<HttpRequestException>(() => AvaUtilities.PostAsync(client, "{}", AvaUtilities.JobCompleteRoute));
        }
    }
}
