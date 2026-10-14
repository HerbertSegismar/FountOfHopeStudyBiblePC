// Helpers/ImageAssets.cs — tiny helper shared by ImageSection / HomeScreen
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FountOfHopeStudyBiblePC.Helpers
{
    public static class ImageAssets
    {
        private static readonly Random Rng = new();

        public static string PickHeroImage()
        {
            var files = Enumerate("Assets/images");
            if (files.Count == 0) files = Enumerate("Assets/images-md");
            if (files.Count == 0) return string.Empty;

            return $"pack://siteoforigin:,,,/Assets/images/{files[Rng.Next(files.Count)]}";
        }

        public static string GetAssetImagePath(string fileName) =>
            $"pack://siteoforigin:,,,/Assets/textures/{fileName}";

        private static List<string> Enumerate(string relative)
        {
            var full = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relative);
            if (!Directory.Exists(full)) return new List<string>();

            return Directory.EnumerateFiles(full)
                .Where(f => f.EndsWith(".jpg") || f.EndsWith(".jpeg") ||
                            f.EndsWith(".png") || f.EndsWith(".webp"))
                .Select(Path.GetFileName)
                .Where(n => n != null)
                .Cast<string>()
                .ToList();
        }
    }
}