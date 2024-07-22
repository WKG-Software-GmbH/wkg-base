using System.Diagnostics;
using System.Text;

namespace Wkg.Text;

// based on the .NET 7.0 implementation of ConfigurableArrayPool<T>, adapted for the modified requirements of StringBuilder
internal sealed class ConfigurableStringBuilderPool : StringBuilderPool
{
    /// <summary>The default maximum capacity of each string builder in the pool (2^16 = 64 KiB).</summary>
    private const int DEFAULT_MAX_STRING_BUILDER_CAPACTITY = 1024 * 64;
    /// <summary>The default maximum number of builders per bucket that are available for rent.</summary>
    private const int DEFAULT_MAX_NUMBER_OF_STRING_BUILDERS_PER_BUCKET = 50;

    private readonly Bucket[] _buckets;

    internal ConfigurableStringBuilderPool() : this(DEFAULT_MAX_STRING_BUILDER_CAPACTITY, DEFAULT_MAX_NUMBER_OF_STRING_BUILDERS_PER_BUCKET)
    {
    }

    internal ConfigurableStringBuilderPool(int maxStringBuilderCapacity, int maxStringBuildersPerBucket)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxStringBuilderCapacity, nameof(maxStringBuilderCapacity));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxStringBuildersPerBucket, nameof(maxStringBuildersPerBucket));

        // Our bucketing algorithm has a min capactity of 2^4 and a max capactity of 2^30.
        // Constrain the actual max used to those values.
        const int MinimumStringBuilderCapacity = 0x10, MaximumStringBuilderCapacity = 0x40000000;
        if (maxStringBuilderCapacity > MaximumStringBuilderCapacity)
        {
            maxStringBuilderCapacity = MaximumStringBuilderCapacity;
        }
        else if (maxStringBuilderCapacity < MinimumStringBuilderCapacity)
        {
            maxStringBuilderCapacity = MinimumStringBuilderCapacity;
        }

        // Create the buckets.
        int maxBuckets = Utilities.SelectBucketIndex(maxStringBuilderCapacity);
        Bucket[] buckets = new Bucket[maxBuckets + 1];
        for (int i = 0; i < buckets.Length; i++)
        {
            buckets[i] = new Bucket(Utilities.GetMaxSizeForBucket(i), maxStringBuildersPerBucket);
        }
        _buckets = buckets;
    }

    public override StringBuilder Rent(int minimumCapacity)
    {
        // StringBuilders can't be smaller than 1.
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minimumCapacity, nameof(minimumCapacity));

        StringBuilder? builder;

        int index = Utilities.SelectBucketIndex(minimumCapacity);
        if (index < _buckets.Length)
        {
            // Search for a builder starting at the 'index' bucket. If the bucket is empty, bump up to the
            // next higher bucket and try that one, but only try at most a few buckets.
            const int MaxBucketsToTry = 2;
            int i = index;
            do
            {
                // Attempt to rent from the bucket. If we get a builder from it, return it.
                builder = _buckets[i].Rent();
                if (builder != null)
                {
                    return builder;
                }
            }
            while (++i < _buckets.Length && i != index + MaxBucketsToTry);

            // The pool was exhausted for this builder size. Allocate a new builder with a size corresponding
            // to the appropriate bucket.
            builder = new StringBuilder(_buckets[index].BuilderCapacity);
        }
        else
        {
            // The request was for a size too large for the pool. Allocate a builder of exactly the requested capacity.
            // When it's returned to the pool, we'll simply throw it away.
            builder = new StringBuilder(minimumCapacity);
        }

        return builder;
    }

    public override void Return(StringBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Determine with what bucket this builder capacity is associated
        int bucket = Utilities.SelectBucketIndex(builder.Capacity);

        // If we can tell that the builder was allocated, drop it. Otherwise, check if we have space in the pool
        bool haveBucket = bucket < _buckets.Length;
        if (haveBucket)
        {
            // Clear the builder's contents and return it to the pool
            builder.Clear();

            // Return the builder to its bucket. In the future, we might consider having Return return false
            // instead of dropping a bucket, in which case we could try to return to a lower-sized bucket,
            // just as how in Rent we allow renting from a higher-sized bucket.
            _buckets[bucket].TryReturn(builder);
        }
    }

    /// <summary>Provides a thread-safe bucket containing builders that can be Rent'd and Return'd.</summary>
    private sealed class Bucket
    {
        internal readonly int BuilderCapacity;
        private readonly StringBuilder?[] _builders;

        private SpinLock _lock; // do not make this readonly; it's a mutable struct
        private int _index;

        /// <summary>
        /// Creates the pool with numberOfBuilders builders where each builder is of builderCapacity length.
        /// </summary>
        internal Bucket(int builderCapacity, int numberOfBuilders)
        {
            _lock = new SpinLock(Debugger.IsAttached); // only enable thread tracking if debugger is attached; it adds non-trivial overheads to Enter/Exit
            _builders = new StringBuilder[numberOfBuilders];
            BuilderCapacity = builderCapacity;
        }

        /// <summary>Gets an ID for the bucket to use with events.</summary>
        internal int Id => GetHashCode();

        /// <summary>Takes a builder from the bucket. If the bucket is empty, returns null.</summary>
        internal StringBuilder? Rent()
        {
            StringBuilder?[] builders = _builders;
            StringBuilder? builder = null;

            // While holding the lock, grab whatever is at the next available index and
            // update the index.  We do as little work as possible while holding the spin
            // lock to minimize contention with other threads.  The try/finally is
            // necessary to properly handle thread aborts on platforms which have them.
            bool lockTaken = false, allocateBuilder = false;
            try
            {
                _lock.Enter(ref lockTaken);

                if (_index < builders.Length)
                {
                    builder = builders[_index];
                    builders[_index++] = null;
                    allocateBuilder = builder == null;
                }
            }
            finally
            {
                if (lockTaken)
                {
                    _lock.Exit(false);
                }
            }

            // While we were holding the lock, we grabbed whatever was at the next available index, if
            // there was one. If we tried and if we got back null, that means we hadn't yet allocated
            // for that slot, in which case we should do so now.
            if (allocateBuilder)
            {
                builder = new StringBuilder(BuilderCapacity);
            }

            return builder;
        }

        /// <summary>
        /// Attempts to return the builder to the bucket.  If successful, the builder will be stored
        /// in the bucket and true will be returned; otherwise, the builder won't be stored, and false
        /// will be returned.
        /// </summary>
        internal bool TryReturn(StringBuilder builder)
        {
            // Check to see if the builder is the correct size for this bucket
            if (builder.Capacity < BuilderCapacity)
            {
                // It's not. Don't put it back in this pool.
                return false;
            }

            bool returned;

            // While holding the spin lock, if there's room available in the bucket,
            // put the builder into the next available slot. Otherwise, we just drop it.
            // The try/finally is necessary to properly handle thread aborts on platforms
            // which have them.
            bool lockTaken = false;
            try
            {
                _lock.Enter(ref lockTaken);

                returned = _index != 0;
                if (returned)
                {
                    _builders[--_index] = builder;
                }
            }
            finally
            {
                if (lockTaken)
                {
                    _lock.Exit(false);
                }
            }
            return returned;
        }
    }
}