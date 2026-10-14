// Helpers/RandomImageSelector.cs
using System;
using FountOfHopeStudyBiblePC.ViewModels;

namespace FountOfHopeStudyBiblePC.Helpers
{
    public class RandomImageSelector
    {
        private readonly AppViewModel _viewModel;
        private readonly Random _rng = new();

        public RandomImageSelector(AppViewModel viewModel) => _viewModel = viewModel;

        public string GetRandomImage(bool isLandscape)
        {
            var assetPath = isLandscape ? "images-md/" : "images/";
            var fileList = isLandscape ? _viewModel.ImageFilesMd : _viewModel.ImageFilesSm;

            if (fileList.Count == 0) return string.Empty;

            var randomFile = fileList[_rng.Next(fileList.Count)];
            return $"pack://siteoforigin:,,,/Assets/{assetPath}{randomFile}";
        }
    }
}