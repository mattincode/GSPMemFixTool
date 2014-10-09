using Securitas.GSP.RiaClient.Contracts;
using Securitas.GSP.RiaClient.Model;
using Securitas.GSP.RiaClient.ViewModels;
using System;

namespace Securitas.GSP.RiaClient.ViewModels
{
    public abstract class EmployeeChildBaseViewModel : GspBaseViewModel, IEmployeeViewModel
    {
        private EmployeeViewModel _employeeViewModel;
        public EmployeeViewModel EmployeeViewModel 
        {
            get 
            {
                return _employeeViewModel; 
            }
            set 
            {
                _employeeViewModel = value;
                _employeeViewModel.EmployeeWasUpdated += OnEmployeeWasUpdated;
            }
        }

        private void OnEmployeeWasUpdated(object sender, EventArgs e)
        {
            //Execute the EmployeeWasUpdatedByAnotherView method of it wasn't this view that raised the event
            if (this.GetType().Equals(sender.GetType()) == false)
            {
                EmployeeWasUpdatedByAnotherView();
            }
        }

        /// <summary>
        /// Will be executed when the parent EmployeeViewModel has received an indication that the employee has been updated by any of the child views.
        /// It's up to each view to do whatever it wants to do with this information, including doing nothing.
        /// </summary>
        protected abstract void EmployeeWasUpdatedByAnotherView();

        public event EventHandler EmployeeWasUpdated;

        /// <summary>
        /// Can be implemented in a employee child view to let other views know that it has updated the employee
        /// </summary>
        protected void NotifyEmployeeWasUpdated() 
        {
            if(EmployeeWasUpdated != null)
                EmployeeWasUpdated(this, null);
        }

        public abstract Employee Employee { get; set; }
        public string MasterViewId { get; set; }
    }
}