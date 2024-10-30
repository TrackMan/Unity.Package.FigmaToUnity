using System.Threading;
using System.Threading.Tasks;

namespace Figma.Inspectors
{
    using Internals;

    internal class FigmaTokenTest : FigmaApi
    {
        #region Properties
        public Me me { get; private set; }
        #endregion
        
        #region Constructors
        internal FigmaTokenTest(string personalAccessToken = default) : base(personalAccessToken) { }
        #endregion

        #region Methods
        internal async Task<bool> TestAsync()
        {
            me = await GetAsync<Me>("me", CancellationToken.None);
            return string.IsNullOrEmpty(me.err) && me.email.Contains("@");
        }
        #endregion
    }
}