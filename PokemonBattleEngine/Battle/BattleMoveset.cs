﻿using Kermalis.PokemonBattleEngine.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Kermalis.PokemonBattleEngine.Battle
{
    public sealed class PBEBattleMoveset : IReadOnlyList<PBEBattleMoveset.PBEBattleMovesetSlot>
    {
        public sealed class PBEBattleMovesetSlot : INotifyPropertyChanged
        {
            private void OnPropertyChanged(string property)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
            }
            public event PropertyChangedEventHandler PropertyChanged;

            private PBEMove _move;
            public PBEMove Move
            {
                get => _move;
                set
                {
                    if (_move != value)
                    {
                        _move = value;
                        OnPropertyChanged(nameof(Move));
                    }
                }
            }
            private int _pp;
            public int PP
            {
                get => _pp;
                set
                {
                    if (_pp != value)
                    {
                        _pp = value;
                        OnPropertyChanged(nameof(PP));
                    }
                }
            }
            private int _maxPP;
            public int MaxPP
            {
                get => _maxPP;
                set
                {
                    if (_maxPP != value)
                    {
                        _maxPP = value;
                        OnPropertyChanged(nameof(MaxPP));
                    }
                }
            }

            internal PBEBattleMovesetSlot()
            {
                _move = PBEMove.MAX;
            }
            internal PBEBattleMovesetSlot(PBEMove move, int pp, int maxPP)
            {
                _move = move;
                _pp = pp;
                _maxPP = maxPP;
            }
        }

        private readonly PBEBattleMovesetSlot[] _list;
        public int Count => _list.Length;
        public PBEBattleMovesetSlot this[int index]
        {
            get
            {
                if (index >= _list.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
                return _list[index];
            }
        }
        public PBEBattleMovesetSlot this[PBEMove move]
        {
            get
            {
                for (int i = 0; i < _list.Length; i++)
                {
                    PBEBattleMovesetSlot slot = _list[i];
                    if (slot.Move == move)
                    {
                        return slot;
                    }
                }
                return null;
            }
        }

        internal PBEBattleMoveset(PBESettings settings)
        {
            int count = settings.NumMoves;
            _list = new PBEBattleMovesetSlot[count];
            for (int i = 0; i < count; i++)
            {
                _list[i] = new PBEBattleMovesetSlot();
            }
        }
        internal PBEBattleMoveset(PBESettings settings, PBEReadOnlyPartyMoveset moveset)
        {
            int count = moveset.Count;
            if (count != settings.NumMoves)
            {
                throw new ArgumentOutOfRangeException(nameof(moveset), $"Moveset count must be equal to \"{nameof(settings.NumMoves)}\" ({settings.NumMoves}).");
            }
            _list = new PBEBattleMovesetSlot[count];
            for (int i = 0; i < count; i++)
            {
                PBEReadOnlyPartyMoveset.PBEReadOnlyPartyMovesetSlot slot = moveset[i];
                PBEMove move = slot.Move;
                int pp = PBEDataUtils.CalcMaxPP(move, slot.PPUps, settings);
                _list[i] = new PBEBattleMovesetSlot(move, pp, pp);
            }
        }
        internal PBEBattleMoveset(PBEBattleMoveset other)
        {
            int count = other._list.Length;
            _list = new PBEBattleMovesetSlot[count];
            for (int i = 0; i < count; i++)
            {
                PBEBattleMovesetSlot oSlot = other._list[i];
                _list[i] = new PBEBattleMovesetSlot(oSlot.Move, oSlot.PP, oSlot.MaxPP);
            }
        }

        public static int GetTransformPP(PBESettings settings, PBEMove move)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }
            if (!settings.IsReadOnly)
            {
                throw new ArgumentException("Settings must be read-only.", nameof(settings));
            }
            else if (move == PBEMove.None)
            {
                return 0;
            }
            else if (move >= PBEMove.MAX || !PBEMoveData.IsMoveUsable(move))
            {
                throw new ArgumentOutOfRangeException(nameof(move));
            }
            else
            {
                return PBEMoveData.Data[move].PPTier == 0 ? 1 : settings.PPMultiplier;
            }
        }
        public static int GetNonTransformPP(PBESettings settings, PBEMove move, byte ppUps)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }
            if (!settings.IsReadOnly)
            {
                throw new ArgumentException("Settings must be read-only.", nameof(settings));
            }
            else if (move == PBEMove.None)
            {
                return 0;
            }
            else if (move >= PBEMove.MAX || !PBEMoveData.IsMoveUsable(move))
            {
                throw new ArgumentOutOfRangeException(nameof(move));
            }
            else
            {
                byte tier = PBEMoveData.Data[move].PPTier;
                return Math.Max(1, (tier * settings.PPMultiplier) + (tier * ppUps));
            }
        }

        internal static void DoTransform(PBEBattlePokemon user, PBEBattlePokemon target)
        {
            PBEBattleMoveset targetKnownBackup = null;
            if (user.Trainer != target.Trainer)
            {
                targetKnownBackup = new PBEBattleMoveset(target.KnownMoves);
            }
            PBESettings settings = user.Battle.Settings;
            for (int i = 0; i < settings.NumMoves; i++)
            {
                PBEBattleMovesetSlot userMove = user.Moves._list[i];
                PBEBattleMovesetSlot userKnownMove = user.KnownMoves._list[i];
                PBEBattleMovesetSlot targetMove = target.Moves._list[i];
                PBEBattleMovesetSlot targetKnownMove = target.KnownMoves._list[i];
                PBEMove move;
                int pp;
                if (user.Trainer == target.Trainer)
                {
                    move = targetMove.Move;
                    pp = move == PBEMove.MAX ? 0 : GetTransformPP(settings, move);
                    userMove.Move = move;
                    userMove.PP = pp;
                    userMove.MaxPP = pp;

                    move = targetKnownMove.Move;
                    pp = move == PBEMove.MAX ? 0 : GetTransformPP(settings, move);
                    userKnownMove.Move = move;
                    userKnownMove.PP = 0;
                    userKnownMove.MaxPP = pp;
                }
                else
                {
                    move = targetMove.Move;
                    pp = move == PBEMove.MAX ? 0 : GetTransformPP(settings, move);
                    userMove.Move = move;
                    userMove.PP = pp;
                    userMove.MaxPP = pp;
                    targetKnownMove.Move = move;
                    // Try to copy known PP from previous known moves
                    PBEBattleMovesetSlot bSlot = targetKnownBackup[move];
                    if (bSlot == null) // bSlot is null if the current move was not previously known
                    {
                        targetKnownMove.PP = 0;
                        targetKnownMove.MaxPP = 0;
                    }
                    else
                    {
                        targetKnownMove.PP = bSlot.PP;
                        targetKnownMove.MaxPP = bSlot.MaxPP;
                    }
                    userKnownMove.Move = move;
                    userKnownMove.PP = 0;
                    userKnownMove.MaxPP = pp;
                }
            }
        }
        internal ReadOnlyCollection<PBEMove> ForTransformPacket()
        {
            var moves = new PBEMove[_list.Length];
            for (int i = 0; i < _list.Length; i++)
            {
                moves[i] = _list[i].Move;
            }
            return new ReadOnlyCollection<PBEMove>(moves);
        }
        internal void Reset(PBEBattleMoveset other)
        {
            for (int i = 0; i < _list.Length; i++)
            {
                PBEBattleMovesetSlot slot = _list[i];
                PBEBattleMovesetSlot oSlot = other._list[i];
                slot.Move = oSlot.Move;
                slot.PP = oSlot.PP;
                slot.MaxPP = oSlot.MaxPP;
            }
        }
        internal void SetUnknown()
        {
            for (int i = 0; i < _list.Length; i++)
            {
                PBEBattleMovesetSlot slot = _list[i];
                slot.Move = PBEMove.MAX;
                slot.PP = 0;
                slot.MaxPP = 0;
            }
        }

        public bool Contains(PBEMove move)
        {
            return this[move] != null;
        }
        public bool Contains(PBEMoveEffect effect)
        {
            for (int i = 0; i < _list.Length; i++)
            {
                PBEMove move = _list[i].Move;
                if (move != PBEMove.None && move != PBEMove.MAX && PBEMoveData.Data[move].Effect == effect)
                {
                    return true;
                }
            }
            return false;
        }

        public IEnumerator<PBEBattleMovesetSlot> GetEnumerator()
        {
            for (int i = 0; i < _list.Length; i++)
            {
                yield return _list[i];
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
