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
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("3053")]
        public int Register_Instruction {
            get {
                return ((int)(this["Register_Instruction"]));
            }
            set {
                this["Register_Instruction"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("3000")]
        public int Register_MixName {
            get {
                return ((int)(this["Register_MixName"]));
            }
            set {
                this["Register_MixName"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("3043")]
        public int Register_Acceleration {
            get {
                return ((int)(this["Register_Acceleration"]));
            }
            set {
                this["Register_Acceleration"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("3044")]
        public int Register_Deceleration {
            get {
                return ((int)(this["Register_Deceleration"]));
            }
            set {
                this["Register_Deceleration"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("3046")]
        public int Register_VacuumInUse {
            get {
                return ((int)(this["Register_VacuumInUse"]));
            }
            set {
                this["Register_VacuumInUse"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("3050")]
        public int Register_VacuumScale {
            get {
                return ((int)(this["Register_VacuumScale"]));
            }
            set {
                this["Register_VacuumScale"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("3056")]
        public int Register_ProgramId {
            get {
                return ((int)(this["Register_ProgramId"]));
            }
            set {
                this["Register_ProgramId"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("3013")]
        public int Register_Speed01 {
            get {
                return ((int)(this["Register_Speed01"]));
            }
            set {
                this["Register_Speed01"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("3023")]
        public int Register_Time01 {
            get {
                return ((int)(this["Register_Time01"]));
            }
            set {
                this["Register_Time01"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("3033")]
        public int Register_Pressure01 {
            get {
                return ((int)(this["Register_Pressure01"]));
            }
            set {
                this["Register_Pressure01"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Problème de connection avec le SpeedMixer")]
        public string Error01 {
            get {
                return ((string)(this["Error01"]));
            }
            set {
                this["Error01"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public int Instruction_Reset {
            get {
                return ((int)(this["Instruction_Reset"]));
            }
            set {
                this["Instruction_Reset"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1")]
        public int VacuumInUse_Yes {
            get {
                return ((int)(this["VacuumInUse_Yes"]));
            }
            set {
                this["VacuumInUse_Yes"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public int VacuumInUse_No {
            get {
                return ((int)(this["VacuumInUse_No"]));
            }
            set {
                this["VacuumInUse_No"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1")]
        public int VacuumScale_Torr {
            get {
                return ((int)(this["VacuumScale_Torr"]));
            }
            set {
                this["VacuumScale_Torr"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("2")]
        public int VacuumScale_mBar {
            get {
                return ((int)(this["VacuumScale_mBar"]));
            }
            set {
                this["VacuumScale_mBar"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("3")]
        public int VacuumScale_inHg {
            get {
                return ((int)(this["VacuumScale_inHg"]));
            }
            set {
                this["VacuumScale_inHg"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("4")]
        public int VacuumScale_PSIA {
            get {
                return ((int)(this["VacuumScale_PSIA"]));
            }
            set {
                this["VacuumScale_PSIA"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("-1")]
        public int VacuumScale_Error {
            get {
                return ((int)(this["VacuumScale_Error"]));
            }
            set {
                this["VacuumScale_Error"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Unité de pression incorrecte")]
        public string Error02 {
            get {
                return ((string)(this["Error02"]));
            }
            set {
                this["Error02"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public int ProgramId_ToUse {
            get {
                return ((int)(this["ProgramId_ToUse"]));
            }
            set {
                this["ProgramId_ToUse"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1")]
        public int Instruction_SetDefinedMixProgram {
            get {
                return ((int)(this["Instruction_SetDefinedMixProgram"]));
            }
            set {
                this["Instruction_SetDefinedMixProgram"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("10")]
        public int Pressure_Multiplicator {
            get {
                return ((int)(this["Pressure_Multiplicator"]));
            }
            set {
                this["Pressure_Multiplicator"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("100")]
        public int Instruction_RunDefinedMixProgram {
            get {
                return ((int)(this["Instruction_RunDefinedMixProgram"]));
            }
            set {
                this["Instruction_RunDefinedMixProgram"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("200")]
        public int Instruction_Stop_Active_Mix {
            get {
                return ((int)(this["Instruction_Stop_Active_Mix"]));
            }
            set {
                this["Instruction_Stop_Active_Mix"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("300")]
        public int Instruction_ResetError {
            get {
                return ((int)(this["Instruction_ResetError"]));
            }
            set {
                this["Instruction_ResetError"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("3100")]
        public int Register_MixerStatus {
            get {
                return ((int)(this["Register_MixerStatus"]));
            }
            set {
                this["Register_MixerStatus"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("382")]
        public int Register_MixerPressure {
            get {
                return ((int)(this["Register_MixerPressure"]));
            }
            set {
                this["Register_MixerPressure"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("3101")]
        public int Register_MixerSpeed {
            get {
                return ((int)(this["Register_MixerSpeed"]));
            }
            set {
                this["Register_MixerSpeed"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public int MixerStatusId_ReadyToRun {
            get {
                return ((int)(this["MixerStatusId_ReadyToRun"]));
            }
            set {
                this["MixerStatusId_ReadyToRun"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1")]
        public int MixerStatusId_MixerRunning {
            get {
                return ((int)(this["MixerStatusId_MixerRunning"]));
            }
            set {
                this["MixerStatusId_MixerRunning"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("2")]
        public int MixerStatusId_MixerError {
            get {
                return ((int)(this["MixerStatusId_MixerError"]));
            }
            set {
                this["MixerStatusId_MixerError"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("3")]
        public int MixerStatusId_LidOpen {
            get {
                return ((int)(this["MixerStatusId_LidOpen"]));
            }
            set {
                this["MixerStatusId_LidOpen"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("4")]
        public int MixerStatusId_LidClosed {
            get {
                return ((int)(this["MixerStatusId_LidClosed"]));
            }
            set {
                this["MixerStatusId_LidClosed"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("5")]
        public int MixerStatusId_SafetyOK {
            get {
                return ((int)(this["MixerStatusId_SafetyOK"]));
            }
            set {
                this["MixerStatusId_SafetyOK"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("7")]
        public int MixerStatusId_RobotAtHome {
            get {
                return ((int)(this["MixerStatusId_RobotAtHome"]));
            }
            set {
                this["MixerStatusId_RobotAtHome"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<ConnectionInfo xmlns:xsd=\"http://www.w3" +
            ".org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">\r\n  <" +
            "ipAddress>10.10.1.2</ipAddress>\r\n  <port>503</port>\r\n</ConnectionInfo>")]
        public global::Driver_MODBUS.ConnectionInfo ConnectionInfo {
            get {
                return ((global::Driver_MODBUS.ConnectionInfo)(this["ConnectionInfo"]));
            }
            set {
                this["ConnectionInfo"] = value;
            }
        }
    }
}
