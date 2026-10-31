// ViewModels/AppViewModel.cs
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using FountOfHopeStudyBiblePC.Helpers;
using FountOfHopeStudyBiblePC.Models;

namespace FountOfHopeStudyBiblePC.ViewModels
{
    public class AppViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return;
            field = value;
            OnPropertyChanged(name);
        }

        private static Color Argb(uint argb) => Color.FromArgb(
            (byte)((argb >> 24) & 0xFF),
            (byte)((argb >> 16) & 0xFF),
            (byte)((argb >> 8) & 0xFF),
            (byte)(argb & 0xFF));

        // ═══════════════════════════════════════════════════════════════
        //  Asset lists
        // ═══════════════════════════════════════════════════════════════
        public List<string> Textures { get; } = new();
        public List<string> ImageFilesSm { get; } = new();
        public List<string> ImageFilesMd { get; } = new();

        // ═══════════════════════════════════════════════════════════════
        //  Animator state
        // ═══════════════════════════════════════════════════════════════
        private float _animatorEditFontSize = 60f;
        public float AnimatorEditFontSize { get => _animatorEditFontSize; set => Set(ref _animatorEditFontSize, value); }

        private bool _animatorEditIsTextElement;
        public bool AnimatorEditIsTextElement { get => _animatorEditIsTextElement; set => Set(ref _animatorEditIsTextElement, value); }

        private string? _animatorEditFontFamily;
        public string? AnimatorEditFontFamily { get => _animatorEditFontFamily; set => Set(ref _animatorEditFontFamily, value); }

        private string? _animatorEditTextAlign;
        public string? AnimatorEditTextAlign { get => _animatorEditTextAlign; set => Set(ref _animatorEditTextAlign, value); }

        private string? _animatorKeyframeTargetElementId;
        public string? AnimatorKeyframeTargetElementId { get => _animatorKeyframeTargetElementId; set => Set(ref _animatorKeyframeTargetElementId, value); }

        private bool _animatorShowKeyframeDialog;

        private bool _showHeaderButtonsColorWheel;
        public bool ShowHeaderButtonsColorWheel { get => _showHeaderButtonsColorWheel; set => Set(ref _showHeaderButtonsColorWheel, value); }

        public bool AnimatorShowKeyframeDialog { get => _animatorShowKeyframeDialog; set => Set(ref _animatorShowKeyframeDialog, value); }

        private bool _animatorColorWheel;
        public bool AnimatorColorWheel { get => _animatorColorWheel; set => Set(ref _animatorColorWheel, value); }

        private Color? _canvasBackgroundColor;
        public Color? CanvasBackgroundColor { get => _canvasBackgroundColor; set => Set(ref _canvasBackgroundColor, value); }

        private Brush? _canvasBackgroundBrush;
        public Brush? CanvasBackgroundBrush { get => _canvasBackgroundBrush; set => Set(ref _canvasBackgroundBrush, value); }

        public Dictionary<string, GradientConfig> AnimatorGradientPairs { get; } = new();
        public ObservableCollection<CanvasElement> AnimatorCanvasElements { get; } = new();

        private HashSet<string> _animatorSelectedElementIds = new();
        public HashSet<string> AnimatorSelectedElementIds { get => _animatorSelectedElementIds; set => Set(ref _animatorSelectedElementIds, value); }

        private string? _animatorSelectedElementId;
        public string? AnimatorSelectedElementId { get => _animatorSelectedElementId; set => Set(ref _animatorSelectedElementId, value); }

        private bool _animatorShowGroupDialog;
        public bool AnimatorShowGroupDialog { get => _animatorShowGroupDialog; set => Set(ref _animatorShowGroupDialog, value); }

        private string _animatorGroupName = "";
        public string AnimatorGroupName { get => _animatorGroupName; set => Set(ref _animatorGroupName, value); }

        public Dictionary<string, string> AnimatorGroupNames { get; } = new();

        private string? _animatorGroupToRenameId;
        public string? AnimatorGroupToRenameId { get => _animatorGroupToRenameId; set => Set(ref _animatorGroupToRenameId, value); }

        private string _animatorGroupRenameText = "";
        public string AnimatorGroupRenameText { get => _animatorGroupRenameText; set => Set(ref _animatorGroupRenameText, value); }

        private string? _animatorElementToRenameId;
        public string? AnimatorElementToRenameId { get => _animatorElementToRenameId; set => Set(ref _animatorElementToRenameId, value); }

        private string _animatorRenameText = "";
        public string AnimatorRenameText { get => _animatorRenameText; set => Set(ref _animatorRenameText, value); }

        private bool _animatorShowColorPicker;
        public bool AnimatorShowColorPicker { get => _animatorShowColorPicker; set => Set(ref _animatorShowColorPicker, value); }

        private string? _animatorElementToColorEditId;
        public string? AnimatorElementToColorEditId { get => _animatorElementToColorEditId; set => Set(ref _animatorElementToColorEditId, value); }

        private bool _animatorShowCustomPolygonDialog;
        public bool AnimatorShowCustomPolygonDialog { get => _animatorShowCustomPolygonDialog; set => Set(ref _animatorShowCustomPolygonDialog, value); }

        private string? _animatorPolygonElementToEditId;
        public string? AnimatorPolygonElementToEditId { get => _animatorPolygonElementToEditId; set => Set(ref _animatorPolygonElementToEditId, value); }

        private string _animatorInitialPolygonString = "";
        public string AnimatorInitialPolygonString { get => _animatorInitialPolygonString; set => Set(ref _animatorInitialPolygonString, value); }

        private bool _animatorInitialIsLineMode;
        public bool AnimatorInitialIsLineMode { get => _animatorInitialIsLineMode; set => Set(ref _animatorInitialIsLineMode, value); }

        private bool _animatorShowEditPropertiesDialog;
        public bool AnimatorShowEditPropertiesDialog { get => _animatorShowEditPropertiesDialog; set => Set(ref _animatorShowEditPropertiesDialog, value); }

        private string? _animatorEditPropertiesElementId;
        public string? AnimatorEditPropertiesElementId { get => _animatorEditPropertiesElementId; set => Set(ref _animatorEditPropertiesElementId, value); }

        private string _animatorEditX = "";
        public string AnimatorEditX { get => _animatorEditX; set => Set(ref _animatorEditX, value); }

        private string _animatorEditY = "";
        public string AnimatorEditY { get => _animatorEditY; set => Set(ref _animatorEditY, value); }

        private string _animatorEditScaleX = "";
        public string AnimatorEditScaleX { get => _animatorEditScaleX; set => Set(ref _animatorEditScaleX, value); }

        private string _animatorEditScaleY = "";
        public string AnimatorEditScaleY { get => _animatorEditScaleY; set => Set(ref _animatorEditScaleY, value); }

        private string _animatorEditRotation = "";
        public string AnimatorEditRotation { get => _animatorEditRotation; set => Set(ref _animatorEditRotation, value); }

        private Color _animatorEditColorForDialog = Colors.White;
        public Color AnimatorEditColorForDialog { get => _animatorEditColorForDialog; set => Set(ref _animatorEditColorForDialog, value); }

        private Color? _animatorEditShadowColorForDialog;
        public Color? AnimatorEditShadowColorForDialog { get => _animatorEditShadowColorForDialog; set => Set(ref _animatorEditShadowColorForDialog, value); }

        private float _animatorEditShadowOffsetX;
        public float AnimatorEditShadowOffsetX { get => _animatorEditShadowOffsetX; set => Set(ref _animatorEditShadowOffsetX, value); }

        private float _animatorEditShadowOffsetY;
        public float AnimatorEditShadowOffsetY { get => _animatorEditShadowOffsetY; set => Set(ref _animatorEditShadowOffsetY, value); }

        private float _animatorEditBorderThickness;
        public float AnimatorEditBorderThickness { get => _animatorEditBorderThickness; set => Set(ref _animatorEditBorderThickness, value); }

        private float _animatorEditPivotX = 0.5f;
        public float AnimatorEditPivotX { get => _animatorEditPivotX; set => Set(ref _animatorEditPivotX, value); }

        private float _animatorEditPivotY = 0.5f;
        public float AnimatorEditPivotY { get => _animatorEditPivotY; set => Set(ref _animatorEditPivotY, value); }

        private Color? _animatorEditBorderColorForDialog;
        public Color? AnimatorEditBorderColorForDialog { get => _animatorEditBorderColorForDialog; set => Set(ref _animatorEditBorderColorForDialog, value); }

        private string _animatorSelectedInputMode = "Add SVG";
        public string AnimatorSelectedInputMode { get => _animatorSelectedInputMode; set => Set(ref _animatorSelectedInputMode, value); }

        private AnimatorDialogType? _animatorDialogType;
        public AnimatorDialogType? AnimatorDialogType { get => _animatorDialogType; set => Set(ref _animatorDialogType, value); }

        private bool _isAnimatorFullScreen;
        public bool IsAnimatorFullScreen { get => _isAnimatorFullScreen; set => Set(ref _isAnimatorFullScreen, value); }

        private bool _proportionalEditing = true;
        public bool ProportionalEditing { get => _proportionalEditing; set => Set(ref _proportionalEditing, value); }

        private bool _showSaveMenu;
        public bool ShowSaveMenu { get => _showSaveMenu; set => Set(ref _showSaveMenu, value); }

        // ═══════════════════════════════════════════════════════════════
        //  Misc / global UI
        // ═══════════════════════════════════════════════════════════════
        private HashSet<string> _disabledVersions = new();
        public HashSet<string> DisabledVersions { get => _disabledVersions; set => Set(ref _disabledVersions, value); }

        private string _versionInfoForDialog = "";
        public string VersionInfoForDialog { get => _versionInfoForDialog; set => Set(ref _versionInfoForDialog, value); }

        private bool _showVersionInfoDialog;
        public bool ShowVersionInfoDialog { get => _showVersionInfoDialog; set => Set(ref _showVersionInfoDialog, value); }

        private string _selectedPrimaryDictLanguage = "English";
        public string SelectedPrimaryDictLanguage { get => _selectedPrimaryDictLanguage; set => Set(ref _selectedPrimaryDictLanguage, value); }

        private string _selectedSecondaryDictLanguage = "English";
        public string SelectedSecondaryDictLanguage { get => _selectedSecondaryDictLanguage; set => Set(ref _selectedSecondaryDictLanguage, value); }

        private string _selectedPrimaryDictionary = "atsbd";
        public string SelectedPrimaryDictionary { get => _selectedPrimaryDictionary; set => Set(ref _selectedPrimaryDictionary, value); }

        private string _selectedSecondaryDictionary = "cbtel";
        public string SelectedSecondaryDictionary { get => _selectedSecondaryDictionary; set => Set(ref _selectedSecondaryDictionary, value); }

        private Color _headerButtonsColor = Colors.White;
        public Color HeaderButtonsColor { get => _headerButtonsColor; set => Set(ref _headerButtonsColor, value); }

        private bool _showStrongs;
        public bool ShowStrongs { get => _showStrongs; set => Set(ref _showStrongs, value); }

        private bool _squareAspectViews = true;
        public bool SquareAspectViews { get => _squareAspectViews; set => Set(ref _squareAspectViews, value); }

        private bool _scrollSyncAction;
        public bool ScrollSyncAction { get => _scrollSyncAction; set => Set(ref _scrollSyncAction, value); }

        private Color _lightThemeReaderFontColor = Argb(0xFF101015);
        public Color LightThemeReaderFontColor { get => _lightThemeReaderFontColor; set => Set(ref _lightThemeReaderFontColor, value); }

        private Color _darkThemeReaderFontColor = Colors.White;
        public Color DarkThemeReaderFontColor { get => _darkThemeReaderFontColor; set => Set(ref _darkThemeReaderFontColor, value); }

        private bool _renderOrbs;
        public bool RenderOrbs { get => _renderOrbs; set => Set(ref _renderOrbs, value); }

        private int _orbsCount = 3;
        public int OrbsCount { get => _orbsCount; set => Set(ref _orbsCount, value); }

        private Color _wordsOfJesus = Argb(0xFFDA4227);
        public Color WordsOfJesus { get => _wordsOfJesus; set => Set(ref _wordsOfJesus, value); }

        private int _editingHighlightColorIndex = -1;
        public int EditingHighlightColorIndex { get => _editingHighlightColorIndex; set => Set(ref _editingHighlightColorIndex, value); }

        private bool _showHighlightColorEditor;
        public bool ShowHighlightColorEditor { get => _showHighlightColorEditor; set => Set(ref _showHighlightColorEditor, value); }

        private bool _isStudyMode;
        public bool IsStudyMode { get => _isStudyMode; set => Set(ref _isStudyMode, value); }

        private Color _wordMarkerColor = Argb(0xDDAC95E1);
        public Color WordMarkerColor { get => _wordMarkerColor; set => Set(ref _wordMarkerColor, value); }

        private bool _showWordMarkerColorWheelDialog;
        public bool ShowWordMarkerColorWheelDialog { get => _showWordMarkerColorWheelDialog; set => Set(ref _showWordMarkerColorWheelDialog, value); }

        private bool _showJesusWordsColorWheelDialog;
        public bool ShowJesusWordsColorWheelDialog { get => _showJesusWordsColorWheelDialog; set => Set(ref _showJesusWordsColorWheelDialog, value); }

        private bool _showLightReaderFontColorWheelDialog;
        public bool ShowLightReaderFontColorWheelDialog { get => _showLightReaderFontColorWheelDialog; set => Set(ref _showLightReaderFontColorWheelDialog, value); }

        private bool _showDarkReaderFontColorWheelDialog;
        public bool ShowDarkReaderFontColorWheelDialog { get => _showDarkReaderFontColorWheelDialog; set => Set(ref _showDarkReaderFontColorWheelDialog, value); }

        private bool _showDarkOverlayColorWheel;
        public bool ShowDarkOverlayColorWheel { get => _showDarkOverlayColorWheel; set => Set(ref _showDarkOverlayColorWheel, value); }

        private bool _showLightOverlayColorWheel;
        public bool ShowLightOverlayColorWheel { get => _showLightOverlayColorWheel; set => Set(ref _showLightOverlayColorWheel, value); }

        private bool _isDictionaryMode = true;
        public bool IsDictionaryMode
        {
            get => _isDictionaryMode;
            set
            {
                if (_isDictionaryMode == value) return;
                _isDictionaryMode = value;

                // Modes are mutually exclusive: turning dictionary on turns
                // highlight off, and vice versa. Subscribers to either property
                // see the correct final state because we notify after both
                // fields have been updated.
                if (value && _isHighlightMode)
                {
                    _isHighlightMode = false;
                    OnPropertyChanged(nameof(IsHighlightMode));
                }
                OnPropertyChanged();
            }
        }

        private bool _isHighlightMode;
        public bool IsHighlightMode
        {
            get => _isHighlightMode;
            set
            {
                if (_isHighlightMode == value) return;
                _isHighlightMode = value;

                if (value && _isDictionaryMode)
                {
                    _isDictionaryMode = false;
                    OnPropertyChanged(nameof(IsDictionaryMode));
                }
                OnPropertyChanged();
            }
        }

        private Color _verseMarkerColor = Argb(0xFF95F198);
        public Color VerseMarkerColor { get => _verseMarkerColor; set => Set(ref _verseMarkerColor, value); }

        private bool _showVerseMarkerColorWheelDialog;
        public bool ShowVerseMarkerColorWheelDialog { get => _showVerseMarkerColorWheelDialog; set => Set(ref _showVerseMarkerColorWheelDialog, value); }

        private bool _showSecondaryNavigationModal;
        public bool ShowSecondaryNavigationModal { get => _showSecondaryNavigationModal; set => Set(ref _showSecondaryNavigationModal, value); }

        private Color _lightModalBackgroundColor = Argb(0xFFEAE7E3);
        public Color LightModalBackgroundColor { get => _lightModalBackgroundColor; set => Set(ref _lightModalBackgroundColor, value); }

        private Color _darkModalBackgroundColor = Argb(0xFF121523);
        public Color DarkModalBackgroundColor { get => _darkModalBackgroundColor; set => Set(ref _darkModalBackgroundColor, value); }

        private int _fontSize = 18;
        public int FontSize { get => _fontSize; set => Set(ref _fontSize, value); }

        private int _customWidth = 1920;
        public int CustomWidth { get => _customWidth; set => Set(ref _customWidth, value); }

        private int _customHeight = 1080;
        public int CustomHeight { get => _customHeight; set => Set(ref _customHeight, value); }

        private bool _darkTheme;
        public bool DarkTheme { get => _darkTheme; set => Set(ref _darkTheme, value); }

        private Color? _selectedColor;
        public Color? SelectedColor { get => _selectedColor; set => Set(ref _selectedColor, value); }

        private bool _isCustomColor;
        public bool IsCustomColor { get => _isCustomColor; set => Set(ref _isCustomColor, value); }

        private Color? _customColor;
        public Color? CustomColor { get => _customColor; set => Set(ref _customColor, value); }

        private string _selectedFontFamily = "system";
        public string SelectedFontFamily { get => _selectedFontFamily; set => Set(ref _selectedFontFamily, value); }

        // ═══════════════════════════════════════════════════════════════
        //  Navigation
        // ═══════════════════════════════════════════════════════════════
        public ObservableCollection<Screen> NavigationStack { get; } = new() { new Screen.Home() };

        public void NavigateTo(Screen screen) => NavigationStack.Add(screen);

        public void GoBack()
        {
            if (NavigationStack.Count > 1)
                NavigationStack.RemoveAt(NavigationStack.Count - 1);
        }

        public void UpdateCurrentScreen(Screen newScreen)
        {
            if (NavigationStack.Count > 0)
                NavigationStack[NavigationStack.Count - 1] = newScreen;
        }

        // ═══════════════════════════════════════════════════════════════
        //  Databases / versions
        // ═══════════════════════════════════════════════════════════════
        private string _currentDbName = "kj2.sqlite3";
        public string CurrentDbName { get => _currentDbName; set => Set(ref _currentDbName, value); }

        private string _currentVersionAbbr = "KJV";
        public string CurrentVersionAbbr { get => _currentVersionAbbr; set => Set(ref _currentVersionAbbr, value); }

        private bool _multiVersion;
        public bool MultiVersion { get => _multiVersion; set => Set(ref _multiVersion, value); }

        private string _secondaryDbName = "kjv+.sqlite3";
        public string SecondaryDbName { get => _secondaryDbName; set => Set(ref _secondaryDbName, value); }

        private string _secondaryVersionAbbr = "KJV+";
        public string SecondaryVersionAbbr { get => _secondaryVersionAbbr; set => Set(ref _secondaryVersionAbbr, value); }

        private string _multiViewLayout = "horizontal";
        public string MultiViewLayout { get => _multiViewLayout; set => Set(ref _multiViewLayout, value); }

        private bool _scrollSync = true;
        public bool ScrollSync { get => _scrollSync; set => Set(ref _scrollSync, value); }

        private bool _isReaderFullScreen;
        public bool IsReaderFullScreen { get => _isReaderFullScreen; set => Set(ref _isReaderFullScreen, value); }

        private bool _showNavigationModal;
        public bool ShowNavigationModal { get => _showNavigationModal; set => Set(ref _showNavigationModal, value); }

        private bool _showPrimaryVersionDropdown;
        public bool ShowPrimaryVersionDropdown { get => _showPrimaryVersionDropdown; set => Set(ref _showPrimaryVersionDropdown, value); }

        private bool _showSecondaryVersionDropdown;
        public bool ShowSecondaryVersionDropdown { get => _showSecondaryVersionDropdown; set => Set(ref _showSecondaryVersionDropdown, value); }

        private bool _showColorThemeDialog;
        public bool ShowColorThemeDialog { get => _showColorThemeDialog; set => Set(ref _showColorThemeDialog, value); }

        private bool _showColorWheelDialog;
        public bool ShowColorWheelDialog { get => _showColorWheelDialog; set => Set(ref _showColorWheelDialog, value); }

        private bool _showBgModal;
        public bool ShowBgModal { get => _showBgModal; set => Set(ref _showBgModal, value); }

        // ═══════════════════════════════════════════════════════════════
        //  Passages
        // ═══════════════════════════════════════════════════════════════
        private PassageSelection _primaryPassage = new(10, "Genesis", 1, 1);
        public PassageSelection PrimaryPassage { get => _primaryPassage; set => Set(ref _primaryPassage, value); }

        private PassageSelection _secondaryPassage = new(500, "John", 1, 1);
        public PassageSelection SecondaryPassage { get => _secondaryPassage; set => Set(ref _secondaryPassage, value); }

        // ═══════════════════════════════════════════════════════════════
        //  Background
        // ═══════════════════════════════════════════════════════════════
        private int _bgImageIndex;
        public int BgImageIndex { get => _bgImageIndex; set => Set(ref _bgImageIndex, value); }

        private string? _customTextureUri;
        public string? CustomTextureUri { get => _customTextureUri; set => Set(ref _customTextureUri, value); }

        private float _overlayOpacity = 0.8f;
        public float OverlayOpacity { get => _overlayOpacity; set => Set(ref _overlayOpacity, value); }

        private Color _lightOverlayColor = Colors.White;
        public Color LightOverlayColor { get => _lightOverlayColor; set => Set(ref _lightOverlayColor, value); }

        private Color _darkOverlayColor = Argb(0xFF100F21);
        public Color DarkOverlayColor { get => _darkOverlayColor; set => Set(ref _darkOverlayColor, value); }

        private string _selectedVerseCommentary = "cbsc";
        public string SelectedVerseCommentary { get => _selectedVerseCommentary; set => Set(ref _selectedVerseCommentary, value); }

        private string _selectedCrossReferenceDatabase = "obx";
        public string SelectedCrossReferenceDatabase { get => _selectedCrossReferenceDatabase; set => Set(ref _selectedCrossReferenceDatabase, value); }

        private bool _isRefreshingDatabases;
        public bool IsRefreshingDatabases { get => _isRefreshingDatabases; set => Set(ref _isRefreshingDatabases, value); }

        private string _lastRefreshMessage = "";
        public string LastRefreshMessage { get => _lastRefreshMessage; set => Set(ref _lastRefreshMessage, value); }

        private bool _lastRefreshSuccess;
        public bool LastRefreshSuccess { get => _lastRefreshSuccess; set => Set(ref _lastRefreshSuccess, value); }

        private bool _showReaderOverlayColorWheel;
        public bool ShowReaderOverlayColorWheel { get => _showReaderOverlayColorWheel; set => Set(ref _showReaderOverlayColorWheel, value); }

        // ═══════════════════════════════════════════════════════════════
        //  Highlight colors
        // ═══════════════════════════════════════════════════════════════
        public ObservableCollection<Color> PredefinedHighlightColors { get; } = new();

        private static readonly Color[] DefaultHighlights =
        {
            Argb(0xFFFBEE4E), Argb(0xFF6EEB7E), Argb(0xFF4EC7EB),
            Argb(0xFF4E7BEB), Argb(0xFFAF4EEB), Argb(0xFFEB4E9E)
        };

        public void ResetHighlightColorsToDefault()
        {
            PredefinedHighlightColors.Clear();
            foreach (var c in DefaultHighlights) PredefinedHighlightColors.Add(c);
        }

        public void UpdateHighlightColor(int index, Color newColor)
        {
            if (index >= 0 && index < PredefinedHighlightColors.Count)
                PredefinedHighlightColors[index] = newColor;
        }

        // ═══════════════════════════════════════════════════════════════
        //  Old / new testament convenience
        // ═══════════════════════════════════════════════════════════════
        public bool IsOldTestament
            => BibleData.GetBookByCustomNumber(PrimaryPassage.BookNumber)?.Testament == Testament.Old;

        public bool IsSecondaryOldTestament
            => BibleData.GetBookByCustomNumber(SecondaryPassage.BookNumber)?.Testament == Testament.Old;

        // ═══════════════════════════════════════════════════════════════
        //  Constructor
        // ═══════════════════════════════════════════════════════════════
        public AppViewModel()
        {
            ResetHighlightColorsToDefault();
            LoadAssets();
        }

        private void LoadAssets()
        {
            Textures.Clear();
            ImageFilesSm.Clear();
            ImageFilesMd.Clear();

            Textures.AddRange(ScanFolder("Assets/textures"));
            ImageFilesSm.AddRange(ScanFolder("Assets/images"));
            ImageFilesMd.AddRange(ScanFolder("Assets/images-md"));
        }

        private static IEnumerable<string> ScanFolder(string relativePath)
        {
            var full = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
            if (!Directory.Exists(full)) return Enumerable.Empty<string>();

            return Directory.EnumerateFiles(full)
                .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
                .Select(Path.GetFileName)
                .Where(n => n != null)
                .Cast<string>()
                .ToList();
        }

        // ═══════════════════════════════════════════════════════════════
        //  Dictionary language helpers
        // ═══════════════════════════════════════════════════════════════
        public void UpdateDictionaryForBibleLanguage(string bibleFile)
        {
            var lang = BibleVersionUtils.GetLanguageForVersion(bibleFile);
            SelectedPrimaryDictLanguage = lang;
            SelectedPrimaryDictionary = InteractiveModalUtils.DictionariesByLanguage
                .GetValueOrDefault(lang)?.FirstOrDefault() ?? "";
        }

        public void UpdateSecondaryDictionaryForBibleLanguage(string bibleFile)
        {
            var lang = BibleVersionUtils.GetLanguageForVersion(bibleFile);
            SelectedSecondaryDictLanguage = lang;
            SelectedSecondaryDictionary = InteractiveModalUtils.DictionariesByLanguage
                .GetValueOrDefault(lang)?.FirstOrDefault() ?? "";
        }

        // ═══════════════════════════════════════════════════════════════
        //  Database refresh
        // ═══════════════════════════════════════════════════════════════
        public async Task RefreshDatabasesAsync(string assetsRoot)
        {
            IsRefreshingDatabases = true;
            LastRefreshMessage = "Starting database refresh...";
            LastRefreshSuccess = false;

            try
            {
                var dirs = new[] { "databases", "dictionaries", "commentaries",
                                   "cross-references", "subheadings", "topical" };
                var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var dir in dirs)
                {
                    var full = Path.Combine(assetsRoot, dir);
                    if (!Directory.Exists(full)) continue;
                    foreach (var file in Directory.EnumerateFiles(full, "*.sqlite3"))
                        found.Add(Path.GetFileName(file));
                }

                if (!string.IsNullOrEmpty(CurrentDbName)) found.Add(CurrentDbName);
                if (!string.IsNullOrEmpty(SecondaryDbName)) found.Add(SecondaryDbName);

                var list = found.ToList();
                var success = 0;
                for (int i = 0; i < list.Count; i++)
                {
                    LastRefreshMessage = $"Refreshing database {i + 1}/{list.Count}: {list[i]}";
                    await Task.Run(() =>
                    {
                        using var helper = new Services.DatabaseHelper(list[i]);
                        if (helper.RefreshDatabase()) success++;
                    });
                }

                if (success == list.Count)
                {
                    LastRefreshMessage = $"✅ Successfully refreshed all {list.Count} databases!";
                    LastRefreshSuccess = true;
                }
                else
                {
                    LastRefreshMessage = $"⚠ Refreshed {success} out of {list.Count} databases. Some may have failed.";
                    LastRefreshSuccess = false;
                }
            }
            catch (Exception e)
            {
                LastRefreshMessage = $"❌ Error refreshing databases: {e.Message}";
                LastRefreshSuccess = false;
            }
            finally
            {
                IsRefreshingDatabases = false;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  Animator element manipulation
        // ═══════════════════════════════════════════════════════════════
        public void UpdateAnimatorElementKeyframes(string noteId, List<CanvasKeyframe> keyframes)
            => UpdateAnimatorElement(noteId, e => e with { Keyframes = keyframes });

        public void UpdateAnimatorElement(string id, Func<CanvasElement, CanvasElement> transform)
        {
            var idx = AnimatorCanvasElements.ToList().FindIndex(e => e.Id == id);
            if (idx >= 0) AnimatorCanvasElements[idx] = transform(AnimatorCanvasElements[idx]);
        }

        public void AddToAnimatorCanvas(CanvasElement note)
            => AnimatorCanvasElements.Add(note with { Id = Guid.NewGuid().ToString() });

        public void RemoveFromAnimatorCanvas(int index)
        {
            if (index >= 0 && index < AnimatorCanvasElements.Count)
                AnimatorCanvasElements.RemoveAt(index);
        }

        public void UpdateAnimatorElementColor(string id, Color color)
            => UpdateAnimatorElement(id, e => e with { BackgroundColor = color });

        public void UpdateAnimatorElementTextColor(string noteId, Color color)
            => UpdateAnimatorElement(noteId, e => e with { TextColor = color });

        public void UpdateAnimatorElementProperties(string id, float x, float y, float width, float height, float rotation)
            => UpdateAnimatorElement(id, e => e with
            {
                Offset = new Point(x, y),
                Width = width,
                Height = height,
                Rotation = rotation
            });

        public void UpdateAnimatorElementScale(string id, float scaleX, float scaleY)
            => UpdateAnimatorElement(id, e => e with { ScaleX = scaleX, ScaleY = scaleY });

        public void UpdateAnimatorElementContent(string id, string newContent)
            => UpdateAnimatorElement(id, e => e with { Content = newContent });

        public void ToggleAnimatorVisibility(string noteId)
            => UpdateAnimatorElement(noteId, e => e with { IsVisible = !e.IsVisible });

        public void ToggleAnimatorLock(string noteId)
            => UpdateAnimatorElement(noteId, e => e with { IsLocked = !e.IsLocked });

        public void RenameAnimatorCanvasElement(string noteId, string newName)
            => UpdateAnimatorElement(noteId, e => e with { CustomName = newName.Trim() });

        public void RenameAnimatorGroup(string groupId, string newName)
            => AnimatorGroupNames[groupId] = newName;

        public void CreateAnimatorGroup(IEnumerable<string> noteIds)
        {
            var ids = noteIds.ToHashSet();
            if (ids.Count < 2) return;
            var groupId = $"vid_group_{Guid.NewGuid().ToString()[..8]}";

            for (int i = 0; i < AnimatorCanvasElements.Count; i++)
            {
                if (ids.Contains(AnimatorCanvasElements[i].Id))
                    AnimatorCanvasElements[i] = AnimatorCanvasElements[i] with { GroupId = groupId };
            }
        }

        public void UngroupAnimatorElements(IEnumerable<string> noteIds)
        {
            var ids = noteIds.ToHashSet();
            for (int i = 0; i < AnimatorCanvasElements.Count; i++)
            {
                if (ids.Contains(AnimatorCanvasElements[i].Id))
                    AnimatorCanvasElements[i] = AnimatorCanvasElements[i] with { GroupId = null };
            }
        }

        public void ReorderAnimatorCanvasElements(int from, int to)
        {
            if (from == to) return;
            if (from < 0 || from >= AnimatorCanvasElements.Count) return;
            if (to < 0 || to >= AnimatorCanvasElements.Count) return;

            var item = AnimatorCanvasElements[from];
            AnimatorCanvasElements.RemoveAt(from);
            AnimatorCanvasElements.Insert(to, item);
        }

        public void UpdateAnimatorElementDuration(string id, long startMs, long endMs)
            => UpdateAnimatorElement(id, e => e with { StartTimeMs = startMs, EndTimeMs = endMs });

        public void UpdateAnimatorElementPivot(string id, float newPivotX, float newPivotY)
        {
            UpdateAnimatorElement(id, element =>
            {
                var newOffset = CanvasElementHelpers.OffsetForPivotChange(
                    element,
                    element.PivotX, element.PivotY,
                    newPivotX, newPivotY);
                return element with { PivotX = newPivotX, PivotY = newPivotY, Offset = newOffset };
            });
        }
    }
}