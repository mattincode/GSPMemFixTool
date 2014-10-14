using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using Jounce.Core.Command;
using Jounce.Core.Event;
using Jounce.Framework.Command;
using Securitas.GSP.Core.Common;
using Securitas.GSP.RiaClient.Contracts;
using Securitas.GSP.RiaClient.Framework.Helpers;
using Securitas.GSP.RiaClient.Framework.Messaging;
using Securitas.GSP.RiaClient.Framework.Services;
using Securitas.GSP.RiaClient.Helpers;
using Securitas.GSP.RiaClient.Helpers.Navigation;
using Securitas.GSP.RiaClient.Model;
using Securitas.GSP.RiaClient.ViewModels.Helpers;
using Localization = Securitas.GSP.RiaClient.UIResources;
using Securitas.GSP.RiaClient.Helpers.Diagnostics;

namespace Securitas.GSP.RiaClient.ViewModels
{
    [ExportAsNamedViewModel(Constants.VIEWMODEL_EMPLOYEE)]
    public partial class EmployeeViewModel : GspBaseViewModel, IEventSink<EmployeeUpdated>, IPartImportsSatisfiedNotification
    {
//TODO MemFix Step 2 - Removed import statement, this comment can be removed after review
		public IEmployeeService EmployeeService { get { return ServiceLocator.Current.GetInstance<IEmployeeService>(); } }
//TODO MemFix Step 2 - Removed import statement, this comment can be removed after review
		public IReportService ReportService { get { return ServiceLocator.Current.GetInstance<IReportService>(); } }
        
		private Dictionary<string, EditingState> _editingStates = new Dictionary<string, EditingState>();
		public ReportControlViewModel ReportControl { get; set; }
		ObservableCollection<EmployeeChildBaseViewModel> _childVM = new ObservableCollection<EmployeeChildBaseViewModel>();
		private GspBaseViewModel _selectedView;
		private bool _isNew;
		private Employee _employee;
		public event EventHandler EmployeeWasUpdated;

		public IActionCommand SetAsSuperUserCommand { get; private set; }

	    public EmployeeViewModel()
	    {
			SetAsSuperUserCommand = new ActionCommand<Action<object>>(OnSetUserAsSuperAdminCommand);
	    }

	    private void OnSetUserAsSuperAdminCommand(Action<object> obj)
	    {
			EmployeeService.TempSetUserAsSuperAdmin((res, err) =>
			{
				if (err.HasErrors)
					return;
			});
	    }

	    public ObservableCollection<EmployeeChildBaseViewModel> ChildVM
        {
            get { return _childVM; }
        }
		
		public GspBaseViewModel SelectedView
		{
			get
			{
				return _selectedView;
			}
			set
			{
				_selectedView = value;
				RaisePropertyChanged(() => SelectedView);
			}

		}

        public Employee Employee
        {
            get
            {
                return _employee;
            }
            set
            {
                if (_employee != value)
                {
                    _employee = value;

                    if (_employee != null)
                    {
                        SetHeader(_employee.TabHeader);
                        if (_employee.EmployeeId > 0)
                            IsNew = false;
                    }
                    else
                        SetHeader("");

                    RaisePropertyChanged(() => Employee);
                    RaisePropertyChanged(() => Header);
                    RaisePropertyChanged(() => IsFake);
                    RaisePropertyChanged(() => HasImage);
                    RaisePropertyChanged(() => HasPhoneNumbers);
                    RaisePropertyChanged(() => CompositeHeader);
                }
            }
        }

		void EmpVmPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
            if (e.PropertyName == ExtractPropertyName(() => Employee))
			{
				// Reload employments
				var vm = _childVM.FirstOrDefault(x => x.ToString().Contains(Constants.VIEWMODEL_EMPLOYMENT));
				var employmentVM = vm as IEmploymentViewModel;
				if (employmentVM != null)
					employmentVM.LoadEmployments();
				// Reload planningperiods
				//vm = _childVM.FirstOrDefault(x => x.ToString().Contains(Constants.VIEWMODEL_PLANNINGPERIOD));
				//var planningPeriodVM = vm as IPlanningPeriodViewModel;
				//if (planningPeriodVM != null)
				//	planningPeriodVM.LoadPlanningPeriods();
			}
		}

