using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace WpfApp1.DragAndDrop
{
    public class ProcessDropEventArgs<ItemType> : EventArgs
        where ItemType : class
    {
        #region Constructor
        internal ProcessDropEventArgs(
            ObservableCollection<ItemType> itemsSource,
            ItemType dataItem,
            int oldIndex,
            int newIndex,
            DragDropEffects allowedEffects)
        {
            this.ItemsSource = itemsSource;
            this.DataItem = dataItem;
            this.OldIndex = oldIndex;
            this.NewIndex = newIndex;
            this.AllowedEffects = allowedEffects;
        }
        #endregion // Constructor

        #region Public Properties
        /// <summary>
        ///     The items source of the ListView where the drop occurred.
        /// </summary>
        public ObservableCollection<ItemType> ItemsSource { get; }

        /// <summary>
        ///     The data object which was dropped.
        /// </summary>
        public ItemType DataItem { get; }

        /// <summary>
        ///     The current index of the data item being dropped, in the ItemsSource collection.
        /// </summary>
        public int OldIndex { get; }

        /// <summary>
        ///     The target index of the data item being dropped, in the ItemsSource collection.
        /// </summary>
        public int NewIndex { get; }

        /// <summary>
        ///     The drag drop effects allowed to be performed.
        /// </summary>
        public DragDropEffects AllowedEffects { get; } = DragDropEffects.None;

        /// <summary>
        ///     The drag drop effect(s) performed on the dropped item.
        /// </summary>
        public DragDropEffects Effects { get; set; } = DragDropEffects.None;
        #endregion // Public Properties
    }
}
