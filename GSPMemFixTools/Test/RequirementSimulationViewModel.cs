using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Jounce.Core.Command;
using Jounce.Core.ViewModel;
using Jounce.Framework;
using Securitas.GSP.RiaClient.Contracts;
using Securitas.GSP.RiaClient.Framework.Helpers;
using Securitas.GSP.RiaClient.Model;
using Securitas.GSP.RiaClient.Framework.Services;
using Securitas.GSP.RiaClient.ViewModels.Helpers;
using Localization = Securitas.GSP.RiaClient.UIResources;

namespace Securitas.GSP.RiaClient.ViewModels
{
    [ExportAsViewModel("RequirementSimulationViewModel")]
    public class RequirementSimulationViewModel : GspBaseViewModel
    {
        [Import]
        public IAirportRequirementService AirportRequirementService { get; set; }
        [Import]
        public IAirportConfigurationService AirportConfigurationService { get; set; }

        private Point _panOffset;
        private Size _zoom;

        public Point PanOffset
        {
            get
            {
                return _panOffset;
            }
            set
            {
                if (this._panOffset != value)
                {
                    this._panOffset = value;
                    RaisePropertyChanged("PanOffset");
                }
            }
        }

        public Size Zoom
        {
            get
            {
                return _zoom;
            }
            set
            {
                if (this._zoom != value)
                {
                    this._zoom = value;
                    RaisePropertyChanged("Zoom");
                }
            }
        }

        private List<AirportRequirement> _data = new List<AirportRequirement>();
        public List<AirportRequirement> Data
        {
            get
            {
                return _data;
            }
            set
            {
                if (this._data != value)
                {
                    this._data = value;
                    RaisePropertyChanged("Data");
                }
            }
        }

        private List<AirportConfiguration> _airports = new List<AirportConfiguration>();
        public List<AirportConfiguration> Airports
        {
            get
            {
                return _airports;
            }
            set
            {
                if (this._airports != value)
                {
                    this._airports = value;
                    RaisePropertyChanged("Airports");
                }
            }
        }

        private List<AirportRequirementType> _types = new List<AirportRequirementType>();
        public List<AirportRequirementType> Types
        {
            get
            {
                return _types;
            }
            set
            {
                if (this._types != value)
                {
                    this._types = value;
                    RaisePropertyChanged("Types");
                }
            }
        }

        private List<AirportRequirementFileType> _fileTypes = new List<AirportRequirementFileType>();
        public List<AirportRequirementFileType> FileTypes
        {
            get
            {
                return _fileTypes;
            }
            set
            {
                if (this._fileTypes != value)
                {
                    this._fileTypes = value;
                    RaisePropertyChanged("FileTypes");
                }
            }
        }

        private DateTime _selectedFromDate;
        public DateTime SelectedFromDate
        {
            get
            {
                return _selectedFromDate;
            }
            set
            {
                if (this._selectedFromDate != value)
                {
                    this._selectedFromDate = value;
                    RaisePropertyChanged("SelectedFromDate");
                }
            }
        }

        private AirportRequirementResolution _selectedResolution;
        public AirportRequirementResolution SelectedResolution
        {
            get
            {
                return _selectedResolution;
            }
            set
            {
                if (this._selectedResolution != value)
                {
                    this._selectedResolution = value;
                    RaisePropertyChanged("SelectedResolution");
                }
            }
        }

        private List<AirportRequirementResolution> _resolution = new List<AirportRequirementResolution>();
        public List<AirportRequirementResolution> Resolution
        {
            get
            {
                return _resolution;
            }
            set
            {
                if (this._resolution != value)
                {
                    this._resolution = value;
                    RaisePropertyChanged("Resolution");
                }
            }
        }

        public RequirementSimulationViewModel()
        {
            InitData();
        }

        protected override void InitializeVm()
        {
            base.InitializeVm();
            LoadInitialData();
        }

        private void InitData()
        {
            Header = Localization.Resources.RequirementSimulationViewModel_Header;
            this.Zoom = new Size(3, 1);
            this.PanOffset = new Point(-10000, 0);
        }

