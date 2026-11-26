using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScoutsMap
{
    internal enum MarkerIcon
    {
        Treasure, Hermit, Reward, Road, City, Water, Shop, Quest, Spell, Capture, Relic, Item,
        Recruit, Hero, Pirate, Wonder, Witch, Hospital, Trade, Apple, Rare, Cage, Salvage, Provoke
    }

    internal sealed class MarkerArt : IDisposable
    {
        private readonly List<Sprite> sprites = new List<Sprite>();
        private readonly List<Texture2D> textures = new List<Texture2D>();

        internal readonly Sprite Arrow, Lock, Panel;
        private enum FrameType { Route, Shop, Site, Person, Loot, Magic, Monster }
        private readonly (Sprite Plate, Sprite Rim, Color32 Accent)[] frames =
            new (Sprite, Sprite, Color32)[Enum.GetValues(typeof(FrameType)).Length];
        private readonly Sprite[,] symbols = new Sprite[Enum.GetValues(typeof(MarkerIcon)).Length, 2];

        internal MarkerArt()
        {
            var pearl = new Color32(255, 250, 225, 255);
            string[] plate =
            {
                "..oooooooooooo..",
                ".obbbbbbbbbbbbo.",
                "obbbbbbbbbbbbbbo",
                "obbbbbbbbbbbbbbo",
                "obbbbbbbbbbbbbbo",
                "obbbbbbbbbbbbbbo",
                "obbbbbbbbbbbbbbo",
                "obbbbbbbbbbbbbbo",
                "obbbbbbbbbbbbbbo",
                "obbbbbbbbbbbbbbo",
                "obbbbbbbbbbbbbbo",
                "obbbbbbbbbbbbbbo",
                "obbbbbbbbbbbbbbo",
                "obbbbbbbbbbbbbbo",
                ".obbbbbbbbbbbbo.",
                "..oooooooooooo.."
            };
            Panel = Draw(Color.white, new Color32(42, 31, 33, 255), pearl, Vector4.one * 4f, plate);
            AddFrame(FrameType.Route, new Color32(226, 194, 146, 255), new Color32(51, 41, 36, 255),
                12, 14, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 14, 12);
            AddFrame(FrameType.Shop, new Color32(255, 177, 105, 255), new Color32(70, 36, 29, 255),
                10, 12, 14, 16, 16, 16, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14);
            AddFrame(FrameType.Site, new Color32(143, 216, 192, 255), new Color32(27, 58, 55, 255),
                8, 12, 12, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16);
            AddFrame(FrameType.Person, new Color32(166, 202, 253, 255), new Color32(32, 43, 69, 255),
                10, 14, 16, 16, 16, 16, 16, 16, 16, 16, 16, 14, 12, 10, 8, 4);
            AddFrame(FrameType.Loot, new Color32(251, 211, 110, 255), new Color32(56, 43, 26, 255),
                8, 12, 14, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 14, 12, 8);
            AddFrame(FrameType.Magic, new Color32(215, 173, 251, 255), new Color32(54, 35, 68, 255),
                4, 8, 12, 14, 14, 16, 16, 16, 16, 16, 16, 14, 14, 12, 8, 4);
            AddFrame(FrameType.Monster, new Color32(255, 141, 142, 255), new Color32(72, 35, 44, 255),
                6, 10, 14, 16, 16, 16, 16, 16, 16, 16, 16, 16, 14, 12, 10, 6);
            Arrow = Draw(new Color32(15, 16, 22, 255), Color.clear, pearl, Vector4.zero,
                new[] { "o......", "o#o....", "o###o..", "o#####o", "o###o..", "o#o....", "o......" });
            Lock = Draw(new Color32(15, 16, 22, 255), new Color32(45, 52, 64, 255), pearl, Vector4.zero,
                new[] { "..ooo..", ".o###o.", ".o#.#o.", "o#####o", "o##b##o", "o##b##o", ".ooooo." });

            Add(MarkerIcon.Treasure, new Color32(217, 147, 58, 255), new Color32(126, 60, 42, 255),
                "..oooooooo..", ".o########o.", "o#+----+#++o", "o++----+++-o",
                "o##########o", "o--+###+---o", "o---+#+----o", "o---+#+----o",
                "o+++---++++o", ".oooooooooo.");
            Add(MarkerIcon.Hermit, new Color32(138, 176, 98, 255), new Color32(68, 96, 70, 255),
                "....oooo....", "...o+++-o...", "..o+#++--o..", ".o++#o+---o.",
                ".o++o##o--o.", ".o+o###o--o.", ".oo+#++-ooo.", ".o++#+----o.",
                "o++-------+o", ".oooooooooo.");
            Add(MarkerIcon.Reward, new Color32(216, 147, 246, 255), new Color32(134, 70, 183, 255),
                ".....o......", "....o#o.....", "...o##+o.o..", "..o##++ooo..",
                ".o##++---o..", "o#+++----+o.", ".o++----+o..", "..o+---+o...",
                "...o+++o....", "....o+o.....", ".....o......");
            Add(MarkerIcon.Road, new Color32(213, 172, 114, 255), new Color32(134, 97, 62, 255),
                "..oo....oo..", ".o##o..o##o.", ".o++#oo#++o.", "..o++##++o..",
                "...o++++o...", "...o+##-o...", "...o+---o...", "...o+##-o...",
                "...o+---o...", "...o++++o...", "....oooo....");
            Add(MarkerIcon.City, new Color32(212, 172, 136, 255), new Color32(138, 93, 79, 255),
                ".oo.oo.oo...", "o##o##o##o..", "o########o..", "o+------#o..",
                "o+-oooo-#o..", "o+o####o#o..", "o+o#..#o#o..", "o+o#..#o#o..",
                "o+o#..#o+o..", ".oo....oo...");
            Add(MarkerIcon.Water, new Color32(101, 202, 224, 255), new Color32(54, 121, 158, 255),
                ".....oo.....", ".....o#o....", ".....o++o...", "...o.o+--o..",
                "..o#.o+---o.", ".o#o.o++++oo", "oooooooooooo", "o+########+o",
                ".o--------o.", "..o++++++o..", "...oooooo...");
            Add(MarkerIcon.Shop, new Color32(243, 133, 91, 255), new Color32(152, 68, 52, 255),
                "..oooooooo..", ".o#++#++#+o.", "o##+##+##++o", "o++-++-++--o",
                ".oooooooooo.", ".o#++++++#o.", ".o+o####o+o.", ".o+o##o#o+o.",
                ".o+o####o+o.", ".oooooooooo.");
            Add(MarkerIcon.Quest, new Color32(218, 184, 115, 255), new Color32(134, 96, 64, 255),
                "..ooooooo.", "..o####oo.", "..o#+++o..", "..o###+o..", "..o++++o..",
                "..o###+o..", "..o++++o..", ".oo++++o..", "o#######o.", ".ooooooo..");
            Add(MarkerIcon.Spell, new Color32(121, 169, 247, 255), new Color32(61, 94, 163, 255),
                ".....o....", "....o#o.o.", "..o.o##oo.", "...oo#+o..", "..o##+o...",
                "...oo+o...", ".oo#oo....", "o##o......", "oooo......");
            Add(MarkerIcon.Capture, new Color32(143, 199, 129, 255), new Color32(59, 108, 77, 255),
                "...oooo...", "..o#..#o..", ".oo++++oo.", "o########o", "o#o+o+o#o.",
                "o#o#o#o#o.", "o#o#o#o#o.", "o++++++++o", ".oooooooo.");
            Add(MarkerIcon.Relic, new Color32(217, 159, 240, 255), new Color32(122, 74, 171, 255),
                "..oooooo..", ".o######o.", "o###++###o", "o##+--+##o", ".o++##++o.",
                "..o####o..", "...o##o...", "...o++o...", "....oo....");
            Add(MarkerIcon.Item, new Color32(152, 204, 225, 255), new Color32(65, 110, 139, 255),
                "...oooo...", "...o##o...", "...o##o...", "..oo++oo..", ".o##+++oo.",
                "o##+++++-o", "o#++#+++-o", "o+--+---+o", ".oooooooo.");
            Add(MarkerIcon.Recruit, new Color32(177, 189, 217, 255), new Color32(83, 102, 139, 255),
                "...oooo...", "..o####o..", ".o##++++o.", "o##++++--o", "o#+o++o+-o",
                "o#++++++-o", ".o++##++o.", "..o++++o..", "...oooo...");
            Add(MarkerIcon.Hero, new Color32(238, 179, 94, 255), new Color32(163, 94, 50, 255),
                "o....o...o", "o#..o#o.#o", "o#oo#++o#o", "o###+#++#o", "o++#+++#+o",
                ".o++++++o.", ".o######o.", "..oooooo..");
            Add(MarkerIcon.Pirate, new Color32(196, 203, 200, 255), new Color32(89, 109, 107, 255),
                "..ooooooo.", "..o#####oo", "..o#o#o#o.", "..o#####oo", "..o++o++o.",
                "..ooooooo.", "..o.......", "..o.......", ".ooo......");
            Add(MarkerIcon.Wonder, new Color32(239, 186, 115, 255), new Color32(147, 85, 83, 255),
                "....oo....", "...o##o...", "..o####o..", ".o######o.", "o+######+o",
                "oooo+oooo.", ".o+#++#+o.", ".o+#++#+o.", "o########o", "oooooooooo");
            Add(MarkerIcon.Witch, new Color32(247, 140, 93, 255), new Color32(169, 60, 61, 255),
                "....o.....", "...o#o....", "...o#+o...", "..o###+o..", ".o##+++-o.",
                "o###o++--o", "o+###+---o", ".o++++++o.", "..oooooo..");
            Add(MarkerIcon.Hospital, new Color32(228, 129, 125, 255), new Color32(151, 57, 78, 255),
                "..oo..oo..", ".o##oo##o.", "o########o", "o###++###o", "o#++++++#o",
                ".o##++##o.", "..o####o..", "...o##o...", "....oo....");
            Add(MarkerIcon.Trade, new Color32(145, 210, 186, 255), new Color32(58, 124, 122, 255),
                "......o...", ".....o#o..", ".ooooo##o.", "o#######oo", ".ooooo##o.",
                "..o..o#o..", ".o#o..o...", "o##oooooo.", "oo#######o", ".o##ooooo.", "..oo......");
            Add(MarkerIcon.Apple, new Color32(235, 125, 94, 255), new Color32(147, 53, 49, 255),
                ".....oo...", "....o++o..", "....o#o...", "..ooo#oo..", ".o##+##+o.",
                "o##+++++-o", "o#+++++--o", "o+++++---o", ".o++-+++o.", "..oooooo..");
            Add(MarkerIcon.Rare, new Color32(115, 212, 228, 255), new Color32(58, 113, 163, 255),
                "...oooo...", "..o####o..", ".o####+oo.", "o##++--#+o", "o#++--+#+o",
                ".o+--++#o.", "..o--+oo..", "...oooo...");
            Add(MarkerIcon.Cage, new Color32(195, 164, 136, 255), new Color32(98, 78, 83, 255),
                "...oooo...", "..o####o..", ".o++++++o.", "o#o#o#o#oo", "o#o#o#o#oo",
                "o#o#o#o#oo", "o#++++++oo", ".oooooooo.");
            Add(MarkerIcon.Salvage, new Color32(209, 158, 102, 255), new Color32(116, 68, 53, 255),
                "..oooooo..", ".o##++##o.", "o###++###o", "oooooooooo", "o++--+--+o",
                "o++--+--+o", "oooooooooo", ".o#+###+o.", "..oooooo..");
            Add(MarkerIcon.Provoke, new Color32(226, 112, 103, 255), new Color32(135, 52, 62, 255),
                "oo......oo", "o#o....o#o", "o#+oooo+#o", "o++++++++o", "o+#++++#+o",
                "o++----++o", ".o+####+o.", "..o+##+o..", "...oooo...");
        }

        internal (Sprite Plate, Sprite Rim, Color32 Accent) Frame(MarkerIcon icon)
        {
            switch (icon)
            {
                case MarkerIcon.Road:
                case MarkerIcon.City:
                case MarkerIcon.Water: return frames[(int)FrameType.Route];
                case MarkerIcon.Shop: return frames[(int)FrameType.Shop];
                case MarkerIcon.Quest:
                case MarkerIcon.Pirate:
                case MarkerIcon.Wonder:
                case MarkerIcon.Witch:
                case MarkerIcon.Hospital:
                case MarkerIcon.Trade: return frames[(int)FrameType.Site];
                case MarkerIcon.Hermit:
                case MarkerIcon.Recruit:
                case MarkerIcon.Hero:
                case MarkerIcon.Cage: return frames[(int)FrameType.Person];
                case MarkerIcon.Reward:
                case MarkerIcon.Spell:
                case MarkerIcon.Relic: return frames[(int)FrameType.Magic];
                case MarkerIcon.Provoke:
                case MarkerIcon.Capture: return frames[(int)FrameType.Monster];
                default: return frames[(int)FrameType.Loot];
            }
        }

        private void AddFrame(FrameType type, Color32 accent, Color32 body, params int[] widths)
        {
            var rim = new string[widths.Length];
            var plate = new string[widths.Length];
            for (int row = 0; row < widths.Length; row++)
            {
                int width = widths[row];
                int interior = row == 0 || row == widths.Length - 1 ? 0 :
                    Math.Min(width - 2, Math.Min(widths[row - 1], widths[row + 1]));
                string padding = new string('.', (16 - width) / 2);
                string edge = new string('o', (width - interior) / 2);
                rim[row] = padding + edge + new string('.', interior) + edge + padding;
                plate[row] = padding + edge + new string('b', interior) + edge + padding;
            }
            frames[(int)type] = (Draw(new Color32(10, 13, 18, 255), body, Color.white, Vector4.zero, plate),
                Draw(Color.white, Color.clear, Color.white, Vector4.zero, rim), accent);
        }

        internal Sprite Symbol(MarkerIcon icon, bool available) => symbols[(int)icon, available ? 0 : 1];

        private void Add(MarkerIcon icon, Color32 middle, Color32 shade, params string[] rows)
        {
            var ink = new Color32(16, 18, 23, 255);
            symbols[(int)icon, 0] = Draw(ink, middle, new Color32(255, 239, 185, 255), Vector4.zero, rows, shade);
            symbols[(int)icon, 1] = Draw(ink, new Color32(147, 158, 172, 255),
                new Color32(208, 216, 221, 255), Vector4.zero, rows, new Color32(81, 95, 111, 255));
        }

        private Sprite Draw(Color32 outline, Color32 body, Color32 highlight, Vector4 border, string[] rows,
            Color32 shade = default)
        {
            int width = rows[0].Length;
            int height = rows.Length;
            var pixels = new Color32[width * height];
            for (int row = 0; row < height; row++)
            for (int x = 0; x < width; x++)
            {
                pixels[(height - row - 1) * width + x] = rows[row][x] switch
                {
                    'o' => outline,
                    'b' => body,
                    '+' => body,
                    '#' => highlight,
                    '-' => shade,
                    _ => (Color32)Color.clear
                };
            }
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, border);
            sprites.Add(sprite);
            textures.Add(texture);
            return sprite;
        }

        public void Dispose()
        {
            foreach (var sprite in sprites)
                UnityEngine.Object.Destroy(sprite);
            foreach (var texture in textures)
                UnityEngine.Object.Destroy(texture);
        }
    }
}
