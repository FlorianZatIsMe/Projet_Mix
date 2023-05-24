using Alarm_Management;
using Database;
using Main.Pages;
using Main.Properties;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System.Drawing.Printing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;
using static Alarm_Management.AlarmManagement;
using System.Collections;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Drawing;
using System.Printing;
using Message;
using System.Runtime.InteropServices;

namespace Main
{
    internal class ReportGeneration
    {
        private string templateNumber;

        private readonly string cycleReportPath = Settings.Default.Report_Path;// @"C:\Temp\Reports\";
        private readonly string cycleTemplateNumber = "DHS-LTRMxxx Rev: 001";

        private readonly string samplingReportPath = Settings.Default.Sampling_Path;// @"C:\Temp\Balande_Daily_Test\";
        private readonly string samplingTemplateNumber = "DHS-SMPxxx Rev: 001";

        //
        // Daily test report
        //

        private readonly string samplingDocTitle = "Rapport de test journalier";

        private string dateTimeSampling;
        private string samplingStatus;
        private string[] samplingRefs;
        private string[] samplingMeas;

        string SamplingDateTimeField = "Date et heure du test: ";
        string statusField = "Statut";

        //
        // Header / Footer
        //

        private readonly double margin = 25;
        private readonly double marginH_Footer = 15;
        private double heightFooter;

        private readonly XImage logo = XImage.FromFile("Resources/Integra-Logo.png");
        private readonly double widthLogo = 100;
        private double heightLogo;

        private readonly string generationDateTimeField = "Date et heure de génération";
        private readonly string application_nameField = "Application";
        private readonly string application_versionField = "Version";

        private readonly string watermarkText = "Non destiné à l'usage humain";
        private readonly XFont fontWatermark = new XFont("BankGothic Lt BT", 60);

        //
        // Info font
        //
        private readonly string fontTitleName = "Times New Roman";
        private readonly double fontTitleSize = 20;
        private readonly double fontSubTitleSize = 10;

        private readonly string fontDoc = "Arial";
        private readonly double fontBodySize1 = 10;
        private readonly double fontBodySize2 = 9;
        private readonly double fontFooterSize = 10;

        private double fontDoc_fontTitleSize_Height;
        private double fontDoc_fontBodySize1_Height;

        //
        // Info titre rapport
        //
        private readonly double rectTitleHeight = 30;
        private readonly double rectTitleOutlineSize = 1.2;
        private readonly double rectSubTitleOutlineSize = 0.8;
        private readonly double marginH_Title_SubT = 5;
        private readonly double marginL_SubT = 10;

        private readonly string cycleDocTitle = "Rapport de cycle";

        private readonly string jobNumberField = "JOB #";
        private readonly string batchNumberField = "LOT #";
        private readonly string qtyNumberField = "QTY";
        private readonly string itemNumberField = "ITEM #";


        // Info tables Séquences
        private readonly double marginL_Tables = 5;
        private readonly double rectTablesSize = 0.5;
        private readonly string sequenceField = "Séquence";

        //
        // Info cycle (arguments du rapport)
        //
        private readonly string na = "N/A";
        private string jobNumber;
        private string batchNumber;
        private string qtyNumber;
        private string itemNumber;
        private string recipeNameVersion;
        private string equipmentName;
        private string dtStartCycle;
        private string dtEndCycle;
        private string user;
        private int firstAlarmId;
        private int lastAlarmId;
        private string comment;
        private bool isTest;
        private string bowlWeight;
        private string lastWeightTh;
        private string lastWeightEff;

        //***************************************************

        private PdfDocument document = new PdfDocument();
        private readonly List<XGraphics> gfxs = new List<XGraphics>();
        private readonly DateTime generationDateTime = DateTime.Now;
        private int pagesNumber = 1;

        // Colors
        private readonly XSolidBrush BrushGrey0 = new XSolidBrush(XColor.FromArgb(100, 100, 100));
        private readonly XSolidBrush BrushGrey1 = new XSolidBrush(XColor.FromArgb(192, 192, 192));
        private readonly XSolidBrush BrushGrey2 = new XSolidBrush(XColor.FromArgb(217, 217, 217));
        private readonly XSolidBrush BrushGrey3 = new XSolidBrush(XColor.FromArgb(242, 242, 242));
        private readonly XSolidBrush BrushGreen1 = new XSolidBrush(XColor.FromArgb(0, 97, 0));
        private readonly XSolidBrush BrushGreen2 = new XSolidBrush(XColor.FromArgb(198, 239, 206));
        private readonly XSolidBrush BrushRed1 = new XSolidBrush(XColor.FromArgb(156, 0, 0));
        private readonly XSolidBrush BrushRed2 = new XSolidBrush(XColor.FromArgb(255, 199, 206));

        private readonly RecipeWeightInfo recipeWeightInfo = new RecipeWeightInfo();
        private readonly RecipeSpeedMixerInfo recipeSpeedMixerInfo = new RecipeSpeedMixerInfo();

        private NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public ReportGeneration()
        {
            logger.Debug("Start");

            jobNumber = na;
            batchNumber = na;
            qtyNumber = na;
            itemNumber = na;
            recipeNameVersion = na;
            equipmentName = na;
            dtStartCycle = na;
            dtEndCycle = na;
            user = na;
            firstAlarmId = -1;
            lastAlarmId = -1;
            comment = "";
        }
        private double DrawTitle(PdfPage page, double y, string title)
        {
            logger.Debug("DrawTitle");
            XRect rect = new XRect(x: margin, y: y, width: page.Width - 2 * margin, height: rectTitleHeight);
            gfxs[pagesNumber - 1].DrawRectangle(new XPen(XColors.Black, rectTitleOutlineSize), rect);
            gfxs[pagesNumber - 1].DrawString(title, new XFont(fontTitleName, fontTitleSize), XBrushes.Black, rect, XStringFormats.Center);

            return y + rectTitleHeight;
        }

