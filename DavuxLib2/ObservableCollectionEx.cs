using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Threading;
using System.ComponentModel;

namespace DavuxLib2
{
    public class ObservableCollectionEx<T> : ObservableCollection<T>
    {
        public void Sort()
        {
            this.Sort(0, Count, null);
        }

        public void Sort(IComparer<T> comparer)
        {
            this.Sort(0, Count, comparer);
        }

        public void Sort(int index, int count, IComparer<T> comparer)
        {
            (Items as List<T>).Sort(index, count, comparer);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void AddRange(T[] items)
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }

        public void AddRange(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }

        public ObservableCollectionEx()
        {
            _currentDispatcher = Dispatcher.CurrentDispatcher;
        }

        public ObservableCollectionEx(System.Collections.Generic.IEnumerable<T> collection)
            : base(collection)
        {
            _currentDispatcher = Dispatcher.CurrentDispatcher;
        }

        private readonly Dispatcher _currentDispatcher;

        private void DoDispatchedAction(Action action)
        {
            if (_currentDispatcher.CheckAccess())
                action.Invoke();
            else
                _currentDispatcher.Invoke(DispatcherPriority.DataBind, action);
        }

        protected override void ClearItems()
        {
            DoDispatchedAction(BaseClearItems);
        }

        private void BaseClearItems()
        {
            base.ClearItems();
        }

        protected override void InsertItem(int index, T item)
        {
            DoDispatchedAction(() => BaseInsertItem(index, item));
        }

        private void BaseInsertItem(int index, T item)
        {
            base.InsertItem(index, item);
        }

        protected override void MoveItem(int oldIndex, int newIndex)
        {
            DoDispatchedAction(() => BaseMoveItem(oldIndex, newIndex));
        }

        private void BaseMoveItem(int oldIndex, int newIndex)
        {
            base.MoveItem(oldIndex, newIndex);
        }

        protected override void RemoveItem(int index)
        {
            DoDispatchedAction(() => BaseRemoveItem(index));
        }

        private void BaseRemoveItem(int index)
        {
            base.RemoveItem(index);
        }

        protected override void SetItem(int index, T item)
        {
            DoDispatchedAction(() => BaseSetItem(index, item));
        }

        private void BaseSetItem(int index, T item)
        {
            base.SetItem(index, item);
        }

        protected override void OnCollectionChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            DoDispatchedAction(() => BaseOnCollectionChanged(e));
        }

        private void BaseOnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            DoDispatchedAction(() => base.OnCollectionChanged(e));
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            DoDispatchedAction(() => BaseOnPropertyChanged(e));
        }

        private void BaseOnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
        }
    }
}
