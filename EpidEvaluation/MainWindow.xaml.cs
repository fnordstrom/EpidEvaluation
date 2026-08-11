using EpidEvaluation.ViewModels;
using System.Windows;
using VMS.DV.PD.Scripting;
using ESAPI = VMS.TPS.Common.Model.API;

namespace EpidEvaluation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(Patient patient, ESAPI.Patient patientEclipse)
        {
            InitializeComponent();
            DataContext = new MainViewModel(patient, patientEclipse);
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            PatientItemViewModel p = e.NewValue as PatientItemViewModel;
            MainViewModel m = (MainViewModel)DataContext;
            m.PortalDoseEvaluationItems.Clear();
            foreach(PortalDoseImage portalDoseImage in p.PortalDoseImages)
                m.PortalDoseEvaluationItems.Add(new PortalDoseEvaluationViewModel(portalDoseImage));
        }
    }
}
