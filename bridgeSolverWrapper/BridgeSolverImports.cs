using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BridgeSolver.Interop
{
    internal static partial class BridgeSolverImports
    {
        private const string dllPath = "bridgesolver";

        // Returns a pointer to a static string inside the DLL — do NOT free it.
        [LibraryImport(dllPath, EntryPoint = "bs_version")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        internal static partial nint bs_version();

        // Release a string returned by any bs_* function. NULL is ignored.
        [LibraryImport(dllPath, EntryPoint = "bs_free_string")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        internal static partial void bs_free_string(nint s);

        // Returns caller-owned JSON: {"tricks": [[…], …], "total": 13}
        [LibraryImport(dllPath, EntryPoint = "bs_dd_table", StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        internal static partial nint bs_dd_table(string deal);

        // Fills tricks[20] row-major (seat*5+strain). Returns 0 on success, -1 on failure.
        [LibraryImport(dllPath, EntryPoint = "bs_dd_table_tricks", StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        internal static partial int bs_dd_table_tricks(string deal, ref byte tricks);

        // Returns caller-owned JSON with par score / contract. vulnerable: "None","NS","EW","All".
        [LibraryImport(dllPath, EntryPoint = "bs_par", StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        internal static partial nint bs_par(string deal, string vulnerable);

        // Returns NS double-dummy tricks (0-13) or -1 on failure.
        // trump: "C","D","H","S","NT" (or "N"); leader: "N","E","S","W".
        [LibraryImport(dllPath, EntryPoint = "bs_solve", StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        internal static partial int bs_solve(string deal, string trump, string leader);

        // Returns caller-owned JSON claim verdict.
        [LibraryImport(dllPath, EntryPoint = "bs_check_claim", StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        internal static partial nint bs_check_claim(string requestJson);

        // Allocates a new play analyzer. Free with bs_analyzer_free or Dispose BsAnalyzerHandle.
        [LibraryImport(dllPath, EntryPoint = "bs_analyzer_new")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        internal static partial BsAnalyzerHandle bs_analyzer_new();

        // Called from BsAnalyzerHandle.ReleaseHandle — takes the raw IntPtr to avoid SafeHandle double-free.
        [LibraryImport(dllPath, EntryPoint = "bs_analyzer_free")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        internal static partial void bs_analyzer_free(IntPtr analyzer);

        // Returns caller-owned JSON play trace.
        [LibraryImport(dllPath, EntryPoint = "bs_analyzer_play", StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        internal static partial nint bs_analyzer_play(BsAnalyzerHandle analyzer, string requestJson);

        // Returns caller-owned JSON alternatives at the given card index (0-based).
        [LibraryImport(dllPath, EntryPoint = "bs_analyzer_play_node", StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        internal static partial nint bs_analyzer_play_node(BsAnalyzerHandle analyzer, string requestJson, nuint node);

        // Returns caller-owned JSON optimal continuation from the given card index (0-based).
        [LibraryImport(dllPath, EntryPoint = "bs_analyzer_optimal_line", StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        internal static partial nint bs_analyzer_optimal_line(BsAnalyzerHandle analyzer, string requestJson, nuint from);

        // Hand the solver's pooled memory on this thread back to the allocator.
        [LibraryImport(dllPath, EntryPoint = "bs_release_memory")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        internal static partial void bs_release_memory();
    }
}
