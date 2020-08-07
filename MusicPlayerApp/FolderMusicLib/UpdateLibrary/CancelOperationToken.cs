using MusicPlayer.UpdateLibrary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace MusicPlayer
{
    public class CancelOperationToken : INotifyPropertyChanged
    {
        private readonly ICollection<CancelOperationToken> children;

        public event EventHandler<CancelTokenResult> Finished;

        public bool IsCanceled { get; private set; }

        public bool IsCompleted { get; private set; }

        public CancelTokenResult? Result { get; private set; }

        public CancelOperationToken()
        {
            children = new List<CancelOperationToken>();
            IsCanceled = false;
            IsCompleted = false;
            Result = null;
        }

        public CancelOperationToken CreateChild()
        {
            CancelOperationToken child = new CancelOperationToken();
            children.Add(child);
            return child;
        }

        public void Cancel()
        {
            Finish(CancelTokenResult.Canceled);
        }

        public void Complete()
        {
            Finish(CancelTokenResult.Completed);
        }

        public void Finish(CancelTokenResult result)
        {
            if (Result.HasValue) return;

            IsCanceled = result == CancelTokenResult.Canceled;
            IsCompleted = result == CancelTokenResult.Completed;
            Result = result;

            Finished?.Invoke(this, result);

            OnPropertyChanged(nameof(Result));
            OnPropertyChanged(nameof(IsCanceled));
            OnPropertyChanged(nameof(IsCompleted));

            foreach (CancelOperationToken child in children)
            {
                child.Finish(result);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
