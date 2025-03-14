﻿using System;
using WpfApp1.ViewModels;

namespace WpfApp1.Stores
{
    /// <summary>
    /// Classe che conserva l'attuale vinewmodel per l'app
    /// </summary>
    public class NavigationStore
    {
        private ViewModelBase _currentViewModel;

        public ViewModelBase CurrentViewModel
        {
            get => _currentViewModel;
            set
            {
                _currentViewModel = value;
                OnCurrentViewModelChanged();
            }
        }

        public event Action CurrentViewModelChanged;

        private void OnCurrentViewModelChanged()
        {
            CurrentViewModelChanged?.Invoke();
        }
    }
}
