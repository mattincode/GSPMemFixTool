using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Jounce.Core.View;
using Jounce.Core.ViewModel;
using Securitas.GSP.RiaClient.Framework.Services;
using Securitas.GSP.RiaClient.Model;
using Securitas.GSP.RiaClient.ViewModels;
using Securitas.GSP.RiaClient.Views.Controls;
using Telerik.Windows.Controls;
using System.Collections.Generic;
using System.Linq;
using ListBox = System.Windows.Controls.ListBox;
using SelectionChangedEventArgs = System.Windows.Controls.SelectionChangedEventArgs;

namespace Securitas.GSP.RiaClient.Views
{
    [ExportAsView(Constants.VIEW_AIRPORTCONFIGURATION)]
	public partial class AirportConfigurationView : UserControl
    {
        public AirportConfigurationView()
        {
            InitializeComponent();

            Loaded += (s1, e1) =>
            {
                var vm = DataContext as AirportConfigurationViewModel;
                if (vm != null)
                {
                    vm.SelectedEntityChanged -= SelectedEntityChanged;
                    vm.SelectedEntityChanged += SelectedEntityChanged;
                }
            };
        }

        [Export]
        public ViewModelRoute Binding
        {
            get { return ViewModelRoute.Create(Constants.VIEWMODEL_AIRPORTCONFIGURATION, Constants.VIEW_AIRPORTCONFIGURATION); }
        }

        private void configTree_Selected(object sender, Telerik.Windows.RadRoutedEventArgs e)
        {
            var vm = DataContext as AirportConfigurationViewModel;
            if (vm != null)
            {
                RadTreeViewItem item = (RadTreeViewItem)e.Source;
                vm.SelectEntity(item.Item);
            }
        }

        private void SelectedEntityChanged(object sender, SelectedEntityChangedEventArgs e)
        {
            if (e.Entity != null)
            {
                configTreeView.SelectedItem = e.Entity;
            }
        }

        private void AddCheckpointStaffBtn_OnClick(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as AirportConfigurationViewModel;
            var ctrl = new EditAirportStaffControl(new AirportCheckpointPassengerConfiguration(), vm.SelectedCheckpoint.PassengerConfigurations);
            ctrl.OnEditFinished += passengerConfiguration =>
            {
                //var vm = DataContext as AirportConfigurationViewModel;
                if (vm != null)
                {
                    vm.AddPassengerConfiguration(passengerConfiguration);
                }
            };

            GSPApplicationService.Current.AppState.ShowPopup(ctrl, AddCheckpointStaffBtn);
        }

        private void EditStaffConfig_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var listItem = sender as FrameworkElement;
            if (listItem != null)
            {
                var vm = DataContext as AirportConfigurationViewModel;
                var oldPassenger = listItem.DataContext as AirportCheckpointPassengerConfiguration;
                if (vm != null && oldPassenger != null)
                {
                    var clonedItem = oldPassenger.DeepClone<AirportCheckpointPassengerConfiguration>();
                    foreach (var sc in clonedItem.StaffConfigurations)
                        sc.Qualification = vm.Qualifications.FirstOrDefault(x => x.QualificationId == sc.QualificationId);

                    var ctrl = new EditAirportStaffControl(clonedItem, vm.SelectedCheckpoint.PassengerConfigurations);
                    ctrl.OnEditFinished += newPassenger =>
                    {
                        vm.EditPassengerConfiguration(newPassenger, oldPassenger);
                    };

                    GSPApplicationService.Current.AppState.ShowPopup(ctrl, listItem);
                }
            }
        }

        // Ugly fix for the fact that ListBox.SelectedItems is not a dependency property
        private void AddEmployeeButton_Click(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as AirportConfigurationViewModel;
            if (vm != null)
            {
                var employees = new List<Model.Employee>();
                foreach (var item in AllEmployeesListBox.SelectedItems)
                {
                    var employee = item as Model.Employee;
                    if (employee != null)
                    {
                        employees.Add(employee);
                    }
                }
                //var employees = AllEmployeesListBox.SelectedItems as IEnumerable<Employee>;
                if (employees.Any())
                {
                    vm.AddEmployees(employees);
                }
            }
            
        }

        private void RemoveEmployeeButton_Click(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as AirportConfigurationViewModel;
            if (vm != null)
            {
                var employees = new List<Model.Employee>();
                foreach (var item in SelectedEmployeesListBox.SelectedItems)
                {
                    var employee = item as Model.Employee;
                    if (employee != null)
                    {
                        employees.Add(employee);
                    }
                }
                //var employees = SelectedEmployeesListBox.SelectedItems as IEnumerable<Employee>;
                if (employees.Any())
                {
                    vm.RemoveEmployees(employees);
                }
            }
        }
    }

    public class AirportConfigurationTemplateSelector : DataTemplateSelector
    {
        private DataTemplate _airportTemplate;
        private DataTemplate _terminalTemplate;
        private DataTemplate _checkpointTemplate;
        private DataTemplate _roleTemplate;

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is AirportConfiguration)
                return AirportTemplate;

            if (item is AirportTerminalConfiguration)
                return TerminalTemplate;

            if (item is AirportCheckpointConfiguration)
                return CheckpointTemplate;

            if (item is PlanningUnit)
                return RoleTemplate;

            return null;
        }

        public DataTemplate AirportTemplate
        {
            get
            {
                return this._airportTemplate;
            }
            set
            {
                this._airportTemplate = value;
            }
        }

        public DataTemplate TerminalTemplate
        {
            get
            {
                return this._terminalTemplate;
            }
            set
            {
                this._terminalTemplate = value;
            }
        }

        public DataTemplate CheckpointTemplate
        {
            get { return this._checkpointTemplate; }
            set { this._checkpointTemplate = value; }
        }

        public DataTemplate RoleTemplate
        {
            get { return this._roleTemplate; }
            set { this._roleTemplate = value; }
        }
    }
}
