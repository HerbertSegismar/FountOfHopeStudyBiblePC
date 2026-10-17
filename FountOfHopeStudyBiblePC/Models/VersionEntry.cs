using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FountOfHopeStudyBiblePC.Models
{
    public class VersionEntry : INotifyPropertyChanged
    {
        private bool _isSelected;

        public string Language { get; set; } = "";
        public string Key { get; set; } = "";   // e.g. "kj2.sqlite3"
        public string ShortName { get; set; } = "";   // e.g. "KJ2"
        public string Description { get; set; } = "";

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                PropertyChanged?.Invoke(this,
                    new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public class VersionGroup
    {
        public string Language { get; set; } = "";
        public List<VersionEntry> Versions { get; set; } = new();
    }
}