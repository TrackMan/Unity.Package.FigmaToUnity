using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Figma
{
    public static class IEnumerableExtensions
    {
        #region Methods
        public static async Task ForEachParallelAsync<T>(this IEnumerable<T> elements, int maxConcurrentRequests, Func<T, Task> func, CancellationToken token)
        {
            using SemaphoreSlim semaphore = new(maxConcurrentRequests);
            Task[] tasks = elements.Select(async (x) =>
            {
                await semaphore.WaitAsync(token);
                await func(x);
                semaphore.Release();
            }).ToArray();
            await Task.WhenAll(tasks);
        }
        #endregion
    }
}