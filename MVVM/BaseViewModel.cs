using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace WpfApp1.MVVM
{
    /// <summary>
    ///     Provide a base class for ViewModels that need implementation for INotifyPropertyChanged and INotifyDataErrorInfo
    /// </summary>
    public abstract class BaseViewModel : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        #region Constants, Fields, Properties, Indexers

        #region Fields, Constants

        /// <summary>
        ///     The ValidationRules for each property
        ///     Key : Name of the property bind to the view
        ///     Value : List of validationRule
        /// </summary>
        protected readonly Dictionary<string, List<ValidationRule>> ValidationRules;

        private readonly Dictionary<string, List<string>> pErrors;

        private readonly object pSyncLock = new object();
        #endregion

        #endregion

        protected BaseViewModel()
        {
            this.pErrors = new Dictionary<string, List<string>>();
            this.ValidationRules = new Dictionary<string, List<ValidationRule>>();
        }

        #region Methods
        /// <summary>
        ///     Check the value of the given propertyName
        ///     Check all associated ValidationRule and Notify the view
        /// </summary>
        /// <param name="value"></param>
        /// <param name="propertyName"></param>
        /// <returns>return false if there is at least one bad validation</returns>
        public bool ValidateProperty(object value, [CallerMemberName] string propertyName = null)
        {
            if (propertyName == null)
            {
                return true;
            }
            lock (this.pSyncLock)
            {
                bool _isValid = true;
                List<ValidationRule> _propertyValidationRules;
                if (!this.ValidationRules.TryGetValue(propertyName, out _propertyValidationRules))
                {
                    return true;
                }

                // Clear previous errors from tested property  
                if (this.pErrors.ContainsKey(propertyName))
                {
                    this.pErrors.Remove(propertyName);
                    this.RaiseErrorsChanged(propertyName);
                }

                _propertyValidationRules.ForEach(
                    validationRule =>
                    {
                        ValidationResult result = validationRule.Validate(value, CultureInfo.CurrentCulture);
                        if (!result.IsValid)
                        {
                            _isValid = false;
                            this.AddError(propertyName, result.ErrorContent as string, false);
                        }
                    });
                return _isValid;
            }
        }

        /// <summary>
        ///     Adds the specified error to the errors collection if it is not already present,
        ///     inserting it in the first position if isWarning is false.
        ///     Raises the ErrorsChanged event if the collection changes.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="error"></param>
        /// <param name="isWarning"></param>
        protected void AddError(string propertyName, string error, bool isWarning)
        {
            if (!this.pErrors.ContainsKey(propertyName))
            {
                this.pErrors[propertyName] = new List<string>();
            }

            if (!this.pErrors[propertyName].Contains(error))
            {
                if (isWarning)
                {
                    this.pErrors[propertyName].Add(error);
                }
                else
                {
                    this.pErrors[propertyName].Insert(0, error);
                }
                this.RaiseErrorsChanged(propertyName);
            }
        }

        /// <summary>
        ///     Removes the specified error from the errors collection if it is present.
        ///     Raises the ErrorsChanged event if the collection changes.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="error"></param>
        protected void RemoveError(string propertyName, string error)
        {
            if (this.pErrors.ContainsKey(propertyName) && this.pErrors[propertyName].Contains(error))
            {
                this.pErrors[propertyName].Remove(error);
                if (this.pErrors[propertyName].Count == 0)
                {
                    this.pErrors.Remove(propertyName);
                }
                this.RaiseErrorsChanged(propertyName);
            }
        }

        private void RaiseErrorsChanged(string propertyName)
        {
            this.ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }
        #endregion

        #region INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region INotifyDataErrorInfo impl
        /// <summary>Gets the validation errors for a specified property or for the entire entity.</summary>
        /// <param name="propertyName">
        ///     The name of the property to retrieve validation errors for; or <see langword="null" /> or
        ///     <see cref="F:System.String.Empty" />, to retrieve entity-level errors.
        /// </param>
        /// <returns>The validation errors for the property or entity.</returns>
        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !this.pErrors.ContainsKey(propertyName))
            {
                return new List<string>();
            }
            return this.pErrors[propertyName];
        }

        /// <summary>Gets the validation errors for a specified property or for the entire entity.</summary>
        /// <param name="propertyName">
        ///     The name of the property to retrieve validation errors for; or <see langword="null" /> or
        ///     <see cref="F:System.String.Empty" />, to retrieve entity-level errors.
        /// </param>
        /// <returns>The validation errors for the property or entity.</returns>
        IEnumerable INotifyDataErrorInfo.GetErrors(string propertyName)
        {
            return this.GetErrors(propertyName);
        }

        /// <summary>Gets a value that indicates whether the entity has validation errors. </summary>
        /// <returns>
        ///     <see langword="true" /> if the entity currently has validation errors; otherwise, <see langword="false" />.
        /// </returns>
        public bool HasErrors
        {
            get
            {
                return this.pErrors.Count > 0;
            }
        }

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        #endregion
    }
}
