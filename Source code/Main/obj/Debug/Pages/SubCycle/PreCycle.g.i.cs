﻿#pragma checksum "..\..\..\..\Pages\SubCycle\PreCycle.xaml" "{8829d00f-11b8-4213-878b-770e8597ac16}" "94EADC7EF7CF84CC51C895871FC732762230ED7FEE1080A3568A1E5B6195CEBE"
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
    /// PreCycle
    /// </summary>
    public partial class PreCycle : System.Windows.Controls.UserControl, System.Windows.Markup.IComponentConnector {
        
        
        #line 11 "..\..\..\..\Pages\SubCycle\PreCycle.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Grid FormLayoutGrid;
        
        #line default
        #line hidden
        
        
        #line 29 "..\..\..\..\Pages\SubCycle\PreCycle.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox tbJobNumber;
        
        #line default
        #line hidden
        
        
        #line 32 "..\..\..\..\Pages\SubCycle\PreCycle.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox tbBatchNumber;
        
        #line default
        #line hidden
        
        
        #line 35 "..\..\..\..\Pages\SubCycle\PreCycle.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox tbRecipeName;
        
        #line default
        #line hidden
        
        
        #line 36 "..\..\..\..\Pages\SubCycle\PreCycle.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ComboBox cbxRecipeName;
        
        #line default
        #line hidden
        
        
        #line 37 "..\..\..\..\Pages\SubCycle\PreCycle.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Primitives.ToggleButton tgBarcodeOption;
        
        #line default
        #line hidden
        
        
        #line 39 "..\..\..\..\Pages\SubCycle\PreCycle.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock lbFinalWeight;
        
        #line default
        #line hidden
        
        
        #line 40 "..\..\..\..\Pages\SubCycle\PreCycle.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox tbFinalWeight;
        
        #line default
        #line hidden
        
        
        #line 42 "..\..\..\..\Pages\SubCycle\PreCycle.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button tbOk;
        
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
            System.Uri resourceLocater = new System.Uri("/MixingApplication;component/pages/subcycle/precycle.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\..\Pages\SubCycle\PreCycle.xaml"
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
            this.FormLayoutGrid = ((System.Windows.Controls.Grid)(target));
            return;
            case 2:
            this.tbJobNumber = ((System.Windows.Controls.TextBox)(target));
            
            #line 29 "..\..\..\..\Pages\SubCycle\PreCycle.xaml"
            this.tbJobNumber.LostFocus += new System.Windows.RoutedEventHandler(this.tbJobBatchNumber_LostFocus);
            
            #line default
            #line hidden
            
            #line 29 "..\..\..\..\Pages\SubCycle\PreCycle.xaml"
            this.tbJobNumber.GotFocus += new System.Windows.RoutedEventHandler(this.ShowKeyBoard);
            
            #line default
            #line hidden
            
            #line 29 "..\..\..\..\Pages\SubCycle\PreCycle.xaml"
            this.tbJobNumber.PreviewKeyDown += new System.Windows.Input.KeyEventHandler(this.HideKeyBoardIfEnter);
            
            #line default
            #line hidden
            return;
            case 3:
            this.tbBatchNumber = ((System.Windows.Controls.TextBox)(target));
            
            #line 32 "..\..\..\..\Pages\SubCycle\PreCycle.xaml"
            this.tbBatchNumber.LostFocus += new System.Windows.RoutedEventHandler(this.tbJobBatchNumber_LostFocus);
            
            #line default
            #line hidden
            
            #line 32 "..\..\..\..\Pages\SubCycle\PreCycle.xaml"
            this.tbBatchNumber.GotFocus += new System.Windows.RoutedEventHandler(this.ShowKeyBoard);
            
            #line default
            #line hidden
            
            #line 32 "..\..\..\..\Pages\SubCycle\PreCycle.xaml"
            this.tbBatchNumber.PreviewKeyDown += new System.Windows.Input.KeyEventHandler(this.HideKeyBoardIfEnter);
            
            #line default
            #line hidden
            return;
            case 4:
            this.tbRecipeName = ((System.Windows.Controls.TextBox)(target));
            
            #line 35 "..\..\..\..\Pages\SubCycle\PreCycle.xaml"
            this.tbRecipeName.LostFocus += new System.Windows.RoutedEventHandler(this.tbRecipeName_LostFocus);
            
            #line default
            #line hidden
            
            #line 35 "..\..\..\..\Pages\SubCycle\PreCycle.xaml"
            this.tbRecipeName.GotFocus += new System.Windows.RoutedEventHandler(this.ShowKeyBoard);
            
            #line default
            #line hidden
            
            #line 35 "..\..\..\..\Pages\SubCycle\PreCycle.xaml"
            this.tbRecipeName.PreviewKeyDown += new System.Windows.Input.KeyEventHandler(this.HideKeyBoardIfEnter);
            
            #line default
            #line hidden
            return;
            case 5:
            this.cbxRecipeName = ((System.Windows.Controls.ComboBox)(target));
            
            #line 36 "..\..\..\..\Pages\SubCycle\PreCycle.xaml"
            this.cbxRecipeName.SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(this.cbxProgramName_SelectionChanged);
            
            #line default
            #line hidden
            return;
            case 6:
            this.tgBarcodeOption = ((System.Windows.Controls.Primitives.ToggleButton)(target));
            
            #line 37 "..\..\..\..\Pages\SubCycle\PreCycle.xaml"
            this.tgBarcodeOption.Click += new System.Windows.RoutedEventHandler(this.tgBarcodeOption_Click);
            
            #line default
            #line hidden
            return;
            case 7:
            this.lbFinalWeight = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 8:
            this.tbFinalWeight = ((System.Windows.Controls.TextBox)(target));
            
            #line 40 "..\..\..\..\Pages\SubCycle\PreCycle.xaml"
            this.tbFinalWeight.GotFocus += new System.Windows.RoutedEventHandler(this.ShowKeyBoard);
            
            #line default
            #line hidden
            
            #line 40 "..\..\..\..\Pages\SubCycle\PreCycle.xaml"
            this.tbFinalWeight.LostFocus += new System.Windows.RoutedEventHandler(this.HideKeyBoard);
            
            #line default
            #line hidden
            
            #line 40 "..\..\..\..\Pages\SubCycle\PreCycle.xaml"
            this.tbFinalWeight.PreviewKeyDown += new System.Windows.Input.KeyEventHandler(this.HideKeyBoardIfEnter);
            
            #line default
            #line hidden
            return;
            case 9:
            this.tbOk = ((System.Windows.Controls.Button)(target));
            
            #line 42 "..\..\..\..\Pages\SubCycle\PreCycle.xaml"
            this.tbOk.Click += new System.Windows.RoutedEventHandler(this.FxOK);
            
            #line default
            #line hidden
            return;
            case 10:
            
            #line 43 "..\..\..\..\Pages\SubCycle\PreCycle.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.FxAnnuler);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}

