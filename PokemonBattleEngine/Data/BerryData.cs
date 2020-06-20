﻿using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Kermalis.PokemonBattleEngine.Data
{
    public sealed class PBEBerryData
    {
        public byte Bitterness { get; }
        public byte Dryness { get; }
        public byte Sourness { get; }
        public byte Spicyness { get; }
        public byte Sweetness { get; }

        /// <summary>The power <see cref="PBEMoveEffect.NaturalGift"/> has when the user is holding this item.</summary>
        public byte NaturalGiftPower { get; }
        /// <summary>The type <see cref="PBEMoveEffect.NaturalGift"/> becomes when the user is holding this item.</summary>
        public PBEType NaturalGiftType { get; }

        private PBEBerryData(byte naturalGiftPower, PBEType naturalGiftType,
            byte bitterness = 0, byte dryness = 0, byte sourness = 0, byte spicyness = 0, byte sweetness = 0)
        {
            Bitterness = bitterness;
            Dryness = dryness;
            Sourness = sourness;
            Spicyness = spicyness;
            Sweetness = sweetness;

            NaturalGiftPower = naturalGiftPower;
            NaturalGiftType = naturalGiftType;
        }

        public static ReadOnlyDictionary<PBEItem, PBEBerryData> Data { get; } = new ReadOnlyDictionary<PBEItem, PBEBerryData>(new Dictionary<PBEItem, PBEBerryData>
        {
            { PBEItem.AguavBerry, new PBEBerryData(60, PBEType.Dragon, bitterness: 15) },
            { PBEItem.ApicotBerry, new PBEBerryData(80, PBEType.Ground, spicyness: 10, dryness: 30, sourness: 30) },
            { PBEItem.AspearBerry, new PBEBerryData(60, PBEType.Ice, sourness: 10) },
            { PBEItem.BabiriBerry, new PBEBerryData(60, PBEType.Steel, spicyness: 25, dryness: 10) },
            { PBEItem.BelueBerry, new PBEBerryData(80, PBEType.Electric, spicyness: 10, sourness: 30) },
            { PBEItem.BlukBerry, new PBEBerryData(70, PBEType.Fire, dryness: 10, sweetness: 10) },
            { PBEItem.ChartiBerry, new PBEBerryData(60, PBEType.Rock, spicyness: 10, dryness: 20) },
            { PBEItem.CheriBerry, new PBEBerryData(60, PBEType.Fire, spicyness: 10) },
            { PBEItem.ChestoBerry, new PBEBerryData(60, PBEType.Water, dryness: 10) },
            { PBEItem.ChilanBerry, new PBEBerryData(60, PBEType.Normal, dryness: 25, sweetness: 10) },
            { PBEItem.ChopleBerry, new PBEBerryData(60, PBEType.Fighting, spicyness: 15, bitterness: 10) },
            { PBEItem.CobaBerry, new PBEBerryData(60, PBEType.Flying, dryness: 10, bitterness: 15) },
            { PBEItem.ColburBerry, new PBEBerryData(60, PBEType.Dark, bitterness: 10, sourness: 20) },
            { PBEItem.CornnBerry, new PBEBerryData(70, PBEType.Bug, dryness: 20, sweetness: 10) },
            { PBEItem.CustapBerry, new PBEBerryData(80, PBEType.Ghost, sweetness: 40, bitterness: 10) },
            { PBEItem.DurinBerry, new PBEBerryData(80, PBEType.Water, bitterness: 30, sourness: 10) },
            { PBEItem.EnigmaBerry, new PBEBerryData(80, PBEType.Bug, spicyness: 40, dryness: 10) },
            { PBEItem.FigyBerry, new PBEBerryData(60, PBEType.Bug, spicyness: 15) },
            { PBEItem.GanlonBerry, new PBEBerryData(80, PBEType.Ice, dryness: 30, sweetness: 10, bitterness: 30) },
            { PBEItem.GrepaBerry, new PBEBerryData(70, PBEType.Flying, dryness: 10, sweetness: 10, sourness: 10) },
            { PBEItem.HabanBerry, new PBEBerryData(60, PBEType.Dragon, sweetness: 10, bitterness: 20) },
            { PBEItem.HondewBerry, new PBEBerryData(70, PBEType.Ground, spicyness: 10, dryness: 10, bitterness: 10) },
            { PBEItem.IapapaBerry, new PBEBerryData(60, PBEType.Dark, sourness: 15) },
            { PBEItem.JabocaBerry, new PBEBerryData(80, PBEType.Dragon, bitterness: 40, sourness: 10) },
            { PBEItem.KasibBerry, new PBEBerryData(60, PBEType.Ghost, dryness: 10, sweetness: 20) },
            { PBEItem.KebiaBerry, new PBEBerryData(60, PBEType.Poison, dryness: 15, sourness: 10) },
            { PBEItem.KelpsyBerry, new PBEBerryData(70, PBEType.Fighting, dryness: 10, bitterness: 10, sourness: 10) },
            { PBEItem.LansatBerry, new PBEBerryData(80, PBEType.Flying, spicyness: 30, dryness: 10, sweetness: 30, bitterness: 10, sourness: 30) },
            { PBEItem.LeppaBerry, new PBEBerryData(60, PBEType.Fighting, spicyness: 10, sweetness: 10, bitterness: 10, sourness: 10) },
            { PBEItem.LiechiBerry, new PBEBerryData(80, PBEType.Grass, spicyness: 30, dryness: 10, sweetness: 30) },
            { PBEItem.LumBerry, new PBEBerryData(60, PBEType.Flying, spicyness: 10, dryness: 10, sweetness: 10, bitterness: 10) },
            { PBEItem.MagoBerry, new PBEBerryData(60, PBEType.Ghost, sweetness: 15) },
            { PBEItem.MagostBerry, new PBEBerryData(70, PBEType.Rock, sweetness: 20, bitterness: 10) },
            { PBEItem.MicleBerry, new PBEBerryData(80, PBEType.Rock, dryness: 40, sweetness: 10) },
            { PBEItem.NanabBerry, new PBEBerryData(70, PBEType.Water, sweetness: 10, bitterness: 10) },
            { PBEItem.NomelBerry, new PBEBerryData(70, PBEType.Dragon, spicyness: 10, sourness: 20) },
            { PBEItem.OccaBerry, new PBEBerryData(60, PBEType.Fire, spicyness: 15, sweetness: 10) },
            { PBEItem.OranBerry, new PBEBerryData(60, PBEType.Poison, spicyness: 10, dryness: 10, bitterness: 10, sourness: 10) },
            { PBEItem.PamtreBerry, new PBEBerryData(70, PBEType.Steel, dryness: 30, sweetness: 10) },
            { PBEItem.PasshoBerry, new PBEBerryData(60, PBEType.Water, dryness: 15, bitterness: 10) },
            { PBEItem.PayapaBerry, new PBEBerryData(60, PBEType.Psychic, sweetness: 10, sourness: 15) },
            { PBEItem.PechaBerry, new PBEBerryData(60, PBEType.Electric, sweetness: 10) },
            { PBEItem.PersimBerry, new PBEBerryData(60, PBEType.Ground, spicyness: 10, dryness: 10, sweetness: 10, sourness: 10) },
            { PBEItem.PetayaBerry, new PBEBerryData(80, PBEType.Poison, spicyness: 30, bitterness: 30, sourness: 10) },
            { PBEItem.PinapBerry, new PBEBerryData(70, PBEType.Grass, spicyness: 10, sourness: 10) },
            { PBEItem.PomegBerry, new PBEBerryData(70, PBEType.Ice, spicyness: 10, sweetness: 10, bitterness: 10) },
            { PBEItem.QualotBerry, new PBEBerryData(70, PBEType.Poison, spicyness: 10, sweetness: 10, sourness: 10) },
            { PBEItem.RabutaBerry, new PBEBerryData(70, PBEType.Ghost, bitterness: 20, sourness: 10) },
            { PBEItem.RawstBerry, new PBEBerryData(60, PBEType.Grass, bitterness: 10) },
            { PBEItem.RazzBerry, new PBEBerryData(60, PBEType.Steel, spicyness: 10, dryness: 10) },
            { PBEItem.RindoBerry, new PBEBerryData(60, PBEType.Grass, spicyness: 10, bitterness: 15) },
            { PBEItem.RowapBerry, new PBEBerryData(80, PBEType.Dark, spicyness: 10, sourness: 40) },
            { PBEItem.SalacBerry, new PBEBerryData(80, PBEType.Fighting, sweetness: 30, bitterness: 10, sourness: 30) },
            { PBEItem.ShucaBerry, new PBEBerryData(60, PBEType.Ground, spicyness: 10, sweetness: 15) },
            { PBEItem.SitrusBerry, new PBEBerryData(60, PBEType.Psychic, dryness: 10, sweetness: 10, bitterness: 10, sourness: 10) },
            { PBEItem.SpelonBerry, new PBEBerryData(70, PBEType.Dark, spicyness: 30, dryness: 10) },
            { PBEItem.StarfBerry, new PBEBerryData(80, PBEType.Psychic, spicyness: 30, dryness: 10, sweetness: 30, bitterness: 10, sourness: 30) },
            { PBEItem.TamatoBerry, new PBEBerryData(70, PBEType.Psychic, spicyness: 20, dryness: 10) },
            { PBEItem.TangaBerry, new PBEBerryData(60, PBEType.Bug, spicyness: 20, sourness: 10) },
            { PBEItem.WacanBerry, new PBEBerryData(60, PBEType.Electric, sweetness: 15, sourness: 10) },
            { PBEItem.WatmelBerry, new PBEBerryData(80, PBEType.Fire, sweetness: 30, bitterness: 10) },
            { PBEItem.WepearBerry, new PBEBerryData(70, PBEType.Electric, bitterness: 10, sourness: 10) },
            { PBEItem.WikiBerry, new PBEBerryData(60, PBEType.Rock, dryness: 15) },
            { PBEItem.YacheBerry, new PBEBerryData(60, PBEType.Ice, dryness: 10, sourness: 15) }
        });
    }
}
