// Copyright Neal Gafter 2019.

//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System.Threading;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// A type that computes the first type in a list of types that a given type has a reference conversion to.
    /// It can be used to efficiently implement a type switch for a large number of types.
    /// </summary>
    public struct TypeSwitchDispatch
    {
        // The elements of this array do not need to be weak as the Dispatch would typically be stored in a static
        // variable of a class that explicitly references these types.  Until the referencing code is unloaded,
        // those referenced types cannot be unloaded.  And once the referencing code is unloaded, the static
        // variable containing the instance of this type would be gone.
        private readonly Type[] _types;

        // These are allocated lazily.  If GetIndex is never called, _lock and _buckets are never allocated.
        private object _lock;
        private Entry[] _buckets;
        private int _nEntries;

        /// <summary>
        /// The initial number of buckets, which must be a power of two.  Since we only expect this to be
        /// used for more than 10 types, and we maintain a load factor in the cache of less than 1/4, we
        /// use the first power of two greater than 10*4.
        /// </summary>
        private const int _initialBuckets = 64;

        private struct Entry
        {
            // We use a weak reference so that dispatching on a type does not prevent it from being unloaded.
            public WeakReference<Type> ReferenceToType;
            public int TypeHash;
            public int Result;
        }

        public TypeSwitchDispatch(params Type[] types)
        {
            if (types is null)
                throw new ArgumentNullException("types");

            int n = types.Length;
            var copiedTypesArray = new Type[n];
            for (int i = 0; i < n; i++)
            {
                var type = types[i];
                if (type is null)
                    throw new ArgumentNullException($"types[{i}]");

                copiedTypesArray[i] = type;
            }

            this._types = copiedTypesArray;
            this._nEntries = 0;

            // _lock and _buckets are created lazily
            this._lock = null;
            this._buckets = null;
        }

        /// <summary>
        /// Get the index of the first type in the original list of types to which this data's type can be assigned.
        /// </summary>
        public int GetIndex(object data)
        {
            if (data is null)
                return -1;

            Type type = data.GetType();
            int typeHash = type.GetHashCode();

            // First, we try a cache fetch without locks.  If the entry is not found, we lock and do it the hard way.
            Entry[] buckets = this._buckets;
            if (buckets is null)
                return GetIndexSlow(type, typeHash);

            int nBuckets = buckets.Length;
            int mask = nBuckets - 1;

            int startBucket = typeHash & mask;
            for (int i = 0; i < nBuckets; i++)
            {
                int bucket = (i + startBucket) & mask;
                Entry entry = buckets[bucket]; // possible struct tearing here, but the writes were ordered so it doesn't matter
                if (entry.ReferenceToType is null)
                {
                    // not found; insert it!
                    return GetIndexSlow(type, typeHash);
                }
                else if (entry.TypeHash == typeHash && entry.ReferenceToType.TryGetTarget(out Type entryType) && type.Equals(entryType))
                {
                    return entry.Result;
                }
            }

            throw new Exception("This location is believed unreachable.");
        }

        private int GetIndexSlow(Type type, int typeHash)
        {
            if (this._lock is null)
                Interlocked.CompareExchange(ref this._lock, new object(), null);

            lock (this._lock)
            {
                if (this._buckets is null)
                    this._buckets = new Entry[_initialBuckets];

            retry:;
                var buckets = this._buckets;
                int nBuckets = buckets.Length;
                int mask = nBuckets - 1;

                int startBucket = typeHash & mask;
                for (int i = 0; i < nBuckets; i++)
                {
                    int bucket = (i + startBucket) & mask;
                    ref Entry entry = ref buckets[bucket];
                    if (entry.ReferenceToType is null)
                    {
                        // not found; insert it!
                        if (ExpandIfNecessary())
                            goto retry;

                        int result = ComputeResult(type);
                        entry.Result = result;
                        entry.TypeHash = typeHash;
                        Interlocked.CompareExchange(ref entry.ReferenceToType, new WeakReference<Type>(type), null);
                        this._nEntries++;
                        return result;
                    }
                    else if (entry.TypeHash == typeHash && entry.ReferenceToType.TryGetTarget(out Type entryType) && type.Equals(entryType))
                    {
                        return entry.Result;
                    }
                }

                throw new Exception("Unreachable");
            }
        }

        /// <summary>
        /// Compute the result.
        /// </summary>
        private int ComputeResult(Type type)
        {
            Type[] types = this._types;
            for (int i = 0, n = types.Length; i < n; i++)
            {
                if (types[i].IsAssignableFrom(type))
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Expand the <see cref="_buckets"/> array if necessary.  To be called while the lock is held.
        /// </summary>
        /// <returns>true if we expanded the <see cref="_buckets"/> array.</returns>
        private bool ExpandIfNecessary()
        {
            // Maintain a load factor of less than 1/4
            var buckets = this._buckets;
            int nBuckets = buckets.Length;
            if ((this._nEntries << 2) < nBuckets)
                return false;

            int newNBuckets = nBuckets << 1;
            int newMask = newNBuckets - 1;
            var newBuckets = new Entry[newNBuckets];

            for (int j = 0; j < nBuckets; j++)
            {
                var entryToMove = buckets[j];
                if (entryToMove.ReferenceToType is null)
                {
                    continue;
                }
                else if (!entryToMove.ReferenceToType.TryGetTarget(out _))
                {
                    // clear an expired weak reference while expanding
                    this._nEntries--;
                    continue;
                }

                int movedStartBucket = entryToMove.TypeHash & newMask;
                for (int k = 0; k < newNBuckets; k++)
                {
                    int newBucket = (k + movedStartBucket) & newMask;
                    ref Entry bucketEntry = ref newBuckets[newBucket];
                    if (bucketEntry.ReferenceToType is null)
                    {
                        bucketEntry = entryToMove;
                        goto nextEntry;
                    }
                }

                throw new Exception("This location is believed unreachable.");

            nextEntry:;
            }

            this._buckets = newBuckets;
            return true;
        }
    }
}
