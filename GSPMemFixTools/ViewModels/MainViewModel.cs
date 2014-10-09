using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Collections.ObjectModel;

namespace GSPMemFixTools.ViewModels
{
    public class MainViewModel : BaseViewModel
    {

        private bool _interfaceFolderOk;
        private ObservableCollection<string> _interfaceList = new ObservableCollection<string>();
        private string _InterfacePath;

        public bool InterfaceFolderOK
        {
            get { return _interfaceFolderOk; }
            set
            {
                _interfaceFolderOk = value;
                RaisePropertyChanged(() => InterfaceFolderOK);
            }
        }

        public ObservableCollection<string> InterfaceList
        {
            get { return _interfaceList; }
            set
            {
                _interfaceList = value;
                RaisePropertyChanged(() => InterfaceList);
            }
        }
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

        #region Part 2

        private bool _riaClientFolderOk;
        private ObservableCollection<string> _importList = new ObservableCollection<string>();
        private string _riaClientPath;

        private bool _simulateImport;

        public bool SimulateImport
        {
            get { return _simulateImport; }
            set 
            { 
                _simulateImport = value;
                RaisePropertyChanged(() => SimulateImport);
            }
        }
        

        public bool RiaClientFolderOk
        {
            get { return _riaClientFolderOk; }
            set
            {
                _riaClientFolderOk = value;
                RaisePropertyChanged(() => RiaClientFolderOk);
            }
        }

        public ObservableCollection<string> ImportList
        {
            get { return _importList; }
            set
            {
                _importList = value;
                RaisePropertyChanged(() => ImportList);
            }
        }
        public string RiaClientPath
        {
            get { return _riaClientPath; }
            set
            {
                _riaClientPath = value;
                RiaClientFolderOk = Directory.Exists(_riaClientPath);
                RaisePropertyChanged(() => RiaClientPath);
            }
        }

        private int _numberOfUpdates;

        public int NumberOfUpdates
        {
            get { return _numberOfUpdates; }
            set 
            { 
                _numberOfUpdates = value;
                RaisePropertyChanged(() => NumberOfUpdates);
            }
        }
        

        /// <summary>
        /// Handle checkout from tfs? -> Simple solution is to copy files to folder, removce readonly-flag... do the job and then paste in the updated files
        /// Read all files, then open content and search for [Import] statements, Replace with ServiceLocator
        /// Should be ok to run directly on tfs-folder (if any problems with tfs run commandline: tfpt online)
        /// </summary>
        List<string> _tempList = new List<string>();        
        public void ReplaceImports()
        {
            if (_riaClientFolderOk)
            {                
                _tempList = new List<string>();
                ImportList.Clear();
                NumberOfUpdates = 0;
                // Process *.cs                
                var files = Directory.GetFiles(_riaClientPath, "*.cs", SearchOption.AllDirectories); // Directory.EnumerateFiles(_riaClientPath, "*.cs");
                ProcessFilesForImport(files);
                // Process *.xaml.cs
                files = Directory.GetFiles(_riaClientPath, "*.xaml.cs", SearchOption.AllDirectories); //Directory.EnumerateFiles(_riaClientPath, "*.xaml.cs"); 
                ProcessFilesForImport(files);
                ImportList = new ObservableCollection<string>(_tempList);
                NumberOfUpdates = _numberOfUpdates;
            }

        }

        private void ProcessFilesForImport(IEnumerable<string> files)
        {            
            foreach (var file in files)
            {
                _tempList.Add(String.Format("Processing {0}", file));
                var content = File.ReadAllLines(file).ToList();
                var updatedRows = 0;
                while (ReplaceImports(content))
                {
                    updatedRows++;
                    _numberOfUpdates++;
                };

                // Only write file if any rows has been updated
                if (updatedRows > 0 && !SimulateImport)
                {
                    // Add a using statement if not already included
                    //using Microsoft.Practices.ServiceLocation;
                    content.Insert(0,"using Microsoft.Practices.ServiceLocation;");

                    File.WriteAllLines(file, content.ToArray(), Encoding.UTF8);
                }
            }
        }

        private bool ReplaceImports(List<string> data)
        {
            var key = "[Import]";
            var commentedKey = "//[Import]";
            var lineIndex = data.FindIndex(x => x.Contains(key) && !x.Contains(commentedKey));
            if (lineIndex != -1)
            {
                // Comment out thee import-row (to be able to keep track of all replacements)
                data[lineIndex] = @"//TODO MemFix Step 2 - Removed import statement, this comment can be removed after review";
                // Replace the next line with a GetInstance-Statement
                lineIndex++;
                var rowData = data[lineIndex];
                var rowValues = rowData.Split(' ', '\t');
                var interfaceName = rowValues.FirstOrDefault(x => x.StartsWith("I") && x.EndsWith("Service"));
                if (interfaceName != null)
                {
                    Debug.WriteLine(String.Format("Interface: {0}", interfaceName));
                    var name = rowValues.FirstOrDefault(x => !x.StartsWith("I") && x.EndsWith("Service"));
                    if (name == null)
                        name = interfaceName.Substring(1); // Use interface name instead
                    data[lineIndex] = String.Format("\t\tpublic {0} {1} {{ get {{ return ServiceLocator.Current.GetInstance<{2}>(); }} set; }}", interfaceName, name, interfaceName);  
                    _tempList.Add(String.Format("\tAdded: {0}", data[lineIndex]));
                    return true;
                }
                else
                {
                    Debug.Assert(interfaceName == null);
                    return false;
                }                
            }
            else
                return false;
        }        

        #endregion
    }
}
