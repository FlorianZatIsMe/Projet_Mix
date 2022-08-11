using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FPO_WPF_Test.Pages
{
    internal class General
    {
        public General()
        {

        }

        public bool Verify_Format(TextBox textBox, bool isNotNull, bool isNumber, int parameter, decimal min = -1, decimal max = -1)
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

                    if ((min != -1 || max != -1) && (decimal.Parse(textBox.Text) < min || decimal.Parse(textBox.Text) > max))
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
    }
}
