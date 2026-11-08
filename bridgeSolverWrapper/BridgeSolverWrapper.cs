using Bridge;
using BridgeSolver.Interop;
using DDS;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BridgeSolver
{
    /// <summary>
    /// Managed wrapper for bridgesolver.dll (https://github.com/bridge-craftwork/bridge-solver).
    /// All deal strings use PBN format, e.g. "N:AKQT3.J6.KJ42.95 652.AK42.AQ87.T4 …".
    /// Strains: "C", "D", "H", "S", "NT" (or "N"). Seats: "N", "E", "S", "W".
    /// Vulnerable: "None", "NS", "EW", "All".
    /// </summary>
    public static class BridgeSolverWrapper
    {
        /// <summary>Returns the library version string, e.g. "2.0.0".</summary>
        public static string GetVersion()
        {
            // bs_version returns a pointer to a static string — do NOT free it.
            var ptr = BridgeSolverImports.bs_version();
            return Marshal.PtrToStringUTF8(ptr) ?? string.Empty;
        }

        /// <summary>
        /// Computes the full 20-cell double-dummy table as JSON.
        /// Returns {"tricks": [[…], …], "total": 13} with rows N,E,S,W and columns C,D,H,S,NT.
        /// On failure returns {"error": "…"}.
        /// </summary>
        //public static string DdTable(string deal)
        //{
        //    ArgumentNullException.ThrowIfNull(deal);
        //    var ptr = BridgeSolverImports.bs_dd_table(deal);
        //    return PtrToStringAndFree(ptr);
        //}

        /// <summary>
        /// Fills a 20-element array with double-dummy trick counts, indexed as
        /// tricks[seat * 5 + strain] with seats N=0, E=1, S=2, W=3
        /// and strains C=0, D=1, H=2, S=3, NT=4.
        /// </summary>
        /// <exception cref="ExternalException">Thrown when bs_dd_table_tricks returns -1 (parse error).</exception>
        public static TableResults DdTableTricks(string deal)
        {
            ArgumentNullException.ThrowIfNull(deal);
            var tricks = new byte[20];
            int rc = BridgeSolverImports.bs_dd_table_tricks(deal, ref tricks[0]);
            if (rc != 0)
                throw new ExternalException($"bs_dd_table_tricks failed with code {rc}.", rc);


            TableResults result;
            for (var hand = Seats.North; hand <= Seats.West; hand++)
            {
                for (var suit = Suits.Clubs; suit <= Suits.NoTrump; suit++)
                {
                    result[hand, suit] = tricks[(int)hand * 5 + (int)suit];
                }
            }
            return result;
        }

        private static readonly ThreadLocal<TableResults[]> tablesResults = new(() => new TableResults[1000]);
        private static readonly ThreadLocal<Stopwatch> stopwatch = new(() => new Stopwatch());

        public static List<TableResults> PossibleTricks(in List<Deal> deals, in TableResults needed, int maxDuration = 5)
        {
            int total = deals.Count;
            var results = tablesResults.Value!;
            if (deals.Count > results.Length)
                throw new ArgumentException($"Too many deals: {deals.Count} > {results.Length}", nameof(deals));

            var sw = stopwatch.Value!;
            sw.Restart();
            var dealsLocal = deals; // avoid capturing 'in' parameter in lambda
            var _needed = needed; // avoid capturing 'in' parameter in lambda
            Parallel.For(0, total, deal =>
            {
                // Check timeout before processing next batch
                var elapsedTime = sw.Elapsed.TotalSeconds;
                if (elapsedTime > maxDuration)
                {
                }
                else
                {
                    var pbn = dealsLocal[deal].ToPBN();
                    //var check = DdTableTricks(pbn);
                    for (var trump = Suits.Clubs; trump <= Suits.NoTrump; trump++)
                    {
                        for (var declarer = Seats.North; declarer <= Seats.West; declarer++)
                        {
                            if (_needed[declarer, trump] > 0)
                            {
                                var tricksNS = Solve(pbn, trump.ToXML(), declarer.Next().ToXML());
                                results[deal][declarer, trump] = declarer.Direction() == Directions.NorthSouth ? tricksNS : (byte)(13 - tricksNS);
                                //if (results[deal][declarer, trump] != check[declarer, trump])
                                //{
                                //    Trace.WriteLine($"Warning: Solve returned 0 tricks for deal {deal}, seat {declarer}, trump {trump}");
                                //}
                            }
                            else
                            {
                                results[deal][declarer, trump] = 255;
                            }
                        }
                    }
                }
            });

            return [.. results.Where(result => result[0, 0] + result[1, 0] > 0)];
        }

        public static List<CardPotential> BestCards(in GameState state, in List<Card> cards)
        {
            using (var analyzer = new Analyzer())
            {
                var response = new List<CardPotential>();
                var requestJson = @$"
                    {{
                        ""dealstr"": ""{state.RemainingCards.ToPBN()}"",
                        ""trump"": ""{state.Trump.ToXML()}"",
                        ""declarer"": ""{state.Declarer.ToXML()}"",
                        ""leader"": ""{state.TrickLeader.ToXML()}"",
                        ""plays"": [{(state.PlayedByMan1.Index == Card.Null.Index ? "" : $"\"{state.PlayedByMan1}\",")}{(state.PlayedByMan2.Index == Card.Null.Index ? "" : $"\"{state.PlayedByMan2}\",")}{(state.PlayedByMan3.Index == Card.Null.Index ? "" : $"\"{state.PlayedByMan3}\",")}""?""]
                    }}
                    ";
                foreach (var card in cards)
                {
                    var responseJson = analyzer.Play(requestJson.Replace("?", card.ToString()));
                    //     "\"contract_tricks\":10"
                    // get value of "contract_tricks" from responseJson
                    var tricks = System.Text.Json.JsonDocument.Parse(responseJson).RootElement.GetProperty("contract_tricks").GetInt32();
                    if (!state.TrickLeader.IsSameDirection(state.Declarer))
                    {
                        tricks = 13 - tricks;
                    }
                    response.Add(new CardPotential(card, tricks, true));
#if DEBUG
                    //Trace.WriteLine($"{card}:{tricks}");
#endif
                }
                return response;
            }   
        }

        /// <summary>
        /// Computes par score and optimal contracts as JSON.
        /// Returns {"score_ns": 620, "optimum_score": "NS 620", "par_contract": "NS 4S="}.
        /// On failure returns {"error": "…"}.
        /// </summary>
        /// <param name="vulnerable">"None", "NS", "EW", or "All".</param>
        public static string Par(string deal, string vulnerable)
        {
            ArgumentNullException.ThrowIfNull(deal);
            ArgumentNullException.ThrowIfNull(vulnerable);
            var ptr = BridgeSolverImports.bs_par(deal, vulnerable);
            return PtrToStringAndFree(ptr);
        }

        /// <summary>
        /// Returns the North-South double-dummy trick count (0–13) with
        /// <paramref name="leader"/> on lead in <paramref name="trump"/>.
        /// </summary>
        /// <param name="trump">"C", "D", "H", "S", "NT" (or "N").</param>
        /// <param name="leader">"N", "E", "S", or "W".</param>
        /// <exception cref="ExternalException">Thrown when the solver returns -1 (parse error).</exception>
        public static int Solve(string deal, string trump, string leader)
        {
            ArgumentNullException.ThrowIfNull(deal);
            ArgumentNullException.ThrowIfNull(trump);
            ArgumentNullException.ThrowIfNull(leader);
            int result = BridgeSolverImports.bs_solve(deal, trump, leader);
            if (result < 0)
                throw new ExternalException($"bs_solve failed with code {result}.", result);
            return result;
        }

        /// <summary>
        /// Checks whether a claim is valid against every layout of the unseen cards.
        /// <paramref name="requestJson"/> follows the bs_check_claim JSON schema.
        /// Returns {"verdict": "valid"|"invalid"|"undecided", …} or {"error": "…"}.
        /// </summary>
        public static string CheckClaim(string requestJson)
        {
            ArgumentNullException.ThrowIfNull(requestJson);
            var ptr = BridgeSolverImports.bs_check_claim(requestJson);
            return PtrToStringAndFree(ptr);
        }

        /// <summary>
        /// Hands the solver's pooled memory on the calling thread back to the allocator.
        /// Call this when a thread that used the solver is about to terminate or go idle.
        /// </summary>
        public static void ReleaseMemory() => BridgeSolverImports.bs_release_memory();

        // -----------------------------------------------------------------------
        //  Play analyser — keeps a per-instance cache of solved positions.
        //  One Analyzer per thread; not safe for concurrent use on the same instance.
        // -----------------------------------------------------------------------

        /// <summary>
        /// Stateful play analyzer that caches solved positions across card-by-card analysis.
        /// Create one instance per thread; not thread-safe for concurrent calls on the same instance.
        /// </summary>
        public sealed class Analyzer : IDisposable
        {
            private readonly BsAnalyzerHandle _handle;

            public Analyzer()
            {
                _handle = BridgeSolverImports.bs_analyzer_new();
                if (_handle.IsInvalid)
                    throw new InvalidOperationException("bs_analyzer_new returned a null handle.");
            }

            /// <summary>
            /// Analyses every card played with its double-dummy cost.
            /// <paramref name="requestJson"/> must contain "dealstr", "trump", "declarer", "leader", "plays".
            /// Returns {"contract_tricks": 10, "trace": […], "cached": false} or {"error": "…"}.
            /// </summary>
            public string Play(string requestJson)
            {
                ObjectDisposedException.ThrowIf(_handle.IsClosed, this);
                ArgumentNullException.ThrowIfNull(requestJson);
                var ptr = BridgeSolverImports.bs_analyzer_play(_handle, requestJson);
                return PtrToStringAndFree(ptr);
            }

            /// <summary>
            /// Returns the alternative cards at the given play index with their double-dummy costs.
            /// <paramref name="node"/> is 0-based.
            /// </summary>
            public string PlayNode(string requestJson, int node)
            {
                ObjectDisposedException.ThrowIf(_handle.IsClosed, this);
                ArgumentNullException.ThrowIfNull(requestJson);
                ArgumentOutOfRangeException.ThrowIfNegative(node);
                var ptr = BridgeSolverImports.bs_analyzer_play_node(_handle, requestJson, (nuint)node);
                return PtrToStringAndFree(ptr);
            }

            /// <summary>
            /// Returns a double-dummy-perfect continuation from play index <paramref name="from"/> onwards.
            /// <paramref name="from"/> is 0-based.
            /// </summary>
            public string OptimalLine(string requestJson, int from)
            {
                ObjectDisposedException.ThrowIf(_handle.IsClosed, this);
                ArgumentNullException.ThrowIfNull(requestJson);
                ArgumentOutOfRangeException.ThrowIfNegative(from);
                var ptr = BridgeSolverImports.bs_analyzer_optimal_line(_handle, requestJson, (nuint)from);
                return PtrToStringAndFree(ptr);
            }

            public void Dispose() => _handle.Dispose();
        }

        // -----------------------------------------------------------------------
        //  Internal helpers
        // -----------------------------------------------------------------------

        /// <summary>
        /// Marshals the UTF-8 string pointed to by <paramref name="ptr"/> to a managed string,
        /// then frees the native buffer via bs_free_string.
        /// </summary>
        private static string PtrToStringAndFree(nint ptr)
        {
            try
            {
                return Marshal.PtrToStringUTF8(ptr) ?? string.Empty;
            }
            finally
            {
                if (ptr != 0)
                    BridgeSolverImports.bs_free_string(ptr);
            }
        }
    }
}
