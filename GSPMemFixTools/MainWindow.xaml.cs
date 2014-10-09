using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace GSPMemFixTools
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainViewModel _vm = new MainViewModel();
        public MainWindow()
        {
            InitializeComponent();
            DataContext = _vm;
            _vm.InterfacePath = @"D:\Stratiteq\TFS\Securitas GSP\MemLeak\Securitas.GSP.RiaClient.Contracts";
        }

        private void ReadServiceInterfacesButton_Click(object sender, RoutedEventArgs e)
        {
            // Scan folder and list all interfaces
            _vm.ScanInterfaceFolder();
        }

        private void BrowseInterfaceFolderButton_Click(object sender, RoutedEventArgs e)
        {
            // Open folder browser
            var dialog = new FolderBrowserDialog();
            dialog.SelectedPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _vm.InterfacePath = dialog.SelectedPath;
            }
        }
    }


    public class MainViewModel : BaseViewModel
    {

        private ObservableCollection<string> _interfaceList = new ObservableCollection<string>();
        public ObservableCollection<string> InterfaceList
        {
            get { return _interfaceList; }
            set 
            { 
                _interfaceList = value; 
                RaisePropertyChanged(() => InterfaceList);
            }
        }

        private string _InterfacePath;
        public string InterfacePath
        {
            get { return _InterfacePath; }
            set 
            { 
                _InterfacePath = value;
                InterfaceFolderOK = Directory.Exists(_InterfacePath);
                RaisePropertyChanged(() => InterfacePath);
            }
        }

        private bool _interfaceFolderOk;
        public bool InterfaceFolderOK
        {
            get { return _interfaceFolderOk; }
            set 
            { 
                _interfaceFolderOk = value;
                RaisePropertyChanged(() => InterfaceFolderOK);
            }
        }
        

        public void ScanInterfaceFolder()
        {
            if (_interfaceFolderOk)
            {


                var files = from fullFilename in Directory.EnumerateFiles(_InterfacePath, "I*Service.cs")
                                select System.IO.Path.GetFileName(fullFilename);
                var list = new List<string>();
                foreach (var file in files)
	            {
                    // Remove extension
                    var name = file.TrimEnd('.', 'c', 's');
                    var implName = name.TrimStart('I');
                    var ioc = String.Format("SimpleIoc.Default.Register<{0},{1}>();", name, implName);
                    //SimpleIoc.Default.Register<IPlanningUnitService, PlanningUnitService>();
                    list.Add(ioc);
                    Debug.WriteLine(ioc);
	            }
                InterfaceList = new ObservableCollection<string>(list);
            }

        }

    }
}
