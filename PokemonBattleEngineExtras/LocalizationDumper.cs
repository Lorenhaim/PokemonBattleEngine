﻿using Kermalis.PokemonBattleEngine.Data;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Kermalis.PokemonBattleEngineExtras
{
    internal class LocalizationDumper
    {
        // You must dump the NARC files yourself (/a/0/0/2 in each language)
        public static void Dump(SqliteConnection con)
        {
            using (var english = new NARC(@"../../../\DumpedData\W2EnglishTexts.narc"))
            using (var french = new NARC(@"../../../\DumpedData\W2FrenchTexts.narc"))
            using (var german = new NARC(@"../../../\DumpedData\W2GermanTexts.narc"))
            using (var italian = new NARC(@"../../../\DumpedData\W2ItalianTexts.narc"))
            using (var japanese = new NARC(@"../../../\DumpedData\W2JapaneseTexts.narc"))
            using (var korean = new NARC(@"../../../\DumpedData\W2KoreanTexts.narc"))
            using (var spanish = new NARC(@"../../../\DumpedData\W2SpanishTexts.narc"))
            using (SqliteTransaction transaction = con.BeginTransaction())
            using (SqliteCommand cmd = con.CreateCommand())
            {
                cmd.Transaction = transaction;

                void CreateTable(string tableName)
                {
                    cmd.CommandText = $"DROP TABLE IF EXISTS {tableName}";
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = $"CREATE TABLE {tableName}(Id INTEGER PRIMARY KEY, English TEXT, French TEXT, German TEXT, Italian TEXT, Japanese_Kana TEXT, Japanese_Kanji TEXT, Korean TEXT, Spanish TEXT)";
                    cmd.ExecuteNonQuery();
                }
                void Insert(string tableName, uint id, string e, string f, string g, string i, string jkana, string jkanji, string k, string s)
                {
                    cmd.CommandText = $"INSERT INTO {tableName} VALUES(@0, @1, @2, @3, @4, @5, @6, @7, @8)";
                    cmd.Parameters.AddWithValue("@0", id);
                    cmd.Parameters.AddWithValue("@1", e);
                    cmd.Parameters.AddWithValue("@2", f);
                    cmd.Parameters.AddWithValue("@3", g);
                    cmd.Parameters.AddWithValue("@4", i);
                    cmd.Parameters.AddWithValue("@5", jkana);
                    cmd.Parameters.AddWithValue("@6", jkanji);
                    cmd.Parameters.AddWithValue("@7", k);
                    cmd.Parameters.AddWithValue("@8", s);
                    cmd.ExecuteNonQuery();
                    cmd.Parameters.Clear();
                }

                string[][] eng, fre, ger, ita, jap, kor, spa;
                void LoadTexts(int fileNum)
                {
                    eng = Utils.ReadTextFile(english, fileNum);
                    fre = Utils.ReadTextFile(french, fileNum);
                    ger = Utils.ReadTextFile(german, fileNum);
                    ita = Utils.ReadTextFile(italian, fileNum);
                    jap = Utils.ReadTextFile(japanese, fileNum);
                    kor = Utils.ReadTextFile(korean, fileNum);
                    spa = Utils.ReadTextFile(spanish, fileNum);
                }
                void WriteTexts(string tableName, uint id, int text)
                {
                    Insert(tableName, id, eng[0][text], fre[0][text], ger[0][text], ita[0][text], jap[0][text], jap[1][text], kor[0][text], spa[0][text]);
                }

                // Abilities
                {
                    void WriteAll(string tableName)
                    {
                        CreateTable(tableName);
                        for (byte i = 0; i < (byte)PBEAbility.MAX; i++)
                        {
                            WriteTexts(tableName, i, i);
                        }
                    }
                    LoadTexts(374);
                    WriteAll("AbilityNames");
                    LoadTexts(375);
                    WriteAll("AbilityDescriptions");
                }
                // Genders (Does not have PBEGender.Genderless)
                {
                    LoadTexts(441);
                    const string tableName = "GenderNames";
                    CreateTable(tableName);
                    WriteTexts(tableName, (byte)PBEGender.Female, 115);
                    Insert(tableName, (byte)PBEGender.Genderless, "Unknown", "Inconnu", "Unbekannt", "Sconosciuto", "不明のすがた", "不明のすがた", "불명의 모습", "Desconocido");
                    WriteTexts(tableName, (byte)PBEGender.Male, 114);
                }
                // Items
                {
                    IEnumerable<PBEItem> allItems = Enum.GetValues(typeof(PBEItem)).Cast<PBEItem>();
                    void WriteAll(string tableName)
                    {
                        CreateTable(tableName);
                        foreach (PBEItem item in allItems)
                        {
                            ushort i = (ushort)item;
                            WriteTexts(tableName, i, i);
                        }
                    }
                    LoadTexts(63);
                    WriteAll("ItemDescriptions");
                    LoadTexts(64);
                    WriteAll("ItemNames");
                }
                // Moves
                {
                    IEnumerable<PBEMove> allMoves = Enum.GetValues(typeof(PBEMove)).Cast<PBEMove>().Except(new[] { PBEMove.MAX });
                    void WriteAll(string tableName)
                    {
                        CreateTable(tableName);
                        foreach (PBEMove move in allMoves)
                        {
                            ushort i = (ushort)move;
                            WriteTexts(tableName, i, i);
                        }
                    }
                    LoadTexts(402);
                    WriteAll("MoveDescriptions");
                    LoadTexts(403);
                    WriteAll("MoveNames");
                }
                // Natures
                {
                    LoadTexts(379);
                    const string tableName = "NatureNames";
                    CreateTable(tableName);
                    for (byte i = 0; i < (byte)PBENature.MAX; i++)
                    {
                        WriteTexts(tableName, i, i + 35); // Nature 0 is at entry 35 in this file
                    }
                }
                // Species
                {
                    void WriteAll(string tableName)
                    {
                        CreateTable(tableName);
                        for (ushort i = 1; i <= 649; i++)
                        {
                            WriteTexts(tableName, i, i);
                        }
                    }
                    LoadTexts(90);
                    WriteAll("SpeciesNames");
                    LoadTexts(442);
                    WriteAll("SpeciesEntries");
                    LoadTexts(464);
                    WriteAll("SpeciesCategories");
                }
                // Types (Does not have PBEType.None)
                {
                    LoadTexts(398);
                    const string tableName = "TypeNames";
                    CreateTable(tableName);
                    const string none =  "-----";
                    Insert(tableName, (byte)PBEType.None, none, none, none, none, none, none, none, none);
                    for (byte i = 0; i < Utils.Gen5Types.Length; i++)
                    {
                        WriteTexts(tableName, (byte)Utils.Gen5Types[i], i);
                    }
                }

                transaction.Commit();
            }
        }
    }
}
