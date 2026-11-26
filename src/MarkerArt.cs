using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScoutsMap
{
    internal sealed class MarkerArt : IDisposable
    {
        private readonly List<Sprite> sprites = new List<Sprite>();
        private readonly List<Texture2D> textures = new List<Texture2D>();

        internal readonly Sprite Plate, Arrow, Lock;
        private readonly Sprite[] symbols;

        internal MarkerArt()
        {
            Plate = Draw(
                "..oooooooooo..",
                ".obbbbbbbbbbo.",
                "obbbbbbbbbbbbo",
                "obbbbbbbbbbbbo",
                "obbbbbbbbbbbbo",
                "obbbbbbbbbbbbo",
                "obbbbbbbbbbbbo",
                "obbbbbbbbbbbbo",
                "obbbbbbbbbbbbo",
                "obbbbbbbbbbbbo",
                "obbbbbbbbbbbbo",
                "obbbbbbbbbbbbo",
                ".obbbbbbbbbbo.",
                "..oooooooooo..");
            Arrow = Draw(
                "..o....",
                "..oo...",
                "..o#o..",
                "..o##o.",
                "..o###o",
                "..o##o.",
                "..o#o..",
                "..oo...",
                "..o....");
            Lock = Draw(
                "..ooo..",
                ".ob.bo.",
                ".ob.bo.",
                "ob###bo",
                "ob#b#bo",
                "ob###bo",
                ".ooooo.");
            symbols = new[]
            {
                Draw("..ooooooo..", ".ob#####bo.", "ob########o", "o#########o",
                     "ooooooooooo", "ob###b###bo", "ob######bbo", ".ooooooooo."),
                Draw("....oo....", "...o##o...", "..o####o..", ".o##b###o.",
                     "oboooooobo", "..ob#bo...", ".o#####o..", "o##b#b##o.", "ob#####bo."),
                Draw("....o....", "...o#o...", "..o###o..", "oo#####oo",
                     "o#######o", ".o#####o.", "..o###o..", "...o#o...", "....o...."),
                Draw("...o##o...", "...o##o...", "..o#b#o...", "..o#b##o..",
                     ".o###b#o..", ".o#b###o..", "o###b###o.", "o########o"),
                Draw("..o#####o..", ".o#######o.", "o#o#o#o#o#o", "ooooooooooo",
                     "..o#####o..", "..o#bbb#o..", "..o#b###o..", "..ooooooo..")
            };
        }

        internal Sprite Symbol(MarkerKind kind) => symbols[(int)kind];

        private Sprite Draw(params string[] rows)
        {
            int width = rows[0].Length;
            int height = rows.Length;
            var pixels = new Color32[width * height];
            for (int row = 0; row < height; row++)
            for (int x = 0; x < width; x++)
            {
                Color32 color;
                switch (rows[row][x])
                {
                    case 'o': color = new Color32(95, 101, 107, 255); break;
                    case 'b': color = new Color32(20, 25, 32, 255); break;
                    case '#': color = new Color32(255, 255, 255, 255); break;
                    default: color = new Color32(0, 0, 0, 0); break;
                }
                pixels[(height - row - 1) * width + x] = color;
            }
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect);
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

