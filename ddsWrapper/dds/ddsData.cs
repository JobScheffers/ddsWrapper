
// ddsRichTypes.cs
// High-level managed types. Nice domain model, strings, arrays.
// Mapped into blittable interop structs.

using System;
using System.Collections.Generic;
using DDS.Interop;

namespace DDS
{
  public enum Suit { Spades = 0, Hearts = 1, Diamonds = 2, Clubs = 3, NT = 4 }
  public enum Rank { Two = 2, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King, Ace }
  public enum Hand { North = 0, East = 1, South = 2, West = 3 }

  public readonly struct Card
  {
    public Suit Suit { get; }
    public Rank Rank { get; }
    public Card(Suit s, Rank r) { Suit = s; Rank = r; }
  }

  // Example of your played-cards structure
  public readonly struct PlayedCards
  {
    public Suit S1 { get; }
    public Suit S2 { get; }
    public Suit S3 { get; }
    public Rank R1 { get; }
    public Rank R2 { get; }
    public Rank R3 { get; }

    public PlayedCards(Suit s1, Rank r1, Suit s2, Rank r2, Suit s3, Rank r3)
    {
      S1 = s1; R1 = r1;
      S2 = s2; R2 = r2;
      S3 = s3; R3 = r3;
    }
  }

  // Your richer Deal type
  public class Deal
  {
    public bool[,,] Cards { get; }  // [seat, suit, rank]
    public Deal(bool[,,] cards) => Cards = cards;
  }

  // ------------------------
  // Conversion helpers
  // ------------------------

  public static unsafe class DdsInteropConverters
  {
    public static ddTableDealPBN ToInteropTableDealPbn(string pbn)
    {
      ddTableDealPBN d = default;
      var span = AsSpan(ref d);
      WriteAnsiToSpan(pbn, span);
      //fixed (sbyte* p = d.cards)
      //  WriteAnsi(pbn, p, 80);
      return d;
    }

    public static ddTableDeal ToInteropTableDeal(Deal deal)
    {
      ddTableDeal d = default;
      for (int seat = 0; seat < 4; seat++)
        for (int suit = 0; suit < 4; suit++)
        {
          uint mask = 0;
          for (int r = 2; r <= 14; r++)
            if (deal.Cards[seat, suit, r])
              mask |= (uint)(1 << (r - 1));

          d.Set(seat, suit, mask);
        }
      return d;
    }

    public static ddTableDeals ToInteropTableDeals(in List<Deal> deals)
    {
      ddTableDeals d = default;
      d.noOfTables = deals.Count;
      //tableDeals = new ddTableDeal[ddsImports.ddsMaxNumberOfBoards * ddsImports.ddsStrains];
      for (int hand = 0; hand < deals.Count; hand++) d.tableDeals[hand] = ToInteropTableDeal(deals[hand]);
      return d;
    }

    public static dealPBN ToInteropDealPBN(
        Suit trump, Hand leader,
        IReadOnlyList<Card> currentTrick, string remaining)
    {
      dealPBN d = default;
      d.trump = (int)trump;
      d.first = (int)leader;

      unsafe
      {
        for (int i = 0; i < currentTrick.Count && i < 3; i++)
        {
          d.currentTrickSuit[i] = (int)currentTrick[i].Suit;
          d.currentTrickRank[i] = (int)currentTrick[i].Rank;
        }


        var span = AsSpan(ref d);
        WriteAnsiToSpan(remaining, span);
      }

      return d;
    }

    public static deal ToInteropDeal(
        Suit trump, Hand leader,
        PlayedCards played,
        Deal dealRemaining)
    {
      deal d = default;
      d.trump = (int)trump;
      d.first = (int)leader;

      unsafe
      {
        d.currentTrickSuit[0] = (int)played.S1;
        d.currentTrickRank[0] = (int)played.R1;
        d.currentTrickSuit[1] = (int)played.S2;
        d.currentTrickRank[1] = (int)played.R2;
        d.currentTrickSuit[2] = (int)played.S3;
        d.currentTrickRank[2] = (int)played.R3;

        for (int seat = 0; seat < 4; seat++)
          for (int suit = 0; suit < 4; suit++)
          {
            uint mask = 0;
            for (int r = 2; r <= 14; r++)
              if (dealRemaining.Cards[seat, suit, r])
                mask |= (uint)(1 << (r - 1));

            d.remainCards[seat * 4 + suit] = mask;
          }
      }
      return d;
    }

    // ASCII write helper
    private static unsafe void WriteAnsi(
        ReadOnlySpan<char> src, sbyte* dest, int cap)
    {
      int i = 0;
      for (; i < src.Length && i < cap - 1; i++)
        dest[i] = (sbyte)(src[i] <= 0x7F ? src[i] : '?');
      dest[i] = 0;
    }


    public static void WriteAnsiToSpan(ReadOnlySpan<char> src, Span<sbyte> dest)
    {
      int max = dest.Length;
      int i = 0;

      // Copy characters until we reach capacity‑1 (reserve space for NUL)
      for (; i < src.Length && i < max - 1; i++)
      {
        char ch = src[i];
        dest[i] = (sbyte)(ch <= 0x7F ? ch : '?');
      }

      // NUL terminate
      dest[i] = 0;
    }


    public static unsafe Span<sbyte> AsSpan(ref dealPBN d)
    {
      fixed (dealPBN* p = &d)
        return new Span<sbyte>(p->remainCards, 80);
    }

    public static unsafe Span<sbyte> AsSpan(ref ddTableDealPBN d)
    {
      fixed (ddTableDealPBN* p = &d)
        return new Span<sbyte>(p->cards, 80);
    }

  }
}
