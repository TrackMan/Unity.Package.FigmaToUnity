using System.Threading;
using System.Threading.Tasks;

namespace Figma.Inspectors
{
    using Internals;

    internal class AuthTest : Api
    {
        #region Properties
        public Me me { get; private set; }
        public bool IsAuthenticated => me != null && string.IsNullOrEmpty(me.err) && me.email.Contains("@");
        #endregion

        #region Constructors
        internal AuthTest(string personalAccessToken = null) : base(personalAccessToken, null) { }
        #endregion

        #region Methods
        internal async Task AuthAsync() => me = await GetAsync<Me>("me", CancellationToken.None);
        #endregion
    }
}