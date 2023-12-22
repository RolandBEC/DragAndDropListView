using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using WpfApp1.MVVM;

namespace WpfApp1
{
    public class MainWindowVM : BaseViewModel
    {
        public ICommand AddNewRuleCommand { get; set; }

        public ICommand DragDropRuleCommand { get; set; }

        public ObservableCollection<RuleVM> Rules { get; } = new ObservableCollection<RuleVM>();

        public RuleVM SourceDragItem { get; set; }

        public RuleVM TargetDropItem { get; set; }



    }
}
