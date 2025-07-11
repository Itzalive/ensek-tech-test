using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Ensek.PeteForrest.Services;

// Loosely adapted from .Net 10 preview https://github.com/dotnet/dotnet/blob/ddf39a1b4690fbe23aea79c78da67004a5c31094/src/runtime/src/libraries/System.Linq.AsyncEnumerable/src/System/Linq/Chunk.cs#L30C13-L35C46

public static class AsyncEnumerableExtensions
{
    /// <summary>Split the elements of a sequence into chunks of size at most <paramref name="size"/>.</summary>
    /// <remarks>
    /// Every chunk except the last will be of size <paramref name="size"/>.
    /// The last chunk will contain the remaining elements and may be of a smaller size.
    /// </remarks>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> whose elements to chunk.</param>
    /// <param name="size">Maximum size of each chunk.</param>
    /// <returns>
    /// An <see cref="IAsyncEnumerable{T}"/> that contains the elements of the input sequence split into chunks of size <paramref name="size"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="size"/> is less than 1.</exception>
    public static IAsyncEnumerable<TSource[]> Chunk<TSource>(
        this IAsyncEnumerable<TSource> source,
        int size)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (size <= 0) throw new InvalidOperationException();

        return Chunk(source, size, CancellationToken.None);

        static async IAsyncEnumerable<TSource[]> Chunk(
            IAsyncEnumerable<TSource> source,
            int size,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await using IAsyncEnumerator<TSource> e = source.GetAsyncEnumerator(cancellationToken);

            // Before allocating anything, make sure there's at least one element.
            if (await e.MoveNextAsync())
            {
                // Now that we know we have at least one item, allocate an initial storage array. This is not
                // the array we'll yield.  It starts out small in order to avoid significantly overallocating
                // when the source has many fewer elements than the chunk size.
                int arraySize = Math.Min(size, 4);
                int i;
                do
                {
                    var array = new TSource[arraySize];

                    // Store the first item.
                    array[0] = e.Current;
                    i = 1;

                    if (size != array.Length)
                    {
                        // This is the first chunk. As we fill the array, grow it as needed.
                        for (; i < size && await e.MoveNextAsync(); i++)
                        {
                            if (i >= array.Length)
                            {
                                arraySize = (int)Math.Min((uint)size, 2 * (uint)array.Length);
                                Array.Resize(ref array, arraySize);
                            }

                            array[i] = e.Current;
                        }
                    }
                    else
                    {
                        // For all but the first chunk, the array will already be correctly sized.
                        // We can just store into it until either it's full or MoveNext returns false.
                        TSource[] local = array; // avoid bounds checks by using cached local (`array` is lifted to iterator object as a field)
                        Debug.Assert(local.Length == size);
                        for (; (uint)i < (uint)local.Length && await e.MoveNextAsync(); i++)
                        {
                            local[i] = e.Current;
                        }
                    }

                    if (i != array.Length)
                    {
                        Array.Resize(ref array, i);
                    }

                    yield return array;
                }
                while (i >= size && await e.MoveNextAsync());
            }
        }
    }
}