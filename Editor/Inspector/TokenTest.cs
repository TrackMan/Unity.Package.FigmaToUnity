using System.Threading;
using System.Threading.Tasks;

namespace Figma.Inspectors
{
    using Internals;

    internal class TokenTest : Api
    {
        #region Properties
        public Me me { get; private set; }
        #endregion
        
        #region Constructors
        internal TokenTest(string personalAccessToken = null) : base(personalAccessToken, null) { }
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