﻿using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonBattleEngine.Packets;
using Kermalis.PokemonBattleEngine.Utils;
using Kermalis.PokemonBattleEngineClient.Views;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Kermalis.PokemonBattleEngineClient.Clients
{
    internal abstract class BattleClient : IDisposable
    {
        protected const int WaitMilliseconds = 1750;

        public string Name { get; }
        public abstract PBEBattle Battle { get; }
        public abstract PBETrainer Trainer { get; }
        public abstract BattleView BattleView { get; }
        public abstract bool HideNonOwned { get; }

        protected BattleClient(string name)
        {
            Name = name;
        }

        public bool ShouldUseKnownInfo(PBETrainer pkmnTrainer)
        {
            return pkmnTrainer != Trainer && HideNonOwned;
        }

        public abstract void Dispose();

        #region Actions
        private readonly List<PBEBattlePokemon> _actions = new List<PBEBattlePokemon>(3);
        public List<PBEBattlePokemon> StandBy { get; } = new List<PBEBattlePokemon>(3);
        public void ActionsLoop(bool begin)
        {
            if (begin)
            {
                foreach (PBEBattlePokemon pkmn in Trainer.Party)
                {
                    pkmn.TurnAction = null;
                }
                _actions.Clear();
                _actions.AddRange(Trainer.ActiveBattlers);
                StandBy.Clear();
            }
            int i = _actions.FindIndex(p => p.TurnAction == null);
            if (i == -1)
            {
                OnActionsReady(_actions.Select(p => p.TurnAction).ToArray());
            }
            else
            {
                BattleView.AddMessage($"What will {_actions[i].Nickname} do?", messageLog: false);
                BattleView.Actions.DisplayActions(_actions[i]);
            }
        }
        protected abstract void OnActionsReady(PBETurnAction[] acts);

        public List<PBESwitchIn> Switches { get; } = new List<PBESwitchIn>(3);
        protected byte _switchesRequired;
        public List<PBEFieldPosition> PositionStandBy { get; } = new List<PBEFieldPosition>(3);
        public void SwitchesLoop(bool begin)
        {
            if (begin)
            {
                Switches.Clear();
                StandBy.Clear();
                PositionStandBy.Clear();
            }
            else
            {
                _switchesRequired--;
            }
            if (_switchesRequired == 0)
            {
                OnSwitchesReady();
            }
            else
            {
                BattleView.AddMessage($"You must send in {_switchesRequired} Pokémon.", messageLog: false);
                BattleView.Actions.DisplaySwitches();
            }
        }
        protected abstract void OnSwitchesReady();
        #endregion

        #region Automatic packet processing
        // Returns true if the next packet should be run immediately
        protected virtual bool ProcessPacket(IPBEPacket packet)
        {
            string NameForTrainer(PBEBattlePokemon pkmn, bool firstLetterCapitalized)
            {
                if (pkmn == null)
                {
                    return string.Empty;
                }
                // Replay/spectator always see prefix, but if you're battling a multi-battle, your Pokémon should still have no prefix
                if (Trainer == null || (pkmn.Trainer != Trainer && pkmn.Team.Trainers.Count > 1))
                {
                    return $"{pkmn.Trainer.Name}'s {pkmn.KnownNickname}";
                }
                string prefix = firstLetterCapitalized
                    ? pkmn.Trainer == Trainer ? string.Empty : "The foe's "
                    : pkmn.Trainer == Trainer ? string.Empty : "the foe's ";
                return prefix + pkmn.KnownNickname;
            }

            switch (packet)
            {
                case PBEAbilityPacket ap:
                {
                    PBEBattlePokemon abilityOwner = ap.AbilityOwnerTrainer.TryGetPokemon(ap.AbilityOwner);
                    PBEBattlePokemon pokemon2 = ap.AbilityOwnerTrainer.TryGetPokemon(ap.Pokemon2);
                    bool abilityOwnerCaps = true,
                            pokemon2Caps = true;
                    string message;
                    switch (ap.Ability)
                    {
                        case PBEAbility.AirLock:
                        case PBEAbility.CloudNine:
                        {
                            switch (ap.AbilityAction)
                            {
                                case PBEAbilityAction.Weather: message = "{0}'s {2} causes the effects of weather to disappear!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ap.AbilityAction));
                            }
                            break;
                        }
                        case PBEAbility.Anticipation:
                        {
                            switch (ap.AbilityAction)
                            {
                                case PBEAbilityAction.Announced: message = "{0}'s {2} made it shudder!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ap.AbilityAction));
                            }
                            break;
                        }
                        case PBEAbility.BadDreams:
                        {
                            switch (ap.AbilityAction)
                            {
                                case PBEAbilityAction.Damage: message = "{1} is tormented by {0}'s {2}!"; abilityOwnerCaps = false; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ap.AbilityAction));
                            }
                            break;
                        }
                        case PBEAbility.BigPecks:
                        {
                            switch (ap.AbilityAction)
                            {
                                case PBEAbilityAction.Stats: message = $"{{0}}'s {PBELocalizedString.GetStatName(PBEStat.Defense).English} was not lowered!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ap.AbilityAction));
                            }
                            break;
                        }
                        case PBEAbility.ClearBody:
                        case PBEAbility.WhiteSmoke:
                        {
                            switch (ap.AbilityAction)
                            {
                                case PBEAbilityAction.Stats: message = "{0}'s {2} prevents stat reduction!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ap.AbilityAction));
                            }
                            break;
                        }
                        case PBEAbility.ColorChange:
                        case PBEAbility.FlowerGift:
                        case PBEAbility.Forecast:
                        case PBEAbility.Imposter:
                        {
                            switch (ap.AbilityAction)
                            {
                                case PBEAbilityAction.ChangedAppearance: message = "{0}'s {2} activated!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ap.AbilityAction));
                            }
                            break;
                        }
                        case PBEAbility.CuteCharm:
                        case PBEAbility.EffectSpore:
                        case PBEAbility.FlameBody:
                        case PBEAbility.Healer:
                        case PBEAbility.PoisonPoint:
                        case PBEAbility.ShedSkin:
                        case PBEAbility.Static:
                        {
                            switch (ap.AbilityAction)
                            {
                                case PBEAbilityAction.ChangedStatus: message = "{0}'s {2} activated!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ap.AbilityAction));
                            }
                            break;
                        }
                        case PBEAbility.Download:
                        case PBEAbility.Intimidate:
                        {
                            switch (ap.AbilityAction)
                            {
                                case PBEAbilityAction.Stats: message = "{0}'s {2} activated!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ap.AbilityAction));
                            }
                            break;
                        }
                        case PBEAbility.Drizzle:
                        case PBEAbility.Drought:
                        case PBEAbility.SandStream:
                        case PBEAbility.SnowWarning:
                        {
                            switch (ap.AbilityAction)
                            {
                                case PBEAbilityAction.Weather: message = "{0}'s {2} activated!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ap.AbilityAction));
                            }
                            break;
                        }
                        case PBEAbility.HyperCutter:
                        {
                            switch (ap.AbilityAction)
                            {
                                case PBEAbilityAction.Stats: message = $"{{0}}'s {PBELocalizedString.GetStatName(PBEStat.Attack)} was not lowered!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ap.AbilityAction));
                            }
                            break;
                        }
                        case PBEAbility.IceBody:
                        case PBEAbility.PoisonHeal:
                        case PBEAbility.RainDish:
                        {
                            switch (ap.AbilityAction)
                            {
                                case PBEAbilityAction.RestoredHP: message = "{0}'s {2} activated!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ap.AbilityAction));
                            }
                            break;
                        }
                        case PBEAbility.Illusion:
                        {
                            switch (ap.AbilityAction)
                            {
                                case PBEAbilityAction.ChangedAppearance: return true;
                                default: throw new ArgumentOutOfRangeException(nameof(ap.AbilityAction));
                            }
                        }
                        case PBEAbility.Immunity:
                        case PBEAbility.Insomnia:
                        case PBEAbility.Limber:
                        case PBEAbility.MagmaArmor:
                        case PBEAbility.Oblivious:
                        case PBEAbility.OwnTempo:
                        case PBEAbility.VitalSpirit:
                        case PBEAbility.WaterVeil:
                        {
                            switch (ap.AbilityAction)
                            {
                                case PBEAbilityAction.ChangedStatus:
                                case PBEAbilityAction.PreventedStatus: message = "{0}'s {2} activated!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ap.AbilityAction));
                            }
                            break;
                        }
                        case PBEAbility.IronBarbs:
                        case PBEAbility.Justified:
                        case PBEAbility.Levitate:
                        case PBEAbility.Mummy:
                        case PBEAbility.Rattled:
                        case PBEAbility.RoughSkin:
                        case PBEAbility.SolarPower:
                        case PBEAbility.Sturdy:
                        case PBEAbility.WeakArmor:
                        case PBEAbility.WonderGuard:
                        {
                            switch (ap.AbilityAction)
                            {
                                case PBEAbilityAction.Damage: message = "{0}'s {2} activated!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ap.AbilityAction));
                            }
                            break;
                        }
                        case PBEAbility.KeenEye:
                        {
                            switch (ap.AbilityAction)
                            {
                                case PBEAbilityAction.Stats: message = $"{{0}}'s {PBELocalizedString.GetStatName(PBEStat.Accuracy).English} was not lowered!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ap.AbilityAction));
                            }
                            break;
                        }
                        case PBEAbility.LeafGuard:
                        {
                            switch (ap.AbilityAction)
                            {
                                case PBEAbilityAction.PreventedStatus: message = "{0}'s {2} activated!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ap.AbilityAction));
                            }
                            break;
                        }
                        case PBEAbility.LiquidOoze:
                        {
                            switch (ap.AbilityAction)
                            {
                                case PBEAbilityAction.Damage: message = "{1} sucked up the liquid ooze!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ap.AbilityAction));
                            }
                            break;
                        }
                        case PBEAbility.MoldBreaker:
                        {
                            switch (ap.AbilityAction)
                            {
                                case PBEAbilityAction.Announced: message = "{0} breaks the mold!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ap.AbilityAction));
                            }
                            break;
                        }
                        case PBEAbility.Moody:
                        case PBEAbility.SpeedBoost:
                        case PBEAbility.Steadfast:
                        {
                            switch (ap.AbilityAction)
                            {
                                case PBEAbilityAction.Stats: message = "{0}'s {2} activated!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ap.AbilityAction));
                            }
                            break;
                        }
                        case PBEAbility.SlowStart:
                        {
                            switch (ap.AbilityAction)
                            {
                                case PBEAbilityAction.Announced: message = "{0} can't get it going!"; break;
                                case PBEAbilityAction.SlowStart_Ended: message = "{0} finally got its act together!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ap.AbilityAction));
                            }
                            break;
                        }
                        case PBEAbility.Teravolt:
                        {
                            switch (ap.AbilityAction)
                            {
                                case PBEAbilityAction.Announced: message = "{0} is radiating a bursting aura!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ap.AbilityAction));
                            }
                            break;
                        }
                        case PBEAbility.Turboblaze:
                        {
                            switch (ap.AbilityAction)
                            {
                                case PBEAbilityAction.Announced: message = "{0} is radiating a blazing aura!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ap.AbilityAction));
                            }
                            break;
                        }
                        default: throw new ArgumentOutOfRangeException(nameof(ap.Ability));
                    }
                    BattleView.AddMessage(string.Format(message, NameForTrainer(abilityOwner, abilityOwnerCaps), NameForTrainer(pokemon2, pokemon2Caps), PBELocalizedString.GetAbilityName(ap.Ability)));
                    return false;
                }
                case PBEAbilityReplacedPacket arp:
                {
                    PBEBattlePokemon abilityOwner = arp.AbilityOwnerTrainer.TryGetPokemon(arp.AbilityOwner);
                    string message;
                    switch (arp.NewAbility)
                    {
                        case PBEAbility.None: message = "{0}'s {1} was suppressed!"; break;
                        default: message = "{0}'s {1} was changed to {2}!"; break;
                    }
                    BattleView.AddMessage(string.Format(message,
                        NameForTrainer(abilityOwner, true),
                        arp.OldAbility.HasValue ? PBELocalizedString.GetAbilityName(arp.OldAbility.Value).ToString() : "Ability",
                        PBELocalizedString.GetAbilityName(arp.NewAbility)));
                    return false;
                }
                case PBEBattleStatusPacket bsp:
                {
                    string message;
                    switch (bsp.BattleStatus)
                    {
                        case PBEBattleStatus.TrickRoom:
                        {
                            switch (bsp.BattleStatusAction)
                            {
                                case PBEBattleStatusAction.Added: message = "The dimensions were twisted!"; break;
                                case PBEBattleStatusAction.Cleared:
                                case PBEBattleStatusAction.Ended: message = "The twisted dimensions returned to normal!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(bsp.BattleStatusAction));
                            }
                            break;
                        }
                        default: throw new ArgumentOutOfRangeException(nameof(bsp.BattleStatus));
                    }
                    BattleView.AddMessage(message);
                    return false;
                }
                case PBEHazePacket _:
                {
                    BattleView.AddMessage("All stat changes were eliminated!");
                    return false;
                }
                case PBEItemPacket ip:
                {
                    PBEBattlePokemon itemHolder = ip.ItemHolderTrainer.TryGetPokemon(ip.ItemHolder);
                    PBEBattlePokemon pokemon2 = ip.Pokemon2Trainer.TryGetPokemon(ip.Pokemon2);
                    bool itemHolderCaps = true,
                            pokemon2Caps = false;
                    string message;
                    switch (ip.Item)
                    {
                        case PBEItem.AguavBerry:
                        case PBEItem.BerryJuice:
                        case PBEItem.FigyBerry:
                        case PBEItem.IapapaBerry:
                        case PBEItem.MagoBerry:
                        case PBEItem.OranBerry:
                        case PBEItem.SitrusBerry:
                        case PBEItem.WikiBerry:
                        {
                            switch (ip.ItemAction)
                            {
                                case PBEItemAction.Consumed: message = "{0} restored its health using its {2}!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ip.ItemAction));
                            }
                            break;
                        }
                        case PBEItem.ApicotBerry:
                        case PBEItem.GanlonBerry:
                        case PBEItem.LiechiBerry:
                        case PBEItem.PetayaBerry:
                        case PBEItem.SalacBerry:
                        case PBEItem.StarfBerry:
                        {
                            switch (ip.ItemAction)
                            {
                                case PBEItemAction.Consumed: message = "{0} used its {2}!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ip.ItemAction));
                            }
                            break;
                        }
                        case PBEItem.BugGem:
                        case PBEItem.DarkGem:
                        case PBEItem.DragonGem:
                        case PBEItem.ElectricGem:
                        case PBEItem.FightingGem:
                        case PBEItem.FireGem:
                        case PBEItem.FlyingGem:
                        case PBEItem.GhostGem:
                        case PBEItem.GrassGem:
                        case PBEItem.GroundGem:
                        case PBEItem.IceGem:
                        case PBEItem.NormalGem:
                        case PBEItem.PoisonGem:
                        case PBEItem.PsychicGem:
                        case PBEItem.RockGem:
                        case PBEItem.SteelGem:
                        case PBEItem.WaterGem:
                        {
                            switch (ip.ItemAction)
                            {
                                case PBEItemAction.Consumed: message = "The {2} strengthened {0}'s power!"; itemHolderCaps = false; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ip.ItemAction));
                            }
                            break;
                        }
                        case PBEItem.BlackSludge:
                        {
                            switch (ip.ItemAction)
                            {
                                case PBEItemAction.Damage: message = "{0} is hurt by its {2}!"; break;
                                case PBEItemAction.RestoredHP: message = "{0} restored a little HP using its {2}!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ip.ItemAction));
                            }
                            break;
                        }
                        case PBEItem.DestinyKnot:
                        {
                            switch (ip.ItemAction)
                            {
                                case PBEItemAction.ChangedStatus: message = "{0}'s {2} activated!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ip.ItemAction));
                            }
                            break;
                        }
                        case PBEItem.FlameOrb:
                        {
                            switch (ip.ItemAction)
                            {
                                case PBEItemAction.ChangedStatus: message = "{0} was burned by its {2}!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ip.ItemAction));
                            }
                            break;
                        }
                        case PBEItem.FocusBand:
                        {
                            switch (ip.ItemAction)
                            {
                                case PBEItemAction.Damage: message = "{0} hung on using its {2}!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ip.ItemAction));
                            }
                            break;
                        }
                        case PBEItem.FocusSash:
                        {
                            switch (ip.ItemAction)
                            {
                                case PBEItemAction.Consumed: message = "{0} hung on using its {2}!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ip.ItemAction));
                            }
                            break;
                        }
                        case PBEItem.Leftovers:
                        {
                            switch (ip.ItemAction)
                            {
                                case PBEItemAction.RestoredHP: message = "{0} restored a little HP using its {2}!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ip.ItemAction));
                            }
                            break;
                        }
                        case PBEItem.LifeOrb:
                        {
                            switch (ip.ItemAction)
                            {
                                case PBEItemAction.Damage: message = "{0} is hurt by its {2}!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ip.ItemAction));
                            }
                            break;
                        }
                        case PBEItem.PowerHerb:
                        {
                            switch (ip.ItemAction)
                            {
                                case PBEItemAction.Consumed: message = "{0} became fully charged due to its {2}!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ip.ItemAction));
                            }
                            break;
                        }
                        case PBEItem.RockyHelmet:
                        {
                            switch (ip.ItemAction)
                            {
                                case PBEItemAction.Damage: message = "{1} was hurt by the {2}!"; pokemon2Caps = true; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ip.ItemAction));
                            }
                            break;
                        }
                        case PBEItem.ToxicOrb:
                        {
                            switch (ip.ItemAction)
                            {
                                case PBEItemAction.ChangedStatus: message = "{0} was badly poisoned by its {2}!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ip.ItemAction));
                            }
                            break;
                        }
                        default: throw new ArgumentOutOfRangeException(nameof(ip.Item));
                    }
                    BattleView.AddMessage(string.Format(message, NameForTrainer(itemHolder, itemHolderCaps), NameForTrainer(pokemon2, pokemon2Caps), PBELocalizedString.GetItemName(ip.Item)));
                    return false;
                }
                case PBEMoveCritPacket mcp:
                {
                    PBEBattlePokemon victim = mcp.VictimTrainer.TryGetPokemon(mcp.Victim);
                    BattleView.AddMessage(string.Format("A critical hit on {0}!", NameForTrainer(victim, false)));
                    return false;
                }
                case PBEMoveResultPacket mrp:
                {
                    PBEBattlePokemon moveUser = mrp.MoveUserTrainer.TryGetPokemon(mrp.MoveUser);
                    PBEBattlePokemon pokemon2 = mrp.Pokemon2Trainer.TryGetPokemon(mrp.Pokemon2);
                    bool pokemon2Caps = true;
                    string message;
                    switch (mrp.Result)
                    {
                        case PBEResult.Ineffective_Ability: message = "{1} is protected by its Ability!"; break;
                        case PBEResult.Ineffective_Gender: message = "It doesn't affect {1}..."; pokemon2Caps = false; break;
                        case PBEResult.Ineffective_Level: message = "{1} is protected by its level!"; break;
                        case PBEResult.Ineffective_MagnetRise: message = $"{{1}} is protected by {PBELocalizedString.GetMoveName(PBEMove.MagnetRise)}!"; break;
                        case PBEResult.Ineffective_Safeguard: message = $"{{1}} is protected by {PBELocalizedString.GetMoveName(PBEMove.Safeguard)}!"; break;
                        case PBEResult.Ineffective_Stat:
                        case PBEResult.Ineffective_Status:
                        case PBEResult.InvalidConditions: message = "But it failed!"; break;
                        case PBEResult.Ineffective_Substitute: message = $"{{1}} is protected by {PBELocalizedString.GetMoveName(PBEMove.Substitute)}!"; break;
                        case PBEResult.Ineffective_Type: message = "{1} is protected by its Type!"; break;
                        case PBEResult.Missed: message = "{0}'s attack missed {1}!"; pokemon2Caps = false; break;
                        case PBEResult.NoTarget: message = "But there was no target..."; break;
                        case PBEResult.NotVeryEffective_Type: message = "It's not very effective on {1}..."; pokemon2Caps = false; break;
                        case PBEResult.SuperEffective_Type: message = "It's super effective on {1}!"; pokemon2Caps = false; break;
                        default: throw new ArgumentOutOfRangeException(nameof(mrp.Result));
                    }
                    BattleView.AddMessage(string.Format(message, NameForTrainer(moveUser, true), NameForTrainer(pokemon2, pokemon2Caps)));
                    return false;
                }
                case PBEMoveUsedPacket mup:
                {
                    PBEBattlePokemon moveUser = mup.MoveUserTrainer.TryGetPokemon(mup.MoveUser);
                    BattleView.AddMessage(string.Format("{0} used {1}!", NameForTrainer(moveUser, true), PBELocalizedString.GetMoveName(mup.Move)));
                    return false;
                }
                case PBEPkmnFaintedPacket pfp:
                {
                    PBEBattlePokemon pokemon = pfp.PokemonTrainer.TryGetPokemon(pfp.Pokemon);
                    PBEBattlePokemon disguisedAsPokemon = pfp.PokemonTrainer.TryGetPokemon(pfp.DisguisedAsPokemon);
                    BattleView.Field.HidePokemon(pokemon, pfp.OldPosition);
                    BattleView.AddMessage(string.Format("{0} fainted!", NameForTrainer(disguisedAsPokemon, true)));
                    return false;
                }
                case PBEPkmnFaintedPacket_Hidden pfph:
                {
                    PBEBattlePokemon pokemon = pfph.PokemonTrainer.TryGetPokemon(pfph.OldPosition);
                    BattleView.Field.HidePokemon(pokemon, pfph.OldPosition);
                    BattleView.AddMessage(string.Format("{0} fainted!", NameForTrainer(pokemon, true)));
                    return false;
                }
                case IPBEPkmnFormChangedPacket pfcp:
                {
                    PBEBattlePokemon pokemon = pfcp.PokemonTrainer.TryGetPokemon(pfcp.Pokemon);
                    BattleView.Field.UpdatePokemon(pokemon, false, true);
                    BattleView.AddMessage(string.Format("{0} transformed!", NameForTrainer(pokemon, true)));
                    return false;
                }
                case PBEPkmnHPChangedPacket phcp:
                {
                    PBEBattlePokemon pokemon = phcp.PokemonTrainer.TryGetPokemon(phcp.Pokemon);
                    int change = phcp.NewHP - phcp.OldHP;
                    int absChange = Math.Abs(change);
                    double percentageChange = phcp.NewHPPercentage - phcp.OldHPPercentage;
                    double absPercentageChange = Math.Abs(percentageChange);
                    BattleView.Field.UpdatePokemon(pokemon, true, false);
                    if (ShouldUseKnownInfo(phcp.PokemonTrainer))
                    {
                        BattleView.AddMessage(string.Format("{0} {1} {2:P2} of its HP!", NameForTrainer(pokemon, true), percentageChange <= 0 ? "lost" : "restored", absPercentageChange));
                    }
                    else
                    {
                        BattleView.AddMessage(string.Format("{0} {1} {2} ({3:P2}) HP!", NameForTrainer(pokemon, true), change <= 0 ? "lost" : "restored", absChange, absPercentageChange));
                    }
                    return false;
                }
                case PBEPkmnHPChangedPacket_Hidden phcph:
                {
                    PBEBattlePokemon pokemon = phcph.PokemonTrainer.TryGetPokemon(phcph.Pokemon);
                    double percentageChange = phcph.NewHPPercentage - phcph.OldHPPercentage;
                    double absPercentageChange = Math.Abs(percentageChange);
                    BattleView.Field.UpdatePokemon(pokemon, true, false);
                    BattleView.AddMessage(string.Format("{0} {1} {2:P2} of its HP!", NameForTrainer(pokemon, true), percentageChange <= 0 ? "lost" : "restored", absPercentageChange));
                    return false;
                }
                case PBEPkmnStatChangedPacket pscp:
                {
                    PBEBattlePokemon pokemon = pscp.PokemonTrainer.TryGetPokemon(pscp.Pokemon);
                    string statName, message;
                    switch (pscp.Stat)
                    {
                        case PBEStat.Accuracy: statName = "Accuracy"; break;
                        case PBEStat.Attack: statName = "Attack"; break;
                        case PBEStat.Defense: statName = "Defense"; break;
                        case PBEStat.Evasion: statName = "Evasion"; break;
                        case PBEStat.SpAttack: statName = "Special Attack"; break;
                        case PBEStat.SpDefense: statName = "Special Defense"; break;
                        case PBEStat.Speed: statName = "Speed"; break;
                        default: throw new ArgumentOutOfRangeException(nameof(pscp.Stat));
                    }
                    int change = pscp.NewValue - pscp.OldValue;
                    switch (change)
                    {
                        case -2: message = "harshly fell"; break;
                        case -1: message = "fell"; break;
                        case +1: message = "rose"; break;
                        case +2: message = "rose sharply"; break;
                        default:
                        {
                            if (change == 0 && pscp.NewValue == -Battle.Settings.MaxStatChange)
                            {
                                message = "won't go lower";
                            }
                            else if (change == 0 && pscp.NewValue == Battle.Settings.MaxStatChange)
                            {
                                message = "won't go higher";
                            }
                            else if (change <= -3)
                            {
                                message = "severely fell";
                            }
                            else if (change >= +3)
                            {
                                message = "rose drastically";
                            }
                            else
                            {
                                throw new ArgumentOutOfRangeException();
                            }
                            break;
                        }
                    }
                    BattleView.AddMessage(string.Format("{0}'s {1} {2}!", NameForTrainer(pokemon, true), statName, message));
                    return false;
                }
                case IPBEPkmnSwitchInPacket psip:
                {
                    if (!psip.Forced)
                    {
                        foreach (IPBEPkmnSwitchInInfo info in psip.SwitchIns)
                        {
                            BattleView.Field.ShowPokemon(psip.Trainer.TryGetPokemon(info.FieldPosition));
                        }
                        BattleView.AddMessage(string.Format("{1} sent out {0}!", PBEUtils.Andify(psip.SwitchIns.Select(s => s.Nickname).ToArray()), psip.Trainer.Name));
                    }
                    return false;
                }
                case PBEPkmnSwitchOutPacket psop:
                {
                    PBEBattlePokemon pokemon = psop.PokemonTrainer.TryGetPokemon(psop.Pokemon);
                    BattleView.Field.HidePokemon(pokemon, psop.OldPosition);
                    if (!psop.Forced)
                    {
                        PBEBattlePokemon disguisedAsPokemon = psop.PokemonTrainer.TryGetPokemon(psop.DisguisedAsPokemon);
                        BattleView.AddMessage(string.Format("{1} withdrew {0}!", disguisedAsPokemon.KnownNickname, psop.PokemonTrainer.Name));
                    }
                    return false;
                }
                case PBEPkmnSwitchOutPacket_Hidden psoph:
                {
                    PBEBattlePokemon pokemon = psoph.PokemonTrainer.TryGetPokemon(psoph.OldPosition);
                    BattleView.Field.HidePokemon(pokemon, psoph.OldPosition);
                    if (!psoph.Forced)
                    {
                        BattleView.AddMessage(string.Format("{1} withdrew {0}!", pokemon.KnownNickname, psoph.PokemonTrainer.Name));
                    }
                    return false;
                }
                case PBEPsychUpPacket pup:
                {
                    PBEBattlePokemon user = pup.UserTrainer.TryGetPokemon(pup.User);
                    PBEBattlePokemon target = pup.TargetTrainer.TryGetPokemon(pup.Target);
                    BattleView.AddMessage(string.Format("{0} copied {1}'s stat changes!", NameForTrainer(user, true), NameForTrainer(target, false)));
                    return false;
                }
                case PBEReflectTypePacket rtp:
                {
                    PBEBattlePokemon user = rtp.UserTrainer.TryGetPokemon(rtp.User);
                    PBEBattlePokemon target = rtp.TargetTrainer.TryGetPokemon(rtp.Target);
                    string type1Str = PBELocalizedString.GetTypeName(rtp.Type1).ToString();
                    BattleView.AddMessage(string.Format("{0} copied {1}'s {2}",
                        NameForTrainer(user, true),
                        NameForTrainer(target, false),
                        rtp.Type2 == PBEType.None ? $"{type1Str} type!" : $"{type1Str} and {PBELocalizedString.GetTypeName(rtp.Type2)} types!"));
                    return false;
                }
                case PBEReflectTypePacket_Hidden rtph:
                {
                    PBEBattlePokemon user = rtph.UserTrainer.TryGetPokemon(rtph.User);
                    PBEBattlePokemon target = rtph.TargetTrainer.TryGetPokemon(rtph.Target);
                    BattleView.AddMessage(string.Format("{0} copied {1}'s types!", NameForTrainer(user, true), NameForTrainer(target, false)));
                    return false;
                }
                case PBESpecialMessagePacket smp:
                {
                    string message;
                    switch (smp.Message)
                    {
                        case PBESpecialMessage.DraggedOut: message = string.Format("{0} was dragged out!", NameForTrainer(((PBETrainer)smp.Params[0]).TryGetPokemon((PBEFieldPosition)smp.Params[1]), true)); break;
                        case PBESpecialMessage.Endure: message = string.Format("{0} endured the hit!", NameForTrainer(((PBETrainer)smp.Params[0]).TryGetPokemon((PBEFieldPosition)smp.Params[1]), true)); break;
                        case PBESpecialMessage.HPDrained: message = string.Format("{0} had its energy drained!", NameForTrainer(((PBETrainer)smp.Params[0]).TryGetPokemon((PBEFieldPosition)smp.Params[1]), true)); break;
                        case PBESpecialMessage.Magnitude: message = string.Format("Magnitude {0}!", (byte)smp.Params[0]); break;
                        case PBESpecialMessage.MultiHit: message = string.Format("Hit {0} time(s)!", (byte)smp.Params[0]); break;
                        case PBESpecialMessage.NothingHappened: message = "But nothing happened!"; break;
                        case PBESpecialMessage.OneHitKnockout: message = "It's a one-hit KO!"; break;
                        case PBESpecialMessage.PainSplit: message = "The battlers shared their pain!"; break;
                        case PBESpecialMessage.PayDay: message = "Coins were scattered everywhere!"; break;
                        case PBESpecialMessage.Recoil: message = string.Format("{0} is damaged by recoil!", NameForTrainer(((PBETrainer)smp.Params[0]).TryGetPokemon((PBEFieldPosition)smp.Params[1]), true)); break;
                        case PBESpecialMessage.Struggle: message = string.Format("{0} has no moves left!", NameForTrainer(((PBETrainer)smp.Params[0]).TryGetPokemon((PBEFieldPosition)smp.Params[1]), true)); break;
                        default: throw new ArgumentOutOfRangeException(nameof(smp.Message));
                    }
                    BattleView.AddMessage(message);
                    return false;
                }
                case PBEStatus1Packet s1p:
                {
                    PBEBattlePokemon status1Receiver = s1p.Status1ReceiverTrainer.TryGetPokemon(s1p.Status1Receiver);
                    BattleView.Field.UpdatePokemon(status1Receiver, true, false);
                    string message;
                    switch (s1p.Status1)
                    {
                        case PBEStatus1.Asleep:
                        {
                            switch (s1p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{0} fell asleep!"; break;
                                case PBEStatusAction.CausedImmobility: message = "{0} is fast asleep."; break;
                                case PBEStatusAction.Cleared:
                                case PBEStatusAction.Ended: message = "{0} woke up!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(s1p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus1.BadlyPoisoned:
                        {
                            switch (s1p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{0} was badly poisoned!"; break;
                                case PBEStatusAction.Cleared: message = "{0} was cured of its poisoning."; break;
                                case PBEStatusAction.Damage: message = "{0} was hurt by poison!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(s1p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus1.Burned:
                        {
                            switch (s1p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{0} was burned!"; break;
                                case PBEStatusAction.Cleared: message = "{0}'s burn was healed."; break;
                                case PBEStatusAction.Damage: message = "{0} was hurt by its burn!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(s1p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus1.Frozen:
                        {
                            switch (s1p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{0} was frozen solid!"; break;
                                case PBEStatusAction.CausedImmobility: message = "{0} is frozen solid!"; break;
                                case PBEStatusAction.Cleared:
                                case PBEStatusAction.Ended: message = "{0} thawed out!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(s1p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus1.Paralyzed:
                        {
                            switch (s1p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{0} is paralyzed! It may be unable to move!"; break;
                                case PBEStatusAction.CausedImmobility: message = "{0} is paralyzed! It can't move!"; break;
                                case PBEStatusAction.Cleared: message = "{0} was cured of paralysis."; break;
                                default: throw new ArgumentOutOfRangeException(nameof(s1p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus1.Poisoned:
                        {
                            switch (s1p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{0} was poisoned!"; break;
                                case PBEStatusAction.Cleared: message = "{0} was cured of its poisoning."; break;
                                case PBEStatusAction.Damage: message = "{0} was hurt by poison!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(s1p.StatusAction));
                            }
                            break;
                        }
                        default: throw new ArgumentOutOfRangeException(nameof(s1p.Status1));
                    }
                    BattleView.AddMessage(string.Format(message, NameForTrainer(status1Receiver, true)));
                    return false;
                }
                case PBEStatus2Packet s2p:
                {
                    PBEBattlePokemon status2Receiver = s2p.Status2ReceiverTrainer.TryGetPokemon(s2p.Status2Receiver);
                    PBEBattlePokemon pokemon2 = s2p.Pokemon2Trainer.TryGetPokemon(s2p.Pokemon2);
                    string message;
                    bool status2ReceiverCaps = true,
                            pokemon2Caps = false;
                    switch (s2p.Status2)
                    {
                        case PBEStatus2.Airborne:
                        {
                            BattleView.Field.UpdatePokemon(status2Receiver, false, true);
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{0} flew up high!"; break;
                                case PBEStatusAction.Ended: return true;
                                default: throw new ArgumentOutOfRangeException(nameof(s2p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus2.Confused:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{0} became confused!"; break;
                                case PBEStatusAction.Announced: message = "{0} is confused!"; break;
                                case PBEStatusAction.Cleared:
                                case PBEStatusAction.Ended: message = "{0} snapped out of its confusion."; break;
                                case PBEStatusAction.Damage: message = "It hurt itself in its confusion!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(s2p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus2.Cursed:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{1} cut its own HP and laid a curse on {0}!"; status2ReceiverCaps = false; pokemon2Caps = true; break;
                                case PBEStatusAction.Damage: message = "{0} is afflicted by the curse!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(s2p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus2.Disguised:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Ended:
                                {
                                    BattleView.Field.UpdatePokemon(status2Receiver, true, true);
                                    message = "{0}'s illusion wore off!";
                                    break;
                                }
                                default: throw new ArgumentOutOfRangeException(nameof(s2p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus2.Flinching:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.CausedImmobility: message = "{0} flinched and couldn't move!"; break;
                                case PBEStatusAction.Ended: return true;
                                default: throw new ArgumentOutOfRangeException(nameof(s2p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus2.HelpingHand:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{1} is ready to help {0}!"; status2ReceiverCaps = false; pokemon2Caps = true; break;
                                case PBEStatusAction.Ended: return true;
                                default: throw new ArgumentOutOfRangeException(nameof(s2p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus2.Identified:
                        case PBEStatus2.MiracleEye:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{0} was identified!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(s2p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus2.Infatuated:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{0} fell in love with {1}!"; break;
                                case PBEStatusAction.Announced: message = "{0} is in love with {1}!"; break;
                                case PBEStatusAction.CausedImmobility: message = "{0} is immobilized by love!"; break;
                                case PBEStatusAction.Cleared:
                                case PBEStatusAction.Ended: message = "{0} got over its infatuation."; break;
                                default: throw new ArgumentOutOfRangeException(nameof(s2p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus2.LeechSeed:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{0} was seeded!"; break;
                                case PBEStatusAction.Damage: message = "{0}'s health is sapped by Leech Seed!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(s2p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus2.LockOn:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{0} took aim at {1}!"; break;
                                case PBEStatusAction.Ended: return true;
                                default: throw new ArgumentOutOfRangeException(nameof(s2p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus2.MagnetRise:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{0} levitated with electromagnetism!"; break;
                                case PBEStatusAction.Ended: message = "{0}'s electromagnetism wore off!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(s2p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus2.Nightmare:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{0} began having a nightmare!"; break;
                                case PBEStatusAction.Damage: message = "{0} is locked in a nightmare!"; break;
                                case PBEStatusAction.Ended: return true;
                                default: throw new ArgumentOutOfRangeException(nameof(s2p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus2.PowerTrick:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{0} switched its Attack and Defense!"; break;
                                case PBEStatusAction.Ended: return true;
                                default: throw new ArgumentOutOfRangeException(nameof(s2p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus2.Protected:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added:
                                case PBEStatusAction.Damage: message = "{0} protected itself!"; break;
                                case PBEStatusAction.Cleared: message = "{1} broke through {0}'s protection!"; status2ReceiverCaps = false; pokemon2Caps = true; break;
                                case PBEStatusAction.Ended: return true;
                                default: throw new ArgumentOutOfRangeException(nameof(s2p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus2.Pumped:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{0} is getting pumped!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(s2p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus2.Roost:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added:
                                case PBEStatusAction.Ended: return true;
                                default: throw new ArgumentOutOfRangeException(nameof(s2p.StatusAction));
                            }
                        }
                        case PBEStatus2.ShadowForce:
                        {
                            BattleView.Field.UpdatePokemon(status2Receiver, false, true);
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{0} vanished instantly!"; break;
                                case PBEStatusAction.Ended: return true;
                                default: throw new ArgumentOutOfRangeException(nameof(s2p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus2.Substitute:
                        {
                            BattleView.Field.UpdatePokemon(status2Receiver, false, true);
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{0} put in a substitute!"; break;
                                case PBEStatusAction.Damage: message = "The substitute took damage for {0}!"; status2ReceiverCaps = false; break;
                                case PBEStatusAction.Ended: message = "{0}'s substitute faded!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(s2p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus2.Transformed:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added:
                                {
                                    BattleView.Field.UpdatePokemon(status2Receiver, false, true);
                                    message = "{0} transformed into {1}!";
                                    break;
                                }
                                default: throw new ArgumentOutOfRangeException(nameof(s2p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus2.Underground:
                        {
                            BattleView.Field.UpdatePokemon(status2Receiver, false, true);
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{0} burrowed its way under the ground!"; break;
                                case PBEStatusAction.Ended: return true;
                                default: throw new ArgumentOutOfRangeException(nameof(s2p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus2.Underwater:
                        {
                            BattleView.Field.UpdatePokemon(status2Receiver, false, true);
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{0} hid underwater!"; break;
                                case PBEStatusAction.Ended: return true;
                                default: throw new ArgumentOutOfRangeException(nameof(s2p.StatusAction));
                            }
                            break;
                        }
                        default: throw new ArgumentOutOfRangeException(nameof(s2p.Status2));
                    }
                    BattleView.AddMessage(string.Format(message, NameForTrainer(status2Receiver, status2ReceiverCaps), NameForTrainer(pokemon2, pokemon2Caps)));
                    return false;
                }
                case PBETeamStatusPacket tsp:
                {
                    PBEBattlePokemon damageVictim = tsp.DamageVictimTrainer?.TryGetPokemon(tsp.DamageVictim);
                    string message;
                    bool teamCaps = true,
                        damageVictimCaps = false;
                    switch (tsp.TeamStatus)
                    {
                        case PBETeamStatus.LightScreen:
                        {
                            switch (tsp.TeamStatusAction)
                            {
                                case PBETeamStatusAction.Added: message = "Light Screen raised {0} team's Special Defense!"; teamCaps = false; break;
                                case PBETeamStatusAction.Cleared:
                                case PBETeamStatusAction.Ended: message = "{0} team's Light Screen wore off!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(tsp.TeamStatusAction));
                            }
                            break;
                        }
                        case PBETeamStatus.LuckyChant:
                        {
                            switch (tsp.TeamStatusAction)
                            {
                                case PBETeamStatusAction.Added: message = "The Lucky Chant shielded {0} team from critical hits!"; teamCaps = false; break;
                                case PBETeamStatusAction.Ended: message = "{0} team's Lucky Chant wore off!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(tsp.TeamStatusAction));
                            }
                            break;
                        }
                        case PBETeamStatus.QuickGuard:
                        {
                            switch (tsp.TeamStatusAction)
                            {
                                case PBETeamStatusAction.Added: message = "Quick Guard protected {0} team!"; teamCaps = false; break;
                                case PBETeamStatusAction.Cleared: message = "{0} team's Quick Guard was destroyed!"; break;
                                case PBETeamStatusAction.Damage: message = "Quick Guard protected {1}!"; break;
                                case PBETeamStatusAction.Ended: return true;
                                default: throw new ArgumentOutOfRangeException(nameof(tsp.TeamStatusAction));
                            }
                            break;
                        }
                        case PBETeamStatus.Reflect:
                        {
                            switch (tsp.TeamStatusAction)
                            {
                                case PBETeamStatusAction.Added: message = "Reflect raised {0} team's Defense!"; teamCaps = false; break;
                                case PBETeamStatusAction.Cleared:
                                case PBETeamStatusAction.Ended: message = "{0} team's Reflect wore off!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(tsp.TeamStatusAction));
                            }
                            break;
                        }
                        case PBETeamStatus.Safeguard:
                        {
                            switch (tsp.TeamStatusAction)
                            {
                                case PBETeamStatusAction.Added: message = "{0} team became cloaked in a mystical veil!"; break;
                                case PBETeamStatusAction.Ended: message = "{0} team is no longer protected by Safeguard!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(tsp.TeamStatusAction));
                            }
                            break;
                        }
                        case PBETeamStatus.Spikes:
                        {
                            switch (tsp.TeamStatusAction)
                            {
                                case PBETeamStatusAction.Added: message = "Spikes were scattered all around the feet of {0} team!"; teamCaps = false; break;
                                //case PBETeamStatusAction.Cleared: message = "The spikes disappeared from around {0} team's feet!"; teamCaps = false; break;
                                case PBETeamStatusAction.Damage: message = "{1} is hurt by the spikes!"; damageVictimCaps = true; break;
                                default: throw new ArgumentOutOfRangeException(nameof(tsp.TeamStatusAction));
                            }
                            break;
                        }
                        case PBETeamStatus.StealthRock:
                        {
                            switch (tsp.TeamStatusAction)
                            {
                                case PBETeamStatusAction.Added: message = "Pointed stones float in the air around {0} team!"; teamCaps = false; break;
                                //case PBETeamStatusAction.Cleared: message = "The pointed stones disappeared from around {0} team!"; teamCaps = false; break;
                                case PBETeamStatusAction.Damage: message = "Pointed stones dug into {1}!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(tsp.TeamStatusAction));
                            }
                            break;
                        }
                        case PBETeamStatus.Tailwind:
                        {
                            switch (tsp.TeamStatusAction)
                            {
                                case PBETeamStatusAction.Added: message = "The tailwind blew from behind {0} team!"; teamCaps = false; break;
                                case PBETeamStatusAction.Ended: message = "{0} team's tailwind petered out!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(tsp.TeamStatusAction));
                            }
                            break;
                        }
                        case PBETeamStatus.ToxicSpikes:
                        {
                            switch (tsp.TeamStatusAction)
                            {
                                case PBETeamStatusAction.Added: message = "Poison spikes were scattered all around {0} team's feet!"; break;
                                case PBETeamStatusAction.Cleared: message = "The poison spikes disappeared from around {0} team's feet!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(tsp.TeamStatusAction));
                            }
                            break;
                        }
                        case PBETeamStatus.WideGuard:
                        {
                            switch (tsp.TeamStatusAction)
                            {
                                case PBETeamStatusAction.Added: message = "Wide Guard protected {0} team!"; break;
                                case PBETeamStatusAction.Cleared: message = "{0} team's Wide Guard was destroyed!"; break;
                                case PBETeamStatusAction.Damage: message = "Wide Guard protected {1}!"; break;
                                case PBETeamStatusAction.Ended: return true;
                                default: throw new ArgumentOutOfRangeException(nameof(tsp.TeamStatusAction));
                            }
                            break;
                        }
                        default: throw new ArgumentOutOfRangeException(nameof(tsp.TeamStatus));
                    }
                    BattleView.AddMessage(string.Format(message,
                        Trainer == null ? $"{tsp.Team.CombinedName}'s" : tsp.Team == Trainer.Team ? teamCaps ? "Your" : "your" : teamCaps ? "The opposing" : "the opposing",
                        NameForTrainer(damageVictim, damageVictimCaps)
                        ));
                    return false;
                }
                case PBETypeChangedPacket tcp:
                {
                    PBEBattlePokemon pokemon = tcp.PokemonTrainer.TryGetPokemon(tcp.Pokemon);
                    string type1Str = PBELocalizedString.GetTypeName(tcp.Type1).ToString();
                    BattleView.AddMessage(string.Format("{0} transformed into the {1}",
                        NameForTrainer(pokemon, true),
                        tcp.Type2 == PBEType.None ? $"{type1Str} type!" : $"{type1Str} and {PBELocalizedString.GetTypeName(tcp.Type2)} types!"));
                    return false;
                }
                case PBEWeatherPacket wp:
                {
                    PBEBattlePokemon damageVictim = wp.DamageVictimTrainer?.TryGetPokemon(wp.DamageVictim);
                    switch (wp.WeatherAction)
                    {
                        case PBEWeatherAction.Added:
                        case PBEWeatherAction.Ended: BattleView.Field.UpdateWeather(); break;
                        case PBEWeatherAction.CausedDamage: break;
                        default: throw new ArgumentOutOfRangeException(nameof(wp.WeatherAction));
                    }
                    string message;
                    switch (wp.Weather)
                    {
                        case PBEWeather.Hailstorm:
                        {
                            switch (wp.WeatherAction)
                            {
                                case PBEWeatherAction.Added: message = "It started to hail!"; break;
                                case PBEWeatherAction.CausedDamage: message = "{0} is buffeted by the hail!"; break;
                                case PBEWeatherAction.Ended: message = "The hail stopped."; break;
                                default: throw new ArgumentOutOfRangeException(nameof(wp.WeatherAction));
                            }
                            break;
                        }
                        case PBEWeather.HarshSunlight:
                        {
                            switch (wp.WeatherAction)
                            {
                                case PBEWeatherAction.Added: message = "The sunlight turned harsh!"; break;
                                case PBEWeatherAction.Ended: message = "The sunlight faded."; break;
                                default: throw new ArgumentOutOfRangeException(nameof(wp.WeatherAction));
                            }
                            break;
                        }
                        case PBEWeather.Rain:
                        {
                            switch (wp.WeatherAction)
                            {
                                case PBEWeatherAction.Added: message = "It started to rain!"; break;
                                case PBEWeatherAction.Ended: message = "The rain stopped."; break;
                                default: throw new ArgumentOutOfRangeException(nameof(wp.WeatherAction));
                            }
                            break;
                        }
                        case PBEWeather.Sandstorm:
                        {
                            switch (wp.WeatherAction)
                            {
                                case PBEWeatherAction.Added: message = "A sandstorm kicked up!"; break;
                                case PBEWeatherAction.CausedDamage: message = "{0} is buffeted by the sandstorm!"; break;
                                case PBEWeatherAction.Ended: message = "The sandstorm subsided."; break;
                                default: throw new ArgumentOutOfRangeException(nameof(wp.WeatherAction));
                            }
                            break;
                        }
                        default: throw new ArgumentOutOfRangeException(nameof(wp.Weather));
                    }
                    BattleView.AddMessage(string.Format(message, NameForTrainer(damageVictim, true)));
                    return false;
                }
                case IPBEAutoCenterPacket acp:
                {
                    PBEBattlePokemon pokemon0 = acp is IPBEAutoCenterPacket_0 acp0
                        ? acp.Pokemon0Trainer.TryGetPokemon(acp0.Pokemon0)
                        : acp.Pokemon0Trainer.TryGetPokemon(acp.Pokemon0OldPosition);
                    PBEBattlePokemon pokemon1 = acp is IPBEAutoCenterPacket_1 acp1
                        ? acp.Pokemon1Trainer.TryGetPokemon(acp1.Pokemon1)
                        : acp.Pokemon1Trainer.TryGetPokemon(acp.Pokemon1OldPosition);
                    BattleView.Field.MovePokemon(pokemon0, acp.Pokemon0OldPosition);
                    BattleView.Field.MovePokemon(pokemon1, acp.Pokemon1OldPosition);
                    BattleView.AddMessage("The battlers shifted to the center!");
                    return false;
                }
                case PBETurnBeganPacket tbp:
                {
                    BattleView.AddMessage($"Turn {tbp.TurnNumber}", messageBox: false);
                    return true;
                }
                case PBEWinnerPacket win:
                {
                    BattleView.AddMessage(string.Format("{0} defeated {1}!", win.WinningTeam.CombinedName, win.WinningTeam.OpposingTeam.CombinedName));
                    return true;
                }
                case PBEMoveLockPacket _:
                case PBEMovePPChangedPacket _:
                case PBEIllusionPacket _:
                case PBETransformPacket _:
                case PBEBattlePacket _:
                case PBEActionsRequestPacket _:
                case PBESwitchInRequestPacket _: return true;
                default: throw new ArgumentOutOfRangeException(nameof(packet));
            }
        }
        #endregion
    }
}
