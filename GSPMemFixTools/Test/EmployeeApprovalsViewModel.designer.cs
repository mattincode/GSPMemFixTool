﻿using System;
using System.Diagnostics;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Securitas.GSP.RiaClient.ViewModels
{
    public partial class EmployeeApprovalsViewModel
    {
        [Conditional("DESIGN")]
        private void _WireDesignerData()
        {
            if (!InDesigner) return;
        }
    }
}
