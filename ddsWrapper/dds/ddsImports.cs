using System.Runtime.InteropServices;

namespace DDS
{
    internal static class ddsImports
    {
        public const int ddsMaxNumberOfBoards = 200;
        public const int ddsStrains = 5;
        public const string dllPath = "dds8a.dll";

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

        [DllImport(dllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern int CalcAllTablesPBN(ddTableDealsPBN deals, int mode, int[] trumpFilter, ref ddTablesResult tableResults, ref allParResults parResults);

        [DllImport(dllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern int CalcAllTables(ddTableDeals deals, int mode, int[] trumpFilter, ref ddTablesResult tableResults, ref allParResults parResults);

        [DllImport(dllPath, CallingConvention = CallingConvention.Cdecl)]
        public static extern int DealerPar(ref ddTableResults tablep, ref parResultsDealer presp, int dealer, int vulnerable);

        static ddsImports()
        {
            ResourceExtractor.ExtractResourceToFile("ddsWrapper.dds.dds.dll", dllPath);
        }
    }

    internal static class ResourceExtractor
    {
        public static void ExtractResourceToFile(string resourceName, string filename)
        {
            if (!File.Exists(filename))
            {
                //var resources = typeof(ddsWrapper).Assembly.GetManifestResourceNames();
                using (Stream s = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)!)
                using (FileStream fs = new FileStream(filename, FileMode.Create))
                {
                    byte[] b = new byte[s.Length];
                    s.Read(b, 0, b.Length);
                    fs.Write(b, 0, b.Length);
                }
            }
        }
    }
}
