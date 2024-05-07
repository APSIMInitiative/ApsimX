using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace APSIM.Shared.Utilities
{
    /// <summary>
    /// A class containing some web utilities
    /// </summary>
    public class WebUtilities
    {
        /// <summary>
        /// HttpClient is intended to be instantiated once per application, rather than per-use. See Remarks.
        /// </summary>
        public static readonly HttpClient client = new HttpClient();

        /// <summary>
        /// Send a string to the specified socket server. Returns the response string. Will throw
        /// if cannot connect.
        /// </summary>
        public static string SocketSend(string serverName, int port, string data)
        {
            string Response = null;
            TcpClient Server = null;
            try
            {
                Server = new TcpClient(serverName, Convert.ToInt32(port, CultureInfo.InvariantCulture));
                Byte[] bData = System.Text.Encoding.ASCII.GetBytes(data);
                Server.GetStream().Write(bData, 0, bData.Length);

                Byte[] bytes = new Byte[8192];

                // Wait for data to become available.
                while (!Server.GetStream().DataAvailable)
                    Thread.Sleep(10);

                // Loop to receive all the data sent by the client.
                while (Server.GetStream().DataAvailable)
                {
                    int NumBytesRead = Server.GetStream().Read(bytes, 0, bytes.Length);
                    Response += System.Text.Encoding.ASCII.GetString(bytes, 0, NumBytesRead);
                }
            }
            finally
            {
                if (Server != null) Server.Close();
            }
            return Response;
        }

        /// <summary>
        /// Async function to issue POST request for a URL and return the result as a Stream
        /// </summary>
        /// <param name="url">URL to be accessed</param>
        /// <param name="content">Data to be posted, as JSON</param>
        /// <returns></returns>
        private static async Task<Stream> AsyncPostStreamTask(string url, string content)
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var data = new StringContent(content, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, data).ConfigureAwait(false);
            return await response.Content.ReadAsStreamAsync();
        }

        /// <summary>Call REST web service using POST.</summary>
        /// Assumes the data returned by the URL is JSON, 
        /// which is then deserialised into the returned object
        /// <typeparam name="T">The return type</typeparam>
        /// <param name="url">The URL of the REST service.</param>
        /// <returns>The return data</returns>
        public static T PostRestService<T>(string url)
        {
            var stream = AsyncPostStreamTask(url, "").Result;
            if (typeof(T).Name == "Object")
                return default(T);
            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.DeserializeAsync<T>(stream, options).Result;
        }


        /// <summary>
        /// Async function to issue GET request for a URL and return the result as a Stream
        /// </summary>
        /// <param name="url">URL to access</param>
        /// <param name="mediaType">Preferred media type to return</param>
        /// <param name="cancellationToken">Token for cancellation</param>
        /// <returns>Data stream obtained from the URL</returns>
        public async static Task<Stream> AsyncGetStreamTask(string url, string mediaType, CancellationToken cancellationToken = default)
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(mediaType));
            HttpResponseMessage response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
            return await response.Content.ReadAsStreamAsync();
        }

        /// <summary>Call REST web service using GET.
        /// Assumes the data returned by the URL is XML, 
        /// which is then deserialised into the returned object
        /// </summary>
        /// <typeparam name="T">The return type</typeparam>
        /// <param name="url">The URL of the REST service.</param>
        /// <param name="cancellationToken">Token for cancellation</param>
        /// <returns>The return data</returns>
        public static T CallRESTService<T>(string url, CancellationToken cancellationToken = default)
        {
            var stream = AsyncGetStreamTask(url, "application/xml", cancellationToken).Result;
            if (typeof(T).Name == "Object")
                return default(T);

            XmlSerializer serializer = new XmlSerializer(typeof(T));
            return (T)serializer.Deserialize(new XmlUtilities.NamespaceIgnorantXmlTextReader(new StreamReader(stream)));
        }

        /// <summary>
        /// Calls a url and returns the web response in a memory stream
        /// </summary>
        /// <param name="url">The url to call</param>
        /// <param name="cancellationToken">Token for cancellation</param>
        /// <returns>The data stream</returns>
        public static async Task<MemoryStream> ExtractDataFromURL(string url, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var result = await AsyncGetStreamTask(url, "*/*", cancellationToken);
                if (result == null)
                    throw new Exception();
                
                MemoryStream memStream = new MemoryStream();
                result.CopyTo(memStream);

                if (Encoding.UTF8.GetString(memStream.ToArray()).Contains("503 Service Unavailable"))
                    throw new Exception();

                return memStream;
            }
            catch (OperationCanceledException)
            {
                throw;  // rethrow to caller
            }
            catch (Exception ex)
            {
                throw new Exception("Cannot get data from " + url, ex);
            }
        }

        /// <summary>
        /// Retrieve data from a URL, providing progress indications along the way
        /// </summary>
        /// <param name="url">The URL to obtain (using GET method)</param>
        /// <param name="destination">A stream to write the results to (typically a FileStream)</param>
        /// <param name="progress">A Progress object (defaults to null)</param>
        /// <param name="cancellationToken">a CancellationToken(defaults to an empty token</param>
        /// <param name="mediaType">Media type to obtain (defaults to */*)</param>
        /// <returns>A Task</returns>
        public static async Task GetAsyncWithProgress(string url, Stream destination,
                       IProgress<double> progress = null, CancellationToken cancellationToken = default, string mediaType = "*/*")
        {
            try
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(mediaType));
                HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(string.Format("The request returned with HTTP status code {0}", response.StatusCode));
                }
                long contentLength = response.Content.Headers.ContentLength.HasValue ? response.Content.Headers.ContentLength.Value : -1L;
                using (Stream download = await response.Content.ReadAsStreamAsync())
                {
                    long totalRead = 0L;
                    byte[] buffer = new byte[8192];
                    int bytesRead;

                    while ((bytesRead = await download.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
                        totalRead += bytesRead;
                        progress?.Report((double)totalRead / (double)contentLength);
                    }
                }
            }
            catch (OperationCanceledException)
            {}
            finally
            {
                if (destination is FileStream)
                    destination.Close();
            }
        }
    }
}
