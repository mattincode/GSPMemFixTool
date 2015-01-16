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

        #region Part 1
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
        #endregion Part 1

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
            var key = "//TODO MemFix Step 2 - Removed import statement, this comment can be removed after review";
            var lineIndex = data.FindIndex(x => x.Contains(key));
            if (lineIndex != -1)
            {
                data.RemoveAt(lineIndex);
                // Comment out thee import-row (to be able to keep track of all replacements)
                //data[lineIndex] = @"//TODO MemFix Step 2 - Removed import statement, this comment can be removed after review";
                _tempList.Add(String.Format(@"\Removed comment: {0}", data[lineIndex]));
                return true;
            }
            else
                return false;
        }        

        #endregion

        #region Part 3
        private ObservableCollection<string> _exportList = new ObservableCollection<string>();
        private bool _simulateExport;

        public bool SimulateExport
        {
            get { return _simulateExport; }
            set
            {
                _simulateExport = value;
                RaisePropertyChanged(() => SimulateExport);
            }
        }


        public ObservableCollection<string> ExportList
        {
            get { return _exportList; }
            set
            {
                _exportList = value;
                RaisePropertyChanged(() => ExportList);
            }
        }

        private int _numberOfExportUpdates;

        public int NumberOfExportUpdates
        {
            get { return _numberOfExportUpdates; }
            set
            {
                _numberOfExportUpdates = value;
                RaisePropertyChanged(() => NumberOfExportUpdates);
            }
        }

        public void ReplaceExports()
        {
            if (_riaClientFolderOk)
            {
                _tempList = new List<string>();
                ExportList.Clear();
                NumberOfExportUpdates = 0;
                // Process *.cs                
                var files = Directory.GetFiles(_riaClientPath, "*.cs", SearchOption.AllDirectories); // Directory.EnumerateFiles(_riaClientPath, "*.cs");
                ProcessFilesForExports(files);
                // Process *.xaml.cs
                files = Directory.GetFiles(_riaClientPath, "*.xaml.cs", SearchOption.AllDirectories); //Directory.EnumerateFiles(_riaClientPath, "*.xaml.cs"); 
                ProcessFilesForExports(files);
                ExportList = new ObservableCollection<string>(_tempList);
                NumberOfExportUpdates = _numberOfExportUpdates;
            }
        }

        private void ProcessFilesForExports(IEnumerable<string> files)
        {
            foreach (var file in files)
            {
                _tempList.Add(String.Format("Processing {0}", file));
                var content = File.ReadAllLines(file).ToList();
                var updatedRows = 0;
                while (ReplaceExports(content))
                {
                    updatedRows++;
                    _numberOfExportUpdates++;
                };

                // Only write file if any rows has been updated
                if (updatedRows > 0 && !SimulateExport)
                {
                    File.WriteAllLines(file, content.ToArray(), Encoding.UTF8);
                }
            }
        }

        private bool ReplaceExports(List<string> data)
        {
            var key = "[ExportAsView(";   // [ExportAsView(Constants.VIEW_PLANNINGUNIT, MenuName = "Planningunit detail")]
            var commentedKey = "//[ExportAsView(";
            var lineIndex = data.FindIndex(x => x.Contains(key) && !x.Contains(commentedKey));
            if (lineIndex != -1)
            {                
                var viewModelName = FindViewModelFromBinding(data);
                var viewName = FindViewNameFromExport(data);
                if (viewModelName.Length > 0 && viewName.Length > 0)
                {
                    // Replace the export with new attribute
                    data[lineIndex] = String.Format("\t[ExportAsNamedView({0}, {1})]", viewName, viewModelName);
                    _tempList.Add(String.Format("\tAdded: {0}", data[lineIndex]));
                    // Remove the old export bindingstatement
                    RemoveExportStatement(data);
                    // Add using statement for new attribute                    
                    data.Insert(0, "using Securitas.GSP.RiaClient.Framework.Messaging;");
                    RemoveJounceUsingsStatements(data);
                    return true;
                }
                else
                {
                    _tempList.Add(String.Format("\t# NOT Added: {0}", data[lineIndex]));
                    Debug.WriteLine(String.Format("\t# NOT Added: {0}", data[lineIndex]));
//                    Debug.Assert(viewModelName.Length > 0);
                    return false;
                }
            }
            else
                return false;   // No export found!
        }

        private void RemoveJounceUsingsStatements(List<string> data)
        {
            RemoveRowFromKey(data, @"using Jounce.Core.View");
            RemoveRowFromKey(data, @"using Jounce.Core.ViewModel");
        }

        private void RemoveRowFromKey(List<string> data, string keyToFind)
        {
            var lineIndex = data.FindIndex(x => x.Contains(keyToFind));
            if (lineIndex != NotFound)
            {
                data.RemoveAt(lineIndex);
            }
        }

        /// <summary>
        /// Find viewmodel name from Export binding
        /// get { return ViewModelRoute.Create(Constants.VIEWMODEL_PLANNINGUNIT, Constants.VIEW_PLANNINGUNIT); }
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private string FindViewModelFromBinding(List<string> data)
        {
            var viewModelName = "";
            string keyToFind = "[Export]";
            var commentedKey = "//[Export]";
            var lineIndex = data.FindIndex(x => x.Contains(keyToFind) && !x.Contains(commentedKey));
            if (lineIndex != NotFound)
            {
                var bindingStatement = ".Create(";
                var bindingStatementPos = data.FindIndex(lineIndex, x => x.Contains(bindingStatement));
                if (bindingStatementPos != NotFound)
                {
                    var pos = data[bindingStatementPos].IndexOf(bindingStatement);                    
                    if (pos > 0)
                    {                        
                        var startPos = pos + bindingStatement.Length;
                        var commaPos = data[bindingStatementPos].IndexOf(',');
                        var length = commaPos - startPos;
                        if (length > 0)
                        {
                            viewModelName = data[bindingStatementPos].Substring(startPos, length);
                        }
                    }
                }
                
            }
            return viewModelName;
        }

        // [ExportAsNamedView(Constants.VIEW_EMPLOYEE, Constants.VIEWMODEL_EMPLOYEE)] 
        // replaces
        // [ExportAsView(Constants.VIEW_EMPLOYEE, MenuName = "Planningunit detail")]
        private string FindViewNameFromExport(List<string> data)
        {
            string viewName = "";
            var key = "[ExportAsView(";   
            var commentedKey = "//[ExportAsView(";
            var lineIndex = data.FindIndex(x => x.Contains(key) && !x.Contains(commentedKey));
            if (lineIndex != NotFound)
            {
                var firstParenthesisPos = data[lineIndex].IndexOf('(');
                var endPos = data[lineIndex].IndexOf(',', firstParenthesisPos);
                if (endPos == NotFound)
                {
                    endPos = data[lineIndex].IndexOf(')', firstParenthesisPos);
                }
                var length = endPos - firstParenthesisPos - 1;
                if (length > 0)
                {
                    viewName = data[lineIndex].Substring(firstParenthesisPos + 1, length);
                }

            }
            return viewName;
        }


        private const int NotFound = -1;
        private void RemoveExportStatement(List<string> data)
        {
            string keyToFind = "[Export]";
            var commentedKey = "//[Export]";
            var lineIndex = data.FindIndex(x => x.Contains(keyToFind) && !x.Contains(commentedKey));
            if (lineIndex != NotFound)
            {                
                RemoveCodeBlock(data, lineIndex + 2);
                data.RemoveAt(lineIndex + 1);   // Remove the code block presequel
                data.RemoveAt(lineIndex);       // Remove the export statement
            }
        }

        /// <summary>
        /// Removes a block of code starting with "{" and ending with "}"
        /// </summary>
        /// <param name="data"></param>
        /// <param name="blockStartPos"></param>
        private void RemoveCodeBlock(List<string> data, int blockStartPos)
        {            
            var length = data.Count();
            int pos = blockStartPos;
            // Verify that we indeed have a start block
            if (data[pos].Contains('{'))
            {
                while(pos < length && (!data[pos].Contains('}') || data[pos].Contains('{')))
                {
                    pos++;
                }
                pos++; // Add one since we want to remove the row we are positioned on as well

                // Remove block
                if (pos != length)
                {
                    var rowsToRemove = pos - blockStartPos; 
                    for (int i = 0; i < rowsToRemove; i++)
                    {
                        data.RemoveAt(blockStartPos);
                    }
                }
            }
        }
        #endregion

        #region Part 4
        private ObservableCollection<string> _exportVmList = new ObservableCollection<string>();
        private bool _simulateVmExport;

        public bool SimulateVmExport
        {
            get { return _simulateVmExport; }
            set
            {
                _simulateVmExport = value;
                RaisePropertyChanged(() => SimulateVmExport);
            }
        }


        public ObservableCollection<string> ExportVmList
        {
            get { return _exportVmList; }
            set
            {
                _exportVmList = value;
                RaisePropertyChanged(() => ExportVmList);
            }
        }

        private int _numberOfVmExportUpdates;

        public int NumberOfVmExportUpdates
        {
            get { return _numberOfVmExportUpdates; }
            set
            {
                _numberOfVmExportUpdates = value;
                RaisePropertyChanged(() => NumberOfVmExportUpdates);
            }
        }        

        public void ReplaceVmExports()
        {
            if (_riaClientFolderOk)
            {
                _tempList = new List<string>();
                ExportVmList.Clear();
                NumberOfVmExportUpdates = 0;
                // Process *.cs                
                var files = Directory.GetFiles(_riaClientPath, "*.cs", SearchOption.AllDirectories); // Directory.EnumerateFiles(_riaClientPath, "*.cs");
                ProcessFilesForVmExports(files);
                ExportVmList = new ObservableCollection<string>(_tempList);
                NumberOfVmExportUpdates = _numberOfVmExportUpdates;
            }
        }

        private void ProcessFilesForVmExports(IEnumerable<string> files)
        {
            foreach (var file in files)
            {
                _tempList.Add(String.Format("Processing {0}", file));
                var content = File.ReadAllLines(file).ToList();
                var updatedRows = 0;
                while (ReplaceVmExports(content))
                {
                    updatedRows++;
                    _numberOfVmExportUpdates++;
                };

                // Only write file if any rows has been updated
                if (updatedRows > 0 && !SimulateVmExport)
                {
                    File.WriteAllLines(file, content.ToArray(), Encoding.UTF8);
                }
            }
        }

        private bool ReplaceVmExports(List<string> data)
        {
            var key = "[ExportAsViewModel(";  
            var commentedKey = "//[ExportAsViewModel(";
            var lineIndex = data.FindIndex(x => x.Contains(key) && !x.Contains(commentedKey));
            if (lineIndex != -1)
            {
                var viewModelName = FindViewNameFromVmExport(data);                
                if (viewModelName.Length > 0)
                {
                    // Replace the export with new attribute
                    data[lineIndex] = String.Format("\t[ExportAsNamedViewModel({0})]", viewModelName);
                    _tempList.Add(String.Format("\tAdded: {0}", data[lineIndex]));
                    // Add using statement for new attribute                    
                    data.Insert(0, "using Securitas.GSP.RiaClient.Framework.Messaging;");
                    RemoveRowFromKey(data, @"using Jounce.Core.ViewModel");
                    return true;
                }
                else
                {
                    _tempList.Add(String.Format("\t# NOT Added: {0}", data[lineIndex]));
                    Debug.WriteLine(String.Format("\t# NOT Added: {0}", data[lineIndex]));
                    //                    Debug.Assert(viewModelName.Length > 0);
                    return false;
                }
            }
            else
                return false;   // No export found!
        }

        // [ExportAsNamedView(Constants.VIEW_EMPLOYEE, Constants.VIEWMODEL_EMPLOYEE)] 
        // replaces
        // [ExportAsView(Constants.VIEW_EMPLOYEE, MenuName = "Planningunit detail")]
        private string FindViewNameFromVmExport(List<string> data)
        {
            string viewName = "";
            var key = "[ExportAsViewModel(";
            var commentedKey = "//[ExportAsViewModel(";
            var lineIndex = data.FindIndex(x => x.Contains(key) && !x.Contains(commentedKey));
            if (lineIndex != NotFound)
            {
                var firstParenthesisPos = data[lineIndex].IndexOf('(');
                var endPos = data[lineIndex].IndexOf(',', firstParenthesisPos);
                if (endPos == NotFound)
                {
                    endPos = data[lineIndex].IndexOf(')', firstParenthesisPos);
                }
                var length = endPos - firstParenthesisPos - 1;
                if (length > 0)
                {
                    viewName = data[lineIndex].Substring(firstParenthesisPos + 1, length);
                }

            }
            return viewName;
        }
        #endregion Part 4

    }
}
