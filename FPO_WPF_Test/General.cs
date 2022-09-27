using Database;
using FPO_WPF_Test.Pages.SubCycle;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FPO_WPF_Test
{
    internal static class General
    {
        private static MyDatabase db = new MyDatabase();
        public static string Role = "";
        public static CycleInfo CurrentCycleInfo;
        public const string application_version = "1.0";
        public const string application_name = "MixingApplication";
        public const string equipement_name = "Mabcxyz";
        public static string loggedUsername = WindowsIdentity.GetCurrent().Name;

        public static void SetframeInfoCycle(Frame frame)
        {

        }

        public static bool Verify_Format(TextBox textBox, bool isNotNull, bool isNumber, int parameter, decimal min = -1, decimal max = -1)
        {
            /*
             * parameter:
             *              - si isNumber = false : le nombre de caractère max
             *              - si isNumber = true : le nombre de chiffre après la virgule
             */

            

            bool result = true;

            if (isNotNull && textBox.Text == "")
            {
                MessageBox.Show("Format incorrect, le champ ne peut pas être vide");
                return false;
            }

            if (isNumber)
            {
                try
                {
                    textBox.Text = Math.Round(decimal.Parse(textBox.Text), parameter).ToString("N" + parameter.ToString());

                    if (min != -1 && max != -1 && (decimal.Parse(textBox.Text) < min || decimal.Parse(textBox.Text) > max))
                    {
                        MessageBox.Show("Format incorrect, valeur en dehors de la gamme [" + min.ToString() + " ; " + max.ToString() + "]");

                        if (decimal.Parse(textBox.Text) < min)
                        {
                            textBox.Text = min.ToString();
                        }
                        else if (decimal.Parse(textBox.Text) > max)
                        {
                            textBox.Text = max.ToString();
                        }
                        else
                        {
                            MessageBox.Show("Drôle de situation");
                            return false;
                        }
                    }
                    else if (min != -1 && decimal.Parse(textBox.Text) < min)
                    {
                        MessageBox.Show("Format incorrect, valeur inférieure au minimum: " + min.ToString());
                        textBox.Text = min.ToString();
                    }
                    else if (max != -1 && decimal.Parse(textBox.Text) > max)
                    {
                        MessageBox.Show("Format incorrect, valeur valeur supérieur au maximum: " + max.ToString());
                        textBox.Text = max.ToString();
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("Format incorrect, ceci n'est pas un nombre");
                    textBox.Text = "";
                    return false;
                }
            }
            else if (textBox.Text.Length > parameter)
            {
                MessageBox.Show("Format incorrect, le champ doit contenir justqu'à " + parameter.ToString() + " caractères");
                return false;
            }
            return result;
        }
        public static void Update_RecipeNames(ComboBox comboBox, List<string> ProgramNames, List<string> ProgramIDs, bool onlyProdRecipes = false)
        {
            ProgramNames.Add("Bon tu la sélectionne cette recette !");
            comboBox.ItemsSource = ProgramNames;

            ProgramNames.Clear();
            ProgramIDs.Clear();

            ProgramNames.Add("Veuillez sélectionner une recette");
            comboBox.SelectedIndex = 0;
            ProgramNames.Clear();

            if (!db.IsConnected()) db.Connect();

            if (db.IsConnected())
            {
                db.SendCommand_GetLastRecipes(onlyProdRecipes);
                string[] array;

                if (!db.IsReaderNotAvailable())
                {
                    array = db.ReadNext();

                    while (array.Length > 0)
                    {
                        ProgramNames.Add(array[0]);
                        ProgramIDs.Add(array[1]);

                        array = db.ReadNext();
                    }
                }
                else
                {
                    MessageBox.Show("Sorry");
                }
                db.Disconnect();
            }
            else
            {
                ProgramNames.Add("###");
                ProgramNames.Add("###");
                ProgramNames.Add("###");

                MessageBox.Show("Not good brotha");
            }
        }
        public static void PrintReport(int id)
        {
            ReportGeneration report = new ReportGeneration();
            report.pdfGenerator(id.ToString());
        }
    }
}