		private void EmplVmPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "IsSaving")
			{
				var vm = _childVM.FirstOrDefault(x => x.ToString().Contains(Constants.VIEWMODEL_PLANNINGPERIOD));
				var planningPeriodVM = vm as IPlanningPeriodViewModel;
				if (planningPeriodVM != null)
					planningPeriodVM.LoadEmployments();
			}
		}

        private void OnEmployeeUpdated(object sender, EventArgs e) 
        {
            if (EmployeeWasUpdated != null)
            {
	            EmployeeWasUpdated(sender, e);
	            
				//GUI-handling after saving "luftgubbe"
				var view = sender as EmployeeDetailViewModel;
				if (view != null)
				{
					if (view.IsNew)
					{
						var otherViewsEmployee = view.Employee;
						Employee = otherViewsEmployee;

						 //Reload planningperiods
						var vm = _childVM.FirstOrDefault(x => x.ToString().Contains(Constants.VIEWMODEL_PLANNINGPERIOD));
						var planningPeriodVM = vm as IPlanningPeriodViewModel;
						if (planningPeriodVM != null)
						{
							if (vm.Employee.EmployeeId == 0)
							{
								planningPeriodVM.LoadPlanningPeriodsForFakeUserCreation(Employee.EmployeeId);
							}
						}

						//A litte gui trick to enable all tabs...
						EventAggregator.Publish(new GspNavigationArgs(Constants.VIEW_EMPLOYEE)
							.AddNamedParameter("Employee", Employee)
							.AddNamedParameter("SelectedView", Constants.VIEW_EMPLOYMENT));
					}
					else
					{
						var otherViewsEmployee = view.Employee;
						Employee = otherViewsEmployee;
					}
				}
            }
        }

		private void LoadChildViewmodels()
		{
			lock (_childVM)
			{
				if (_employee != null && !InDesigner)
				{
					var isEnabled = (!IsNew);
					var isEnabledForFakeUser = (!IsNew);

				    var empVm = new EmployeeDetailViewModel();   //(EmployeeDetailViewModel) Router.GetNonSharedViewModel(Constants.VIEWMODEL_EMPLOYEEDETAIL);
					empVm.IsNew = IsNew;
					empVm.Employee = _employee;
					empVm.PropertyChanged += EmpVmPropertyChanged;
                    empVm.EmployeeViewModel = this;
                    empVm.EmployeeWasUpdated += OnEmployeeUpdated;
					SetViewModelState(empVm, true);
					_childVM.Add(empVm);

                    if (ViewPermissionHelper.IsViewVisible(Constants.VIEW_EMPLOYEEDATEINTERVALSVIEW))
                    {
                        var empdivm = new EmployeeDateIntervalsViewModel(); //(EmployeeDateIntervalsViewModel)Router.GetNonSharedViewModel(Constants.VIEWMODEL_EMPLOYEEDATEINTERVAL);
                        empdivm.Employee = _employee;
                        empdivm.EmployeeViewModel = this;
                        SetViewModelState(empdivm, isEnabled);
                        empdivm.EmployeeWasUpdated += OnEmployeeUpdated;
                        _childVM.Add(empdivm);
                    }

					if (ViewPermissionHelper.IsViewVisible(Constants.VIEW_USERDETAIL))
					{
					    var userVm = new UserViewModel(); //(UserViewModel) Router.GetNonSharedViewModel(Constants.VIEWMODEL_USER);
						userVm.Employee = _employee;
						userVm.EmployeeViewModel = this;
						SetViewModelState(userVm, isEnabled);
						if (NewOrganizationCreated != null)
							userVm.NewOrganizationCreated = NewOrganizationCreated;
						userVm.EmployeeWasUpdated += OnEmployeeUpdated;
						_childVM.Add(userVm);
					}

				    var planningVm = new PlanningPeriodViewModel(); //(PlanningPeriodViewModel) Router.GetNonSharedViewModel(Constants.VIEWMODEL_PLANNINGPERIOD);
                    planningVm.Employee = _employee;
                    planningVm.EmployeeViewModel = this;
					SetViewModelState(planningVm, isEnabled);
                    planningVm.EmployeeWasUpdated += OnEmployeeUpdated;
					_childVM.Add(planningVm);

				    var emplVm = new EmploymentViewModel(); //(EmploymentViewModel) Router.GetNonSharedViewModel(Constants.VIEWMODEL_EMPLOYMENT);
                    emplVm.Employee = _employee;
					emplVm.PropertyChanged += EmplVmPropertyChanged;
                    emplVm.EmployeeViewModel = this;
					SetViewModelState(emplVm, isEnabled);
                    emplVm.EmployeeWasUpdated += OnEmployeeUpdated;
					_childVM.Add(emplVm);

					if (ViewPermissionHelper.IsViewVisible(Constants.VIEW_EMPLOYEEWORKSCHEDULE))
					{
					    var workScheduleVm = new WorkScheduleViewModel(); // (WorkScheduleViewModel) Router.GetNonSharedViewModel(Constants.VIEWMODEL_WORKSCHEDULE);
						workScheduleVm.Employee = _employee;
						workScheduleVm.EmployeeViewModel = this;
						SetViewModelState(workScheduleVm, isEnabled);
						workScheduleVm.EmployeeWasUpdated += OnEmployeeUpdated;
						_childVM.Add(workScheduleVm);
					}

					if (ViewPermissionHelper.IsViewVisible(Constants.VIEW_EMPLOYEENOTE))
					{
					    var employeeNoteViewModel = new EmployeeNoteViewModel(); // (EmployeeNoteViewModel) Router.GetNonSharedViewModel(Constants.VIEWMODEL_EMPLOYEENOTE);
						employeeNoteViewModel.Employee = _employee;
						employeeNoteViewModel.EmployeeViewModel = this;
						SetViewModelState(employeeNoteViewModel, isEnabled);
						employeeNoteViewModel.EmployeeWasUpdated += OnEmployeeUpdated;
						_childVM.Add(employeeNoteViewModel);
					}

					if (ViewPermissionHelper.IsViewVisible(Constants.VIEW_EMPLOYEESALARYSUPPLEMENT))
					{
					    var employeeSalarySupplementsViewModel = new EmployeeSalarySupplementsViewModel();
							//(EmployeeSalarySupplementsViewModel) Router.GetNonSharedViewModel(Constants.VIEWMODEL_EMPLOYEESALARYSUPPLEMENT);
						employeeSalarySupplementsViewModel.Employee = _employee;
						employeeSalarySupplementsViewModel.EmployeeViewModel = this;
						if (Employee.FakeUser)
							isEnabledForFakeUser = false;
						SetViewModelState(employeeSalarySupplementsViewModel, isEnabledForFakeUser);
						employeeSalarySupplementsViewModel.EmployeeWasUpdated += OnEmployeeUpdated;
						_childVM.Add(employeeSalarySupplementsViewModel);
					}

					if (ViewPermissionHelper.IsViewVisible(Constants.VIEW_EMPLOYEEACCUMULATOR))
					{
					    var empAccumulatorVm = new EmployeeAccumulatorViewModel(); //(EmployeeAccumulatorViewModel)Router.GetNonSharedViewModel(Constants.VIEWMODEL_EMPLOYEEACCUMULATOR);
						empAccumulatorVm.Employee = _employee;
						empAccumulatorVm.EmployeeViewModel = this;
						SetViewModelState(empAccumulatorVm, isEnabled);
						empAccumulatorVm.EmployeeWasUpdated += OnEmployeeUpdated;
						_childVM.Add(empAccumulatorVm);
					}

					if (ViewPermissionHelper.IsViewVisible(Constants.VIEW_EMPLOYEECONTRACT))
					{
					    var employeeContractViewModel = new EmployeeContractViewModel();
                            //(EmployeeContractViewModel)Router.GetNonSharedViewModel(Constants.VIEWMODEL_EMPLOYEECONTRACT);
                        employeeContractViewModel.Employee = _employee;
                        employeeContractViewModel.EmployeeViewModel = this;
                        SetViewModelState(employeeContractViewModel, isEnabled);
                        employeeContractViewModel.EmployeeWasUpdated += OnEmployeeUpdated;
                        _childVM.Add(employeeContractViewModel);
                    }

				    var employeeQualificationsViewModel = new EmployeeQualificationsViewModel(); // Router.GetNonSharedViewModel(Constants.VIEWMODEL_EMPLOYEEQUALIFICATIONS);
                    employeeQualificationsViewModel.Employee = _employee;
                    employeeQualificationsViewModel.EmployeeViewModel = this;
					SetViewModelState(employeeQualificationsViewModel, isEnabled);
                    employeeQualificationsViewModel.EmployeeWasUpdated += OnEmployeeUpdated;
					_childVM.Add(employeeQualificationsViewModel);

				    var employeeSkillsViewModel = new EmployeeSkillsViewModel();//(EmployeeSkillsViewModel) Router.GetNonSharedViewModel(Constants.VIEWMODEL_EMPLOYEESKILLS);
                    employeeSkillsViewModel.Employee = _employee;
                    employeeSkillsViewModel.EmployeeViewModel = this;
					SetViewModelState(employeeSkillsViewModel, isEnabled);
                    employeeSkillsViewModel.EmployeeWasUpdated += OnEmployeeUpdated;
					_childVM.Add(employeeSkillsViewModel);

				    var employeeApprovalsViewModel = new EmployeeApprovalsViewModel(); //(EmployeeApprovalsViewModel)Router.GetNonSharedViewModel(Constants.VIEWMODEL_EMPLOYEEAPPROVALS);
					employeeApprovalsViewModel.Employee = _employee;
					employeeApprovalsViewModel.EmployeeViewModel = this;
					SetViewModelState(employeeApprovalsViewModel, isEnabled);
					employeeApprovalsViewModel.EmployeeWasUpdated += OnEmployeeUpdated;
					_childVM.Add(employeeApprovalsViewModel);

				    var employeePlanningUnitKnowledgeViewModel = new EmployeePlanningUnitKnowledgeViewModel(); // (EmployeePlanningUnitKnowledgeViewModel)Router.GetNonSharedViewModel(Constants.VIEWMODEL_EMPLOYEEPLANNINGUNITKNOWLEDGE);
                    employeePlanningUnitKnowledgeViewModel.Employee = _employee;
                    employeePlanningUnitKnowledgeViewModel.EmployeeViewModel = this;
					SetViewModelState(employeePlanningUnitKnowledgeViewModel, isEnabled);
                    employeePlanningUnitKnowledgeViewModel.EmployeeWasUpdated += OnEmployeeUpdated;
					_childVM.Add(employeePlanningUnitKnowledgeViewModel);

					// Monitor childVM:s
					_editingStates.Clear();
					foreach (var vm in _childVM)
					{
					    if (vm is EmployeeDetailViewModel)
                            vm.MasterViewId = ViewId;

						if (!_editingStates.ContainsKey(vm.ViewId))
						{
							vm.EditingChanged += vm_EditingChanged;
							_editingStates.Add(vm.ViewId, new EditingState() {ViewId = vm.ViewId, IsEditing = false});
						}
					}
				}

			}
		}

	    private void SetViewModelState(GspBaseViewModel vm, bool enabled)
		{
			vm.IsTabEnabled = enabled;
		}

	    private void SetHeader(string newHeader)
	    {
			if (!string.IsNullOrEmpty(newHeader))
			{
				Header = newHeader;
				RaisePropertyChanged(() => Header);
			}
			else
			{
				if (IsNew)
				{
					Header = Localization.Resources.EmployeeViewModel_NewEmployee;
					return;
				}

				int employeeNumber = -1;
				
				if (Employee != null)
				{
					employeeNumber = Employee.EmployeeNumber;
				}

				Header = string.Format("{0} {1}", Localization.Resources.EmployeeViewModel_Header, employeeNumber);
			}
	    }

		public string CompositeHeader
		{
			get
			{
				if (IsNew)
					return Localization.Resources.EmployeeViewModel_NewEmployee;

				int employeeNumber = -1;
				string employeeName = "";
				if (Employee != null)
				{
					employeeNumber = Employee.EmployeeNumber;
					employeeName = Employee.FullName;
				}

				return string.Format("{0} {1}", employeeNumber, employeeName);
			}
		}

		public bool CanSeeReportButton
		{
			get { return ContextService.Current.Role.HasPermission(ApplicationPermissions.Reports); }
		}

	    void vm_EditingChanged(object sender, bool isEditing)
        {
            EditingState editingState;
            var vm = sender as GspBaseViewModel;
            if (vm != null && _editingStates.TryGetValue(vm.ViewId, out editingState))
            {
                editingState.IsEditing = isEditing;
            }
            IsEditing = _editingStates.Any(x => x.Value.IsEditing);

        }

		private void LoadEmployee(int employeeId, DateTime? searchDate, Action action)
		{
            var query = new QueryEmployeeById
            {
                EmployeeId = employeeId,
                BetweenStartAndEndDate = searchDate
            };

            StopwatchService.StartWatch(_performanceWatchTimerId + "_TotalBeTime");

		    // Reload employee from server 
			EmployeeService.GetEmployee(query, (res, err) =>
			{
                StopwatchService.StopWatch(_performanceWatchTimerId + "_TotalBeTime");

			    if (err.HasErrors)
			    {
			        if (err.ApiErrorMessage == "NoAccess")
			        {
                        //Thread.CurrentThread.CurrentCulture = new CultureInfo(GSPApplicationService.Current.CurrentLangId);
                        //Thread.CurrentThread.CurrentUICulture = new CultureInfo(GSPApplicationService.Current.CurrentLangId);
                        var dialogMessage = string.Format(UIResources.Resources.Global_NoAccessMessge, UIResources.Resources.Global_Employee);
                        ThreadHelper.ExecuteOnUI(
                            () => Dialog.ShowDialog(UIResources.Resources.Global_NoAccessTitle, dialogMessage, false, null));
                        GspNavigation.CloseView("EmployeeView", ViewId);
			        }
			        return;
			    }
				ThreadHelper.ExecuteOnUI(() =>
				{
					Employee = res;
					LoadChildViewmodels();
					action.Invoke();
				});
			});
		}

		private Organization NewOrganizationCreated { get; set; }

		public bool IsNew
		{
			get { return _isNew; }
			set
			{
				if (value == _isNew)
					return;
				_isNew = value;
				
				RaisePropertyChanged(() => IsNew);
				RaisePropertyChanged(() => IsFake);
				RaisePropertyChanged(() => HasImage);
				RaisePropertyChanged(() => HasPhoneNumbers);
				RaisePropertyChanged(() => Header);
				RaisePropertyChanged(() => CompositeHeader);
			}
		}

		public bool IsFake
		{
			get
			{
				return Employee != null && Employee.FakeUser;
			}
		}

		public bool HasImage
		{
			get { return Employee != null && Employee.Image != null; }
		}

		public bool HasPhoneNumbers
		{
			get
			{
				return Employee != null &&
					   (!string.IsNullOrEmpty(Employee.Telephone1) || !string.IsNullOrEmpty(Employee.Telephone2));
			}
		}

        private string _performanceWatchTimerId = Guid.NewGuid().ToString();
        private void StartPerformanceTimer() 
        {
            GSPApplicationService.Current.AppState.BusyComplete += AppState_BusyComplete;
            StopwatchService.StartWatch(_performanceWatchTimerId);
        }
        
        public void Activate(string viewName, IDictionary<string, object> viewParameters)        
        {
            StartPerformanceTimer();

            string view = "";
            DateTime? period = null;
            Employee employee = null;
            DateTime? searchDate = null;

            object value = null;
            if (viewParameters.TryGetValue("New", out value))
                IsNew = (bool)value;

            if (viewParameters.ContainsKey("NewProfitCenter"))
                NewOrganizationCreated = (Organization)viewParameters["NewProfitCenter"];

            employee = (Employee)viewParameters["Employee"];

            // Check if any specific view should be set as active
            if (viewParameters.ContainsKey("SelectedView"))
                view = viewParameters["SelectedView"].ToString();
            // Check if a period should be loaded
            if (viewParameters.ContainsKey("Period") && viewParameters["Period"].ToString().Length > 0)
            {
                DateTime result;
                DateTime.TryParse(viewParameters["Period"].ToString(), out result);
                period = result;
            }

            if (viewParameters.ContainsKey("SearchDate") && viewParameters["SearchDate"] != null && viewParameters["SearchDate"].ToString().Length > 0)
            {
                DateTime result;
                DateTime.TryParse(viewParameters["SearchDate"].ToString(), out result);
                searchDate = result;
            }

            ReportControl = new ReportControlViewModel();
            if (employee != null)
            {
                ReportControl.CurrentEmployeeNumber = employee.EmployeeNumberStr;
                ReportControl.CurrentEmployeeId = employee.EmployeeId;
            }
            ReportControl.LoadReportsForView((int)ReportViews.Employee, ReportService);
            RaisePropertyChanged(() => ReportControl);

            // Sanity check!
            if (employee == null) return;

            // Check if new or existing employee
            if (employee.EmployeeId > 0)
            {
                // Load employee from server
                LoadEmployee(employee.EmployeeId, searchDate, () => SetActiveView(view, period));
            }
            else
            {
                // New employee
                Employee = employee;
                LoadChildViewmodels();
                // Select first view
                if (_childVM != null && _childVM.Count > 0)
                {
                    SelectedView = _childVM[0];
                }
            }       
        }

        protected override void ActivateView(string viewName, System.Collections.Generic.IDictionary<string, object> viewParameters)
        {
            StartPerformanceTimer();

			string view = "";
            DateTime? period = null;
			Employee employee = null;
            DateTime? searchDate = null;

			object value = null;
			if (viewParameters.TryGetValue("New", out value))
				IsNew = (bool)value;
	        
			if (viewParameters.ContainsKey("NewProfitCenter"))
				NewOrganizationCreated = (Organization)viewParameters["NewProfitCenter"];

			employee = (Employee)viewParameters["Employee"];

			// Check if any specific view should be set as active
			if (viewParameters.ContainsKey("SelectedView"))
				view = viewParameters["SelectedView"].ToString();
            // Check if a period should be loaded
            if (viewParameters.ContainsKey("Period") && viewParameters["Period"].ToString().Length > 0)
            {
                DateTime result;
                DateTime.TryParse(viewParameters["Period"].ToString(), out result);
                period = result;
            }

            if (viewParameters.ContainsKey("SearchDate") && viewParameters["SearchDate"] !=null && viewParameters["SearchDate"].ToString().Length > 0)
            {
                DateTime result;
                DateTime.TryParse(viewParameters["SearchDate"].ToString(), out result);
                searchDate = result;
            }

			ReportControl = new ReportControlViewModel();
			if (employee != null)
			{
				ReportControl.CurrentEmployeeNumber = employee.EmployeeNumberStr;
				ReportControl.CurrentEmployeeId = employee.EmployeeId;
			}
			ReportControl.LoadReportsForView((int)ReportViews.Employee, ReportService);
			RaisePropertyChanged(() => ReportControl);

			// Sanity check!
			if (employee == null) return;	

			// Check if new or existing employee
			if (employee.EmployeeId > 0)
			{
				// Load employee from server
			    LoadEmployee(employee.EmployeeId, searchDate, () => SetActiveView(view, period));
			}
			else
			{
				// New employee
				Employee = employee;
				LoadChildViewmodels();
				// Select first view
				if (_childVM != null && _childVM.Count > 0)
				{
					SelectedView = _childVM[0];
				}
			}
        }

        void AppState_BusyComplete(object sender, EventArgs e)
        {
            StopwatchService.StopWatch(_performanceWatchTimerId);

            NewRelicBrowserAgentHelper.InlineHit(new NewRelicInlineHitCommand("EmployeeView") 
            {
                FeTime = StopwatchService.Elapsed(_performanceWatchTimerId),
                TotalBeTime = StopwatchService.Elapsed(_performanceWatchTimerId + "_TotalBeTime"),
            });

            GSPApplicationService.Current.AppState.BusyComplete -= AppState_BusyComplete;
        }

		private void SetActiveView(string view, DateTime? period)
		{
			if (_childVM != null && _childVM.Count > 0)
			{
				GspBaseViewModel selectedView = null;
			    if (view.Length > 0)
			    {
			        if (view == Constants.VIEW_WORKSCHEDULE)
			        {
                        selectedView = _childVM.SingleOrDefault(x => x.ToString().EndsWith(view + "Model"));
			        }
			        else
			        {
                        selectedView = _childVM.SingleOrDefault(x => x.ToString().Contains(view));
			        }
			        
			    }
			    SelectedView = selectedView ?? _childVM[0];

                if (period.HasValue && SelectedView != null && SelectedView.GetType() == typeof(WorkScheduleViewModel))
                {
                    var workScheduleView = (WorkScheduleViewModel)SelectedView;
                    ContextService.Current.CommonDataContextService.StartDate = period.Value;
                    workScheduleView.LoadOnActivate = true;
                }

			}
		}

	   public void HandleEvent(EmployeeUpdated publishedEvent)
        {

            if (publishedEvent.UpdatedEmployee.Equals(Employee) || publishedEvent.IsNew)
            {
                _employee = publishedEvent.UpdatedEmployee;
                RaisePropertyChanged(() => Employee);
            }
        }

        public void OnImportsSatisfied()
        {
            EventAggregator.Subscribe(this);
        }
    }
}
