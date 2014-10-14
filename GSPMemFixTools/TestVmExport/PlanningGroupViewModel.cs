using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using Jounce.Core.ViewModel;
using Securitas.GSP.Core.Common;
using Securitas.GSP.RiaClient.Contracts;
using Securitas.GSP.RiaClient.Framework.Services;
using Securitas.GSP.RiaClient.Helpers;
using Securitas.GSP.RiaClient.Model;
using Securitas.GSP.RiaClient.ViewModels.Helpers;
using Localization = Securitas.GSP.RiaClient.UIResources;

namespace Securitas.GSP.RiaClient.ViewModels
{
    [ExportAsViewModel(Constants.VIEWMODEL_PLANNINGGROUP)]
    public partial class PlanningGroupViewModel : GspBaseViewModel
    {
        readonly ObservableCollection<GspBaseViewModel> _childVm = new ObservableCollection<GspBaseViewModel>();
		private Dictionary<string, EditingState> _editingStates = new Dictionary<string, EditingState>();
		public ReportControlViewModel ReportControl { get; set; }
        private PlanningGroup _planningGroup;
        private ProfitCenter _currentProfitCenter;
        private bool _isNew;
        
//TODO MemFix Step 2 - Removed import statement, this comment can be removed after review
		public IReportService ReportService { get { return ServiceLocator.Current.GetInstance<IReportService>(); } }

        public PlanningGroupViewModel()
        {
            _WireDesignerData();
        }

        public ObservableCollection<GspBaseViewModel> ChildVm
        {
            get { return _childVm; }
        }

        public bool IsNew
        {
            get { return _isNew; }
            set
            {
                if (value == _isNew)
                    return;
                _isNew = value;
				SetHeader();
                RaisePropertyChanged(() => IsNew);
                RaisePropertyChanged(() => CompositeHeader);
            }
        }

        public ProfitCenter CurrentProfitCenter
        {
            get { return _currentProfitCenter; }

            set
            {
                _currentProfitCenter = value;
				SetHeader();
                RaisePropertyChanged(() => CurrentProfitCenter);
                RaisePropertyChanged(() => CompositeHeader);
            }
        }

