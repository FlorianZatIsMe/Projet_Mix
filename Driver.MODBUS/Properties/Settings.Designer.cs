﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Ce code a été généré par un outil.
//     Version du runtime :4.0.30319.42000
//
//     Les modifications apportées à ce fichier peuvent provoquer un comportement incorrect et seront perdues si
//     le code est régénéré.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Driver.MODBUS.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "17.2.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1000")]
        public int ScanAlarmTimer_Interval {
            get {
                return ((int)(this["ScanAlarmTimer_Interval"]));
            }
            set {
                this["ScanAlarmTimer_Interval"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("\r\n          <ConnectionInfo xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xs" +
            "i=\"http://www.w3.org/2001/XMLSchema-instance\">\r\n            <ipAddress>localhost" +
            "</ipAddress>\r\n            <port>root</port>\r\n          </ConnectionInfo>\r\n      " +
            "  ")]
        public global::Driver_MODBUS.ConnectionInfo ConnectionInfo {
            get {
                return ((global::Driver_MODBUS.ConnectionInfo)(this["ConnectionInfo"]));
            }
            set {
                this["ConnectionInfo"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1")]
        public int Alarm_Connection_id1 {
            get {
                return ((int)(this["Alarm_Connection_id1"]));
            }
            set {
                this["Alarm_Connection_id1"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public int Alarm_Connection_id2 {
            get {
                return ((int)(this["Alarm_Connection_id2"]));
            }
            set {
                this["Alarm_Connection_id2"] = value;
            }
        }
    }
}
