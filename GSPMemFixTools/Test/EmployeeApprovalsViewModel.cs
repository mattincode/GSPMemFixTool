using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using Jounce.Core.Command;
using Jounce.Core.ViewModel;
using Jounce.Framework.Command;
using Securitas.GSP.Core.DomainObjects.Enums;
using Securitas.GSP.RiaClient.Contracts;
using Securitas.GSP.RiaClient.Framework.Helpers;
using Securitas.GSP.RiaClient.Framework.Services;
using Securitas.GSP.RiaClient.Model;
using Telerik.Windows.Controls;
using Localization = Securitas.GSP.RiaClient.UIResources;
using Securitas.GSP.Core.Common;

namespace Securitas.GSP.RiaClient.ViewModels
{
    [ExportAsViewModel(Constants.VIEWMODEL_EMPLOYEEAPPROVALS)]
    public partial class EmployeeApprovalsViewModel : EmployeeChildBaseViewModel
    {
        private Employee _employee;
        private ObservableCollection<EmployeeApproval> _approvals;
        private ObservableCollection<Approval> _allApprovals;
        public PagedCollectionView PagedEmployeeApprovalViewCollection { get; set; }
        private int _validationErrors;

        [Import]
        public IApprovalService ApprovalService { get; set; }
        [Import]
        public IEmployeeApprovalService EmployeeApprovalService { get; set; }

        //[Import]
        public IEmployeeApprovalService EmployeeApprovalService { get; set; }

        [Import]
        public IApprovalService ApprovalService { get; set; }

        [Import]
        public IAirportRequirementService AirportRequirementService { get; set; }
        [Import]
        public IAirportConfigurationService AirportConfigurationService { get; set; }


        public IActionCommand AddCommand { get; private set; }
        public IActionCommand DeleteCommand { get; private set; }
        public IActionCommand CommitCommand { get; private set; }
        public IActionCommand CancelCommand { get; private set; }
        public IActionCommand BeginEditCommand { get; private set; }
        public IActionCommand<ValidationErrorEventArgs> RegisterValidationErrorsCommand { get; private set; }

        public EmployeeApprovalsViewModel()
        {
            Header = Localization.Resources.EmployeeApprovalsView_Header;

            _WireDesignerData();

            if (InDesigner) return;

            RegisterValidationErrorsCommand = new ActionCommand<ValidationErrorEventArgs>(e =>
            {
                if (e.Action == ValidationErrorEventAction.Added)
                {
                    _validationErrors++;
                }
                else
                {
                    _validationErrors--;
                }
                RaisePropertyChanged(() => HasValidationErrors);
                CommitCommand.RaiseCanExecuteChanged();
            });

            CommitCommand = new ActionCommand<object>(obj =>
            {
                var dataForm = obj as RadDataForm;
				Action commitAction = () => { dataForm.CancelEdit(); }; // Using cancel edit since we are reloading!
                var editedApproval = PagedEmployeeApprovalViewCollection.CurrentItem as EmployeeApproval;

                if (editedApproval != null)
                    HandleAddUpdate(editedApproval, commitAction);
            }, obj =>
            {
                bool serverValidationError = false;
                var dataForm = obj as RadDataForm;
                if (dataForm != null)
                {
                    var current = dataForm.CurrentItem as EmployeeSkill;
                    if (current != null && current.HasErrors)
                        serverValidationError = true;
                }

                return IsEditing && !serverValidationError && !HasValidationErrors && ContextService.Current.Role.HasPermission(
                           EntityPermissions.Employee,
                           OperationPermissions.Update | OperationPermissions.Create);
            });

            CancelCommand = new ActionCommand<object>(obj =>
            {
				var dataForm = obj as RadDataForm;
				if (dataForm == null)
					return;

				CheckCancel(cancel =>
				{
					if (cancel)
					{
						dataForm.CancelEdit();
						Error = false;
						ApiError = false;
						IsEditing = false;
						UpdateCommandsCanExecuteStatus();
					}
				});

            }, obj => IsEditing);

            BeginEditCommand = new ActionCommand<object>(obj =>
            {
                var dataForm = obj as RadDataForm;
                if (dataForm != null)
                {
                    dataForm.BeginEdit();
                }
            }, obj => false);

            AddCommand = new ActionCommand<object>(obj =>
            {
                var dataForm = obj as RadDataForm;
                if (dataForm != null)
                {
                    dataForm.AddNewItem();
                }
            }, obj => false);

            DeleteCommand = new ActionCommand<object>(obj =>
            {
				var currentItem = PagedEmployeeApprovalViewCollection.CurrentItem as EmployeeApproval;
				var description = currentItem != null ? currentItem.Description : string.Empty;
				var dialogMessage = UIResources.Resources.Global_DeleteMessage + description + "?";
				var dialogService = GSPApplicationService.Current.DeploymentService.Container.GetExportedValue<IDialogService>();

	            dialogService.ShowDialog(UIResources.Resources.Global_DeleteQuestion, dialogMessage, true, res =>
		            {
			            if (!res)
			            {
				            return;
			            }

			            var dataForm = obj as RadDataForm;
			            Action dataFormDeleteAction = () => { dataForm.DeleteItem(); };

			            if (currentItem != null)
			            {
				            HandleDelete(currentItem, dataFormDeleteAction);
			            }
		            });
            }, obj => false);
        }

