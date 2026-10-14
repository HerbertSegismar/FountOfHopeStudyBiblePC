// ViewModels/AppViewModel.cs
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace FountOfHopeStudyBiblePC.ViewModels
{
    public class AppViewModel : INotifyPropertyChanged
    {
        public List<string> Textures { get; } = new();
        public List<string> ImageFilesSm { get; } = new();
        public List<string> ImageFilesMd { get; } = new();

        private bool _darkTheme = true;
        public bool DarkTheme
        {
            get => _darkTheme;
            set { _darkTheme = value; OnPropertyChanged(); }
        }

        public AppViewModel()
        {
            LoadAssets();
        }

        private void LoadAssets()
        {
            Textures.AddRange(ScanFolder("Assets/textures"));
            ImageFilesSm.AddRange(ScanFolder("Assets/images"));
            ImageFilesMd.AddRange(ScanFolder("Assets/images-md"));

            if (Textures.Count == 0)
                Textures.AddRange(Enumerable.Range(0, 12).Select(i => $"texture_{i:D2}.jpg"));
            if (ImageFilesSm.Count == 0)
                ImageFilesSm.AddRange(Enumerable.Range(0, 12).Select(i => $"image_{i:D2}.jpg"));
            if (ImageFilesMd.Count == 0)
                ImageFilesMd.AddRange(Enumerable.Range(0, 12).Select(i => $"image_{i:D2}.jpg"));
        }

        private static IEnumerable<string> ScanFolder(string relativePath)
        {
            var full = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, relativePath);
            if (!Directory.Exists(full)) return Enumerable.Empty<string>();

            return Directory
                .EnumerateFiles(full)
                .Where(f => f.EndsWith(".jpg") || f.EndsWith(".jpeg") ||
                            f.EndsWith(".png") || f.EndsWith(".webp"))
                .Select(Path.GetFileName)
                .Where(n => n != null)
                .Cast<string>()
                .ToList();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
