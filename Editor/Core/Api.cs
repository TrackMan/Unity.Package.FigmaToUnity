using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Trackman;

namespace Figma
{
    using Internals;

    internal abstract class Api
    {
        #region Fields
        protected readonly string fileKey;
        protected readonly Dictionary<string, string> headers;
        #endregion

        #region Constructors
        protected Api(string personalAccessToken, string fileKey)
        {
            this.fileKey = fileKey;
            headers = new Dictionary<string, string> { { "X-FIGMA-TOKEN", personalAccessToken } };
        }
        #endregion

        #region Support Methods
        protected async Task<string> GetJsonAsync(string get, CancellationToken token = default) => GetString(await $"{Const.api}/{get}".HttpGetAsync(headers, cancellationToken: token));
        protected async Task<T> ConvertOnBackgroundAsync<T>(string json, CancellationToken token) where T : class => await Task.Run(() => Task.FromResult(JsonUtility.FromJson<T>(json)), token);
        protected async Task<T> GetAsync<T>(string get, CancellationToken token = default) where T : class => await ConvertOnBackgroundAsync<T>(await GetJsonAsync(get, token), token);
        string GetString(byte[] bytes) => Encoding.UTF8.GetString(bytes);
        #endregion
    }
}