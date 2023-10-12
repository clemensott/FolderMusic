using System.ComponentModel;

namespace MusicPlayer.UpdateLibrary
{
    public abstract class BaseUpdateProgress : INotifyPropertyChanged
    {
        private int percent, currentCount, totalCount;
        private string currentStepName;

        public int Percent
        {
            get { return percent; }

            protected set
            {
                if (value == percent) return;

                percent = value;
                OnPropertyChanged(nameof(Percent));
            }
        }

        public int CurrentCount
        {
            get { return currentCount; }
            protected set
            {
                if (value == currentCount) return;

                currentCount = value;
                OnPropertyChanged(nameof(CurrentCount));

                Percent = TotalCount != 0 ? (CurrentCount * 100) / TotalCount : 0;
            }
        }

        public int TotalCount
        {
            get { return totalCount; }
            set
            {
                if (value == totalCount) return;

                totalCount = value;
                OnPropertyChanged(nameof(TotalCount));

                Percent = TotalCount != 0 ? CurrentCount / (TotalCount * 100) : 0;
            }
        }

        public string CurrentStepName
        {
            get { return currentStepName; }
            set
            {
                if (value == currentStepName) return;

                currentStepName = value;
                OnPropertyChanged(nameof(CurrentStepName));
            }
        }

        public CancelOperationToken CancelToken { get; }

        protected BaseUpdateProgress(CancelOperationToken token)
        {
            CancelToken = token;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
