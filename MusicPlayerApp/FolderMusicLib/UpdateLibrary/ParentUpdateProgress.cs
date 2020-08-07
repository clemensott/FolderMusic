using System.ComponentModel;

namespace MusicPlayer.UpdateLibrary
{
    public class ParentUpdateProgress : BaseUpdateProgress
    {
        private ChildUpdateProgress child;

        public ChildUpdateProgress Child
        {
            get { return child; }
            private set
            {
                if (value == child) return;

                if (child != null) child.PropertyChanged -= Child_PropertyChanged;
                child = value;
                if (child != null) child.PropertyChanged += Child_PropertyChanged;

                OnPropertyChanged(nameof(Child));
            }
        }

        private void Child_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Child.Percent) && TotalCount != 0)
            {
                Percent = (int)((CurrentCount * 100 + Child.Percent) / TotalCount);
            }
        }

        public ParentUpdateProgress(CancelOperationToken token) : base(token)
        {
        }

        public ChildUpdateProgress Next()
        {
            if (Child != null) CurrentCount++;
            return Child = new ChildUpdateProgress(CancelToken.CreateChild());
        }

        public void FinishChildren()
        {
            CurrentCount = TotalCount;
            Child = null;
        }
    }
}
