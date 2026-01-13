using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Exporters;
using DDS;
using Bridge;

[Config(typeof(BenchmarkConfig))]
[SimpleJob(RuntimeMoniker.Net70)]
[MemoryDiagnoser]
public class SolveBoardBench
{
    private Deal deal1;
    private Deal deal2;
    private Deal deal3;

    [GlobalSetup]
    public void Setup()
    {
        // Ensure DDS is initialized
        ddsWrapper.ForgetPreviousBoard();
        deal1 = new Deal("N:T9.2.732.T .JT5.T4.J4 54...A9862 .A874.K9.");
        deal2 = new Deal("N:JT984.T7.AQ83.4 Q7532.82.97.832 K.AQJ53.KJ42.AK A6.K964.T65.Q96");
        deal3 = new Deal("N:9..85432.QJ9 754.JT73.KT. J82.KQ6.QJ.6 AKQT63.5..8");
    }

    [Benchmark]
    public void BestCards_AllStates()
    {
        var state1 = new GameState(in deal1, Suits.Spades, Seats.West, CardDeck.Instance[Suits.Hearts, Ranks.King], Bridge.Card.Null, Bridge.Card.Null);
        var state2 = new GameState(in deal2, Suits.Hearts, Seats.South);
        var state3 = new GameState(in deal3, Suits.Spades, Seats.West, CardDeck.Instance[Suits.Clubs, Ranks.Seven], Bridge.Card.Null, Bridge.Card.Null);

        var r1 = ddsWrapper.BestCards(state1);
        var r2 = ddsWrapper.BestCards(state2);
        var r3 = ddsWrapper.BestCards(in state3);
    }
}

public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddLogger(ConsoleLogger.Default);
        AddExporter(MarkdownExporter.GitHub);
    }
}