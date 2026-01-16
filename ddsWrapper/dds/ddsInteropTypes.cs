// ddsInteropTypes.cs: Definitions of interop structs for DDS library.
// Interop-only blittable structs. No ref structs. No string. No managed arrays.
// Everything is fixed buffers or primitive fields.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DDS.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct parResultsDealer
    {
        public int number;
        public int score;
        public fixed sbyte contracts[10];
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct ddTableResults
    {
        public fixed int resTable[20];

        public int this[int hand, int suit]
        {
            get
            {
                Debug.Assert(hand >= 0 && hand < 4);
                Debug.Assert(suit >= 0 && suit < 5);
                return resTable[hand + suit * 4];
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct ddTableDealPBN
    {
        public fixed sbyte cards[80];
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct ddTableDealsPBN : IDisposable
    {
        public int noOfTables;
        public ddTableDealPBN* deals;

        /// <summary>
        /// Allocates an unmanaged array of <see cref="ddTableDealPBN"/> with length <paramref name="count"/>.
        /// Caller owns the memory and must call Dispose() to free it.
        /// </summary>
        public ddTableDealsPBN(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            noOfTables = count;
            if (count == 0) { deals = null; return; }

            // Allocate count elements of ddTableDealPBN
            //deals = (ddTableDealPBN*)NativeMemory.Alloc((nuint)count, (nuint)sizeof(ddTableDealPBN));

            // Zero-init the allocated bytes
            nuint bytes = (nuint)count * (nuint)sizeof(ddTableDealPBN);
            //Unsafe.InitBlockUnaligned((void*)deals, 0, (uint)bytes);
        }

        public void Dispose()
        {
            if (deals != null)
            {
                NativeMemory.Free(deals);
                deals = null;
                noOfTables = 0;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct ddTableDeal
    {
        public fixed uint cards[16];  // flattened [hand * 4 + suit]

        public void Set(int hand, int suit, uint mask)
        {
            Debug.Assert(hand >= 0 && hand < 4);
            Debug.Assert(suit >= 0 && suit < 4);
            cards[hand * 4 + suit] = mask;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct ddTableDeals : IDisposable
    {
        public int noOfTables;
        public ddTableDeal* tableDeals;

        /// <summary>
        /// Allocates an unmanaged array of <see cref="ddTableDeal"/> with length <paramref name="deals"/>.
        /// Caller owns the memory and must call Dispose() to free it.
        /// </summary>
        public ddTableDeals(int deals)
        {
            if (deals < 0) throw new ArgumentOutOfRangeException(nameof(deals));
            if (deals > ddsImports.ddsMaxNumberOfBoards) throw new ArgumentOutOfRangeException(nameof(deals));
            noOfTables = deals;
            //if (deals == 0) { tableDeals = null; return; }

            //deals = ddsImports.ddsMaxNumberOfBoards * ddsImports.ddsStrains;

            tableDeals = (ddTableDeal*)NativeMemory.Alloc((nuint)deals, (nuint)sizeof(ddTableDeal));
            nuint bytes = (nuint)deals * (nuint)sizeof(ddTableDeal);
            //Unsafe.InitBlockUnaligned((void*)tableDeals, 0, (uint)bytes);
        }

        public void Dispose()
        {
            if (tableDeals != null)
            {
                NativeMemory.Free(tableDeals);
                tableDeals = null;
                noOfTables = 0;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct ddTablesResult : IDisposable
    {
        public int noOfBoards;
        public ddTableResults* results;

        /// <summary>
        /// Allocates unmanaged memory for <paramref name="deals"/> ddTableResults entries.
        /// Caller owns the memory and must call Dispose() to free it.
        /// </summary>
        public ddTablesResult(int deals)
        {
            if (deals < 0) throw new ArgumentOutOfRangeException(nameof(deals));
            if (deals > ddsImports.ddsMaxNumberOfBoards) throw new ArgumentOutOfRangeException(nameof(deals));
            noOfBoards = deals;
            //if (deals == 0) { results = null; return; }

            //deals = ddsImports.ddsMaxNumberOfBoards * ddsImports.ddsStrains;

            // Allocate 'deals' elements of ddTableResults
            results = (ddTableResults*)NativeMemory.Alloc((nuint)deals, (nuint)sizeof(ddTableResults));

            // Zero-init the allocated bytes
            nuint bytes = (nuint)deals * (nuint)sizeof(ddTableResults);
            //Unsafe.InitBlockUnaligned((void*)results, 0, (uint)bytes);
        }

        public void Dispose()
        {
            if (results != null)
            {
                NativeMemory.Free(results);
                results = null;
                noOfBoards = 0;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct parScore
    {
        public fixed sbyte score[16];
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct parContract
    {
        public fixed sbyte score[128];
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct parResults
    {
        public fixed byte _scores[32];
        public fixed byte _contracts[256];

        public ref parScore Score(int i)
        {
            Debug.Assert(i >= 0 && i < (32 / 16));
            fixed (byte* p = _scores) return ref ((parScore*)p)[i];
        }

        public ref parContract Contract(int i)
        {
            Debug.Assert(i >= 0 && i < (256 / 128));
            fixed (byte* p = _contracts) return ref ((parContract*)p)[i];
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct allParResults
    {
        public fixed byte _pad[5760];   // 20 parResults = 20 × 288 = 5760

        public ref parResults Result(int i)
        {
            Debug.Assert(i >= 0 && i < 20);
            fixed (byte* p = _pad) return ref ((parResults*)p)[i];
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct dealPBN
    {
        public int trump;                 // 0..4
        public int first;                 // 0..3
        public fixed int currentTrickSuit[3];
        public fixed int currentTrickRank[3];
        public fixed sbyte remainCards[80];
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct deal
    {
        public int trump;
        public int first;
        public fixed int currentTrickSuit[3];
        public fixed int currentTrickRank[3];
        public fixed uint remainCards[16];  // flattened [hand*4+suit]
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct FutureTricks
    {
        public int nodes;
        public int cards;
        public fixed int suit[13];
        public fixed int rank[13];
        public fixed int equals[13];
        public fixed int score[13];
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct DDSInfo
    {
        public int major, minor, patch;
        public fixed sbyte versionString[10];
        public int system;
        public int numBits;
        public int compiler;
        public int constructor;
        public int numCores;
        public int threading;
        public int noOfThreads;
        public fixed sbyte threadSizes[128];
        public fixed sbyte systemString[1024];
    }
}
