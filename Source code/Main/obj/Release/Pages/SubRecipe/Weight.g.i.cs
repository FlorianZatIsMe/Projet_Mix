﻿#pragma checksum "..\..\..\..\Pages\SubRecipe\Weight.xaml" "{8829d00f-11b8-4213-878b-770e8597ac16}" "5D7FC491F5A374DC8F5832B2310647D786293B6DC217C1B85877EAC28028F0EA"
//------------------------------------------------------------------------------
// <auto-generated>
//     Ce code a été généré par un outil.
//     Version du runtime :4.0.30319.42000
//
//     Les modifications apportées à ce fichier peuvent provoquer un comportement incorrect et seront perdues si
//     le code est régénéré.
// </auto-generated>
//------------------------------------------------------------------------------

using Main.Pages.SubRecipe;
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


namespace Main.Pages.SubRecipe {
    
    
    /// <summary>
    /// Weight
    /// </summary>
    public partial class Weight : System.Windows.Controls.Page, System.Windows.Markup.IComponentConnector {
        
        
        #line 30 "..\..\..\..\Pages\SubRecipe\Weight.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock tbSeqNumber;
        
        #line default
        #line hidden
        
        
        #line 48 "..\..\..\..\Pages\SubRecipe\Weight.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.RadioButton rbSpeedMixer;
        
        #line default
        #line hidden
        
        
        #line 68 "..\..\..\..\Pages\SubRecipe\Weight.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox tbProduct;
        
        #line default
        #line hidden
        
        
        #line 75 "..\..\..\..\Pages\SubRecipe\Weight.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Primitives.ToggleButton cbIsSolvent;
        
        #line default
        #line hidden
        
        
        #line 83 "..\..\..\..\Pages\SubRecipe\Weight.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox tbBarcode;
        
        #line default
        #line hidden
        
        
        #line 92 "..\..\..\..\Pages\SubRecipe\Weight.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Primitives.ToggleButton cbIsBarcode;
        
        #line default
        #line hidden
        
        
        #line 134 "..\..\..\..\Pages\SubRecipe\Weight.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox tbSetpoint;
        
        #line default
        #line hidden
        
        
        #line 147 "..\..\..\..\Pages\SubRecipe\Weight.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox tbRange;
        
        #line default
        #line hidden
        
        
        #line 162 "..\..\..\..\Pages\SubRecipe\Weight.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.CheckBox cbIsBarcode_old;
        
