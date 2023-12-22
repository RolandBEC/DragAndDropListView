using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using WpfApp1.UI;

namespace WpfApp1.DragAndDrop
{
    /// <summary>
    ///     Manages the dragging and dropping of ListViewItems in a ListView.
    ///     The ItemType type parameter indicates the type of the objects in
    ///     the ListView's items source.  The ListView's ItemsSource must be
    ///     set to an instance of ObservableCollection of ItemType, or an
    ///     Exception will be thrown.
    /// </summary>
    /// <typeparam name="ItemType">The type of the ListView's items.</typeparam>
    public class ListViewDragDropManager<ItemType>
        where ItemType : class
    {
        #region Constants, Fields, Properties, Indexers

        #region Fields, Constants
        bool canInitiateDrag;
        DragAdorner dragAdorner;
        double dragAdornerOpacity;
        int indexToSelect;
        ItemType itemUnderDragCursor;
        ListView listView;
        Point ptMouseDown;
        bool showDragAdorner;
        #endregion

        #region Properties, Indexers
        /// <summary>
        ///     Gets/sets the opacity of the drag adorner.  This property has no
        ///     effect if ShowDragAdorner is false. The default value is 0.7
        /// </summary>
        public double DragAdornerOpacity
        {
            get
            {
                return this.dragAdornerOpacity;
            }
            set
            {
                if (this.IsDragInProgress)
                {
                    throw new InvalidOperationException(
                        "Cannot set the DragAdornerOpacity property during a drag operation.");
                }

                if (value < 0.0 || value > 1.0)
                {
                    throw new ArgumentOutOfRangeException("DragAdornerOpacity", value, "Must be between 0 and 1.");
                }

                this.dragAdornerOpacity = value;
            }
        }

        /// <summary>
        ///     Gets/sets the ListView whose dragging is managed.  This property
        ///     can be set to null, to prevent drag management from occuring.  If
        ///     the ListView's AllowDrop property is false, it will be set to true.
        /// </summary>
        public ListView ListView
        {
            get
            {
                return this.listView;
            }
            set
            {
                if (this.IsDragInProgress)
                {
                    throw new InvalidOperationException("Cannot set the ListView property during a drag operation.");
                }

                if (this.listView != null)
                {
                    #region Unhook Events
                    this.listView.PreviewMouseLeftButtonDown -= this.ListView_PreviewMouseLeftButtonDown;
                    this.listView.PreviewMouseMove -= this.ListView_PreviewMouseMove;
                    this.listView.DragOver -= this.ListView_DragOver;
                    this.listView.DragLeave -= this.ListView_DragLeave;
                    this.listView.DragEnter -= this.ListView_DragEnter;
                    this.listView.Drop -= this.ListView_Drop;
                    #endregion // Unhook Events
                }

                this.listView = value;

                if (this.listView != null)
                {
                    if (!this.listView.AllowDrop)
                    {
                        this.listView.AllowDrop = true;
                    }

                    #region Hook Events
                    this.listView.PreviewMouseLeftButtonDown += this.ListView_PreviewMouseLeftButtonDown;
                    this.listView.PreviewMouseMove += this.ListView_PreviewMouseMove;
                    this.listView.DragOver += this.ListView_DragOver;
                    this.listView.DragLeave += this.ListView_DragLeave;
                    this.listView.DragEnter += this.ListView_DragEnter;
                    this.listView.Drop += this.ListView_Drop;
                    #endregion // Hook Events
                }
            }
        }

        /// <summary>
        ///     Gets/sets whether a visual representation of the ListViewItem being dragged
        ///     follows the mouse cursor during a drag operation.  The default value is true.
        /// </summary>
        public bool ShowDragAdorner
        {
            get
            {
                return this.showDragAdorner;
            }
            set
            {
                if (this.IsDragInProgress)
                {
                    throw new InvalidOperationException(
                        "Cannot set the ShowDragAdorner property during a drag operation.");
                }

                this.showDragAdorner = value;
            }
        }

        bool CanStartDragOperation
        {
            get
            {
                if (Mouse.LeftButton != MouseButtonState.Pressed)
                {
                    return false;
                }

                if (!this.canInitiateDrag)
                {
                    return false;
                }

                if (this.indexToSelect == -1)
                {
                    return false;
                }

                if (false.Equals(this.DraggingPrecondition?.Invoke(this.ptMouseDown)))
                {
                    return false;
                }

                return this.HasCursorLeftDragThreshold;
            }
        }

        bool HasCursorLeftDragThreshold
        {
            get
            {
                if (this.indexToSelect < 0)
                {
                    return false;
                }

                ListViewItem item = this.GetListViewItem(this.indexToSelect);
                Rect bounds = VisualTreeHelper.GetDescendantBounds(item);
                Point ptInItem = this.listView.TranslatePoint(this.ptMouseDown, item);

                // In case the cursor is at the very top or bottom of the ListViewItem
                // we want to make the vertical threshold very small so that dragging
                // over an adjacent item does not select it.
                double topOffset = Math.Abs(ptInItem.Y);
                double btmOffset = Math.Abs(bounds.Height - ptInItem.Y);
                double vertOffset = Math.Min(topOffset, btmOffset);

                double width = (this.MinimumHorizontalDragDistance ?? SystemParameters.MinimumHorizontalDragDistance)
                               * 2;
                double height =
                    Math.Min(
                        this.MinimumVerticalDragDistance ?? SystemParameters.MinimumVerticalDragDistance,
                        vertOffset) * 2;
                Size szThreshold = new Size(width, height);

                Rect rect = new Rect(this.ptMouseDown, szThreshold);
                rect.Offset(szThreshold.Width / -2, szThreshold.Height / -2);
                Point ptInListView = MouseUtilities.GetMousePosition(this.listView);
                return !rect.Contains(ptInListView);
            }
        }

        /// <summary>
        ///     Returns the index of the ListViewItem underneath the
        ///     drag cursor, or -1 if the cursor is not over an item.
        /// </summary>
        int IndexUnderDragCursor
        {
            get
            {
                int index = -1;
                for (int i = 0; i < this.listView.Items.Count; ++i)
                {
                    ListViewItem item = this.GetListViewItem(i);
                    if (item == null)
                    {
                        continue;
                    }
                    if (this.IsMouseOver(item))
                    {
                        index = i;
                        break;
                    }
                }
                return index;
            }
        }

        /// <summary>
        ///     Returns true if the mouse cursor is over a scrollbar in the ListView.
        /// </summary>
        bool IsMouseOverScrollbar
        {
            get
            {
                Point ptMouse = MouseUtilities.GetMousePosition(this.listView);
                HitTestResult res = VisualTreeHelper.HitTest(this.listView, ptMouse);
                if (res == null)
                {
                    return false;
                }

                DependencyObject depObj = res.VisualHit;
                while (depObj != null)
                {
                    if (depObj is ScrollBar)
                    {
                        return true;
                    }

                    // VisualTreeHelper works with objects of type Visual or Visual3D.
                    // If the current object is not derived from Visual or Visual3D,
                    // then use the LogicalTreeHelper to find the parent element.
                    if (depObj is Visual || depObj is Visual3D)
                    {
                        depObj = VisualTreeHelper.GetParent(depObj);
                    }
                    else
                    {
                        depObj = LogicalTreeHelper.GetParent(depObj);
                    }
                }

                return false;
            }
        }

        ItemType ItemUnderDragCursor
        {
            get
            {
                return this.itemUnderDragCursor;
            }
            set
            {
                if (this.itemUnderDragCursor == value)
                {
                    return;
                }

                // The first pass handles the previous item under the cursor.
                // The second pass handles the new one.
                for (int i = 0; i < 2; ++i)
                {
                    if (i == 1)
                    {
                        this.itemUnderDragCursor = value;
                    }

                    if (this.itemUnderDragCursor != null)
                    {
                        ListViewItem listViewItem = this.GetListViewItem(this.itemUnderDragCursor);
                        if (listViewItem != null)
                        {
                            ListViewItemDragState.SetIsUnderDragCursor(listViewItem, i == 1);
                        }
                    }
                }
            }
        }

        bool ShowDragAdornerResolved
        {
            get
            {
                return this.ShowDragAdorner && this.DragAdornerOpacity > 0.0;
            }
        }
        #endregion

        #endregion

        /// <summary>
        ///     Initializes a new instance of ListViewDragManager.
        /// </summary>
        public ListViewDragDropManager()
        {
            this.canInitiateDrag = false;
            this.dragAdornerOpacity = 0.7;
            this.indexToSelect = -1;
            this.showDragAdorner = true;
        }

        /// <summary>
        ///     Initializes a new instance of ListViewDragManager.
        /// </summary>
        /// <param name="listView"></param>
        public ListViewDragDropManager(ListView listView)
            : this()
        {
            this.ListView = listView;
        }

        /// <summary>
        ///     Initializes a new instance of ListViewDragManager.
        /// </summary>
        /// <param name="listView"></param>
        /// <param name="dragAdornerOpacity"></param>
        public ListViewDragDropManager(ListView listView, double dragAdornerOpacity)
            : this(listView)
        {
            this.DragAdornerOpacity = dragAdornerOpacity;
        }

        /// <summary>
        ///     Initializes a new instance of ListViewDragManager.
        /// </summary>
        /// <param name="listView"></param>
        /// <param name="showDragAdorner"></param>
        public ListViewDragDropManager(ListView listView, bool showDragAdorner)
            : this(listView)
        {
            this.ShowDragAdorner = showDragAdorner;
        }

        #region Methods
        private void FinishDragOperation(ListViewItem draggedItem, AdornerLayer adornerLayer)
        {
            // Let the ListViewItem know that it is not being dragged anymore.
            ListViewItemDragState.SetIsBeingDragged(draggedItem, false);

            this.IsDragInProgress = false;

            if (this.ItemUnderDragCursor != null)
            {
                this.ItemUnderDragCursor = null;
            }

            // Remove the drag adorner from the adorner layer.
            if (adornerLayer != null)
            {
                adornerLayer.Remove(this.dragAdorner);
                this.dragAdorner = null;
            }
        }

        private ListViewItem GetListViewItem(int index)
        {
            if (this.listView.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
            {
                return null;
            }

            return this.listView.ItemContainerGenerator.ContainerFromIndex(index) as ListViewItem;
        }

        private ListViewItem GetListViewItem(ItemType dataItem)
        {
            if (this.listView.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
            {
                return null;
            }

            return this.listView.ItemContainerGenerator.ContainerFromItem(dataItem) as ListViewItem;
        }

        private AdornerLayer InitializeAdornerLayer(ListViewItem itemToDrag)
        {
            // Create a brush which will paint the ListViewItem onto
            // a visual in the adorner layer.
            VisualBrush brush = new VisualBrush(itemToDrag);

            // Create an element which displays the source item while it is dragged.
            this.dragAdorner = new DragAdorner(this.listView, itemToDrag.RenderSize, brush);

            // Set the drag adorner's opacity.
            this.dragAdorner.Opacity = this.DragAdornerOpacity;

            AdornerLayer layer = AdornerLayer.GetAdornerLayer(this.listView);
            layer.Add(this.dragAdorner);

            // Save the location of the cursor when the left mouse button was pressed.
            this.ptMouseDown = MouseUtilities.GetMousePosition(this.listView);

            return layer;
        }

        private void InitializeDragOperation(ListViewItem itemToDrag)
        {
            // Set some flags used during the drag operation.
            this.IsDragInProgress = true;
            this.canInitiateDrag = false;

            // Let the ListViewItem know that it is being dragged.
            ListViewItemDragState.SetIsBeingDragged(itemToDrag, true);
        }

        private bool IsMouseOver(Visual target)
        {
            // We need to use MouseUtilities to figure out the cursor
            // coordinates because, during a drag-drop operation, the WPF
            // mechanisms for getting the coordinates behave strangely.

            Rect bounds = VisualTreeHelper.GetDescendantBounds(target);
            Point mousePos = MouseUtilities.GetMousePosition(target);
            return bounds.Contains(mousePos);
        }

        private void PerformDragOperation()
        {
            ItemType selectedItem = this.listView.SelectedItem as ItemType;
            DragDropEffects allowedEffects = DragDropEffects.Move | DragDropEffects.Move | DragDropEffects.Link;
            if (DragDrop.DoDragDrop(this.listView, selectedItem, allowedEffects) != DragDropEffects.None)
            {
                // The item was dropped into a new location,
                // so make it the new selected item.
                this.listView.SelectedItem = selectedItem;
            }
        }

        private void UpdateDragAdornerLocation()
        {
            if (this.dragAdorner != null)
            {
                Point ptCursor = MouseUtilities.GetMousePosition(this.ListView);

                double left = ptCursor.X - this.ptMouseDown.X;

                // 4/13/2007 - Made the top offset relative to the item being dragged.
                ListViewItem itemBeingDragged = this.GetListViewItem(this.indexToSelect);
                Point itemLoc = itemBeingDragged.TranslatePoint(new Point(0, 0), this.ListView);
                double top = itemLoc.Y + ptCursor.Y - this.ptMouseDown.Y;

                this.dragAdorner.SetOffsets(left, top);
            }
        }
        #endregion

        /// <summary>
        ///     Raised when a drop occurs.  By default the dropped item will be moved
        ///     to the target index.  Handle this event if relocating the dropped item
        ///     requires custom behavior.  Note, if this event is handled the default
        ///     item dropping logic will not occur.
        /// </summary>
        public event EventHandler<ProcessDropEventArgs<ItemType>> ProcessDrop;

        #region IsDragInProgress
        /// <summary>
        ///     Returns true if there is currently a drag operation being managed.
        /// </summary>
        public bool IsDragInProgress { get; private set; }

        /// <summary>
        ///     Gets or sets optional minimal distance mouse pointer has to be dragged horizontally before it is considered a drag
        ///     operation.
        ///     If value is not set, <see cref="SystemParameters.MinimumHorizontalDragDistance" /> is used.
        ///     Set to <see cref="double.PositiveInfinity" /> to prevent horizontal drag
        ///     (which does not make sense with standard ListView's vertical layout).
        /// </summary>
        public double? MinimumHorizontalDragDistance { get; set; }

        /// <summary>
        ///     Gets or sets optional minimal distance mouse pointer has to be dragged vertically before it is considered a drag
        ///     operation.
        ///     If value is not set, <see cref="SystemParameters.MinimumVerticalDragDistance" /> is used.
        ///     Set to <see cref="double.PositiveInfinity" /> to prevent vertical drag.
        /// </summary>
        public double? MinimumVerticalDragDistance { get; set; }

        /// <summary>
        ///     Gets or sets precondition for dragging. If set to <c>null</c> (default value) drag is always allowed.
        ///     Provided <see cref="Point" /> is the mouse position relative to the list view.
        /// </summary>
        public Func<Point, bool> DraggingPrecondition { get; set; }
        #endregion

        #region Listview's events
        private void ListView_DragLeave(object sender, DragEventArgs e)
        {
            if (!this.IsMouseOver(this.listView))
            {
                if (this.ItemUnderDragCursor != null)
                {
                    this.ItemUnderDragCursor = null;
                }

                if (this.dragAdorner != null)
                {
                    this.dragAdorner.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void ListView_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;

            if (this.ShowDragAdornerResolved)
            {
                this.UpdateDragAdornerLocation();
            }

            // Update the item which is known to be currently under the drag cursor.
            int index = this.IndexUnderDragCursor;
            this.ItemUnderDragCursor = index < 0 ? null : this.ListView.Items[index] as ItemType;
        }

        private void ListView_DragEnter(object sender, DragEventArgs e)
        {
            if (this.dragAdorner != null && this.dragAdorner.Visibility != Visibility.Visible)
            {
                // Update the location of the adorner and then show it.
                this.UpdateDragAdornerLocation();
                this.dragAdorner.Visibility = Visibility.Visible;
            }
        }

        private void ListView_Drop(object sender, DragEventArgs e)
        {
            if (this.ItemUnderDragCursor != null)
            {
                this.ItemUnderDragCursor = null;
            }

            e.Effects = DragDropEffects.None;

            if (!e.Data.GetDataPresent(typeof(ItemType)))
            {
                return;
            }

            // Get the data object which was dropped.
            ItemType data = e.Data.GetData(typeof(ItemType)) as ItemType;
            if (data == null)
            {
                return;
            }

            // Get the ObservableCollection<ItemType> which contains the dropped data object.
            ObservableCollection<ItemType> itemsSource = this.listView.ItemsSource as ObservableCollection<ItemType>;
            if (itemsSource == null)
            {
                throw new Exception(
                    "A ListView managed by ListViewDragManager must have its ItemsSource set to an ObservableCollection<ItemType>.");
            }

            int oldIndex = itemsSource.IndexOf(data);
            int newIndex = this.IndexUnderDragCursor;

            if (newIndex < 0)
            {
                // The drag started somewhere else, and our ListView is empty
                // so make the new item the first in the list.
                if (itemsSource.Count == 0)
                {
                    newIndex = 0;
                }

                // The drag started somewhere else, but our ListView has items
                // so make the new item the last in the list.
                else if (oldIndex < 0)
                {
                    newIndex = itemsSource.Count;
                }

                // The user is trying to drop an item from our ListView into
                // our ListView, but the mouse is not over an item, so don't
                // let them drop it.
                else
                {
                    return;
                }
            }

            // Dropping an item back onto itself is not considered an actual 'drop'.
            if (oldIndex == newIndex)
            {
                return;
            }

            if (this.ProcessDrop != null)
            {
                // Let the client code process the drop.
                ProcessDropEventArgs<ItemType> args = new ProcessDropEventArgs<ItemType>(
                    itemsSource,
                    data,
                    oldIndex,
                    newIndex,
                    e.AllowedEffects);
                this.ProcessDrop(this, args);
                e.Effects = args.Effects;
            }
            else
            {
                // Move the dragged data object from it's original index to the
                // new index (according to where the mouse cursor is).  If it was
                // not previously in the ListBox, then insert the item.
                if (oldIndex > -1)
                {
                    itemsSource.Move(oldIndex, newIndex);
                }
                else
                {
                    itemsSource.Insert(newIndex, data);
                }

                // Set the Effects property so that the call to DoDragDrop will return 'Move'.
                e.Effects = DragDropEffects.Move;
            }
        }

        private void ListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (this.IsMouseOverScrollbar)
            {
                // 4/13/2007 - Set the flag to false when cursor is over scrollbar.
                this.canInitiateDrag = false;
                return;
            }

            int index = this.IndexUnderDragCursor;
            this.canInitiateDrag = index > -1;

            if (this.canInitiateDrag)
            {
                // Remember the location and index of the ListViewItem the user clicked on for later.
                this.ptMouseDown = MouseUtilities.GetMousePosition(this.listView);
                this.indexToSelect = index;
            }
            else
            {
                this.ptMouseDown = new Point(-10000, -10000);
                this.indexToSelect = -1;
            }
        }

        private void ListView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!this.CanStartDragOperation)
            {
                return;
            }

            // Select the item the user clicked on.
            if (this.listView.SelectedIndex != this.indexToSelect)
            {
                this.listView.SelectedIndex = this.indexToSelect;
            }

            // If the item at the selected index is null, there's nothing
            // we can do, so just return
            if (this.listView.SelectedItem == null)
            {
                return;
            }

            ListViewItem itemToDrag = this.GetListViewItem(this.listView.SelectedIndex);
            if (itemToDrag == null)
            {
                return;
            }

            AdornerLayer adornerLayer = this.ShowDragAdornerResolved ? this.InitializeAdornerLayer(itemToDrag) : null;

            this.InitializeDragOperation(itemToDrag);
            this.PerformDragOperation();
            this.FinishDragOperation(itemToDrag, adornerLayer);
        }
        #endregion
    }
}