        private double DrawStringColumns(PdfPage page, double y, string[] values, int nColumns = 2)
        {
            logger.Debug("DrawStringColumns");
            double textHeight = fontDoc_fontBodySize1_Height;
            double generalInfoHeight = marginH_GeneralInfoItems + textHeight;

            for (int i = 0; i < values.Length; i++)
            {
                gfxs[pagesNumber - 1].DrawString(values[i], new XFont(fontDoc, fontBodySize1), XBrushes.Black, x: margin + (i % nColumns) * ((page.Width - 2 * margin) / nColumns), y: y + marginH_GeneralInfo + (int)(i / nColumns) * generalInfoHeight, XStringFormats.TopLeft);
            }

            return y + marginH_GeneralInfo + (int)((values.Length - 1) / nColumns) * generalInfoHeight + textHeight + smSeq_marginL_Cells;
        }
        private double GenerateHeader(PdfPage page)
        {
            logger.Debug("GenerateHeader");

            //generateCroquis1(page);

            heightLogo = logo.PixelHeight * (widthLogo / logo.PixelWidth);

            // Header
            gfxs[pagesNumber - 1].DrawImage(logo, margin, margin, widthLogo, heightLogo);

            XRect rect = new XRect(x: margin, y: margin, width: page.Width - 2 * margin, height: heightLogo);
            gfxs[pagesNumber - 1].DrawString(templateNumber, new XFont(fontDoc, fontBodySize1), XBrushes.Black, rect, XStringFormats.CenterRight);

            // Footer
            gfxs[pagesNumber - 1].DrawString(generationDateTimeField + ": " + generationDateTime.ToString(Settings.Default.DateTime_Format_Read),
                new XFont(fontDoc, fontFooterSize),
                XBrushes.Black, x: margin, y: page.Height - margin, XStringFormats.BottomLeft);
            gfxs[pagesNumber - 1].DrawString(application_nameField + ": " + General.application_name + " - " + application_versionField + ": " + General.application_version,
                new XFont(fontDoc, fontFooterSize),
                XBrushes.Black, x: margin, y: page.Height - margin - marginH_Footer, XStringFormats.BottomLeft);


            return margin + heightLogo;
        }
        private double GenerateSecondHeader(PdfPage page)
        {
            logger.Debug("GenerateSecondHeader");

            // Info traçabilité
            XRect rect;
            string traceabilityText1 = jobNumberField + ": " + jobNumber + " ; " + batchNumberField + ": " + batchNumber;
            string traceabilityText2 = ""; // qtyNumberField + ": " + qtyNumber + " ; " + itemNumberField + ": " + itemNumber;
            rect = new XRect(x: 0, y: margin, width: page.Width, height: heightLogo / 2);
            gfxs[pagesNumber - 1].DrawString(traceabilityText1, new XFont(fontDoc, fontBodySize1), XBrushes.Black, rect, XStringFormats.BottomCenter);
            rect.Y += heightLogo / 2;
            gfxs[pagesNumber - 1].DrawString(traceabilityText2, new XFont(fontDoc, fontBodySize1), XBrushes.Black, rect, XStringFormats.TopCenter);

            return GenerateHeader(page);
        }
        private double GenerateFirstTitle(PdfPage page, double y)
        {
            logger.Debug("GenerateFirstTitle");

            XRect rect;
            double currentY;
            string[,] tableValues = new string[,] {
                { jobNumberField + ": ", jobNumber },
                { batchNumberField + ": ", batchNumber },
                { qtyNumberField + ": ", qtyNumber },
                { itemNumberField + ": ", itemNumber } };

            // TITLE
            currentY = DrawTitle(page, y, cycleDocTitle);

            // 
            // SUBTITLE
            // 
            // Draw Sub-Title rectangle
            rect = new XRect(x: margin, y: currentY + marginH_Title_SubT, width: page.Width - 2 * margin, height: rectTitleHeight);
            gfxs[pagesNumber - 1].DrawRectangle(new XPen(XColors.Black, rectSubTitleOutlineSize), rect);

            double xShift = (page.Width - 2 * (margin + marginL_SubT)) / tableValues.GetLength(0);
            double rectShift;
            double font;

            for (int i = 0; i < tableValues.GetLength(0); i++)
            {
                // On affiche la valeur xField
                rect = new XRect(x: margin + marginL_SubT + i * xShift, y: currentY + marginH_Title_SubT, width: xShift - marginL_SubT, height: rectTitleHeight);
                gfxs[pagesNumber - 1].DrawString(tableValues[i, 0], new XFont(fontTitleName, fontSubTitleSize), XBrushes.Black, rect, XStringFormats.CenterLeft);
                rectShift = gfxs[pagesNumber - 1].MeasureString(tableValues[i, 0], new XFont(fontTitleName, fontSubTitleSize)).Width;
                rect.X += rectShift;
                rect.Width -= rectShift;

                // Display text
                font = fontSubTitleSize;
                while (gfxs[pagesNumber - 1].MeasureString(tableValues[i, 1], new XFont(fontTitleName, font)).Width > rect.Width) font--;
                gfxs[pagesNumber - 1].DrawString(tableValues[i, 1], new XFont(fontTitleName, font), XBrushes.Black, rect, XStringFormats.CenterLeft);
            }

            return rect.Y + rect.Height;
        }

        //
        // generateGeneralInfo
        // 
        private readonly double marginH_GeneralInfo = 25;
        private readonly double marginH_GeneralInfoItems = 8;

        private readonly string recipeNameVersionField = "ID recette et révision";
        private readonly string equipmentNameField = "ID équipement";
        private readonly string dtStartCycleField = "Début du cycle";
        private readonly string dtEndCycleField = "Fin du cycle";
        private readonly string cycleTimeField = "Temps de cycle";
        private readonly string userField = "Utilisateur";
        private readonly string lastDailyTestField = "Date et heure du dernier étalonnage de la balance";

        private readonly int statusId = 3;

        private double GenerateGeneralInfo(PdfPage page, double y)
        {
            logger.Debug("GenerateGeneralInfo");

            string cycleTime;

            double textHeight = fontDoc_fontBodySize1_Height;
            double generalInfoHeight = marginH_GeneralInfoItems + textHeight;
            double currentY;

            DateTime? dtStartCycle_t = null;

            // Calcul du temps de cycle
            try
            {
                dtStartCycle_t = Convert.ToDateTime(dtStartCycle);
                DateTime dtEndCycle_t = Convert.ToDateTime(dtEndCycle);
                TimeSpan cycleTime_t = dtEndCycle_t.Subtract((DateTime)dtStartCycle_t);
                cycleTime = cycleTime_t.Hours.ToString("00") + ":" + cycleTime_t.Minutes.ToString("00") + ":" + cycleTime_t.Seconds.ToString("00");
            }
            catch (Exception)
            {
                cycleTime = na;
            }

            string[] values = new string[] {
                recipeNameVersionField + ": " + recipeNameVersion,
                equipmentNameField + ": " + equipmentName,
                dtStartCycleField + ": " + dtStartCycle,
                dtEndCycleField + ": " + dtEndCycle,
                cycleTimeField + ": " + cycleTime,
                userField + ": " + user};

            currentY = DrawStringColumns(page, y, values, 2);

            DailyTestInfo dailyTestInfo = new DailyTestInfo();
            object[] dailyTestValues = new object[dailyTestInfo.Ids.Count()];
            dailyTestValues[dailyTestInfo.Status] = DatabaseSettings.General_TrueValue_Write;

            string lastDailyTest;
            bool isDailyTestGood = false;

            try
            {
                Task<object> t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetLastDailyTestDate(dailyTestInfo, dailyTestValues, Convert.ToDateTime(dtStartCycle)); });

                DateTime? lastDailyTestDate = (DateTime?)t.Result;

                if (lastDailyTestDate == null)
                {
                    lastDailyTest = "";
                }
                else
                {
                    lastDailyTest = ((DateTime)lastDailyTestDate).ToString(Settings.Default.DateTime_Format_Read);
                    if (dtStartCycle_t != null)
                    {
                        isDailyTestGood = ((DateTime)lastDailyTestDate).CompareTo(((DateTime)dtStartCycle_t).AddDays(-Settings.Default.LastDailyTest_Days).AddHours(-Settings.Default.LastDailyTest_Hours)) > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                lastDailyTest = "";
            }

            double fieldWidth = gfxs[pagesNumber - 1].MeasureString(lastDailyTestField + ": ", new XFont(fontDoc, fontBodySize1)).Width;

            gfxs[pagesNumber - 1].DrawString(lastDailyTestField + ": ", new XFont(fontDoc, fontBodySize1), 
                XBrushes.Black, 
                x: margin, 
                y: currentY, XStringFormats.TopLeft);

            gfxs[pagesNumber - 1].DrawString(lastDailyTest, new XFont(fontDoc, fontBodySize1),
                (isDailyTestGood ? XBrushes.Black : XBrushes.Red),
                x: margin + fieldWidth,
                y: currentY, XStringFormats.TopLeft);

            currentY += generalInfoHeight;

            // Generate general weigth information

            string status;
            if (lastWeightTh == "" || lastWeightEff == "")
            {
                status = statusFAIL;
            }
            else
            {
                decimal eff = decimal.Parse(lastWeightEff, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowThousands);
                decimal th = decimal.Parse(lastWeightTh, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowThousands);
                status = Pages.SubCycle.CycleWeightOld.IsFinalWeightCorrect(eff, th) /*(Math.Abs(eff - th) < th * Settings.Default.LastWeightRatio)*/ ? statusPASS : statusFAIL;
            }


            string[,] tableValues = new string[,] {
                { "Masse du contenant (g)", bowlWeight },
                { "Masse final théorique (g)", lastWeightTh == "" ? na : lastWeightTh },
                { "Masse final effective (g)", decimal.Parse(lastWeightEff) == -1 ? na : lastWeightEff },
                { "Statut", status } };
            double xShift = (page.Width - 2 * margin) / tableValues.GetLength(0);
            XRect rect;

            for (int i = 0; i < tableValues.GetLength(0); i++)
            {
                // Champs
                rect = new XRect(x: margin + i * xShift, y: currentY, width: xShift, height: smSeq_RowHeight);
                gfxs[pagesNumber - 1].DrawRectangle(new XPen(XColors.Black, rectTablesSize), BrushGrey2, rect);
                gfxs[pagesNumber - 1].DrawString(tableValues[i, 0], new XFont(fontDoc, fontBodySize2), XBrushes.Black, rect, XStringFormats.Center);

                // Valeurs
                rect.Y += smSeq_RowHeight;
                gfxs[pagesNumber - 1].DrawRectangle(new XPen(XColors.Black, rectTablesSize),
                    (i == statusId ? (status == statusPASS ? BrushGreen2 : BrushRed2) :
                    XBrushes.White),
                    rect);
                gfxs[pagesNumber - 1].DrawString(tableValues[i, 1], new XFont(fontDoc, fontBodySize2),
                    (i == statusId ? (status == statusPASS ? BrushGreen1 : BrushRed1) :
                    XBrushes.Black),
                    rect, XStringFormats.Center);
            }
            currentY += 2 * smSeq_RowHeight;

            return currentY;
        }
        private double GenerateSequence(int seqType, PdfPage page, int n, double y, object[] cycleSeqValues)
        {
            if (seqType == recipeWeightInfo.SeqType)
            {
                return GenerateWeightSeq(page, n, y, cycleSeqValues);
            }
            else if (seqType == recipeSpeedMixerInfo.SeqType)
            {
                return GenerateSpeedMixerSeq(page, n, y, cycleSeqValues);

            }

            MyMessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - Et bah alors !");
            return -2;
        }

