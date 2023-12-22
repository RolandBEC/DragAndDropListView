using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Input;

namespace WpfApp1.MVVM
{

    public class RelayCommand : ICommand
    {
        #region Fields

        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;
        private readonly List<EventHandler> canExecuteDelegates;
        private EventHandler _canExecuteChanged;

        #endregion // Fields

        #region Constructors

        public RelayCommand(Action<object> execute)
            : this(execute, null)
        {
        }

        public RelayCommand(Action<object> execute, Predicate<object> canExecute)
        {
            if (execute == null)
                throw new ArgumentNullException(nameof(execute));

            _execute = execute;
            _canExecute = canExecute;
            canExecuteDelegates = new List<EventHandler>();
        }

        #endregion // Constructors

        #region ICommand Members

        [DebuggerStepThrough]
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add
            {
                _canExecuteChanged += value;
                CommandManager.RequerySuggested += value;

                canExecuteDelegates.Add(value);
            }
            remove
            {
                _canExecuteChanged -= value;
                CommandManager.RequerySuggested -= value;

                canExecuteDelegates.Remove(value);
            }
        }

        /// <summary>
        /// Execute delegate with parameter
        /// Exception are logged and not propagated : this method is usually called by WPF framework.
        /// Uncatched exceptions can crash application.
        /// </summary>
        /// <param name="parameter"></param>
        public void Execute(object parameter)
        {
            try
            {
                _execute(parameter);
            }
            catch (Exception ex)
            {
                //Log.Error("Execute failed", ex);
            }
        }

        #endregion // ICommand Members     

        /// <summary>
        /// method force the evaluation of CanExecute
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            if (_canExecute != null)
                OnCanExecuteChanged();
        }

        /// <summary>
        /// This method is used to walk the delegate chain and well WPF that
        /// our command execution status has changed.
        /// </summary>
        protected virtual void OnCanExecuteChanged()
        {
            _canExecuteChanged?.Invoke(this, EventArgs.Empty);
        }


        #region Unsubscribe
        public void UnsubscribeCanExecute()
        {
            foreach (EventHandler canExecuteDelegate in canExecuteDelegates)
            {
                _canExecuteChanged -= canExecuteDelegate;
                CommandManager.RequerySuggested -= canExecuteDelegate;
            }

            canExecuteDelegates.Clear();
        }
        #endregion
    }
}
