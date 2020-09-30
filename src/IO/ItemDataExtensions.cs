using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClassicUO.Utility.Logging;

namespace ClassicUO.IO
{
    internal static class ItemDataExtensions
    {
        static readonly Dictionary<ushort, ItemExt> _extData = new Dictionary<ushort, ItemExt>();

        public static void Load()
        {
            try
            {
                _extData.Clear();

                var path = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Client", "item_extensions.txt");
                var xml = File.ReadAllText(path);

                using (var sr = new StringReader(xml))
                {
                    string line;

                    while ((line = sr.ReadLine()) != null)
                    {
                        string[] parts = line.Split(',')
                                             .Select(s => s.Trim())
                                             .ToArray();

                        var graphic = ushort.Parse(parts[0]);
                        var paperdrollAppearance = (ItemExt_PaperdollAppearance)Enum.Parse(typeof(ItemExt_PaperdollAppearance), parts[1]);
                        var requiredHands = (ItemExt_RequiredHands)Enum.Parse(typeof(ItemExt_RequiredHands), parts[2]);

                        _extData[graphic] = new ItemExt
                        {
                            Graphic = graphic,
                            PaperdollAppearance = paperdrollAppearance,
                            RequiredHands = requiredHands
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"{nameof(ItemDataExtensions)}::{nameof(Load)}() - " +
                        $"there was a problem during load: \r\n" +
                        $"{ex}");
            }
        }

        public static bool TryGetValue(ushort graphic, out ItemExt extensions)
        {
            if (_extData.TryGetValue(graphic, out extensions))
            {
                return true;
            }

            extensions = new ItemExt
            {
                Graphic = graphic,
                PaperdollAppearance = ItemExt_PaperdollAppearance.Invalid,
                RequiredHands = ItemExt_RequiredHands.Invalid
            };

            return false;
        }
    }

    public enum ItemExt_PaperdollAppearance : byte
    {
        Invalid = 0x0,

        // shields, lanterns, etc.
        Left = 0x1,

        // mostly weapons, spellbooks, etc.
        Right = 0x2
    }

    public enum ItemExt_RequiredHands : byte
    {
        Invalid = 0x0,

        // shields, most weapons, etc.
        One = 0x1,

        // halberd, hxbow, etc.
        Two = 0x2
    }

    internal struct ItemExt
    {
        public ushort Graphic;
        public ItemExt_PaperdollAppearance PaperdollAppearance;
        public ItemExt_RequiredHands RequiredHands;
    }
}