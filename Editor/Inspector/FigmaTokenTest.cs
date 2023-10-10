using System.Threading.Tasks;

namespace Figma.Inspectors
{
    using global;

    class FigmaTokenTest : FigmaApi
    {
        #region Constructors
        internal FigmaTokenTest(string personalAccessToken = default) : base(personalAccessToken) { }
        #endregion

        #region Methods
        internal async Task<bool> TestAsync()
        {
            Me me = await GetAsync<Me>("me");
            return string.IsNullOrEmpty(me.err) && me.email.Contains("@");
        }
        #endregion
    }
}