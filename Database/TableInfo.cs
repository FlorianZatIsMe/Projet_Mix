﻿using Database.Properties;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Database
{
    public class Column
    {
        public string id { get; }
        public string displayName { get; }
        public string value { get; set; }
        public Column(string id_arg = "", string displayName_arg = "")
        {
            id = id_arg;
            displayName = displayName_arg;
        }
    }

    public interface ITableInfo
    {
        string name { get; }
        List<Column> columns { get; set; }
        int id { get; }
    }

    public interface ISeqInfo : ITableInfo
    {
        int seqType { get; }
        int nextSeqType { get; }
        int nextSeqId { get; }
    }

    public interface ICycleSeqInfo : ISeqInfo
    {
        void SetRecipeParameters(string[] array); // ou get
    }

    public class AuditTrailInfo : ITableInfo
    {
        public AuditTrailInfo()
        {
            StringCollection colNames = Settings.Default.AuditTrail_ColIds;
            name = Settings.Default.AuditTrail_TableName;

            columns = new List<Column>();
            for (int i = 0; i < colNames.Count; i++) columns.Add(new Column(colNames[i]));

            id = Settings.Default.AuditTrail_ColN_id;
            dateTime = Settings.Default.AuditTrail_ColN_dateTime;
            username = Settings.Default.AuditTrail_ColN_username;
            eventType = Settings.Default.AuditTrail_ColN_eventType;
            description = Settings.Default.AuditTrail_ColN_description;
            valueBefore = Settings.Default.AuditTrail_ColN_valueBefore;
            valueAfter = Settings.Default.AuditTrail_ColN_valueAfter;
            comment = Settings.Default.AuditTrail_ColN_comment;
        }
        public string name { get; }
        public List<Column> columns { get; set; }
        public int id { get; }

        public int dateTime { get; }
        public int username { get; }
        public int eventType { get; }
        public int description { get; }
        public int valueBefore { get; }
        public int valueAfter { get; }
        public int comment { get; }
    }

    public class RecipeInfo : ISeqInfo
    {
        public RecipeInfo()
        {
            StringCollection colNames = Settings.Default.Recipe_ColIds;
            name = Settings.Default.Recipe_TableName;

            columns = new List<Column>();
            for (int i = 0; i < colNames.Count; i++) columns.Add(new Column(colNames[i]));

            id = Settings.Default.Recipe_ColN_id;
            nextSeqType = Settings.Default.Recipe_ColN_nextSeqType;
            nextSeqId = Settings.Default.Recipe_ColN_nextSeqId;
            recipeName = Settings.Default.Recipe_ColN_recipeName;
            version = Settings.Default.Recipe_ColN_version;
            status = Settings.Default.Recipe_ColN_status;
        }
        public string name { get; }
        public List<Column> columns { get; set; }
        public int seqType { get; }
        public int id { get; }
        public int nextSeqType { get; }
        public int nextSeqId { get; }

        public int recipeName { get; }
        public int version { get; }
        public int status { get; }
    }

    public class RecipeWeightInfo : ISeqInfo
    {
        public RecipeWeightInfo()
        {
            StringCollection colNames = Settings.Default.RecipeWeight_ColIds;
            name = Settings.Default.RecipeWeight_TableName;

            columns = new List<Column>();
            for (int i = 0; i < colNames.Count; i++) columns.Add(new Column(colNames[i]));

            seqType = Settings.Default.RecipeWeight_seqType;

            id = Settings.Default.RecipeWeight_ColN_id;
            nextSeqType = Settings.Default.RecipeWeight_ColN_nextSeqType;
            nextSeqId = Settings.Default.RecipeWeight_ColN_nextSeqId;
            seqName = Settings.Default.RecipeWeight_ColN_seqName;
            isBarcodeUsed = Settings.Default.RecipeWeight_ColN_isBarcodeUsed;
            barcode = Settings.Default.RecipeWeight_ColN_barcode;
            unit = Settings.Default.RecipeWeight_ColN_unit;
            decimalNumber = Settings.Default.RecipeWeight_ColN_decimalNumber;
            setpoint = Settings.Default.RecipeWeight_ColN_setpoint;
            min = Settings.Default.RecipeWeight_ColN_min;
            max = Settings.Default.RecipeWeight_ColN_max;
        }
        public string name { get; }
        public List<Column> columns { get; set; }
        public int seqType { get; }
        public int id { get; }
        public int nextSeqType { get; }
        public int nextSeqId { get; }

        public int seqName { get; }
        public int isBarcodeUsed { get; }
        public int barcode { get; }
        public int unit { get; }
        public int decimalNumber { get; }
        public int setpoint { get; }
        public int min { get; }
        public int max { get; }
    }

    public class RecipeSpeedMixerInfo : ISeqInfo
    {
        public RecipeSpeedMixerInfo()
        {
            StringCollection colNames = Settings.Default.RecipeSpeedMixer_ColIds;
            name = Settings.Default.RecipeSpeedMixer_TableName;

            columns = new List<Column>();
            for (int i = 0; i < colNames.Count; i++) columns.Add(new Column(colNames[i]));

            seqType = Settings.Default.RecipeSpeedMixer_seqType;

            id = Settings.Default.RecipeSpeedMixer_ColN_id;
            nextSeqType = Settings.Default.RecipeSpeedMixer_ColN_nextSeqType;
            nextSeqId = Settings.Default.RecipeSpeedMixer_ColN_nextSeqId;
            seqName = Settings.Default.RecipeSpeedMixer_ColN_seqName;
            acceleration = Settings.Default.RecipeSpeedMixer_ColN_acceleration;
            deceleration = Settings.Default.RecipeSpeedMixer_ColN_deceleration;
            vaccum_control = Settings.Default.RecipeSpeedMixer_ColN_vaccumControl;
            isVentgasAir = Settings.Default.RecipeSpeedMixer_ColN_isVentgasAir;
            monitorType = Settings.Default.RecipeSpeedMixer_ColN_monitorType;
            pressureUnit = Settings.Default.RecipeSpeedMixer_ColN_pressureUnit;
            scurve = Settings.Default.RecipeSpeedMixer_ColN_scurve;
            coldtrap = Settings.Default.RecipeSpeedMixer_ColN_coldtrap;
            speed00 = Settings.Default.RecipeSpeedMixer_ColN_speed00;
            time00 = Settings.Default.RecipeSpeedMixer_ColN_time00;
            pressure00 = Settings.Default.RecipeSpeedMixer_ColN_pressure00;
            speedMin = Settings.Default.RecipeSpeedMixer_ColN_speedMin;
            speedMax = Settings.Default.RecipeSpeedMixer_ColN_speedMax;
            pressureMin = Settings.Default.RecipeSpeedMixer_ColN_pressureMin;
            pressureMax = Settings.Default.RecipeSpeedMixer_ColN_pressureMax;
        }
        public string name { get; }
        public List<Column> columns { get; set; }
        public int seqType { get; }
        public int id { get; }
        public int nextSeqType { get; }
        public int nextSeqId { get; }

        public int seqName { get; }
        public int acceleration { get; }
        public int deceleration { get; }
        public int vaccum_control { get; }
        public int isVentgasAir { get; }
        public int monitorType { get; }
        public int pressureUnit { get; }
        public int scurve { get; }
        public int coldtrap { get; }
        public int speed00 { get; }
        public int time00 { get; }
        public int pressure00 { get; }
        public int speedMin { get; }
        public int speedMax { get; }
        public int pressureMin { get; }
        public int pressureMax { get; }

    }

    public class CycleTableInfo : ISeqInfo
    {
        public CycleTableInfo()
        {
            StringCollection colNames = Settings.Default.Cycle_ColIds;
            name = Settings.Default.Cycle_TableName;

            columns = new List<Column>();
            for (int i = 0; i < colNames.Count; i++) columns.Add(new Column(colNames[i]));

            id = Settings.Default.Cycle_ColN_id;
            nextSeqType = Settings.Default.Cycle_ColN_nextSeqType;
            nextSeqId = Settings.Default.Cycle_ColN_nextSeqId;
            jobNumber = Settings.Default.Cycle_ColN_jobNumber;
            batchNumber = Settings.Default.Cycle_ColN_batchNumber;
            quantityValue = Settings.Default.Cycle_ColN_quantityValue;
            quantityUnit = Settings.Default.Cycle_ColN_quantityUnit;
            itemNumber = Settings.Default.Cycle_ColN_itemNumber;
            recipeName = Settings.Default.Cycle_ColN_recipeName;
            recipeVersion = Settings.Default.Cycle_ColN_recipeVersion;
            equipmentName = Settings.Default.Cycle_ColN_equipmentName;
            dateTimeStartCycle = Settings.Default.Cycle_ColN_dateTimeStartCycle;
            dateTimeEndCycle = Settings.Default.Cycle_ColN_dateTimeEndCycle;
            username = Settings.Default.Cycle_ColN_username;
            firstAlarmId = Settings.Default.Cycle_ColN_firstAlarmId;
            lastAlarmId = Settings.Default.Cycle_ColN_lastAlarmId;
            comment = Settings.Default.Cycle_ColN_comment;
            isItATest = Settings.Default.Cycle_ColN_isItATest;

        }
        public string name { get; }
        public List<Column> columns { get; set; }
        public int seqType { get; }
        public int id { get; }
        public int nextSeqType { get; }
        public int nextSeqId { get; }

        public int jobNumber { get; }
        public int batchNumber { get; }
        public int quantityValue { get; }
        public int quantityUnit { get; }
        public int itemNumber { get; }
        public int recipeName { get; }
        public int recipeVersion { get; }
        public int equipmentName { get; }
        public int dateTimeStartCycle { get; }
        public int dateTimeEndCycle { get; }
        public int username { get; }
        public int firstAlarmId { get; }
        public int lastAlarmId { get; }
        public int comment { get; }
        public int isItATest { get; }
    }

    public class CycleWeightInfo : ICycleSeqInfo
    {
        public CycleWeightInfo()
        {
            StringCollection colNames = Settings.Default.CycleWeight_ColIds;
            name = Settings.Default.CycleWeight_TableName;

            columns = new List<Column>();
            for (int i = 0; i < colNames.Count; i++) columns.Add(new Column(colNames[i]));

            seqType = Settings.Default.CycleWeight_seqType;

            id = Settings.Default.CycleWeight_ColN_id;
            nextSeqType = Settings.Default.CycleWeight_ColN_nextSeqType;
            nextSeqId = Settings.Default.CycleWeight_ColN_nextSeqId;
            product = Settings.Default.CycleWeight_ColN_product;
            wasWeightManual = Settings.Default.CycleWeight_ColN_wasWeightManual;
            dateTime = Settings.Default.CycleWeight_ColN_dateTime;
            actualValue = Settings.Default.CycleWeight_ColN_actualValue;
            setpoint = Settings.Default.CycleWeight_ColN_setpoint;
            min = Settings.Default.CycleWeight_ColN_min;
            max = Settings.Default.CycleWeight_ColN_max;
            unit = Settings.Default.CycleWeight_ColN_unit;
            decimalNumber = Settings.Default.CycleWeight_ColN_decimalNumber;
        }
        public string name { get; }
        public List<Column> columns { get; set; }
        public int seqType { get; }
        public int id { get; }
        public int nextSeqType { get; }
        public int nextSeqId { get; }


        public int product { get; }
        public int wasWeightManual { get; }
        public int dateTime { get; }
        public int actualValue { get; }
        public int setpoint { get; }
        public int min { get; }
        public int max { get; }
        public int unit { get; }
        public int decimalNumber { get; }


        private RecipeWeightInfo recipeWeightInfo = new RecipeWeightInfo();
        public void SetRecipeParameters(string[] array)
        {
            NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

            if (array.Length != recipeWeightInfo.columns.Count())
            {
                logger.Error("Taille du tableau incorrect");
                return;
            }

            columns[product].value = array[recipeWeightInfo.seqName];
            columns[setpoint].value = array[recipeWeightInfo.setpoint];
            columns[min].value = array[recipeWeightInfo.min];
            columns[max].value = array[recipeWeightInfo.max];
            columns[unit].value = array[recipeWeightInfo.unit];
            columns[decimalNumber].value = array[recipeWeightInfo.decimalNumber];
        }
    }

    public class CycleSpeedMixerInfo : ICycleSeqInfo
    {
        public CycleSpeedMixerInfo()
        {
            StringCollection colNames = Settings.Default.CycleSpeedMixer_ColIds;
            name = Settings.Default.CycleSpeedMixer_TableName;

            columns = new List<Column>();
            for (int i = 0; i < colNames.Count; i++) columns.Add(new Column(colNames[i]));

            seqType = Settings.Default.CycleSpeedMixer_seqType;

            id = Settings.Default.CycleSpeedMixer_ColN_id;
            nextSeqType = Settings.Default.CycleSpeedMixer_ColN_nextSeqType;
            nextSeqId = Settings.Default.CycleSpeedMixer_ColN_nextSeqId;
            dateTimeStart = Settings.Default.CycleSpeedMixer_ColN_dateTimeStart;
            dateTimeEnd = Settings.Default.CycleSpeedMixer_ColN_dateTimeEnd;
            timeMixTh = Settings.Default.CycleSpeedMixer_ColN_timeMixTh;
            timeMixEff = Settings.Default.CycleSpeedMixer_ColN_timeMixEff;
            pressureUnit = Settings.Default.CycleSpeedMixer_ColN_pressureUnit;
            speedMin = Settings.Default.CycleSpeedMixer_ColN_speedMin;
            speedMax = Settings.Default.CycleSpeedMixer_ColN_speedMax;
            pressureMin = Settings.Default.CycleSpeedMixer_ColN_pressureMin;
            pressureMax = Settings.Default.CycleSpeedMixer_ColN_pressureMax;
            speedMean = Settings.Default.CycleSpeedMixer_ColN_speedMean;
            pressureMean = Settings.Default.CycleSpeedMixer_ColN_pressureMean;
            speedStd = Settings.Default.CycleSpeedMixer_ColN_speedStd;
            pressureStd = Settings.Default.CycleSpeedMixer_ColN_pressureStd;

        }
        public string name { get; }
        public List<Column> columns { get; set; }
        public int seqType { get; }
        public int id { get; }
        public int nextSeqType { get; }
        public int nextSeqId { get; }

        public int dateTimeStart { get; }
        public int dateTimeEnd { get; }
        public int timeMixTh { get; }
        public int timeMixEff { get; }
        public int pressureUnit { get; }
        public int speedMin { get; }
        public int speedMax { get; }
        public int pressureMin { get; }
        public int pressureMax { get; }
        public int speedMean { get; }
        public int pressureMean { get; }
        public int speedStd { get; }
        public int pressureStd { get; }


        private RecipeSpeedMixerInfo recipeSpeedMixerInfo = new RecipeSpeedMixerInfo();
        public void SetRecipeParameters(string[] array)
        {
            NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

            if (array.Length != recipeSpeedMixerInfo.columns.Count())
            {
                logger.Error("Taille du tableau incorrect");
                return;
            }

            int i = 0;
            int timeTh_seconds = 0;

            while (i != 10 && array[recipeSpeedMixerInfo.time00 + 3 * i] != "")
            {
                timeTh_seconds += int.Parse(array[recipeSpeedMixerInfo.time00 + 3 * i]); // Ajoute un try et faire ça partout
                i++;
            }

            columns[timeMixTh].value = TimeSpan.FromSeconds(timeTh_seconds).ToString();
            columns[pressureUnit].value = array[recipeSpeedMixerInfo.pressureUnit];
            columns[speedMin].value = array[recipeSpeedMixerInfo.speedMin];
            columns[speedMax].value = array[recipeSpeedMixerInfo.speedMax];
            columns[pressureMin].value = array[recipeSpeedMixerInfo.pressureMin];
            columns[pressureMax].value = array[recipeSpeedMixerInfo.pressureMax];
        }
    }


}