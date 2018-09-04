using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FolderMusic
{
    public delegate void UpdateFinishedEventHandler<T>(IUpdateSellectedItemCollection<T> sender);

    public interface IUpdateSellectedItemCollection<T> : IEnumerable<T>, INotifyCollectionChanged
    {
        event UpdateFinishedEventHandler<T> UpdateFinished;
    }
}
