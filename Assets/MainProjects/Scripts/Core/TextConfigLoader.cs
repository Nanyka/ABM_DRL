using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Serialization;

namespace Sugarscape
{
    [CreateAssetMenu(menuName = "Sugarscape/Config/TextConfigLoader")]
    public class TextConfigLoader : ScriptableObject
    {
        public TextAsset sugarText;
        public TextAsset spiceText;
        
        public int Width { get; private set; }
        public int Height { get; private set; }

        private int[,] sugarGrid;
        private int[,] spiceGrid;

        public void Init()
        {
            LoadSugarGrid();
            LoadSpiceGrid();
        }

        private void LoadSugarGrid()
        {
            if (sugarText == null) {
                Debug.LogError("No TextAsset assigned to TextConfigLoader");
                return;
            }

            var lines = sugarText.text.Split(new[] {'\n'}, System.StringSplitOptions.RemoveEmptyEntries);
            Height = lines.Length;
            var rowValues = new List<int[]>();
            foreach (var line in lines)
            {
                var tokens = line.Split(' ', '	');
                var vals = new List<int>();
                foreach (var tok in tokens)
                {
                    if (int.TryParse(tok, out int v)) vals.Add(v);
                }

                rowValues.Add(vals.ToArray());
            }

            Width = rowValues[0].Length;
            // Debug.Log($"Loaded {Width}x{Height} sugar grid");
            sugarGrid = new int[Width, Height];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    sugarGrid[x, y] = rowValues[y][x];
                }
            }
        }

        private void LoadSpiceGrid()
        {
            if (spiceText == null) {
                Debug.LogError("No TextAsset assigned to TextConfigLoader");
                return;
            }

            var lines = spiceText.text.Split(new[] {'\n'}, System.StringSplitOptions.RemoveEmptyEntries);
            Height = lines.Length;
            var rowValues = new List<int[]>();
            foreach (var line in lines)
            {
                var tokens = line.Split(' ', '	');
                var vals = new List<int>();
                foreach (var tok in tokens)
                {
                    if (int.TryParse(tok, out int v)) vals.Add(v);
                }

                rowValues.Add(vals.ToArray());
            }

            Width = rowValues[0].Length;
            // Debug.Log($"Loaded {Width}x{Height} spice grid");
            spiceGrid = new int[Width, Height];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    spiceGrid[x, y] = rowValues[y][x];
                }
            }
        }

        public int GetSugar(int x, int y, int defaultValue = 0)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
                return sugarGrid[x, y];
            return defaultValue;
        }
        
        public int GetSpice(int x, int y, int defaultValue = 0)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
                return spiceGrid[x, y];
            return defaultValue;
        }
    }
}