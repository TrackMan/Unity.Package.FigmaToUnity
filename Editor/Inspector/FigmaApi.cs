using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Trackman;

namespace Figma.Inspectors
{
    using global;

    abstract class FigmaApi
    {
        const string api = "https://api.figma.com/v1";

        #region Fields
        protected readonly string title;
        protected readonly Dictionary<string, string> headers;
        #endregion

        #region Constructors
        protected FigmaApi(string personalAccessToken = default, string title = default)
        {
            this.title = title;
            headers = new Dictionary<string, string> { { "X-FIGMA-TOKEN", personalAccessToken } };
        }
        #endregion

        #region Support Methods
        protected async Task<string> GetJsonAsync(string get, CancellationToken token = default)
        {
            return GetString(await $"{api}/{get}".HttpGetAsync(headers, cancellationToken: token));
        }
        protected async Task<T> ConvertOnBackgroundAsync<T>(string json, CancellationToken token) where T : class
        {
            return await Task.Run(() => Task.FromResult(JsonUtility.FromJson<T>(json)), token);
        }
        protected async Task<T> GetAsync<T>(string get, CancellationToken token = default) where T : class
        {
            string json = await GetJsonAsync(get, token);
            return await ConvertOnBackgroundAsync<T>(json, token);
        }
        string GetString(byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }
        #endregion
    }
}