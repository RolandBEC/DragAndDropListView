using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfApp1.UI;

namespace WpfApp1.DragAndDrop
{
    /// <summary>
    /// Logique d'interaction pour ListViewWithDragAndDrop.xaml
    /// </summary>
    public partial class ListViewWithDragAndDrop : ListView
    {
        public static readonly DependencyProperty DragDropRuleCommandCommandProperty =
           DependencyProperty.Register(
               "DragDropRuleCommand",
               typeof(ICommand),
               typeof(ListViewWithDragAndDrop),
               new UIPropertyMetadata(null));

        public ICommand DragDropRuleCommand
        {
            get => (ICommand)GetValue(DragDropRuleCommandCommandProperty);
            set => SetValue(DragDropRuleCommandCommandProperty, value);
        }

        public static readonly DependencyProperty SourceDragItemProperty =
            DependencyProperty.Register(
                "SourceDragItem",
                typeof(object),
                typeof(ListViewWithDragAndDrop),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public object SourceDragItem
        {
            get => (object)GetValue(SourceDragItemProperty);
            set => SetValue(SourceDragItemProperty, value);
        }

        public static readonly DependencyProperty TargetDropItemProperty =
            DependencyProperty.Register(
                "TargetDropItem",
                typeof(object),
                typeof(ListViewWithDragAndDrop),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public object TargetDropItem
        {
            get => (object)GetValue(TargetDropItemProperty);
            set => SetValue(TargetDropItemProperty, value);
        }

        public ListViewWithDragAndDrop()
        {
            InitializeComponent();
        }

        private void ListViewItem_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed &&
                sender is FrameworkElement frameworkElement)
            {
                object todoItem = frameworkElement.DataContext;

                DragDropEffects dragDropResult = DragDrop.DoDragDrop(frameworkElement,
                    new DataObject(DataFormats.Serializable, todoItem),
                    DragDropEffects.Move);

                if (dragDropResult == DragDropEffects.None)
                {
                    //AddTodoItem(todoItem);
                }
            }
        }

        private void ListViewItem_DragOver(object sender, DragEventArgs e)
        {
            if (DragDropRuleCommand?.CanExecute(null) ?? false)
            {
                if (sender is FrameworkElement element)
                {
                    TargetDropItem = element.DataContext;
                    SourceDragItem = e.Data.GetData(DataFormats.Serializable);

                    DragDropRuleCommand?.Execute(null);
                }
            }
        }
    }
}