        //
        // generateWeightSeq
        //
        private readonly double weightSeq_RowHeight = 25;
        private readonly string weightSeq_SeqField = "Pesée";
        private readonly string weightSeq_manualWeight = "(manuelle)";
        private readonly string weightSeq_autoWeight = "(automatique)";
        private readonly string weightSeq_productField = "Produit: ";
        private readonly string weightSeq_DateTimeField = "Date et heure de pesée";
        private readonly string weightSeq_ValueField = "Valeur pesée";
        private readonly string weightSeq_setpointField = "Valeur nominal";
        private readonly string weightSeq_minimumField = "Minimum";
        private readonly string weightSeq_maximumField = "Maximum";
        private readonly string weightSeq_statusField = "Réussi / Echoué";
        private readonly string statusPASS = "REUSSI";
        private readonly string statusFAIL = "ECHOUE";
        private readonly int weightSeq_statusColumnNumber = 4;

        private double GenerateWeightSeq(PdfPage page, int n, double y, object[] cycleSeqValues)
        {
            logger.Debug("GenerateWeightSeq");

            CycleWeightInfo cycleWeightInfo = new CycleWeightInfo();

            if (cycleSeqValues == null) return -1;

            string product = cycleSeqValues[cycleWeightInfo.Product].ToString() + (cycleSeqValues[cycleWeightInfo.IsSolvent].ToString() == DatabaseSettings.General_TrueValue_Read ? " (solvant)" : "");//3
            string wasWeightManual = cycleSeqValues[cycleWeightInfo.WasWeightManual].ToString();//4
            string dateTime = (cycleSeqValues[cycleWeightInfo.DateTime].ToString() == "" ||
                cycleSeqValues[cycleWeightInfo.DateTime] == null) ? na :
                Convert.ToDateTime(cycleSeqValues[cycleWeightInfo.DateTime].ToString()).ToString(Settings.Default.DateTime_Format_Read); //5
            string actualValue;
            string setpoint;
            string minimum;
            string maximum;
            string unit = cycleSeqValues[cycleWeightInfo.Unit].ToString();//10
            string decimalNumber = cycleSeqValues[cycleWeightInfo.DecimalNumber].ToString();//11

            try
            {
                actualValue = decimal.Parse(cycleSeqValues[cycleWeightInfo.ActualValue].ToString()).ToString("N" + decimalNumber); //6
            }
            catch (Exception)
            {
                actualValue = na;
            }

            try
            {
                setpoint = decimal.Parse(cycleSeqValues[cycleWeightInfo.Setpoint].ToString()).ToString("N" + decimalNumber);//7
            }
            catch (Exception)
            {
                setpoint = na;
            }

            try
            {
                minimum = decimal.Parse(cycleSeqValues[cycleWeightInfo.Min].ToString()).ToString("N" + decimalNumber);//8
            }
            catch (Exception)
            {
                minimum = na;
            }

            try
            {
                maximum = decimal.Parse(cycleSeqValues[cycleWeightInfo.Max].ToString()).ToString("N" + decimalNumber);
            }
            catch (Exception)
            {
                maximum = na;
            }

            double returnValue = y + 4 * weightSeq_RowHeight;

            if (IsIndextoLow(page, returnValue)) return -1; //  if(returnValue > (page.Height - margin - heightFooter)

            string status;

            if (actualValue == na)
            {
                status = na;
            }
            else
            {
                try
                {
                    status = (double.Parse(actualValue) >= double.Parse(minimum) &&
                        double.Parse(actualValue) <= double.Parse(maximum)) ? statusPASS : statusFAIL;
                }
                catch (Exception)
                {
                    status = statusFAIL;
                }
            }

            string weightTypeText = wasWeightManual == DatabaseSettings.General_TrueValue_Read ? " " + weightSeq_manualWeight :
                                   (wasWeightManual == DatabaseSettings.General_FalseValue_Read ? " " + weightSeq_autoWeight : "");
            string unitText = " (" + unit + ")";

            string[,] tableValues = new string[,] {
                { weightSeq_ValueField + unitText, actualValue },
                { weightSeq_setpointField + unitText, setpoint },
                { weightSeq_minimumField + unitText, minimum },
                { weightSeq_maximumField + unitText, maximum },
                { weightSeq_statusField, status } };

            XRect rect;
            double xShift = (page.Width - 2 * margin) / tableValues.GetLength(0);

            // Séquence Pesée
            rect = new XRect(x: margin, y: y, width: page.Width - 2 * margin, height: weightSeq_RowHeight);
            gfxs[pagesNumber - 1].DrawRectangle(new XPen(XColors.Black, rectTablesSize), BrushGrey1, rect);
            rect.X += marginL_Tables;
            rect.Width -= marginL_Tables;
            gfxs[pagesNumber - 1].DrawString(sequenceField + " " + n.ToString() + " - " + weightSeq_SeqField + weightTypeText, new XFont(fontDoc, fontBodySize1), XBrushes.Black, rect, XStringFormats.CenterLeft);

            // Produit
            rect = new XRect(x: margin, y: y + 1 * weightSeq_RowHeight, width: (page.Width / 2) - margin, height: weightSeq_RowHeight);
            gfxs[pagesNumber - 1].DrawRectangle(new XPen(XColors.Black, rectTablesSize), BrushGrey2, rect);
            rect.X += marginL_Tables;
            rect.Width -= marginL_Tables;
            gfxs[pagesNumber - 1].DrawString(weightSeq_productField + product, new XFont(fontDoc, fontBodySize1), XBrushes.Black, rect, XStringFormats.CenterLeft);

            // Date et heure de pesée
            double widthDateTimeField = gfxs[pagesNumber - 1].MeasureString(weightSeq_DateTimeField, new XFont(fontDoc, fontBodySize1)).Width;
            rect = new XRect(x: (page.Width / 2), y: y + 1 * weightSeq_RowHeight, width: widthDateTimeField + 2 * marginL_Tables, height: weightSeq_RowHeight);
            gfxs[pagesNumber - 1].DrawRectangle(new XPen(XColors.Black, rectTablesSize), BrushGrey2, rect);
            rect.X += marginL_Tables;
            rect.Width -= marginL_Tables;
            gfxs[pagesNumber - 1].DrawString(weightSeq_DateTimeField, new XFont(fontDoc, fontBodySize1), XBrushes.Black, rect, XStringFormats.CenterLeft);

            rect = new XRect(x: (page.Width / 2) + widthDateTimeField + 2 * marginL_Tables, y: y + 1 * weightSeq_RowHeight, width: (page.Width / 2) - widthDateTimeField - 2 * marginL_Tables - margin, height: weightSeq_RowHeight);
            gfxs[pagesNumber - 1].DrawRectangle(new XPen(XColors.Black, rectTablesSize), rect);
            rect.X += marginL_Tables;
            rect.Width -= marginL_Tables;
            gfxs[pagesNumber - 1].DrawString(dateTime, new XFont(fontDoc, fontBodySize1), XBrushes.Black, rect, XStringFormats.CenterLeft);

            gfxs[pagesNumber - 1].DrawLine(XPens.Black,
                x1: margin, y1: y + 2 * weightSeq_RowHeight,
                x2: page.Width - margin, y2: y + 2 * weightSeq_RowHeight);

            // Tableau des valeurs
            for (int i = 0; i < tableValues.GetLength(0); i++)
            {
                // Champs
                rect = new XRect(x: margin + i * xShift, y: y + 2 * weightSeq_RowHeight, width: xShift, height: weightSeq_RowHeight);
                gfxs[pagesNumber - 1].DrawRectangle(new XPen(XColors.Black, rectTablesSize), BrushGrey2, rect);
                gfxs[pagesNumber - 1].DrawString(tableValues[i, 0], new XFont(fontDoc, fontBodySize2), XBrushes.Black, rect, XStringFormats.Center);

                // Valeurs
                rect.Y += weightSeq_RowHeight;
                gfxs[pagesNumber - 1].DrawRectangle(new XPen(XColors.Black, rectTablesSize),
                    (i == 0 ? XBrushes.White :
                    (i == weightSeq_statusColumnNumber ?
                    (status == statusPASS ? BrushGreen2 :
                    (status == statusFAIL ? BrushRed2 : BrushGrey1)) : BrushGrey3)),
                    rect);
                gfxs[pagesNumber - 1].DrawString(tableValues[i, 1], new XFont(fontDoc, fontBodySize2),
                    i == weightSeq_statusColumnNumber ? (status == statusPASS ? BrushGreen1 : (status == statusFAIL ? BrushRed1 : BrushGrey0)) : XBrushes.Black, rect,
                    XStringFormats.Center);
            }
            return returnValue;
        }

