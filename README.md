# ddsWrapper

A .NET wrapper for double-dummy bridge solvers, bundling two native engines:

- **[DDS](https://github.com/dds-bridge/dds)** — Bo Haglund's battle-tested double-dummy solver.
- **[bridge-solver](https://github.com/bridge-craftwork/bridge-solver)** — a modern Rust-based solver with card-by-card play analysis and claim checking.

Deals are expressed in [PBN](https://www.tistis.nl/pbn/) format, e.g.:

```
N:AKQT3.J6.KJ42.95 652.AK42.AQ87.T4 J74.QT95.T.AK863 98.873.9653.QJ72
```

---

## Requirements

- Windows x64
- .NET 10 or later
- The Visual C++ Redistributable (MSVC runtime) must be installed, as `bridgesolver.dll` and `dds.dll` depend on it.

---

## Installation

```
dotnet add package ddsWrapper
```

Both native DLLs (`dds.dll` and `bridgesolver.dll`) are automatically copied to your output directory on build.

---

## Usage

### DDS — Double-Dummy Solver

Wraps `dds.dll` for high-performance, multi-threaded double-dummy analysis.

```csharp
using DDS;

// Solve a single board: which cards can be played and how many tricks does each score?
var state = new GameState(...);
List<CardPotential> cards = ddsWrapper.BestCards(state);

// Full double-dummy table: tricks for every seat × strain combination
TableResults table = ddsWrapper.CalcDDtable(deal);
byte tricksNorthSpades = table[Seats.North, Suits.Spades];
```

### BridgeSolverWrapper — bridge-solver

Wraps `bridgesolver.dll` for double-dummy tables, card-by-card play analysis, and claim checking.

#### Library version

```csharp
using BridgeSolver;

string version = BridgeSolverWrapper.GetVersion(); // e.g. "2.0.0"
```

#### Double-dummy table

Returns trick counts for all 20 seat × strain combinations.

```csharp
string deal = "N:AKQT3.J6.KJ42.95 652.AK42.AQ87.T4 J74.QT95.T.AK863 98.873.9653.QJ72";
TableResults tricks = BridgeSolverWrapper.DdTableTricks(deal);

byte northInSpades = tricks[Seats.North, Suits.Spades];
```

#### Batch double-dummy analysis

Solve up to 1,000 deals in parallel, returning only deals where at least one contract is makeable.

```csharp
List<Deal> deals = ...; // up to 1000 deals
TableResults needed = ...; // which contracts to evaluate (255 = skip)

List<TableResults> results = BridgeSolverWrapper.PossibleTricks(deals, needed, maxDuration: 5);
```

#### Card-by-card play analysis

Evaluate which of a set of candidate cards gives the best outcome for declarer.

```csharp
GameState state = ...;
List<Bridge.Card> candidates = ...;

List<CardPotential> best = BridgeSolverWrapper.BestCards(state, candidates);
```

#### Stateful play analysis with `Analyzer`

`Analyzer` maintains state across cards played in the same deal. Dispose when done.

```csharp
using var analyzer = new BridgeSolverWrapper.Analyzer();

string requestJson = """
    {
        "dealstr": "N:AKQT3.J6.KJ42.95 652.AK42.AQ87.T4 J74.QT95.T.AK863 98.873.9653.QJ72",
        "trump": "S",
        "declarer": "N",
        "leader": "E",
        "plays": ["C2"]
    }
    """;

string responseJson = analyzer.Play(requestJson);
// {"contract_tricks": 10, ...}
```

---

## Conventions

| Concept | Values |
|---|---|
| Strains | `"C"`, `"D"`, `"H"`, `"S"`, `"NT"` |
| Seats | `"N"`, `"E"`, `"S"`, `"W"` |
| Vulnerable | `"None"`, `"NS"`, `"EW"`, `"All"` |
| Deal format | PBN, e.g. `"N:AK.... ..."` |

Trick counts in `TableResults` are indexed as `[seat, strain]` using the `Seats` and `Suits` enums,
with seats N=0, E=1, S=2, W=3 and strains C=0, D=1, H=2, S=3, NT=4.

---

## Native DLLs

| File | Source | Licence |
|---|---|---|
| `dds.dll` | [dds-bridge/dds](https://github.com/dds-bridge/dds) | Apache-2.0 |
| `bridgesolver.dll` | [bridge-craftwork/bridge-solver](https://github.com/bridge-craftwork/bridge-solver) | MIT OR Apache-2.0 |

Third-party notices for `bridgesolver.dll` are included in `bridge-solver/THIRD-PARTY-NOTICES`.

---

## Licence

MIT OR Apache-2.0 — see repository for details.
