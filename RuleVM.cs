using System.Windows.Input;
using WpfApp1.MVVM;

namespace WpfApp1
{
    public class RuleVM : BaseViewModel
    {
        public ICommand DecreaseRankCommand { get; set; }
        public ICommand IncreaseRankCommand { get; set; }

        private int _ruleRank;
        public int RuleRank
        {
            get => _ruleRank;
            set
            {
                _ruleRank = value;
                NotifyChanged(nameof(RuleRank));
            }
        }

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                NotifyChanged(nameof(Name));
            }
        }
    }
}
