using System;
using System.Windows;
using WpfApp1.DragAndDrop;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            MainWindowController controller = new MainWindowController();
            this.DataContext = controller.ViewModel;

            ListViewDragDropManager<RuleVM> dndMngr = new ListViewDragDropManager<RuleVM>(this.ListView1);
            dndMngr.ShowDragAdorner = true;

            ListViewDragDropManager<RuleVM> dndMngr2 = new ListViewDragDropManager<RuleVM>(this.ListView2);
            dndMngr2.ShowDragAdorner = true;
            dndMngr2.ProcessDrop += DndMngr_ProcessDrop;
        }

        private void DndMngr_ProcessDrop(object? sender, ProcessDropEventArgs<RuleVM> e)
        {
            // should implement stuff
            throw new NotImplementedException();
        }
    }
}
