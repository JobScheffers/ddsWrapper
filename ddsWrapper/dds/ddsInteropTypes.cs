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
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            noOfTables = count;
            if (count == 0) { deals = null; return; }

            // Allocate count elements of ddTableDealPBN
            //deals = (ddTableDealPBN*)NativeMemory.Alloc((nuint)count, (nuint)sizeof(ddTableDealPBN));

            // Zero-init the allocated bytes
            //nuint bytes = (nuint)count * (nuint)sizeof(ddTableDealPBN);
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
    public unsafe struct ddTableDeals
    {
        public int noOfTables;
        public fixed uint tableDeals[ddsImports.ddsMaxNumberOfBoards * ddsImports.ddsStrains * 16];

        /// <summary>
        /// Indexer to access the 16-card array for a specific deal
        /// </summary>
        public Span<uint> this[int deal]
        {
            get
            {
                if (deal < 0 || deal >= noOfTables)
                    throw new ArgumentOutOfRangeException(nameof(deal));

                fixed (uint* p = tableDeals)
                {
                    // Each deal has 16 uints (4 hands × 4 suits)
                    return new Span<uint>(p + deal * 16, 16);
                }
            }
        }

        /// <summary>
        /// Convenience: access one card by deal, hand, suit
        /// </summary>
        public uint this[int deal, int hand, int suit]
        {
            get
            {
                var span = this[deal];
                return span[hand * 4 + suit];
            }
            set
            {
                var span = this[deal];
                span[hand * 4 + suit] = value;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct ddTablesResult
    {
        public int noOfBoards;
        public fixed int results[
            ddsImports.ddsMaxNumberOfBoards *
            ddsImports.ddsStrains *
            20];

        public ddTablesResult(int deals, int strains)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(deals);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(deals * strains, ddsImports.ddsMaxNumberOfBoards);
            noOfBoards = deals;
        }

        public int this[int deal, int hand, int suit]
        {
            get
            {
                Debug.Assert(deal >= 0 && deal < noOfBoards);
                Debug.Assert(hand >= 0 && hand < 4);
                Debug.Assert(suit >= 0 && suit < 5);

                int tableOffset = deal * 20;
                int cell = hand + suit * 4;
                return results[tableOffset + cell];
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
        public fixed sbyte parScores[2 * 16];       // 2 parScore
        public fixed sbyte parContracts[2 * 128];   // 2 parContract
    }


    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct allParResults
    {
        public fixed sbyte results[20 * (2 * 16 + 2 * 128)];
        // 20 * 288 = 5760 bytes
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

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct TrumpFilter5
    {
        public fixed int values[5];
    }
}
