﻿#pragma checksum "..\..\..\..\Pages\SubCycle\CycleWeightOld.xaml" "{8829d00f-11b8-4213-878b-770e8597ac16}" "05C850C4331985B32F9F972FD91CCE0C9D4225E9A11089ACC56E633C40C3C810"
//------------------------------------------------------------------------------
// <auto-generated>
//     Ce code a été généré par un outil.
//     Version du runtime :4.0.30319.42000
//
//     Les modifications apportées à ce fichier peuvent provoquer un comportement incorrect et seront perdues si
//     le code est régénéré.
// </auto-generated>
//------------------------------------------------------------------------------

using Main.Pages.SubCycle;
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


namespace Main.Pages.SubCycle {
    
    
    /// <summary>
    /// CycleWeightOld
    /// </summary>
    public partial class CycleWeightOld : System.Windows.Controls.Page, System.Windows.Markup.IComponentConnector {
        
        
        #line 15 "..\..\..\..\Pages\SubCycle\CycleWeightOld.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock labelMessage;
        
        #line default
        #line hidden
        
        
        #line 20 "..\..\..\..\Pages\SubCycle\CycleWeightOld.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock labelSetpoint;
        
        #line default
        #line hidden
        
        
        #line 26 "..\..\..\..\Pages\SubCycle\CycleWeightOld.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock labelWeight;
        
        #line default
        #line hidden
        
        
        #line 32 "..\..\..\..\Pages\SubCycle\CycleWeightOld.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox tbScan;
        
        #line default
        #line hidden
        
        
        #line 41 "..\..\..\..\Pages\SubCycle\CycleWeightOld.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox tbWeight;
        
        #line default
        #line hidden
        
        
        #line 52 "..\..\..\..\Pages\SubCycle\CycleWeightOld.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button btNext;
        
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
            System.Uri resourceLocater = new System.Uri("/MixingApplication;component/pages/subcycle/cycleweightold.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\..\Pages\SubCycle\CycleWeightOld.xaml"
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
            this.labelMessage = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 2:
            this.labelSetpoint = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 3:
            this.labelWeight = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 4:
            this.tbScan = ((System.Windows.Controls.TextBox)(target));
            
            #line 38 "..\..\..\..\Pages\SubCycle\CycleWeightOld.xaml"
            this.tbScan.LostFocus += new System.Windows.RoutedEventHandler(this.tbScan_LostFocus);
            
            #line default
            #line hidden
            
            #line 39 "..\..\..\..\Pages\SubCycle\CycleWeightOld.xaml"
            this.tbScan.KeyDown += new System.Windows.Input.KeyEventHandler(this.tbScan_KeyDown);
            
            #line default
            #line hidden
            
            #line 40 "..\..\..\..\Pages\SubCycle\CycleWeightOld.xaml"
            this.tbScan.IsVisibleChanged += new System.Windows.DependencyPropertyChangedEventHandler(this.tbScan_IsVisibleChanged);
            
            #line default
            #line hidden
            return;
            case 5:
            this.tbWeight = ((System.Windows.Controls.TextBox)(target));
            
            #line 47 "..\..\..\..\Pages\SubCycle\CycleWeightOld.xaml"
            this.tbWeight.GotFocus += new System.Windows.RoutedEventHandler(this.ShowKeyBoard);
            
            #line default
            #line hidden
            
            #line 48 "..\..\..\..\Pages\SubCycle\CycleWeightOld.xaml"
            this.tbWeight.LostFocus += new System.Windows.RoutedEventHandler(this.HideKeyBoard);
            
            #line default
            #line hidden
            
            #line 49 "..\..\..\..\Pages\SubCycle\CycleWeightOld.xaml"
            this.tbWeight.PreviewKeyDown += new System.Windows.Input.KeyEventHandler(this.HideKeyBoardIfEnter);
            
            #line default
            #line hidden
            return;
            case 6:
            this.btNext = ((System.Windows.Controls.Button)(target));
            
            #line 58 "..\..\..\..\Pages\SubCycle\CycleWeightOld.xaml"
            this.btNext.Click += new System.Windows.RoutedEventHandler(this.Button_Click);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}