        public PlanningGroup PlanningGroup
        {
            get
            {
                return _planningGroup;
            }
            set
            {
                _planningGroup = value;
                if (_planningGroup != null && _planningGroup.ProfitCenterId != 0)
                {
                    var org = ContextService.Current.OrganizationList.FirstOrDefault(x => x.InternalId == _planningGroup.ProfitCenterId);
                    if (org != null)
                        CurrentProfitCenter = new ProfitCenter() { Name = org.Name, ProfitCenterNumber = org.ExternalId };
                }
				SetHeader();
                RaisePropertyChanged(() => PlanningGroup);
                RaisePropertyChanged(() => CompositeHeader);
                if (_planningGroup != null && !InDesigner)
                {
                    var plUnitVm = new PlanningGroupDetailViewModel(); // (PlanningGroupDetailViewModel)Router.GetNonSharedViewModel(Constants.VIEWMODEL_PLANNINGGROUPDETAIL);
					plUnitVm.IsNew = IsNew;
                    plUnitVm.PlanningGroup = _planningGroup;
                    plUnitVm.ParentViewId = ViewId;
                    _childVm.Add(plUnitVm);

                    var plGrUnitVm = new PlanningGroupPlanningUnitsViewModel(); // (PlanningGroupPlanningUnitsViewModel)Router.GetNonSharedViewModel(Constants.VIEWMODEL_PLANNINGGROUPPLANNINGUNITS);
					plGrUnitVm.PlanningGroup = _planningGroup;
        			_childVm.Add(plGrUnitVm);

                    var plGrGenderVm = new PlanningGroupGenderConstraintsViewModel(); //(PlanningGroupGenderConstraintsViewModel)Router.GetNonSharedViewModel(Constants.VIEWMODEL_PLANNINGGROUPGENDERCONSTRAINTS);
                    plGrGenderVm.PlanningGroup = _planningGroup;
                    _childVm.Add(plGrGenderVm);

                    if (ViewPermissionHelper.IsViewVisible(Constants.VIEW_PLANNINGGROUPINVOICEVIEW))
                    {
                        var plGrInvoiceVm = new PlanningGroupInvoiceViewModel(); // (PlanningGroupInvoiceViewModel)Router.GetNonSharedViewModel(Constants.VIEWMODEL_PLANNINGGROUPINVOICE);
                        plGrInvoiceVm.PlanningGroup = _planningGroup;
                        _childVm.Add(plGrInvoiceVm);
                    }

                    // Monitor childVM:s
					_editingStates.Clear();
					foreach (var vm in _childVm)
					{
						if (!_editingStates.ContainsKey(vm.ViewId))
						{
							vm.EditingChanged += vm_EditingChanged;
							_editingStates.Add(vm.ViewId, new EditingState() {ViewId = vm.ViewId, IsEditing = false});
						}
					}
                }
            }
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

        protected override void ActivateView(string viewName, System.Collections.Generic.IDictionary<string, object> viewParameters)
        {
			try
			{
				IsNew = (bool)viewParameters["New"];
			}
			catch (KeyNotFoundException)
			{
			}

            var planningGroup = (PlanningGroup)viewParameters["PlanningGroup"];
            if (planningGroup != null)
                PlanningGroup = planningGroup;

			ReportControl = new ReportControlViewModel();
			if (planningGroup != null)
				ReportControl.CurrentPlanningGroupId = planningGroup.PlanningGroupId;
			ReportControl.LoadReportsForView((int)ReportViews.PlanningGroup, ReportService);
			RaisePropertyChanged(() => ReportControl);

            try
            {
                var profitCenterOrganization = (Organization)viewParameters["Organization"];
                if (profitCenterOrganization != null)
                {
                    CurrentProfitCenter = new ProfitCenter()
                                              {
                                                  Name = profitCenterOrganization.Name,
                                                  ProfitCenterNumber = profitCenterOrganization.ExternalId
                                              };
                }
            }
            catch (KeyNotFoundException)
            {
            }    
        }

        public string CompositeHeader
        {
            get
            {
                
                if (IsNew)
                    return Localization.Resources.PlanningGroupViewModel_NewPlanningGroup;

				string planningGroupName = "";
				string planningGroupNumber = "";
                if (PlanningGroup != null)
                {
					planningGroupName = PlanningGroup.PlanningGroupName;
					planningGroupNumber = PlanningGroup.PlanningGroupNumber;
                }

                string profitCenterName = "";
                string profitCenterNumber = "-1";
                if (CurrentProfitCenter != null)
                {
                    profitCenterName = CurrentProfitCenter.Name;
                    profitCenterNumber = CurrentProfitCenter.ProfitCenterNumber;
                }
                
				return string.Format("{5}: {2} ({3}), {4}: {0} ({1})", planningGroupNumber, planningGroupName, profitCenterNumber, profitCenterName, Localization.Resources.Global_PlanningGroup, Localization.Resources.Global_ProfitCenter);
            }
        }

		public bool CanSeeReportButton
		{
			get { return ContextService.Current.Role.HasPermission(ApplicationPermissions.Reports); }
		}

        private void SetHeader()
        {
			if (IsNew)
			{
				Header = Localization.Resources.PlanningGroupViewModel_NewPlanningGroup;
			}
			else
			{
				string PlanningGroupNumber = "";
				if (PlanningGroup != null)
				{
					PlanningGroupNumber = PlanningGroup.PlanningGroupNumber;
				}

				string profitCenterNumber = "-1";
				if (CurrentProfitCenter != null)
				{
					profitCenterNumber = CurrentProfitCenter.ProfitCenterNumber;
				}

				Header = string.Format("{0} {1} {2}", Localization.Resources.ShortName_PlanningGroup, profitCenterNumber, PlanningGroupNumber);
			}
        }
    }
}
