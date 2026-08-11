using EpidEvaluationStandalone.ViewModels;
using System;
using System.ComponentModel;
using System.Windows;
using ESAPI = VMS.TPS.Common.Model.API;
using PD = VMS.DV.PD.Scripting;

namespace EpidEvaluationStandalone
{
    /// <summary>
    /// Interaction logic for StandaloneWindow.xaml
    /// </summary>
    public partial class OpenPatientWindow : Window
    {
        private readonly ESAPI.Application appESAPI;
        private readonly PD.Application appPD;

        private readonly OpenPatientViewModel viewModel;

        public OpenPatientWindow()
        {
            InitializeComponent();

            try
            {
                appESAPI = ESAPI.Application.CreateApplication();
                appPD = PD.Application.CreateApplication();

                viewModel = new OpenPatientViewModel(appESAPI, appPD);
                viewModel.PatientOpened += ViewModel_PatientOpened;
                viewModel.PatientClosed += ViewModel_PatientClosed;
                DataContext = viewModel;

            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void ViewModel_PatientClosed(object sender, EventArgs e)
        {
            Show();
        }

        private void ViewModel_PatientOpened(object sender, EventArgs e)
        {
            Hide();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (viewModel != null)
                    viewModel.PatientOpened -= ViewModel_PatientOpened;
                if (appPD != null)
                {
                    appPD.ClosePatient();
                    appPD.Dispose();
                }
                if (appESAPI != null)
                {
                    appESAPI.ClosePatient();
                    appESAPI.Dispose();
                }
            }
            catch
            { }
        }
    }
}