        public ObservableCollection<Approval> AllAvailableApprovals
        {
            get { return new ObservableCollection<Approval>(_allApprovals.Where(x => !_approvals.Any(z => z.ApprovalId == x.ApprovalId)).OrderBy(x => x.Description)); }
        }

        public override Employee Employee
        {
            get { return _employee; }
            set
            {
                _employee = value;
                RaisePropertyChanged(() => Employee);

                UpdateApprovalsFromServer();
            }
        }

        public ObservableCollection<Approval> AllApprovals
        {
            get { return _allApprovals; }
            set
            {
                if (_allApprovals == value)
                    return;
                _allApprovals = value;
                RaisePropertyChanged(() => AllApprovals);
            }
        }

        public bool HasValidationErrors
        {
            get
            {
                if (_validationErrors == 0)
                    return false;
                return true;
            }
        }

        private void HandleDelete(EmployeeApproval current, Action action)
        {
			EmployeeApprovalService.DeleteEmployeeApproval(current.EmployeeId, current.ApprovalId, (entity, response) => ThreadHelper.ExecuteOnUI(() =>
	            {
		            if (CheckResponse(response))
		            {
			            current.SetError(response.ApiErrorParamName, response.ErrorMessage);
			            return;
		            }
		            action.Invoke();
	            }));
        }

        private void HandleAddUpdate(EmployeeApproval current, Action action)
        {
            if (current.EmployeeApprovalId == 0)
            {
                current.EmployeeId = Employee.EmployeeId;
                EmployeeApprovalService.CreateEmployeeApproval(current,
                                                (entity, response) => HandleCommit(current, entity, response, action));
            }
            else
            {
                EmployeeApprovalService.UpdateEmployeeApproval(current,
                                                   (entity, response) => HandleCommit(current, entity, response, action));
            }
        }

        private void HandleCommit(EmployeeApproval current, EmployeeApproval result, IGSPServiceResponse response, Action action)
        {
			ThreadHelper.ExecuteOnUI(() =>
            {
                if (CheckResponse(response))
                {
                    current.SetError(response.ApiErrorParamName, response.ErrorMessage);
                    UpdateCommandsCanExecuteStatus();
                    return;
                }

                UpdateApprovals();
                UpdateCurrentItem(result);
				action.Invoke();

                UpdateCommandsCanExecuteStatus();
            });
        }

        private void UpdateCurrentItem(EmployeeApproval result)
        {
            _approvals[_approvals.IndexOf((EmployeeApproval)PagedEmployeeApprovalViewCollection.CurrentItem)] = result;
            PagedEmployeeApprovalViewCollection.MoveCurrentTo(result);
        }

        private void UpdateApprovalsFromServer()
        {
            ApprovalService.GetAll((enumerable, response) =>
            {
                if (response.HasErrors)
                    return;

                if (enumerable == null)
                    return;

				ThreadHelper.ExecuteOnUI(() =>
                {
                    AllApprovals = new ObservableCollection<Approval>(enumerable.OrderBy(model => model.Description).ToList());
                    UpdateApprovals();
                });
            });
        }

        private void EmployeeApprovalsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                var item = (EmployeeApproval)e.NewItems[0];
                item.ValidFrom = DateTime.Now.Date;
                item.EmployeeId = 0;
            }
        }

        private void PagedEmployeeApprovalViewCollectionPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsEditingItem" || e.PropertyName == "IsAddingNew")
            {
                IsEditing = (PagedEmployeeApprovalViewCollection.IsEditingItem || PagedEmployeeApprovalViewCollection.IsAddingNew);
                UpdateCommandsCanExecuteStatus();
            }
        }

        private void UpdateCommandsCanExecuteStatus()
        {
            CancelCommand.RaiseCanExecuteChanged();
            CommitCommand.RaiseCanExecuteChanged();
            BeginEditCommand.RaiseCanExecuteChanged();
            AddCommand.RaiseCanExecuteChanged();
            DeleteCommand.RaiseCanExecuteChanged();
        }

        private void UpdateApprovals()
        {
            EmployeeApprovalService.GetAll(_employee.EmployeeId, (res, err) =>
            {
                if (err.HasErrors)
                    return;

				ThreadHelper.ExecuteOnUI(() =>
                {
                    _approvals = new ObservableCollection<EmployeeApproval>(res.OrderBy(x => x.Description).ToList());
                    _approvals.CollectionChanged += new NotifyCollectionChangedEventHandler(EmployeeApprovalsCollectionChanged);
                    PagedEmployeeApprovalViewCollection = new PagedCollectionView(_approvals);
                    PagedEmployeeApprovalViewCollection.PropertyChanged += PagedEmployeeApprovalViewCollectionPropertyChanged;
                    RaisePropertyChanged(() => PagedEmployeeApprovalViewCollection);

                    RaisePropertyChanged(() => AllAvailableApprovals);
                });
            });
        }

        protected override void EmployeeWasUpdatedByAnotherView()
        {
        }
    }
}