        #line default
        #line hidden
        
        
        #line 163 "..\..\..\..\Pages\SubRecipe\Weight.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox tbBarcode_old;
        
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
            System.Uri resourceLocater = new System.Uri("/MixingApplication;component/pages/subrecipe/weight.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\..\Pages\SubRecipe\Weight.xaml"
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
            this.tbSeqNumber = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 2:
            
            #line 41 "..\..\..\..\Pages\SubRecipe\Weight.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.Button_Click);
            
            #line default
            #line hidden
            return;
            case 3:
            this.rbSpeedMixer = ((System.Windows.Controls.RadioButton)(target));
            
            #line 52 "..\..\..\..\Pages\SubRecipe\Weight.xaml"
            this.rbSpeedMixer.Click += new System.Windows.RoutedEventHandler(this.RadioButton_Click_1);
            
            #line default
            #line hidden
            return;
            case 4:
            this.tbProduct = ((System.Windows.Controls.TextBox)(target));
            
            #line 71 "..\..\..\..\Pages\SubRecipe\Weight.xaml"
            this.tbProduct.LostFocus += new System.Windows.RoutedEventHandler(this.TbProduct_LostFocus);
            
            #line default
            #line hidden
            
            #line 71 "..\..\..\..\Pages\SubRecipe\Weight.xaml"
            this.tbProduct.GotFocus += new System.Windows.RoutedEventHandler(this.ShowKeyBoard);
            
            #line default
            #line hidden
            
            #line 71 "..\..\..\..\Pages\SubRecipe\Weight.xaml"
            this.tbProduct.PreviewKeyDown += new System.Windows.Input.KeyEventHandler(this.HideKeyBoardIfEnter);
            
            #line default
            #line hidden
            return;
            case 5:
            this.cbIsSolvent = ((System.Windows.Controls.Primitives.ToggleButton)(target));
            return;
            case 6:
            this.tbBarcode = ((System.Windows.Controls.TextBox)(target));
            
            #line 91 "..\..\..\..\Pages\SubRecipe\Weight.xaml"
            this.tbBarcode.LostFocus += new System.Windows.RoutedEventHandler(this.TbBarcode_LostFocus);
            
            #line default
            #line hidden
            
            #line 91 "..\..\..\..\Pages\SubRecipe\Weight.xaml"
            this.tbBarcode.GotFocus += new System.Windows.RoutedEventHandler(this.ShowKeyBoard);
            
            #line default
            #line hidden
            
            #line 91 "..\..\..\..\Pages\SubRecipe\Weight.xaml"
            this.tbBarcode.PreviewKeyDown += new System.Windows.Input.KeyEventHandler(this.HideKeyBoardIfEnter);
            
            #line default
            #line hidden
            return;
            case 7:
            this.cbIsBarcode = ((System.Windows.Controls.Primitives.ToggleButton)(target));
            
            #line 98 "..\..\..\..\Pages\SubRecipe\Weight.xaml"
            this.cbIsBarcode.Checked += new System.Windows.RoutedEventHandler(this.CbIsBarcode_Checked);
            
            #line default
            #line hidden
            
            #line 99 "..\..\..\..\Pages\SubRecipe\Weight.xaml"
            this.cbIsBarcode.Unchecked += new System.Windows.RoutedEventHandler(this.CbIsBarcode_Unchecked);
            
            #line default
            #line hidden
            return;
            case 8:
            this.tbSetpoint = ((System.Windows.Controls.TextBox)(target));
            
            #line 137 "..\..\..\..\Pages\SubRecipe\Weight.xaml"
            this.tbSetpoint.LostFocus += new System.Windows.RoutedEventHandler(this.TbSetpoint_LostFocus);
            
            #line default
            #line hidden
            
            #line 137 "..\..\..\..\Pages\SubRecipe\Weight.xaml"
            this.tbSetpoint.GotFocus += new System.Windows.RoutedEventHandler(this.ShowKeyBoard);
            
            #line default
            #line hidden
            
            #line 137 "..\..\..\..\Pages\SubRecipe\Weight.xaml"
            this.tbSetpoint.PreviewKeyDown += new System.Windows.Input.KeyEventHandler(this.HideKeyBoardIfEnter);
            
            #line default
            #line hidden
            return;
            case 9:
            this.tbRange = ((System.Windows.Controls.TextBox)(target));
            
            #line 150 "..\..\..\..\Pages\SubRecipe\Weight.xaml"
            this.tbRange.LostFocus += new System.Windows.RoutedEventHandler(this.TbRange_LostFocus);
            
            #line default
            #line hidden
            
            #line 150 "..\..\..\..\Pages\SubRecipe\Weight.xaml"
            this.tbRange.GotFocus += new System.Windows.RoutedEventHandler(this.ShowKeyBoard);
            
            #line default
            #line hidden
            
            #line 150 "..\..\..\..\Pages\SubRecipe\Weight.xaml"
            this.tbRange.PreviewKeyDown += new System.Windows.Input.KeyEventHandler(this.HideKeyBoardIfEnter);
            
            #line default
            #line hidden
            return;
            case 10:
            this.cbIsBarcode_old = ((System.Windows.Controls.CheckBox)(target));
            
            #line 162 "..\..\..\..\Pages\SubRecipe\Weight.xaml"
            this.cbIsBarcode_old.Checked += new System.Windows.RoutedEventHandler(this.CbIsBarcode_Checked);
            
            #line default
            #line hidden
            
            #line 162 "..\..\..\..\Pages\SubRecipe\Weight.xaml"
            this.cbIsBarcode_old.Unchecked += new System.Windows.RoutedEventHandler(this.CbIsBarcode_Unchecked);
            
            #line default
            #line hidden
            return;
            case 11:
            this.tbBarcode_old = ((System.Windows.Controls.TextBox)(target));
            
            #line 163 "..\..\..\..\Pages\SubRecipe\Weight.xaml"
            this.tbBarcode_old.LostFocus += new System.Windows.RoutedEventHandler(this.TbBarcode_LostFocus);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}

