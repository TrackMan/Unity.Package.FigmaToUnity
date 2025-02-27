using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Figma
{
    internal static class EnumerableExtensions
    {
        #region Methods
        internal static async Task ForEachParallelAsync<T>(this IEnumerable<T> elements, int maxConcurrentRequests, Func<T, Task> func, CancellationToken token)
        {
            using SemaphoreSlim semaphore = new(maxConcurrentRequests);
            Task[] tasks = elements.Select(async x =>
            {
                await semaphore.WaitAsync(token);
                await func(x);
                semaphore.Release();
            }).ToArray();
            await Task.WhenAll(tasks);
        }
        internal static IEnumerable<List<TSource>> Chunk<TSource>(this IEnumerable<TSource> source, int size)
        {
            using IEnumerator<TSource> enumerator = source.GetEnumerator();
            bool hasMoreElements = true;

            do
            {
                List<TSource> chunk = new(size);
                while (size > chunk.Count && (hasMoreElements = enumerator.MoveNext()))
                    chunk.Add(enumerator.Current);
                if (chunk.Count == 0)
                    break;
                yield return chunk;
            } while (hasMoreElements);
        }
        #endregion
    }
}