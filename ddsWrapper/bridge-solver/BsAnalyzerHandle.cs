using BridgeSolver.Interop;
using Microsoft.Win32.SafeHandles;

namespace BridgeSolver
{
    /// <summary>
    /// SafeHandle wrapper around the opaque BsAnalyzer* returned by bs_analyzer_new().
    /// Ensures bs_analyzer_free() is always called, even if an exception is thrown.
    /// </summary>
    internal sealed class BsAnalyzerHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        // Called by the LibraryImport source-generator when bs_analyzer_new() returns.
        public BsAnalyzerHandle() : base(ownsHandle: true) { }

        protected override bool ReleaseHandle()
        {
            // Pass the raw IntPtr directly to avoid triggering SafeHandle's own ref-counting.
            BridgeSolverImports.bs_analyzer_free(handle);
            return true;
        }
    }
}
