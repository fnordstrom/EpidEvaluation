using EpidEvaluation.ViewModels;
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

namespace EpidEvaluation.Views
{
    /// <summary>
    /// Interaction logic for EvaluationControl.xaml
    /// </summary>
    public partial class EvaluationControl : UserControl
    {
        public EvaluationControl()
        {
            InitializeComponent();
        }

        private void Details_MouseDown(object sender, MouseButtonEventArgs e)
        {
            PortalDoseEvaluationViewModel m = (PortalDoseEvaluationViewModel)DataContext;
            new PredictionDetailsWindow(m.PortalDoseImage).ShowDialog();
        }
    }
}
