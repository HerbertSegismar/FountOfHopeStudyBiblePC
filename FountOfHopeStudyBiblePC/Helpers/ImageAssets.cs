using System.IO;

namespace FountOfHopeStudyBiblePC.Helpers
{
    public static class ImageAssets
    {
        private const string ImagesFolder = "Assets/images";
        private const string ImagesMdFolder = "Assets/images-md";
        private const string TexturesFolder = "Assets/textures";

        private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        public static string PickHeroImage()
        {
            var folder = ImagesFolder;
            var files = Enumerate(folder);
            if (files.Count == 0)
            {
                folder = ImagesMdFolder;
                files = Enumerate(folder);
            }
            if (files.Count == 0) return string.Empty;

            return $"pack://siteoforigin:,,,/{folder}/{files[Random.Shared.Next(files.Count)]}";
        }

        public static string GetAssetImagePath(string? fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return string.Empty;
            return $"pack://siteoforigin:,,,/{TexturesFolder}/{fileName}";
        }

        private static List<string> Enumerate(string relative)
        {
            var full = Path.Combine(AppContext.BaseDirectory, relative);
            if (!Directory.Exists(full)) return [];

            var result = new List<string>();
            foreach (var path in Directory.EnumerateFiles(full))
            {
                if (SupportedExtensions.Contains(Path.GetExtension(path)))
                    result.Add(Path.GetFileName(path));
            }
            return result;
        }
    }
}