        //
        // generateSpeedMixerSeq
        // 
        private readonly double smSeq_marginL_Cells = 10; // Et marginL_Tables alors ?
        private readonly double smSeq_RowHeight = 25;
        private readonly string smSeq_SeqField = "SpeedMixer";
        private readonly string smSeq_dtStartField = "Début séquence";
        private readonly string smSeq_dtEndField = "Fin séquence";
        private readonly string smSeq_timeMixThField = "Durée Mix théorique";
        private readonly int smSeq_timeMixThId = 2;
        private readonly string smSeq_timeMixEffField = "Durée Mix effective";
        private readonly string smSeq_timeSeqEffField = "Durée séquence";

        private readonly string smSeq_descriptionField = "Description";
        private readonly string smSeq_speedField = "Vitesse";
        private readonly string smSeq_pressureField = "Pression";
        private readonly string smSeq_recipeParamField = "Paramètre de recette";
        private readonly int smSeq_recipeParamId = 1;
        private readonly string smSeq_meanField = "Moyenne";
        private readonly int smSeq_meanId = 2;
        private readonly string smSeq_stdField = "Ecart type";

        private readonly string smSeq_speedUnit = "RPM";

        private double GenerateSpeedMixerSeq(PdfPage page, int n, double y, object[] cycleSeqValues)
        {
            logger.Debug("GenerateSpeedMixerSeq");

            CycleSpeedMixerInfo cycleSpeedMixerInfo = new CycleSpeedMixerInfo();

            string dtStartSpeedMixerSeq = cycleSeqValues[cycleSpeedMixerInfo.DateTimeStart].ToString() == "" ? na : Convert.ToDateTime(cycleSeqValues[cycleSpeedMixerInfo.DateTimeStart].ToString()).ToString(Settings.Default.DateTime_Format_Read);
            string dtEndSpeedMixerSeq = cycleSeqValues[cycleSpeedMixerInfo.DateTimeEnd].ToString() == "" ? na : Convert.ToDateTime(cycleSeqValues[cycleSpeedMixerInfo.DateTimeEnd].ToString()).ToString(Settings.Default.DateTime_Format_Read);
            string timeMixTh = cycleSeqValues[cycleSpeedMixerInfo.TimeSeqTh].ToString();
            string timeMixEff = cycleSeqValues[cycleSpeedMixerInfo.TimeSeqEff].ToString() == "" ? na : cycleSeqValues[cycleSpeedMixerInfo.TimeSeqEff].ToString();
            string timeSpeedMixerSeq;

            string pressureUnit = cycleSeqValues[cycleSpeedMixerInfo.PressureUnit].ToString();

            string speedMin = cycleSeqValues[cycleSpeedMixerInfo.SpeedMin].ToString();
            string speedMax = cycleSeqValues[cycleSpeedMixerInfo.SpeedMax].ToString();
            string speedParam = speedMin + smSeq_speedUnit + " - " + speedMax + smSeq_speedUnit;

            string pressureMin = cycleSeqValues[cycleSpeedMixerInfo.PressureMin].ToString();
            string pressureMax = cycleSeqValues[cycleSpeedMixerInfo.PressureMax].ToString();
            string pressureParam = pressureMin + pressureUnit + " - " + pressureMax + pressureUnit;

            double speedMean;
            try
            {
                speedMean = double.Parse(cycleSeqValues[cycleSpeedMixerInfo.SpeedAvg].ToString());
            }
            catch (Exception ex)
            {
                speedMean = -1;
                logger.Error(ex.Message);
            }
            string speedMean_s = speedMean == -1 ? na : (speedMean).ToString("N0") + smSeq_speedUnit;

            double pressureMean;
            try
            {
                pressureMean = double.Parse(cycleSeqValues[cycleSpeedMixerInfo.PressureAvg].ToString());
            }
            catch (Exception ex)
            {
                pressureMean = -1;
                logger.Error(ex.Message);
            }

            string pressureMean_s = pressureMean == -1 ? na : (pressureMean).ToString("N2") + pressureUnit;
            //string pressureMean_s = cycleSeqValues[cycleSpeedMixerInfo.PressureAvg].ToString() == "" ? na : (double.Parse(cycleSeqValues[cycleSpeedMixerInfo.PressureAvg].ToString()).ToString("N2") + pressureUnit);

            string speedSTD = cycleSeqValues[cycleSpeedMixerInfo.SpeedStd].ToString() == "" ? na : (double.Parse(cycleSeqValues[cycleSpeedMixerInfo.SpeedStd].ToString()).ToString("N0") + smSeq_speedUnit);
            string pressureSTD = cycleSeqValues[cycleSpeedMixerInfo.PressureStd].ToString() == "" ? na : (double.Parse(cycleSeqValues[cycleSpeedMixerInfo.PressureStd].ToString()).ToString("N2") + pressureUnit);

            XRect rect;
            double currentShift = 0;
            double currentCellWidth;
            XStringFormat stringFormat;
            double returnValue = y + 6 * smSeq_RowHeight;

            // Calcul de la durée de la séquence
            try
            {
                DateTime dtStartSpeedMixerSeq_t = Convert.ToDateTime(dtStartSpeedMixerSeq);
                DateTime dtEndSpeedMixerSeq_t = Convert.ToDateTime(dtEndSpeedMixerSeq);
                TimeSpan timeSpeedMixerSeq_t = dtEndSpeedMixerSeq_t.Subtract(dtStartSpeedMixerSeq_t);
                timeSpeedMixerSeq = timeSpeedMixerSeq_t.Hours.ToString("00") + ":" + timeSpeedMixerSeq_t.Minutes.ToString("00") + ":" + timeSpeedMixerSeq_t.Seconds.ToString("00");
            }
            catch (Exception)
            {
                timeSpeedMixerSeq = na;
            }

            // Caclul du résultat des moyennes
            bool meanResultSpeed;
            try
            {
                meanResultSpeed = speedMean >= double.Parse(speedMin) && speedMean <= double.Parse(speedMax);
            }
            catch (Exception ex)
            {
                meanResultSpeed = false;
                logger.Error(ex.Message);
            }

            bool meanResultPressure;

            try
            {
                meanResultPressure = pressureMean >= double.Parse(pressureMin) && pressureMean <= double.Parse(pressureMax);
            }
            catch (Exception ex)
            {
                meanResultPressure = false;
                logger.Error(ex.Message);
            }

            string[,] tableValues1 = new string[,] {
                { smSeq_dtStartField, dtStartSpeedMixerSeq },
                { smSeq_dtEndField, dtEndSpeedMixerSeq },
                { smSeq_timeMixThField, timeMixTh },
                { smSeq_timeMixEffField, timeMixEff },
                { smSeq_timeSeqEffField, timeSpeedMixerSeq } };
            double xShift1 = (page.Width - 2 * margin) / tableValues1.GetLength(0);

            string[,] tableValues2 = new string[,] {
                { smSeq_descriptionField, smSeq_speedField, smSeq_pressureField },
                { smSeq_recipeParamField, speedParam, pressureParam },
                { smSeq_meanField, speedMean_s, pressureMean_s },
                { smSeq_stdField, speedSTD, pressureSTD } };
            double xShift2 = (page.Width - 2 * margin) / (tableValues2.GetLength(0) + 1);

            if (IsIndextoLow(page, returnValue)) return -1;

            // Séquence SpeedMixer
            rect = new XRect(x: margin, y: y, width: page.Width - 2 * margin, height: smSeq_RowHeight);
            gfxs[pagesNumber - 1].DrawRectangle(new XPen(XColors.Black, rectTablesSize), BrushGrey1, rect);
            rect.X += marginL_Tables;
            rect.Width -= marginL_Tables;
            gfxs[pagesNumber - 1].DrawString(sequenceField + " " + n.ToString() + " - " + smSeq_SeqField, new XFont(fontDoc, fontBodySize1), XBrushes.Black, rect, XStringFormats.CenterLeft);

            // Lignes 2 et 3 - Info générale de la séquence (Date, time)
            for (int i = 0; i < tableValues1.GetLength(0); i++)
            {
                // Champs
                rect = new XRect(x: margin + i * xShift1, y: y + 1 * smSeq_RowHeight, width: xShift1, height: smSeq_RowHeight);
                gfxs[pagesNumber - 1].DrawRectangle(new XPen(XColors.Black, rectTablesSize), BrushGrey2, rect);
                gfxs[pagesNumber - 1].DrawString(tableValues1[i, 0], new XFont(fontDoc, fontBodySize2), XBrushes.Black, rect, XStringFormats.Center);

                // Valeurs
                rect.Y += smSeq_RowHeight;
                gfxs[pagesNumber - 1].DrawRectangle(new XPen(XColors.Black, rectTablesSize),
                    (i == smSeq_timeMixThId ? BrushGrey3 : XBrushes.White),
                    rect);
                gfxs[pagesNumber - 1].DrawString(tableValues1[i, 1], new XFont(fontDoc, fontBodySize2), XBrushes.Black, rect, XStringFormats.Center);
            }

            gfxs[pagesNumber - 1].DrawLine(XPens.Black,
                x1: margin, y1: y + 3 * smSeq_RowHeight,
                x2: page.Width - margin, y2: y + 3 * smSeq_RowHeight);

            // Lignes 4 à 6 - Info vitesse et pression
            for (int i = 0; i < tableValues2.GetLength(0); i++)
            {
                if (i == 0) currentCellWidth = 2 * xShift2;
                else currentCellWidth = xShift2;

                // Rectangle ligne 4 - Description
                rect = new XRect(x: margin + currentShift, y: y + 3 * smSeq_RowHeight, width: currentCellWidth, height: smSeq_RowHeight);
                gfxs[pagesNumber - 1].DrawRectangle(new XPen(XColors.Black, rectTablesSize), BrushGrey2, rect);
                rect.Y += smSeq_RowHeight;

                // Rectangle ligne 5 - Vitesse
                gfxs[pagesNumber - 1].DrawRectangle(new XPen(XColors.Black, rectTablesSize),
                    (i == 0 ? BrushGrey3 : (i == smSeq_recipeParamId ? BrushGrey3 :
                    (i == smSeq_meanId ?
                    (tableValues2[i, 1] == na ? BrushGrey1 : (meanResultSpeed ? BrushGreen2 : BrushRed2)) : XBrushes.White))),
                    rect);
                rect.Y += smSeq_RowHeight;

                // Rectangle ligne 6 - Pression
                gfxs[pagesNumber - 1].DrawRectangle(new XPen(XColors.Black, rectTablesSize),
                    (i == 0 ? BrushGrey3 : (i == smSeq_recipeParamId ? BrushGrey3 :
                    (i == smSeq_meanId ?
                    (tableValues2[i, 2] == na ? BrushGrey1 : (meanResultPressure ? BrushGreen2 : BrushRed2)) : XBrushes.White))),
                    rect);

                if (i == 0)
                {
                    rect.X += marginL_Tables;
                    rect.Width -= marginL_Tables;
                    stringFormat = XStringFormats.CenterLeft;
                }
                else
                {
                    stringFormat = XStringFormats.Center;
                }

                // Champs
                rect.Y = y + 3 * smSeq_RowHeight;
                gfxs[pagesNumber - 1].DrawString(tableValues2[i, 0], new XFont(fontDoc, fontBodySize2), XBrushes.Black, rect, stringFormat);

                // Vitesse
                rect.Y += smSeq_RowHeight;
                gfxs[pagesNumber - 1].DrawString(tableValues2[i, 1], new XFont(fontDoc, fontBodySize2),
                    i == smSeq_meanId ?
                    (tableValues2[i, 1] == na ? BrushGrey0 : (meanResultSpeed ? BrushGreen1 : BrushRed1)) : XBrushes.Black,
                    rect, stringFormat);

                // Pression
                rect.Y += smSeq_RowHeight;
                gfxs[pagesNumber - 1].DrawString(tableValues2[i, 2], new XFont(fontDoc, fontBodySize2),
                    i == smSeq_meanId ?
                    (tableValues2[i, 2] == na ? BrushGrey0 : (meanResultPressure ? BrushGreen1 : BrushRed1)) : XBrushes.Black,
                    rect, stringFormat);

                currentShift += currentCellWidth;
            }

            return returnValue;
        }

