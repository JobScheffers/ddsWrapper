
// ddsImports.cs
// Target: net7.0 or net8.0

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace DDS
{
  [SuppressUnmanagedCodeSecurity] // Optional: only if acceptable in your trust boundary
  internal static partial class ddsImports
  {
    // Omit ".dll" to allow cross-platform probing (Windows: dds.dll, Linux: libdds.so, macOS: libdds.dylib)
    private const string dllPath = "dds";

    public const int ddsMaxNumberOfBoards = 200;
    public const int ddsStrains = 5;
    public const int ddsMaxThreads = 16;

    private static readonly Lazy<int> _maxThreads = new(() =>
    {
      DDSInfo info = default;
      GetDDSInfo(ref info);
      return info.noOfThreads > ddsMaxThreads ? ddsMaxThreads : info.noOfThreads;
    });

    public static int MaxThreads => _maxThreads.Value;

    // -----------------------------
    // LibraryImport declarations
    // -----------------------------

    [LibraryImport(dllPath, EntryPoint = "CalcDDtablePBN")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial int CalcDDtablePBN(
        in ddTableDealPBN tableDealPbn,
        ref ddTableResults tablep);

    [LibraryImport(dllPath, EntryPoint = "SolveBoardPBN")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial int SolveBoardPBN(
        in dealPBN dealPBN,
        int target,
        int solutions,
        int mode,
        ref FutureTricks futureTricks,
        int threadIndex);

    [LibraryImport(dllPath, EntryPoint = "SolveBoard")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial int SolveBoard(
        in deal deal,
        int target,
        int solutions,
        int mode,
        ref FutureTricks futureTricks,
        int threadIndex);

    [LibraryImport(dllPath, EntryPoint = "CalcAllTablesPBN")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial int CalcAllTablesPBN(
        in ddTableDealsPBN deals,
        int mode,
        [MarshalAs(UnmanagedType.LPArray)] int[] trumpFilter,
        ref ddTablesResult tableResults,
        ref allParResults parResults);

    [LibraryImport(dllPath, EntryPoint = "CalcAllTables")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial int CalcAllTables(
        in ddTableDeals deals,
        int mode,
        [MarshalAs(UnmanagedType.LPArray)] int[] trumpFilter,
        ref ddTablesResult tableResults,
        ref allParResults parResults);

    [LibraryImport(dllPath, EntryPoint = "DealerPar")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial int DealerPar(
        ref ddTableResults tablep,
        ref parResultsDealer presp,
        int dealer,
        int vulnerable);

    [LibraryImport(dllPath, EntryPoint = "SetMaxThreads")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial void SetMaxThreads(int userThreads);

    [LibraryImport(dllPath, EntryPoint = "GetDDSInfo")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial void GetDDSInfo(ref DDSInfo info);

    // If the native function expects a writable char buffer, use an unsafe pointer for performance.
    // Safe IntPtr version (if you must):
    [LibraryImport(dllPath, EntryPoint = "ErrorMessage")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial void ErrorMessage(int code, IntPtr buffer);

    // Faster unsafe version for hot paths:
    [LibraryImport(dllPath, EntryPoint = "ErrorMessage")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void ErrorMessageUnsafe(int code, sbyte* buffer);

    [LibraryImport(dllPath, EntryPoint = "SetResources")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial void SetResources(int maxMemoryMB, int maxThreads);

    [LibraryImport(dllPath, EntryPoint = "FreeMemory")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial int FreeMemory();

    // -----------------------------
    // Convenience wrappers
    // -----------------------------

    internal static unsafe void ErrorMessage(int code, Span<sbyte> buffer)
    {
      fixed (sbyte* p = buffer)
      {
        ErrorMessageUnsafe(code, p);
      }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfError(int rc, string apiName)
    {
      if (rc >= 0) return;

      Span<sbyte> buf = stackalloc sbyte[256];
      ErrorMessage(rc, buf);

      string msg = AnsiToString(buf);
      throw new ExternalException($"{apiName} failed with code {rc}: {msg}", rc);
    }

    private static unsafe string AnsiToString(Span<sbyte> buf)
    {
      int len = 0;
      for (; len < buf.Length; len++)
      {
        if (buf[len] == 0) break;
      }
      if (len == 0) return string.Empty;

      fixed (sbyte* p = buf)
      {
        return new string(p, 0, len);
      }
    }
  }

  // -----------------------------------------
  // Placeholder blittable structs — replace with real layouts
  // -----------------------------------------

  //[StructLayout(LayoutKind.Sequential)]
  //internal struct DDSInfo
  //{
  //  public int noOfThreads;
  //  public int versionMajor;
  //  public int versionMinor;
  //}

  //[StructLayout(LayoutKind.Sequential)]
  //internal struct dealPBN
  //{
  //  public int dealer;
  //  public int vulnerable;
  //  // ...
  //}

  //[StructLayout(LayoutKind.Sequential)]
  //internal struct deal
  //{
  //  public int dealer;
  //  public int vulnerable;
  //  // ...
  //}

  //[StructLayout(LayoutKind.Sequential)]
  //internal struct FutureTricks
  //{
  //  public int nodes;
  //  public int cards;
  //  // ...
  //}

  //[StructLayout(LayoutKind.Sequential)]
  //internal struct ddTableDealPBN
  //{
  //  public int dummy;
  //  // ...
  //}

  //[StructLayout(LayoutKind.Sequential)]
  //internal struct ddTableDealsPBN
  //{
  //  public IntPtr deals; // or use unsafe fixed buffers if fixed-size
  //  public int noOfDeals;
  //}

  //[StructLayout(LayoutKind.Sequential)]
  //internal struct ddTableDeals
  //{
  //  public IntPtr deals;
  //  public int noOfDeals;
  //}

  //[StructLayout(LayoutKind.Sequential)]
  //internal struct ddTableResults
  //{
  //  public int score00;
  //  public int score01;
  //  // ...
  //}

  //[StructLayout(LayoutKind.Sequential)]
  //internal struct ddTablesResult
  //{
  //  public IntPtr pResults;
  //  public int noOfBoards;
  //}

  //[StructLayout(LayoutKind.Sequential)]
  //internal struct allParResults
  //{
  //  public int dummy;
  //}

  //[StructLayout(LayoutKind.Sequential)]
  //internal struct parResultsDealer
  //{
  //  public int dealerScore;
  //}
}
