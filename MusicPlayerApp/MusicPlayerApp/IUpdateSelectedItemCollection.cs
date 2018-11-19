using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FolderMusic
{
    public interface IUpdateSelectedItemCollection<T> : IEnumerable<T>, INotifyCollectionChanged
    {
        event EventHandler UpdateFinished;
    }
}
