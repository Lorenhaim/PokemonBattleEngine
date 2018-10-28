﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Kermalis.PokemonBattleEngine.Data;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Kermalis.PokemonBattleEngineClient.Views
{
    class PokemonView : UserControl, INotifyPropertyChanged
    {
        void OnPropertyChanged(string property) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        public new event PropertyChangedEventHandler PropertyChanged;

        PPokemon pokemon;
        public PPokemon Pokemon
        {
            get => pokemon;
            set
            {
                pokemon = value;
                PokemonChanged();
                OnPropertyChanged(nameof(Pokemon));
            }
        }
        double scale;
        public double Scale
        {
            get => scale;
            private set
            {
                scale = value;
                OnPropertyChanged(nameof(Scale));
            }
        }
        bool visible;
        public bool Visible
        {
            get => visible;
            private set
            {
                visible = value;
                OnPropertyChanged(nameof(Visible));
            }
        }
        Point location;
        public Point Location
        {
            get => location;
            set
            {
                location = value;
                OnPropertyChanged(nameof(Location));
            }
        }
        Uri uri;
        public Uri Source
        {
            get => uri; private set
            {
                uri = value;
                OnPropertyChanged(nameof(Source));
            }
        }

        public PokemonView()
        {
            AvaloniaXamlLoader.Load(this);
            DataContext = this;
        }

        void PokemonChanged()
        {
            if (pokemon == null)
            {
                Visible = false;
            }
            else
            {
                Visible = true;
                Scale = pokemon.LocallyOwned ? 2 : 1;

                // Loading the correct sprite requires checking first
                string sss = $"{(ushort)pokemon.Shell.Species:D3}";
                // Get available resources (including sprites)
                string[] resources = Assembly.GetExecutingAssembly().GetManifestResourceNames();
                // Check for sss.gif where sss is the species number
                // Would be false if the species sprites are sss-M.gif and sss-F.gif
                bool spriteIsGenderNeutral = resources.Any(r => r.EndsWith($".{sss}.gif"));
                // sss.gif if the sprite is gender neutral, else sss-F.gif if the pokemon is female, otherwise sss-M.gif
                string suffix = spriteIsGenderNeutral ? "" : pokemon.Shell.Gender == PGender.Female ? "-F" : "-M";
                // Set the result
                Source = new Uri($"resm:Kermalis.PokemonBattleEngineClient.Assets.{(pokemon.LocallyOwned ? "Back_Sprites" : "Front_Sprites")}.{sss}{suffix}.gif?assembly=PokemonBattleEngineClient");
            }
        }
    }
}