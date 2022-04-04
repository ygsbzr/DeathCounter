using System.ComponentModel;

namespace DeathCounter
{
    public class GlobalSettings
    {
        private bool _showDeathCounter = true;
        private bool _showHitCounter = true;
        private int Position = 0;

        public bool ShowDeathCounter
        {
            get => _showDeathCounter; 
            set
            {
                if (_showDeathCounter == value)
                    return;
                _showDeathCounter = value;
                OnPropertyChanged(nameof(ShowDeathCounter));
            }
        }

        public bool ShowHitCounter
        {
            get => _showHitCounter; 
            set
            {
                if (_showHitCounter == value)
                    return;
                _showHitCounter = value;
                OnPropertyChanged(nameof(ShowHitCounter));
            }
        }

        public bool AboveMasks
        {
            get => Position == 0;
            set
            {
                SetPosition(value, 0);
            }
        }

        public bool BesideGeoCount
        {
            get => Position == 1;
            set
            {
                SetPosition(value, 1);
            }
        }

        public bool UnderGeoCount
        {
            get => Position == 2;
            set
            {
                SetPosition(value, 2);
            }
        }

        public bool BesideEssenceCount
        {
            get => Position == 3;
            set
            {
                SetPosition(value, 3);
            }
        }

        public bool UnderEssenceCount
        {
            get => Position == 4;
            set
            {
                SetPosition(value, 4);
            }
        }

        public bool OnLeftEdge
        {
            get => Position == 5;
            set
            {
                SetPosition(value, 5);
            }
        }

        private void SetPosition(bool value, int position)
        {
            if (value && Position == position)
                return;
            if (value)
            {
                Position = position;
                OnPropertyChanged(nameof(Position));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
