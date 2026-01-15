
// Interop-only blittable structs. No ref structs. No string. No managed arrays.
// Everything is fixed buffers or primitive fields.

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

    public readonly int Get(int hand, int suit) => resTable[4 * suit + hand];
    public void Set(int hand, int suit, int val) => resTable[4 * suit + hand] = val;
  }

  [StructLayout(LayoutKind.Sequential)]
  public unsafe struct ddTableDealPBN
  {
    public fixed sbyte cards[80];
  }

  [StructLayout(LayoutKind.Sequential)]
  public unsafe struct ddTableDealsPBN
  {
    public int noOfTables;
    public ddTableDealPBN* deals;
  }

  [StructLayout(LayoutKind.Sequential)]
  public unsafe struct ddTableDeal
  {
    public fixed uint cards[16];  // flattened [hand * 4 + suit]

    public void Set(int hand, int suit, uint mask)
        => cards[hand * 4 + suit] = mask;
  }

  [StructLayout(LayoutKind.Sequential)]
  public unsafe struct ddTableDeals
  {
    public int noOfTables;
    public ddTableDeal* tableDeals;
  }

  [StructLayout(LayoutKind.Sequential)]
  public unsafe struct ddTablesResult
  {
    public int noOfBoards;
    public ddTableResults* results;

    public ddTablesResult(int deals)
    {
      noOfBoards = deals;

      // Allocate unmanaged memory for `deals` ddTableResults entries
      results = (ddTableResults*)NativeMemory.Alloc(
          (nuint)deals,
          (nuint)sizeof(ddTableResults));

      // Initialize each entry (optional but recommended)
      for (int i = 0; i < deals; i++)
      {
        results[i] = default;
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
    { fixed (byte* p = _scores) return ref ((parScore*)p)[i]; }

    public ref parContract Contract(int i)
    { fixed (byte* p = _contracts) return ref ((parContract*)p)[i]; }
  }

  [StructLayout(LayoutKind.Sequential)]
  public unsafe struct allParResults
  {
    public fixed byte _pad[5760];   // 20 parResults = 20 × 288 = 5760

    public ref parResults Result(int i)
    { fixed (byte* p = _pad) return ref ((parResults*)p)[i]; }
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