        //
        // generateAlarmHistory
        //
        private readonly double Alarm_marginB_Title = 15;
        private readonly double Alarm_marginB_Alarms = 5;
        private readonly string Alarm_Title = "Historique des alarmes";
        private readonly string Alarm_Continued = "(suite)";
        private readonly string Alarm_NoAlarm = "Il n'y a pas eu d'alarme pendant le cycle";

        private double GenerateAlarmHistory(PdfPage page, double y)
        {
            logger.Debug("GenerateAlarmHistory");

            List<Alarm> historicAlarms = new List<Alarm>();
            double minAlarmHeight = page.Height / 2;

            double currentY = y;
            double heightAlarmText = fontDoc_fontBodySize1_Height;
            string alarm;
            string timestamp;
            string description;
            string status;
            double totalHeight;

            totalHeight = 2 * Alarm_marginB_Title + fontDoc_fontTitleSize_Height + Alarm_marginB_Title +
                (historicAlarms.Count == 0 ? 1 : historicAlarms.Count) * (heightAlarmText + Alarm_marginB_Alarms)
                - Alarm_marginB_Alarms;

            if (y > minAlarmHeight && (page.Height - y - margin - heightFooter) < totalHeight)
            {
                currentY = NewPage(page);
            }
            else
            {
                currentY += 2 * Alarm_marginB_Title;
            }

            // Titre
            gfxs[pagesNumber - 1].DrawString(Alarm_Title,
                new XFont(fontDoc, fontTitleSize),
                XBrushes.Black, x: margin, y: currentY, XStringFormats.TopLeft);
            currentY += Alarm_marginB_Title + fontDoc_fontTitleSize_Height;

            if (firstAlarmId != lastAlarmId && lastAlarmId != -1)
            {
                //AuditTrailInfo auditTrailInfo = new AuditTrailInfo();
                // nul corriger ça
                Task<object> t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetAlarms_new(firstAlarmId, lastAlarmId); });
                AuditTrailInfo auditTrailInfo = new AuditTrailInfo();
                List<object[]> tablesValues = (List<object[]>)t.Result;
                //List <AuditTrailInfo> tables = MyDatabase.GetAlarms(firstAlarmId, lastAlarmId);

                for (int i = 0; i < tablesValues.Count; i++)
                {
                    if (tablesValues[i] != null)
                    {
                        timestamp = tablesValues[i][auditTrailInfo.DateTime].ToString();
                        description = tablesValues[i][auditTrailInfo.Description].ToString();
                        status = tablesValues[i][auditTrailInfo.ValueAfter].ToString();
                        alarm = timestamp + " - " + description + " - " + status;

                        gfxs[pagesNumber - 1].DrawString(alarm,
                            new XFont(fontDoc, fontBodySize1),
                            status == AlarmStatus.ACTIVE.ToString() ? XBrushes.Red :
                            ((status == AlarmStatus.ACK.ToString() || status == AlarmStatus.RAZACK.ToString()) ? XBrushes.Blue :
                            ((status == AlarmStatus.INACTIVE.ToString() || status == AlarmStatus.RAZ.ToString()) ? XBrushes.Green : XBrushes.Black)),
                            x: margin, y: currentY, XStringFormats.TopLeft);
                        currentY += Alarm_marginB_Alarms + heightAlarmText;

                        // Si on a atteint le bas de la page, on crée un nouvelle page
                        if (IsIndextoLow(page, currentY + Alarm_marginB_Alarms + heightAlarmText))
                        {
                            currentY = NewPage(page);

                            gfxs[pagesNumber - 1].DrawString(Alarm_Title + " " + Alarm_Continued,
                                new XFont(fontDoc, fontTitleSize),
                                XBrushes.Black, x: margin, y: currentY, XStringFormats.TopLeft);
                            currentY += Alarm_marginB_Title + fontDoc_fontTitleSize_Height;
                        }
                    }
                }
            }
            else
            {
                gfxs[pagesNumber - 1].DrawString(Alarm_NoAlarm,
                    new XFont(fontDoc, fontBodySize1),
                    XBrushes.Black,
                    x: margin, y: currentY, XStringFormats.TopLeft);
                currentY += Alarm_marginB_Alarms + heightAlarmText;
            }

