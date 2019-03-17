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

        private Entry[] _buckets;
        private int _nBuckets;
        private int _mask;
        private int _nEntries;

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

            int mask;
            Entry[] buckets;
            int nBuckets;
            int nEntries;
            lock (this)
            {
                mask = this._mask;
                buckets = this._buckets;
                nBuckets = this._nBuckets;
                nEntries = this._nEntries;
            }

            // First, try a lookup with minimal locking
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

            Debug.Assert(buckets is null);
            return GetIndexSlow(type, typeHash);
        }

        private int GetIndexSlow(Type type, int typeHash)
        {
            lock (this)
            {
                if (this._buckets is null)
                {
                    const int startNbuckets = 32;
                    this._nBuckets = startNbuckets;
                    this._buckets = new Entry[startNbuckets];
                    this._mask = _nBuckets - 1;
                    this._nEntries = 0;
                }

            retry:;
                var mask = this._mask;
                var buckets = this._buckets;
                var nBuckets = this._nBuckets;
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
                    if ((nEntries << 1) < nBuckets)
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

                    this._mask = newMask;
                    this._buckets = newBuckets;
                    this._nBuckets = newNBuckets;
                    return true;
                }
            }
        }
    }
}
