// ddsImports.cs

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
}