            return currentY - Alarm_marginB_Alarms;
        }

        //
        // generateCommentSignature
        //
        private readonly double marginH_TitleComment = 20;
        private readonly double marginB_TitleComment = 10;
        private readonly double margin_CommentText = 10;
        private readonly string CommentTitle = "Commentaires";
        private readonly string signatureUserName = "Nom d'utilisateur";
        private readonly string signatureDate = "Date";
        private readonly string signatureVISA = "VISA utilisateur";
        private readonly double rectCommentOutlineSize = 0.5;
        private readonly double rectSignatureOutlineSize = 1;
        private readonly double signatureRowHeight = 30;
        private readonly double minCommentHeight = 100;
        private readonly double cycleResultRowHeight = 30;
        private readonly string cycleResultText = "Résultat de cycle (barrer mention inutile): REUSSI / ECHOUE";

        private void GenerateCommentSignature(PdfPage page, double y)
        {

            logger.Debug("GenerateCommentSignature");

            double currentY = y;
            XRect rect;
            double commentHeight;
            string[] arraySignature = new string[] { signatureUserName, "", signatureDate, "" };

            if (CalculateCommentHeight(page, y) < minCommentHeight)
            {
                // New page
                currentY = NewPage(page);
            }

            commentHeight = CalculateCommentHeight(page, currentY);

            // Commentaire titre
            gfxs[pagesNumber - 1].DrawString(CommentTitle,
                new XFont(fontDoc, fontTitleSize),
                XBrushes.Black, x: margin, y: currentY, XStringFormats.TopLeft);
            currentY += marginB_TitleComment + fontDoc_fontTitleSize_Height;

            // Commentaire rectangle
            rect = new XRect(x: margin, y: currentY, width: page.Width - 2 * margin, height: commentHeight);
            gfxs[pagesNumber - 1].DrawRectangle(new XPen(XColors.Black, rectCommentOutlineSize), rect);
            rect.X += margin_CommentText;
            rect.Y += margin_CommentText;
            rect.Width -= margin_CommentText;
            rect.Height -= margin_CommentText;
            gfxs[pagesNumber - 1].DrawString(comment, new XFont(fontDoc, fontBodySize1), XBrushes.Black, rect, XStringFormats.TopLeft);
            currentY += commentHeight;

            // Résultat de test
            rect = new XRect(x: margin, y: currentY, width: page.Width - 2 * margin, height: cycleResultRowHeight);
            gfxs[pagesNumber - 1].DrawString(cycleResultText, new XFont(fontDoc, fontBodySize1), XBrushes.Black, rect, XStringFormats.CenterLeft);
            currentY += rect.Height;

            // Signature: Nom opérateur et date
            for (int i = 0; i < arraySignature.Length; i++)
            {
                rect = new XRect(x: margin + i % 2 * (page.Width - 2 * margin) / 4, y: currentY + (int)(i / 2) * signatureRowHeight, width: (page.Width - 2 * margin) / 4, height: signatureRowHeight);
                gfxs[pagesNumber - 1].DrawRectangle(new XPen(XColors.Black, rectCommentOutlineSize),
                    i % 2 == 0 ? BrushGrey2 : XBrushes.White,
                    rect);
                gfxs[pagesNumber - 1].DrawString(arraySignature[i], new XFont(fontDoc, fontBodySize1), XBrushes.Black, rect, XStringFormats.Center);
            }

            // VISA opérateur
            rect = new XRect(x: margin + (page.Width - 2 * margin) / 2, y: currentY, width: (page.Width - 2 * margin) / 4, height: 2 * signatureRowHeight);
            gfxs[pagesNumber - 1].DrawRectangle(new XPen(XColors.Black, rectCommentOutlineSize), BrushGrey2, rect);
            gfxs[pagesNumber - 1].DrawString(signatureVISA, new XFont(fontDoc, fontBodySize1), XBrushes.Black, rect, XStringFormats.Center);
            rect.X += rect.Width;
            gfxs[pagesNumber - 1].DrawRectangle(new XPen(XColors.Black, rectCommentOutlineSize), rect);

            // Rectangles gras
            rect = new XRect(x: margin, y: currentY, width: (page.Width - 2 * margin) / 2, height: 2 * signatureRowHeight);
            gfxs[pagesNumber - 1].DrawRectangle(new XPen(XColors.Black, rectSignatureOutlineSize), rect);
            rect.X += rect.Width;
            gfxs[pagesNumber - 1].DrawRectangle(new XPen(XColors.Black, rectSignatureOutlineSize), rect);
        }
        private void GenerateCroquis1(PdfPage page)
        {
            logger.Debug("GenerateCroquis1");

            // Header / Footer
            gfxs[pagesNumber - 1].DrawLine(XPens.LightGray,
                x1: 0, y1: margin,
                x2: page.Width, y2: margin);
            gfxs[pagesNumber - 1].DrawLine(XPens.LightGray,
                x1: margin, y1: 0,
                x2: margin, y2: page.Height);
            gfxs[pagesNumber - 1].DrawLine(XPens.LightGray,
                x1: 0, y1: page.Height - margin,
                x2: page.Width, y2: page.Height - margin);
            gfxs[pagesNumber - 1].DrawLine(XPens.LightGray,
                x1: page.Width - margin, y1: 0,
                x2: page.Width - margin, y2: page.Height);
            gfxs[pagesNumber - 1].DrawLine(XPens.LightGray,
                x1: margin, y1: margin + heightLogo,
                x2: page.Width - margin, y2: margin + heightLogo);
            gfxs[pagesNumber - 1].DrawLine(XPens.LightGray,
                x1: 0, y1: page.Height - margin - marginH_Footer,
                x2: page.Width, y2: page.Height - margin - marginH_Footer);
            gfxs[pagesNumber - 1].DrawLine(XPens.LightGray,
                x1: 0, y1: page.Height - margin - heightFooter,
                x2: page.Width, y2: page.Height - margin - heightFooter);
        }
        private void GenerateCroquis2(PdfPage page)
        {
            logger.Debug("GenerateCroquis2");

            double xShiftTotal = page.Width - margin - marginL_SubT - (margin + marginL_SubT);
            double widthLogo = this.widthLogo;
            double heightLogo = logo.PixelHeight * (widthLogo / logo.PixelWidth);

            // Subtitle
            gfxs[pagesNumber - 1].DrawLine(XPens.LightGray,
                x1: margin + marginL_SubT, y1: margin + heightLogo + rectTitleHeight + marginH_Title_SubT,
                x2: margin + marginL_SubT, y2: margin + heightLogo + rectTitleHeight + marginH_Title_SubT + rectTitleHeight);

            gfxs[pagesNumber - 1].DrawLine(XPens.LightGray,
                x1: margin + marginL_SubT + xShiftTotal, y1: margin + heightLogo + rectTitleHeight + marginH_Title_SubT,
                x2: margin + marginL_SubT + xShiftTotal, y2: margin + heightLogo + rectTitleHeight + marginH_Title_SubT + rectTitleHeight);
            gfxs[pagesNumber - 1].DrawLine(XPens.LightGray,
                x1: margin + marginL_SubT + (xShiftTotal / 4), y1: margin + heightLogo + rectTitleHeight + marginH_Title_SubT,
                x2: margin + marginL_SubT + (xShiftTotal / 4), y2: margin + heightLogo + rectTitleHeight + marginH_Title_SubT + rectTitleHeight);
            gfxs[pagesNumber - 1].DrawLine(XPens.LightGray,
                x1: margin + marginL_SubT + (2 * xShiftTotal / 4), y1: margin + heightLogo + rectTitleHeight + marginH_Title_SubT,
                x2: margin + marginL_SubT + (2 * xShiftTotal / 4), y2: margin + heightLogo + rectTitleHeight + marginH_Title_SubT + rectTitleHeight);
            gfxs[pagesNumber - 1].DrawLine(XPens.LightGray,
                x1: margin + marginL_SubT + (3 * xShiftTotal / 4), y1: margin + heightLogo + rectTitleHeight + marginH_Title_SubT,
                x2: margin + marginL_SubT + (3 * xShiftTotal / 4), y2: margin + heightLogo + rectTitleHeight + marginH_Title_SubT + rectTitleHeight);
            //gfx.DrawRectangle(new XPen(XColors.Blue, rectSubTitleOutlineSize), rectSubTitle_Items);
        }
        private bool IsIndextoLow(PdfPage page, double y)
        {
            logger.Debug("IsIndextoLow");

            return y > (page.Height - margin - heightFooter);
        }
        private void UpdatePagination(PdfPage page, XGraphics gfx, int i)
        {
            logger.Debug("UpdatePagination");

            gfx.DrawString("Page " + i.ToString() + " sur " + pagesNumber.ToString(),
                new XFont(fontDoc, fontFooterSize),
                XBrushes.Black, x: page.Width - margin - gfx.MeasureString("Page 1 sur x", new XFont(fontDoc, fontFooterSize)).Width, y: page.Height - margin, XStringFormats.BottomLeft);

            if (isTest)
            {
                // Variation 1: Draw a watermark as a text string.

                // Get the size (in points) of the text.
                var size = gfx.MeasureString(watermarkText, fontWatermark);

                // Define a rotation transformation at the center of the page.
                gfx.TranslateTransform(page.Width / 2, page.Height / 2);
                gfx.RotateTransform(-Math.Atan(page.Height / page.Width) * 180 / Math.PI);
                gfx.TranslateTransform(-page.Width / 2, -page.Height / 2);

                // Create a string format.
                var format = new XStringFormat
                {
                    Alignment = XStringAlignment.Near,
                    LineAlignment = XLineAlignment.Near
                };

                // Create a dimmed red brush.
                XBrush brush = new XSolidBrush(XColor.FromArgb(128, 255, 0, 0));

                // Draw the string.
                gfx.DrawString(watermarkText, fontWatermark, brush,
                    new XPoint((page.Width - size.Width) / 2, (page.Height - size.Height) / 2),
                    format);
            }
        }
        private double NewPage(PdfPage page)
        {
            pagesNumber++;
            page = document.AddPage();
            gfxs.Add(XGraphics.FromPdfPage(page));
            return GenerateSecondHeader(page);
        }
        private double CalculateCommentHeight(PdfPage page, double y)
        {
            logger.Debug("CalculateCommentHeight");

            return page.Height - y - marginB_TitleComment - fontDoc_fontTitleSize_Height - cycleResultRowHeight - 2 * signatureRowHeight - heightFooter - margin;
        }
        public void GenerateCycleReport(int id)
        {
            logger.Debug("PdfGenerator");

            int nextSeqType;
            int seqNumber = 1;
            Task<object> t;

            // Initialize cycle information
            // A CORRIGER : IF RESULT IS FALSE
            t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow_new(new CycleTableInfo(), id); });
            CycleTableInfo cycleTableInfo = new CycleTableInfo();
            object[] cycleTableValues = (object[])t.Result;

            ISeqTabInfo cycleSeqInfo;
            object[] cycleSeqValues;

            if (cycleTableValues == null)
            {
                logger.Error(Settings.Default.Report_Info_CycleInfoNotFound);
                MyMessageBox.Show(Settings.Default.Report_Info_CycleInfoNotFound);
                return;
            }

            jobNumber = cycleTableValues[cycleTableInfo.JobNumber].ToString();
            batchNumber = cycleTableValues[cycleTableInfo.BatchNumber].ToString();
            qtyNumber = cycleTableValues[cycleTableInfo.FinalWeight].ToString() + cycleTableValues[cycleTableInfo.FinalWeightUnit].ToString();
            itemNumber = cycleTableValues[cycleTableInfo.ItemNumber].ToString();
            recipeNameVersion = cycleTableValues[cycleTableInfo.RecipeName].ToString() + " - Révision " + int.Parse(cycleTableValues[cycleTableInfo.RecipeVersion].ToString()).ToString("000");
            equipmentName = cycleTableValues[cycleTableInfo.EquipmentName].ToString();
            dtStartCycle = Convert.ToDateTime(cycleTableValues[cycleTableInfo.DateTimeStartCycle].ToString()).ToString(Settings.Default.DateTime_Format_Read);
            dtEndCycle = Convert.ToDateTime(cycleTableValues[cycleTableInfo.DateTimeEndCycle].ToString()).ToString(Settings.Default.DateTime_Format_Read);
            user = cycleTableValues[cycleTableInfo.Username].ToString();
            firstAlarmId = cycleTableValues[cycleTableInfo.FirstAlarmId].ToString() == "" ? -1 : int.Parse(cycleTableValues[cycleTableInfo.FirstAlarmId].ToString());
            lastAlarmId = cycleTableValues[cycleTableInfo.LastAlarmId].ToString() == "" ? -1 : int.Parse(cycleTableValues[cycleTableInfo.LastAlarmId].ToString());
            comment = cycleTableValues[cycleTableInfo.Comment].ToString();
            isTest = cycleTableValues[cycleTableInfo.IsItATest].ToString() == DatabaseSettings.General_TrueValue_Read;
            bowlWeight = cycleTableValues[cycleTableInfo.bowlWeight].ToString();
            lastWeightTh = cycleTableValues[cycleTableInfo.lastWeightTh].ToString();
            lastWeightEff = cycleTableValues[cycleTableInfo.lastWeightEff].ToString();

            templateNumber = cycleTemplateNumber;

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            document = new PdfDocument();

            PdfPage page = document.AddPage();
            gfxs.Add(XGraphics.FromPdfPage(page));

            // Calcul des tailles de texte
            fontDoc_fontTitleSize_Height = gfxs[pagesNumber - 1].MeasureString("X", new XFont(fontDoc, fontTitleSize)).Height;
            fontDoc_fontBodySize1_Height = gfxs[pagesNumber - 1].MeasureString("X", new XFont(fontDoc, fontBodySize1)).Height;

            // Calcul de la hauteur du footer
            heightFooter = 2 * marginH_Footer;

            double currentY;
            currentY = GenerateHeader(page);
            currentY = GenerateFirstTitle(page, currentY);
            currentY = GenerateGeneralInfo(page, currentY + marginH_GeneralInfo);

            cycleSeqInfo = cycleTableInfo;
            cycleSeqValues = cycleTableValues;

            while (cycleSeqValues[cycleSeqInfo.NextSeqType] != null && cycleSeqValues[cycleSeqInfo.NextSeqType].ToString() != "")
            {
                nextSeqType = int.Parse(cycleSeqValues[cycleSeqInfo.NextSeqType].ToString());
                // A CORRIGER : IF RESULT IS FALSE
                t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow_new(Pages.Sequence.list[nextSeqType].subCycleInfo, int.Parse(cycleSeqValues[cycleSeqInfo.NextSeqId].ToString())); });
                cycleSeqValues = (object[])t.Result;

                if (cycleSeqValues == null)
                {
                    logger.Error(Settings.Default.Report_Info_CycleInfoNotFound);
                    MyMessageBox.Show(Settings.Default.Report_Info_CycleInfoNotFound);
                    return;
                }

                currentY = GenerateSequence(nextSeqType, page, seqNumber, currentY + smSeq_marginL_Cells, cycleSeqValues);
                if (currentY == -1)
                {
                    currentY = NewPage(page);
                    currentY = GenerateSequence(nextSeqType, page, seqNumber, currentY + smSeq_marginL_Cells, cycleSeqValues);
                }
                if (currentY == -2) return;

                seqNumber++;
            }

            // Historique d'alarme
            currentY = GenerateAlarmHistory(page, currentY);

            GenerateCommentSignature(page, currentY + marginH_TitleComment);

            for (int i = 0; i < document.Pages.Count; i++) UpdatePagination(document.Pages[i], gfxs[i], i + 1);

            string fileName = cycleReportPath + generationDateTime.ToString("yyyy.MM.dd_HH.mm.ss") + "_" +
                jobNumber + "_" + batchNumber + "_" + itemNumber;// + ".pdf";
            string suffix = "";
            int index = 0;

            while (File.Exists(fileName + suffix + ".pdf"))
            {
                index++;
                suffix = "_" + index.ToString();
            }

            fileName = fileName + suffix + ".pdf";

            if (!File.Exists(fileName))
            {
                try
                {
                    document.Save(fileName);
                    PrintPaperOnce(fileName);
                }
                catch (Exception ex)
                {
                    MyMessageBox.Show(ex.Message);
                }
            }
        }

        public void GenerateDailyTestReport(int id)
        {

            logger.Debug("GenerateSamplingReport");

            Task<object> t;

            // Initialize cycle information
            // A CORRIGER : IF RESULT IS FALSE
            t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow_new(new DailyTestInfo(), id); });
            DailyTestInfo dailyTestInfo = new DailyTestInfo();
            object[] dailyTestValues = (object[])t.Result;


            if (dailyTestValues == null)
            {
                logger.Error(Settings.Default.Report_Info_CycleInfoNotFound);
                MyMessageBox.Show(Settings.Default.Report_Info_CycleInfoNotFound);
                return;
            }

            user = dailyTestValues[dailyTestInfo.Username].ToString();
            dateTimeSampling = Convert.ToDateTime(dailyTestValues[dailyTestInfo.DateTime].ToString()).ToString(Settings.Default.DateTime_Format_Read);
            equipmentName = dailyTestValues[dailyTestInfo.EquipmentName].ToString();
            samplingStatus = dailyTestValues[dailyTestInfo.Status].ToString() == DatabaseSettings.General_TrueValue_Read ? statusPASS : statusFAIL;

            templateNumber = samplingTemplateNumber;

            int nSamples = 0;

            while (dailyTestValues[dailyTestInfo.Measure1 + nSamples].ToString() != "" && nSamples < dailyTestInfo.SamplesNumber)
            {
                nSamples++;
            }

            samplingRefs = new string[nSamples];
            samplingMeas = new string[nSamples];

            for (int i = 0; i < nSamples; i++)
            {
                try
                {
                    samplingRefs[i] = decimal.Parse(dailyTestValues[dailyTestInfo.Setpoint1 + i].ToString()).ToString("N" + Settings.Default.RecipeWeight_NbDecimal);
                    samplingMeas[i] = decimal.Parse(dailyTestValues[dailyTestInfo.Measure1 + i].ToString()).ToString("N" + Settings.Default.RecipeWeight_NbDecimal);
                }
                catch (Exception ex)
                {
                    logger.Error(ex.Message);
                    samplingRefs[i] = na;
                    samplingMeas[i] = na;
                    samplingStatus = statusFAIL;
                }
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            document = new PdfDocument();

            PdfPage page = document.AddPage();
            gfxs.Add(XGraphics.FromPdfPage(page));

            // Calcul des tailles de texte
            fontDoc_fontTitleSize_Height = gfxs[pagesNumber - 1].MeasureString("X", new XFont(fontDoc, fontTitleSize)).Height;
            fontDoc_fontBodySize1_Height = gfxs[pagesNumber - 1].MeasureString("X", new XFont(fontDoc, fontBodySize1)).Height;

            // Calcul de la hauteur du footer
            heightFooter = 2 * marginH_Footer;

            double currentY;
            currentY = GenerateHeader(page);
            currentY = DrawTitle(page, currentY, samplingDocTitle);

            string[] values = new string[] {
                equipmentNameField + ": " + equipmentName,
                SamplingDateTimeField + dateTimeSampling,
                userField + ": " + user,
                statusField + ": " + samplingStatus};

            currentY = DrawStringColumns(page, currentY, values, 2);

            string[,] tableValues = new string[4,3];

            tableValues[0, 0] = "Masse étalon (g)";
            tableValues[1, 0] = "Masse pesée (g)";
            tableValues[2, 0] = "Ecart (g)";
            tableValues[3, 0] = "Statut";

            for (int i = 0; i < samplingRefs.Length; i++)
            {
                tableValues[0, 1 + i] = samplingRefs[i];
                tableValues[1, 1 + i] = samplingMeas[i];
                tableValues[2, 1 + i] = (decimal.Parse(samplingRefs[i]) - decimal.Parse(samplingMeas[i])).ToString("N" + Settings.Default.RecipeWeight_NbDecimal);
                tableValues[3, 1 + i] = Pages.SubCycle.CycleWeightOld.IsSampWeightCorrect(decimal.Parse(samplingMeas[i]), decimal.Parse(samplingRefs[i])) ? "PASS" : "FAIL";
            }
            
            double xShift = (page.Width - 2 * margin) / tableValues.GetLength(0);
            XRect rect;

            for (int i = 0; i < tableValues.GetLength(0); i++)
            {
                // Champs
                rect = new XRect(x: margin + i * xShift, y: currentY, width: xShift, height: smSeq_RowHeight);
                gfxs[pagesNumber - 1].DrawRectangle(new XPen(XColors.Black, rectTablesSize), BrushGrey2, rect);
                gfxs[pagesNumber - 1].DrawString(tableValues[i, 0], new XFont(fontDoc, fontBodySize2), XBrushes.Black, rect, XStringFormats.Center);

                // Valeurs
                for (int j = 0; j < 2; j++)
                {
                    rect.Y += smSeq_RowHeight;
                    gfxs[pagesNumber - 1].DrawRectangle(new XPen(XColors.Black, rectTablesSize),
                        (i == statusId ? (tableValues[i, 1 + j] == statusPASS ? BrushGreen2 : BrushRed2) :
                        XBrushes.White),
                        rect);
                    gfxs[pagesNumber - 1].DrawString(tableValues[i, 1 + j], new XFont(fontDoc, fontBodySize2),
                        (i == statusId ? (tableValues[i, 1 + j] == statusPASS ? BrushGreen1 : BrushRed1) :
                        XBrushes.Black),
                        rect, PdfSharp.Drawing.XStringFormats.Center);
                }
            }
            currentY += tableValues.GetLength(1) * smSeq_RowHeight;

            GenerateCommentSignature(page, currentY + marginH_TitleComment);

            for (int i = 0; i < document.Pages.Count; i++) UpdatePagination(document.Pages[i], gfxs[i], i + 1);

            string fileName = samplingReportPath + generationDateTime.ToString("yyyy.MM.dd_HH.mm.ss") + "_" + samplingStatus;
            string suffix = "";
            int index = 0;

            while (File.Exists(fileName + suffix + ".pdf"))
            {
                index++;
                suffix = "_" + index.ToString();
            }

            fileName = fileName + suffix + ".pdf";

            if (!File.Exists(fileName))
            {
                document.Save(fileName);
                PrintPaperOnce(fileName);
            }
        }

        private bool PrintPaperOnce(string fileName)
        {
            //PrintQueue printQueue;
            //int count = 0;
            bool result;// = false;

            logger.Debug("On va lancer l'impression");

            ProcessStartInfo info = new ProcessStartInfo();
            info.Verb = "print";
            info.FileName = fileName;
            info.CreateNoWindow = true;
            info.WindowStyle = ProcessWindowStyle.Hidden;
            Process p = new Process();
            p.StartInfo = info;
            result = p.Start();

            if (!result && MyMessageBox.Show("L'impression a échoué, voulez-vous la relancer ?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                return PrintPaperOnce(fileName);
            }
            else if (!result)
            {
                return false;
            }

            MessageBoxResult boxResult = MyMessageBox.Show("L'impression a été lancé, pouvez-vous confirmer ?", MessageBoxButton.YesNo);

            try
            {
                if (!p.CloseMainWindow()) p.Kill();
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                MyMessageBox.Show(ex.Message);
            }

            if (boxResult == MessageBoxResult.No)
            {
                return PrintPaperOnce(fileName);
            }

            return result;
        }

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool LogonUser(string lpszUsername, string lpszDomain, string lpszPassword,
            int dwLogonType, int dwLogonProvider, out IntPtr phToken);
    }


    public class Impersonator : IDisposable
    {
        private readonly WindowsImpersonationContext _context;

        public Impersonator(string username, string password)
        {
            // Se connecter avec le nom d'utilisateur et le mot de passe fournis
            IntPtr token = IntPtr.Zero;
            bool success = LogonUser(username, null, password, 2, 0, out token);

            if (!success)
            {
                int error = Marshal.GetLastWin32Error();
                throw new Exception($"Impossible de se connecter avec les informations d'identification fournies. Code d'erreur : {error}");
            }

            // Obtenir l'identité de l'utilisateur connecté
            WindowsIdentity identity = new WindowsIdentity(token);

            // Commencer l'impersonation
            _context = identity.Impersonate();
        }

        public void Dispose()
        {
            // Terminer l'impersonation
            _context?.Undo();
        }

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool LogonUser(string lpszUsername, string lpszDomain, string lpszPassword,
            int dwLogonType, int dwLogonProvider, out IntPtr phToken);
    }

}