﻿#pragma checksum "..\..\..\Pages\ArchivingOld.xaml" "{8829d00f-11b8-4213-878b-770e8597ac16}" "DFEB587167DF2C79625ED306991BC89D723AEF8989AF40FBDB7063DC1F85C1B9"
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
    /// ArchivingOld
    /// </summary>
    public partial class ArchivingOld : System.Windows.Controls.Page, System.Windows.Markup.IComponentConnector {
        
        
        #line 37 "..\..\..\Pages\ArchivingOld.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.DatePicker dpLastRecord;
        
        #line default
        #line hidden
        
        
        #line 51 "..\..\..\Pages\ArchivingOld.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ListBox lbArchives;
        
        #line default
        #line hidden
        
        
        #line 60 "..\..\..\Pages\ArchivingOld.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.WrapPanel wpStatus;
        
        #line default
        #line hidden
        
        
        #line 61 "..\..\..\Pages\ArchivingOld.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock labelStatus;
        
        #line default
        #line hidden
        
        
        #line 66 "..\..\..\Pages\ArchivingOld.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ProgressBar progressBar;
        
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
            System.Uri resourceLocater = new System.Uri("/MixingApplication;component/pages/archivingold.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\Pages\ArchivingOld.xaml"
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
            
            #line 30 "..\..\..\Pages\ArchivingOld.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.Archive_Click);
            
            #line default
            #line hidden
            return;
            case 2:
            this.dpLastRecord = ((System.Windows.Controls.DatePicker)(target));
            return;
            case 3:
            
            #line 50 "..\..\..\Pages\ArchivingOld.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.Restore_Click);
            
            #line default
            #line hidden
            return;
            case 4:
            this.lbArchives = ((System.Windows.Controls.ListBox)(target));
            return;
            case 5:
            this.wpStatus = ((System.Windows.Controls.WrapPanel)(target));
            return;
            case 6:
            this.labelStatus = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 7:
            this.progressBar = ((System.Windows.Controls.ProgressBar)(target));
            
            #line 66 "..\..\..\Pages\ArchivingOld.xaml"
            this.progressBar.Loaded += new System.Windows.RoutedEventHandler(this.progressBar_Loaded);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}
