using System.Runtime.InteropServices;

namespace DDS
{
    internal static class ddsImports
    {
#if NET6_0_OR_GREATER
        public const int ddsMaxNumberOfBoards = 200;
#else
        public const int ddsMaxNumberOfBoards = 160;
#endif
        public const int ddsStrains = 5;
        public const int ddsMaxThreads = 16;
        private const string dllPath = "dds.dll";
        public static readonly int MaxThreads;

        [DllImport(dllPath)]
        public static extern int CalcDDtablePBN(ddTableDealPBN tableDealPbn, ref ddTableResults tablep);

        /// <summary>
        /// For equivalent  cards only the highest is returned.
        /// target=1-13, solutions=1:  Returns only one of the cards. 
        /// Its returned score is the same as target whentarget or higher tricks can be won. 
        ///  Otherwise, score –1 is returned if target cannot be reached, or score 0 if no tricks can be won. 
        /// target=-1, solutions=1:  Returns only one of the optimum cards and its score.
        /// </summary>
        /// <param name="dealPBN"></param>
        /// <param name="target">the number of tricks to be won by the side to play, -1 means that the program shall find the maximum number</param>
        /// <param name="solutions"></param>
        /// <param name="mode"></param>
        /// <param name="futureTricks"></param>
        /// <param name="threadIndex"></param>
        /// <returns></returns>
        [DllImport(dllPath)]
        public static extern int SolveBoardPBN(dealPBN dealPBN, int target, int solutions, int mode, ref FutureTricks futureTricks, int threadIndex);

        [DllImport(dllPath)]
        public static extern int SolveBoard(deal deal, int target, int solutions, int mode, ref FutureTricks futureTricks, int threadIndex);

        [DllImport(dllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern int CalcAllTablesPBN(ddTableDealsPBN deals, int mode, int[] trumpFilter, ref ddTablesResult tableResults, ref allParResults parResults);

        [DllImport(dllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern int CalcAllTables(ddTableDeals deals, int mode, int[] trumpFilter, ref ddTablesResult tableResults, ref allParResults parResults);

        [DllImport(dllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern int DealerPar(ref ddTableResults tablep, ref parResultsDealer presp, int dealer, int vulnerable);

        [DllImport(dllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetMaxThreads(int userThreads);

        [DllImport(dllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern void GetDDSInfo(ref DDSInfo info);

        [DllImport(dllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ErrorMessage(int code, Char[] line);

        [DllImport(dllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetResources(int maxMemoryMB, int maxThreads);

        [DllImport(dllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern int FreeMemory();

        static ddsImports()
        {
            DDSInfo info = default;
            GetDDSInfo(ref info);
            MaxThreads = info.noOfThreads > ddsMaxThreads ? ddsMaxThreads : info.noOfThreads;
        }
    }
}
