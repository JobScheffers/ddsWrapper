// ddsImports.cs

using Bridge;
using DDS.Interop;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DDS
{
    internal static unsafe partial class ddsImports
    {
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

        // ---------------------
        //   LibraryImport API
        // ---------------------

        [LibraryImport(dllPath, EntryPoint = "CalcDDtablePBN")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        internal static partial int CalcDDtablePBN(
            in ddTableDealPBN deal,
            ref ddTableResults tablep);

        [LibraryImport(dllPath, EntryPoint = "CalcDDtable")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        internal static partial int CalcDDtable(
            in ddTableDeal deal,
            ref ddTableResults tablep);

        [LibraryImport(dllPath, EntryPoint = "SolveBoardPBN")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        internal static partial int SolveBoardPBN(
            in dealPBN deal,
            int target, int solutions, int mode,
            ref FutureTricks futureTricks,
            int threadIndex);

        [LibraryImport(dllPath, EntryPoint = "SolveBoard")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        internal static partial int SolveBoard(
            in deal deal,
            int target, int solutions, int mode,
            ref FutureTricks futureTricks,
            int threadIndex);

        [LibraryImport(dllPath, EntryPoint = "CalcAllTablesPBN")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        internal static partial int CalcAllTablesPBN(
            in ddTableDealsPBN deals,
            int mode,
            int[] trumpFilter,
            ref ddTablesResult results,
            ref allParResults parResults);

        //[LibraryImport(dllPath, EntryPoint = "CalcAllTables")]
        //[UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        //internal static partial int CalcAllTables(
        //    in ddTableDeals deals,
        //    int mode,
        //    int[] trumpFilter,
        //    ref ddTablesResult results,
        //    ref allParResults parResults);

        [LibraryImport(dllPath, EntryPoint = "CalcAllTables")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        internal static partial int CalcAllTables(
            in ddTableDeals deals,
            int mode,
            in TrumpFilter5 trumpFilter,
            ref ddTablesResult results,
            ref allParResults parResults);



        [LibraryImport(dllPath, EntryPoint = "DealerPar")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        internal static partial int DealerPar(
            ref ddTableResults tablep,
            ref parResultsDealer presp,
            int dealer,
            int vulnerable);

        [LibraryImport(dllPath, EntryPoint = "GetDDSInfo")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        internal static partial void GetDDSInfo(ref DDSInfo info);

        // Unsafe version for perf
        [LibraryImport(dllPath, EntryPoint = "ErrorMessage")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static partial void ErrorMessageNative(int code, sbyte* buffer);

        [LibraryImport(dllPath, EntryPoint = "SetMaxThreads")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        internal static partial void SetMaxThreads(int userThreads);

        [LibraryImport(dllPath, EntryPoint = "SetResources")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        internal static partial void SetResources(int maxMemoryMB, int maxThreads);

        [LibraryImport(dllPath, EntryPoint = "FreeMemory")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        internal static partial int FreeMemory();

        // -------------------------
        //   Convenience wrappers
        // -------------------------

        private static unsafe string GetErrorMessage(int code)
        {
            Span<sbyte> buf = stackalloc sbyte[256];
            fixed (sbyte* p = buf)
                ErrorMessageNative(code, p);

            int len = 0;
            while (len < buf.Length && buf[len] != 0) len++;

            return new string((sbyte*)Unsafe.AsPointer(ref buf[0]), 0, len);
        }

        internal static void ThrowIfError(int rc, string name)
        {
            if (rc >= 0)
                return;

            throw new ExternalException(
                $"{name} failed with code {rc}: {GetErrorMessage(rc)}",
                rc);
        }
    }

    //internal static class ddsImportsOld
    //{
    //    private const string dllPath = "dds.dll";

    //    [DllImport(dllPath, CallingConvention = CallingConvention.Cdecl)]
    //    public static extern int CalcAllTables(ddTableDealsOld deals, int mode, int[] trumpFilter, ref ddTablesResultOld tableResults, ref allParResultsOld parResults);
    //}

    //[StructLayout(LayoutKind.Sequential)]
    //internal readonly struct ddTableDealsOld
    //{
    //    public readonly int noOfTables;
    //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = ddsImports.ddsMaxNumberOfBoards * ddsImports.ddsStrains)]
    //    public readonly ddTableDealOld[] tableDeals;

    //    public ddTableDealsOld(in List<Deal> deals)
    //    {
    //        noOfTables = deals.Count;
    //        tableDeals = new ddTableDealOld[ddsImports.ddsMaxNumberOfBoards * ddsImports.ddsStrains];
    //        for (int hand = 0; hand < deals.Count; hand++) tableDeals[hand] = new ddTableDealOld(deals[hand]);
    //    }
    //}

    //[StructLayout(LayoutKind.Sequential)]
    //internal readonly struct ddTableDealOld
    //{
    //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    //    public readonly uint[,] cards;

    //    public ddTableDealOld(in Deal deal)
    //    {
    //        cards = new uint[4, 4];
    //        for (Seats seat = Seats.North; seat <= Seats.West; seat++)
    //        {
    //            var hand = (int)DdsEnum.Convert(seat);
    //            for (Suits suit = Suits.Clubs; suit <= Suits.Spades; suit++)
    //            {
    //                var ddsSuit = (int)DdsEnum.Convert(suit);
    //                for (Ranks rank = Ranks.Two; rank <= Ranks.Ace; rank++)
    //                {
    //                    if (deal[seat, suit, rank])
    //                    {
    //                        cards[hand, ddsSuit] |= (uint)(2 << ((int)rank + 2) - 1);
    //                    }
    //                }
    //            }
    //        }
    //    }
    //}

    //[StructLayout(LayoutKind.Sequential)]
    //internal readonly struct ddTablesResultOld
    //{
    //    public readonly int noOfBoards;
    //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = ddsImports.ddsMaxNumberOfBoards * ddsImports.ddsStrains)]
    //    public readonly ddTableResults[] results;

    //    public ddTablesResultOld(int deals)
    //    {
    //        noOfBoards = deals;
    //        results = new ddTableResults[ddsImports.ddsMaxNumberOfBoards * ddsImports.ddsStrains];
    //        for (int deal = 0; deal < deals; deal++) results[deal] = new ddTableResults();
    //    }
    //}

    //[StructLayout(LayoutKind.Sequential)]
    //internal readonly struct allParResultsOld
    //{
    //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
    //    public readonly parResultsOld[] results;

    //    public allParResultsOld()
    //    {
    //        results = new parResultsOld[20];
    //        for (int i = 0; i < 20; i++) results[i] = new parResultsOld();
    //    }
    //}

    //[StructLayout(LayoutKind.Sequential)]
    //internal readonly struct parResultsOld
    //{
    //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
    //    public readonly parScoreOld[] parScores;
    //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
    //    public readonly parContractOld[] parContracts;

    //    public parResultsOld()
    //    {
    //        parScores = new parScoreOld[2];
    //        parContracts = new parContractOld[2];
    //    }
    //}

    //[StructLayout(LayoutKind.Sequential)]
    //internal readonly struct parScoreOld
    //{
    //    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
    //    public readonly string score;
    //}

    //[StructLayout(LayoutKind.Sequential)]
    //internal readonly struct parContractOld
    //{
    //    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    //    public readonly string score;
    //}
}
