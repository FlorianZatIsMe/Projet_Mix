﻿#pragma checksum "..\..\..\Pages\AuditTrail_old.xaml" "{8829d00f-11b8-4213-878b-770e8597ac16}" "AE9212A14E5ABD1A5556313AA5168E764CA9636AD07A2C2B0694E33C733918F0"
//------------------------------------------------------------------------------
// <auto-generated>
//     Ce code a été généré par un outil.
//     Version du runtime :4.0.30319.42000
//
//     Les modifications apportées à ce fichier peuvent provoquer un comportement incorrect et seront perdues si
//     le code est régénéré.
// </auto-generated>
//------------------------------------------------------------------------------

using Main.Pages;
using Main.Properties;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace Main.Pages {
    
    
    /// <summary>
    /// AuditTrailOld
    /// </summary>
    public partial class AuditTrailOld : System.Windows.Controls.Page, System.Windows.Markup.IComponentConnector {
        
        
        #line 14 "..\..\..\Pages\AuditTrail_old.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Grid grid;
        
        #line default
        #line hidden
        
        
        #line 26 "..\..\..\Pages\AuditTrail_old.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.DatePicker dpDateBefore;
        
        #line default
        #line hidden
        
        
        #line 32 "..\..\..\Pages\AuditTrail_old.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox tbTimeBefore;
        
        #line default
        #line hidden
        
        
        #line 47 "..\..\..\Pages\AuditTrail_old.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.DatePicker dpDateAfter;
        
        #line default
        #line hidden
        
        
        #line 53 "..\..\..\Pages\AuditTrail_old.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox tbTimeAfter;
        
        #line default
        #line hidden
        
        
        #line 68 "..\..\..\Pages\AuditTrail_old.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.CheckBox cbEvent;
        
        #line default
        #line hidden
        
        
        #line 74 "..\..\..\Pages\AuditTrail_old.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.CheckBox cbAlarm;
        
        #line default
        #line hidden
        
        
        #line 81 "..\..\..\Pages\AuditTrail_old.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.CheckBox cbWarning;
        
        #line default
        #line hidden
        
        
        #line 97 "..\..\..\Pages\AuditTrail_old.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.DataGrid dataGridAuditTrail;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/MixingApplication;component/pages/audittrail_old.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\Pages\AuditTrail_old.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            this.grid = ((System.Windows.Controls.Grid)(target));
            return;
            case 2:
            this.dpDateBefore = ((System.Windows.Controls.DatePicker)(target));
            
            #line 30 "..\..\..\Pages\AuditTrail_old.xaml"
            this.dpDateBefore.LayoutUpdated += new System.EventHandler(this.DpDateBefore_LayoutUpdated);
            
            #line default
            #line hidden
            
            #line 31 "..\..\..\Pages\AuditTrail_old.xaml"
            this.dpDateBefore.PreviewMouseDown += new System.Windows.Input.MouseButtonEventHandler(this.DpDateBefore_PreviewMouseDown);
            
            #line default
            #line hidden
            return;
            case 3:
            this.tbTimeBefore = ((System.Windows.Controls.TextBox)(target));
            
            #line 37 "..\..\..\Pages\AuditTrail_old.xaml"
            this.tbTimeBefore.PreviewMouseLeftButtonDown += new System.Windows.Input.MouseButtonEventHandler(this.TbTimeBefore_PreviewMouseLeftButtonDown);
            
            #line default
            #line hidden
            
            #line 38 "..\..\..\Pages\AuditTrail_old.xaml"
            this.tbTimeBefore.LayoutUpdated += new System.EventHandler(this.TbTimeBefore_LayoutUpdated);
            
            #line default
            #line hidden
            
            #line 39 "..\..\..\Pages\AuditTrail_old.xaml"
            this.tbTimeBefore.PreviewKeyDown += new System.Windows.Input.KeyEventHandler(this.TbTimeBefore_PreviewKeyDown);
            
            #line default
            #line hidden
            
            #line 40 "..\..\..\Pages\AuditTrail_old.xaml"
            this.tbTimeBefore.GotFocus += new System.Windows.RoutedEventHandler(this.ShowKeyBoard);
            
            #line default
            #line hidden
            
            #line 41 "..\..\..\Pages\AuditTrail_old.xaml"
            this.tbTimeBefore.LostFocus += new System.Windows.RoutedEventHandler(this.HideKeyBoard);
            
            #line default
            #line hidden
            return;
            case 4:
            this.dpDateAfter = ((System.Windows.Controls.DatePicker)(target));
            
            #line 51 "..\..\..\Pages\AuditTrail_old.xaml"
            this.dpDateAfter.PreviewMouseDown += new System.Windows.Input.MouseButtonEventHandler(this.DpDateAfter_PreviewMouseDown);
            
            #line default
            #line hidden
            
            #line 52 "..\..\..\Pages\AuditTrail_old.xaml"
            this.dpDateAfter.LayoutUpdated += new System.EventHandler(this.DpDateAfter_LayoutUpdated);
            
            #line default
            #line hidden
            return;
            case 5:
            this.tbTimeAfter = ((System.Windows.Controls.TextBox)(target));
            
            #line 58 "..\..\..\Pages\AuditTrail_old.xaml"
            this.tbTimeAfter.PreviewMouseLeftButtonDown += new System.Windows.Input.MouseButtonEventHandler(this.TbTimeAfter_PreviewMouseLeftButtonDown);
            
            #line default
            #line hidden
            
            #line 59 "..\..\..\Pages\AuditTrail_old.xaml"
            this.tbTimeAfter.LayoutUpdated += new System.EventHandler(this.TbTimeAfter_LayoutUpdated);
            
            #line default
            #line hidden
            
            #line 60 "..\..\..\Pages\AuditTrail_old.xaml"
            this.tbTimeAfter.PreviewKeyDown += new System.Windows.Input.KeyEventHandler(this.TbTimeAfter_PreviewKeyDown);
            
            #line default
            #line hidden
            
            #line 61 "..\..\..\Pages\AuditTrail_old.xaml"
            this.tbTimeAfter.GotFocus += new System.Windows.RoutedEventHandler(this.ShowKeyBoard);
            
            #line default
            #line hidden
            
            #line 62 "..\..\..\Pages\AuditTrail_old.xaml"
            this.tbTimeAfter.LostFocus += new System.Windows.RoutedEventHandler(this.HideKeyBoard);
            
            #line default
            #line hidden
            return;
            case 6:
            this.cbEvent = ((System.Windows.Controls.CheckBox)(target));
            return;
            case 7:
            this.cbAlarm = ((System.Windows.Controls.CheckBox)(target));
            return;
            case 8:
            this.cbWarning = ((System.Windows.Controls.CheckBox)(target));
            return;
            case 9:
            
            #line 95 "..\..\..\Pages\AuditTrail_old.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.ButtonFilter_Click);
            
            #line default
            #line hidden
            return;
            case 10:
            this.dataGridAuditTrail = ((System.Windows.Controls.DataGrid)(target));
            
            #line 99 "..\..\..\Pages\AuditTrail_old.xaml"
            this.dataGridAuditTrail.Loaded += new System.Windows.RoutedEventHandler(this.LoadAuditTrail);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}