        private void LoadInitialData()
        {
            AirportConfigurationService.GetAllConfigurations((res, err) =>
            {
                if (err.HasErrors)
                    return;
                ThreadHelper.ExecuteOnUI(() =>
                {
                    Airports = res.ToList();
                });
            });

            AirportRequirementService.GetAirportRequirementTypes((res, err) =>
            {
                if (err.HasErrors)
                    return;
                ThreadHelper.ExecuteOnUI(() =>
                {
                    Types = res.ToList();
                });
            });

            AirportRequirementService.GetAirportRequirementFileTypes((res, err) =>
            {
                if (err.HasErrors)
                    return;
                ThreadHelper.ExecuteOnUI(() =>
                {
                    FileTypes = res.ToList();
                });
            });

            Resolution = new List<AirportRequirementResolution>();
            Resolution.Add(new AirportRequirementResolution() { ResolutionEnum = StaffingPrognosisResolutionEnum.HalfHour, Text = Localization.Resources.AirportRequirementView_ResolutionHalfHour });
            Resolution.Add(new AirportRequirementResolution() { ResolutionEnum = StaffingPrognosisResolutionEnum.Day, Text = Localization.Resources.AirportRequirementView_ResolutionDay });
            Resolution.Add(new AirportRequirementResolution() { ResolutionEnum = StaffingPrognosisResolutionEnum.Week, Text = Localization.Resources.AirportRequirementView_ResolutionWeek });
            Resolution.Add(new AirportRequirementResolution() { ResolutionEnum = StaffingPrognosisResolutionEnum.Month, Text = Localization.Resources.AirportRequirementView_ResolutionMonth });
            //Resolution.Add(new AirportRequirementResolution() { ResolutionEnum = StaffingPrognosisResolutionEnum.Year, Text = Localization.Resources.AirportRequirementView_ResolutionYear });
            // Set inital date and resolution
            SelectedFromDate = DateTime.Now;
            SelectedResolution = Resolution.First();
        }

        //public void ImportFile(byte[] file, string fileEnding, AirportConfiguration airport, AirportRequirementType type, AirportRequirementFileType fileType)
        //{
        //    var data = new AirportRequirementImportData {AirportConfiguration = airport, File = file, FileType = fileType, FileEnding = fileEnding};
        //    AirportRequirementService.ImportRequirementsFromFile(data, (res, err) =>
        //    {
        //        if (err.HasErrors)
        //        {
        //            return;
        //        }
        //        ThreadHelper.ExecuteOnUI(() =>
        //        {
        //            Data = res.ToList();
        //            var firstItem = res.FirstOrDefault();
        //            if (firstItem != null)
        //            {
        //                SelectedFromDate = firstItem.Occasion;
        //                SelectedResolution = GetResolutionFromPeriod(firstItem.Period);
        //            }
        //        });
        //    });
        //}

        //public void UpdateData(AirportConfiguration airportConfiguration, DateTime fromDate, AirportRequirementResolution resolution)
        //{

        //    AirportRequirementService.GetAirportRequirements(airportConfiguration.AirportConfigurationId, fromDate, GetToDateFromResolution(fromDate, resolution), (res, err) =>
        //    {
        //        if (err.HasErrors)
        //            return;

        //        ThreadHelper.ExecuteOnUI(() =>
        //        {
        //            Data = res.ToList();
        //        });
        //    });
        //}

        // Convert resolution to a suitable span to show
        private DateTime GetToDateFromResolution(DateTime fromDate, AirportRequirementResolution resolution)
        {
            switch (resolution.ResolutionEnum)
            {
                case StaffingPrognosisResolutionEnum.Day:
                    return fromDate.AddDays(31); // Show 1 month as days
                case StaffingPrognosisResolutionEnum.Week:
                    return fromDate.AddDays(365); // Show 1 year as weeks
                case StaffingPrognosisResolutionEnum.Month:
                    return fromDate.AddDays(365 * 3); // Show 3 years as months
                //case StaffingPrognosisResolutionEnum.Year:
                //	return fromDate.AddDays(365 * 5); // Show 5 years
                default:
                    return fromDate.AddDays(7); // Show 7 days as halfhour values
            }
        }

        private AirportRequirementResolution GetResolutionFromPeriod(int period)
        {
            switch (period)
            {
                case 1:
                    return Resolution.First(x => x.ResolutionEnum == StaffingPrognosisResolutionEnum.Day);
                case 2:
                    return Resolution.First(x => x.ResolutionEnum == StaffingPrognosisResolutionEnum.Week);
                case 3:
                    return Resolution.First(x => x.ResolutionEnum == StaffingPrognosisResolutionEnum.Month);
                //case 4:
                //	return Resolution.First(x => x.ResolutionEnum == StaffingPrognosisResolutionEnum.Year);
                default:
                    return Resolution.First(x => x.ResolutionEnum == StaffingPrognosisResolutionEnum.HalfHour);
            }
        }
    }

    //public enum AirportRequirementResolutionEnum
    //{
    //	HalfHour = 0,
    //	Day = 1,
    //	Week = 2,
    //	Month = 3,
    //	Year = 4
    //}

    //public class AirportRequirementResolution
    //{
    //	public AirportRequirementResolutionEnum ResolutionEnum { get; set; }
    //	public string Text { get; set; }
    //}

}
