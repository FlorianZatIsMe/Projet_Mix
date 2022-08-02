using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FPO_WPF_Test.Pages
{
    /// <summary>
    /// Logique d'interaction pour Recipe.xaml
    /// </summary>
    public partial class Recipe : Page
    {
        private int nRow;
        public Recipe()
        {
            nRow = 1;
            InitializeComponent();
        }
        ~Recipe()
        {

        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            gridMain.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto)});

            Frame frame = new Frame();
            frame.MouseDoubleClick += new MouseButtonEventHandler(frame_test);
            frame.ContentRendered += Frame_ContentRendered;

            frame.Content = new SubRecipe.Weight(frame, nRow.ToString());

            Grid.SetRow(frame, gridMain.RowDefinitions.Count() - 1);
            gridMain.Children.Add(frame);
            nRow++;
        }

        private void Frame_ContentRendered(object sender, EventArgs e)
        {
            Frame frame = sender as Frame;
            
            if (frame.Content == null)
            {
                Grid grid = frame.Parent as Grid;
                grid.Children.Remove(frame);
                Update_SequenceNumbers();
                nRow--;
            }
        }

        private void Update_SequenceNumbers()
        {
            int i = 1;

            foreach (UIElement element in gridMain.Children)
            {
                if (element.GetType().Equals(typeof(Frame)))
                {
                    Frame frame = element as Frame;
                    if (frame.Content.GetType().Equals(typeof(SubRecipe.Weight)))
                    {
                        SubRecipe.Weight currentpage = frame.Content as SubRecipe.Weight;
                        currentpage.setSeqNumber(i.ToString());
                        i++;
                    }
                    else if (frame.Content.GetType().Equals(typeof(SubRecipe.SpeedMixer)))
                    {
                        SubRecipe.SpeedMixer currentpage = frame.Content as SubRecipe.SpeedMixer;
                        currentpage.setSeqNumber(i.ToString());
                        i++;
                    }
                }
            }
        }

        private void frame_test(object sender, RoutedEventArgs e)
        {
            Frame frame = sender as Frame;
            Grid grid = frame.Parent as Grid;
            int index = grid.Children.IndexOf(frame);

            MessageBox.Show(gridMain.RowDefinitions.Count().ToString() + " Row: " + Grid.GetRow(frame).ToString());
        }

/*
private class RecipeRow
{
public TextBlock textBlock { get; set; }
public Button button { get; set; }
public Border border { get; set; }
public Frame frame { get; set; }
public RecipeRow parent { get; set; }
}*/

    }
}
