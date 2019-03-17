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

using System.Diagnostics;
using System.Threading;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// A type that computes the first type in a list of types that a given type has a reference conversion to.
    /// It can be used to efficiently implement a type switch for a large number of types.
    /// </summary>
    public class TypeSwitchDispatch
    {
        // The elements of this array do not need to be weak as the Dispatch would typically be stored in a static
        // variable of a class that explicitly references these types.  Until the referencing code is unloaded,
        // those referenced types cannot be unloaded.  And once the referencing code is unloaded, the static
        // variable containing the instance of this type would be gone.
        private readonly Type[] _types;

        private object _lock;
        private Entry[] _buckets;
        private int _nEntries;

        /// <summary>
        /// The initial number of buckets, which must be a power of two.
        /// </summary>
        private const int _initialBuckets = 32;

        struct Entry
        {
            public WeakReference<Type> ReferenceToType;
            public int TypeHash;
            public int Result;
        }

        public TypeSwitchDispatch(Type[] types)
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
                var nEntries = this._nEntries;

                int startBucket = typeHash & mask;
                for (int i = 0; i < nBuckets; i++)
                {
                    int bucket = (i + startBucket) & mask;
                    ref Entry entry = ref buckets[bucket];
                    if (entry.ReferenceToType is null)
                    {
                        // not found; insert it!
                        if (expandIfNecessary())
                            goto retry;
                        int result = computeResult();
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

                int computeResult()
                {
                    var types = _types;
                    for (int i = 0, n = types.Length; i < n; i++)
                    {
                        if (types[i].IsAssignableFrom(type))
                            return i;
                    }

                    return -1;
                }

                bool expandIfNecessary()
                {
                    if ((nEntries << 2) < nBuckets)
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
                            nEntries = _nEntries = (nEntries - 1);
                            continue;
                        }

                        int movedStartBucket = entryToMove.TypeHash & newMask;
                        for (int k = 0; k < newNBuckets; k++)
                        {
                            int newBucket = (k + movedStartBucket) & mask;
                            if (newBuckets[newBucket].ReferenceToType is null)
                            {
                                newBuckets[newBucket] = entryToMove;
                                goto nextEntry;
                            }
                        }

                        throw new Exception("Unreachable");

                    nextEntry:;
                    }

                    this._buckets = newBuckets;
                    return true;
                }
            }
        }
    }
}
