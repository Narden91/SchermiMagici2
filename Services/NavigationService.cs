﻿using System;
using WpfApp1.Stores;
using WpfApp1.ViewModels;

namespace WpfApp1.Services
{
    /// <summary>
    /// Abstract class for managing navigation through the application
    /// </summary>
    public class NavigationService
    {
        private readonly NavigationStore _navigationStore;
        private readonly Func<ViewModelBase> _createViewModel;

        public NavigationService(NavigationStore navigationStore, Func<ViewModelBase> createViewModel)
        {
            _navigationStore = navigationStore;
            _createViewModel = createViewModel;
        }

        public void Navigate()
        {
            _navigationStore.CurrentViewModel = _createViewModel();
        }
    }
}
