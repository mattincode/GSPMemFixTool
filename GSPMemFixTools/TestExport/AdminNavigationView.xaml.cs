using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows.Controls;
using Jounce.Core.View;
using Jounce.Core.ViewModel;
using Jounce.Regions.Core;
using Securitas.GSP.RiaClient.Framework.Views;

namespace Securitas.GSP.RiaClient.Views
{
    [ExportAsView(Constants.VIEW_ADMINNAVIGATION, Category = "NavigationRoot", MenuName = "Navigation_TopLevel_Admin")]
    [ExportAsViewIcon(Constants.VIEW_ADMINNAVIGATION, ImagePath = @"/Securitas.GSP.RiaClient.UIResources;component/Images/Settings.png")]
    [ExportViewToRegion(Constants.VIEW_ADMINNAVIGATION, Constants.REGION_OUTLOOKBAR)]
    public partial class NavigationView : UserControl
    {
        public NavigationView()
        {
            InitializeComponent();
            //ExportAsViewAttribute[] attrs = (ExportAsViewAttribute[])typeof(NavigationView).GetCustomAttributes(typeof(ExportAsViewAttribute), false);
            //if (attrs.Length > 0)
            //{
            //    string newValue = Framework.Resources.ResourceManager.GetString(attrs[0].MenuName);
            //    if (newValue != null)
            //        attrs[0].MenuName = newValue;
            //}
        }

        /// <summary>
        ///     This will allow the binding of the view model to the view
        /// </summary>
        [Export]
        public ViewModelRoute Binding
        {
            get { return ViewModelRoute.Create(Constants.VIEWMODEL_ADMINNAVIGATION, Constants.VIEW_ADMINNAVIGATION); }
        }
    }
}
