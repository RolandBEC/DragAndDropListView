using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfApp1.MVVM;

namespace WpfApp1
{
    public class MainWindowController
    {
        public MainWindowVM ViewModel { get; } = new MainWindowVM();

        public MainWindowController() 
        {
            ViewModel.AddNewRuleCommand = new RelayCommand(ExecuteAddRuleCommand);
            ViewModel.DragDropRuleCommand = new RelayCommand(ExecuteDragDropRuleCommand);

            ViewModel.Rules.Add(new RuleVM { Name = "Super rule 1",  RuleRank = 0 });
            ViewModel.Rules.Add(new RuleVM { Name = "Super rule 2", RuleRank = 1 });
            ViewModel.Rules.Add(new RuleVM { Name = "Super rule 3", RuleRank = 2 });
            ViewModel.Rules.Add(new RuleVM { Name = "Super rule 4", RuleRank = 3 });
        }

        private void ExecuteAddRuleCommand(object param)
        {
            var rule = new RuleVM()
            {
                RuleRank = ViewModel.Rules.Count,
                Name = "added rule",
            };

            ViewModel.Rules.Add(rule);
        }

        private void ExecuteDragDropRuleCommand(object param)
        {
            if (ViewModel.SourceDragItem == ViewModel.TargetDropItem)
            {
                return;
            }

            int oldIndex = ViewModel.Rules.IndexOf(ViewModel.SourceDragItem);
            int nextIndex = ViewModel.Rules.IndexOf(ViewModel.TargetDropItem);

            if (oldIndex != -1 && nextIndex != -1)
            {
                ViewModel.Rules.Move(oldIndex, nextIndex);
            }
        }

    }
}
