using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Jounce.Core.Event;
using Jounce.Core.View;
using Jounce.Core.ViewModel;
using Securitas.GSP.RiaClient.Framework.Helpers;

namespace Securitas.GSP.RiaClient.Views
{

    [ExportAsView(Constants.VIEW_PLANNINGUNIT, MenuName = "Planningunit detail")]
    public partial class PlanningUnitView : UserControl
    {
        //[Import]
        //public IEventAggregator EventAggregator { get; set; }

        public PlanningUnitView()
        {
            InitializeComponent();
        }

        [Export]
        public ViewModelRoute Binding
        {
            get { return ViewModelRoute.Create(Constants.VIEWMODEL_PLANNINGUNIT, Constants.VIEW_PLANNINGUNIT); }
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
        }

	    private void TabControlOnLoaded(object sender, RoutedEventArgs e)
	    {
			var scrollLeft = VisualElementHelper.FindName<ButtonBase>("LeftScrollButtonElement", TabControl);
			var scrollRight = VisualElementHelper.FindName<ButtonBase>("RightScrollButtonElement", TabControl);
			var dropDown = VisualElementHelper.FindName<ToggleButton>("DropDownButtonElement", TabControl);

			if (scrollLeft != null && scrollLeft is RepeatButton)
			{
				ToolTipService.SetToolTip(scrollLeft, UIResources.Resources.BookingControl_Tooltip_Tabs);
			}

			if (scrollRight != null && scrollRight is RepeatButton)
			{
				ToolTipService.SetToolTip(scrollRight, UIResources.Resources.BookingControl_Tooltip_Tabs);
			}

			if (dropDown != null && dropDown is ToggleButton)
			{
				ToolTipService.SetToolTip(dropDown, UIResources.Resources.Tabs_Tooltip_SeeAll);
			}
	    }
    }
}
