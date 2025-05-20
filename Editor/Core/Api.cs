using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Figma
{
    using Internals;

    internal abstract class Api : IDisposable
    {
        #region Fields
        protected readonly string fileKey;
        protected readonly HttpClient httpClient;
        #endregion

        #region Constructors
        protected Api(string personalAccessToken, string fileKey)
        {
            this.fileKey = fileKey;
            httpClient = new();
            httpClient.DefaultRequestHeaders.Add("X-FIGMA-TOKEN", personalAccessToken);
        }
        #endregion

        #region Methods
        void IDisposable.Dispose() => httpClient.Dispose();
        #endregion

        #region Support Methods
        protected async Task<T> ConvertOnBackgroundAsync<T>(string json, CancellationToken token) where T : class => await Task.Run(() => Task.FromResult(JsonUtility.FromJson<T>(json)), token);
        protected async Task<T> GetAsync<T>(string get, CancellationToken token = default) where T : class => await ConvertOnBackgroundAsync<T>(await GetJsonAsync(get, token), token);
        protected async Task<string> GetJsonAsync(string get, CancellationToken token = default) => await HttpGetAsync($"{Internals.Const.api}/{get}", token);
        async Task<string> HttpGetAsync(string url, CancellationToken token = default)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, url);
            HttpResponseMessage response = await httpClient.SendAsync(request, token);

            if (response.IsSuccessStatusCode)
                return await response.Content.ReadAsStringAsync();

            throw new HttpRequestException($"{HttpMethod.Get} {url} {response.StatusCode.ToString()}");
        }
        #endregion
    }
}