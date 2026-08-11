using EpidEvaluation;
using EpidEvaluationStandalone;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ESAPI = VMS.TPS.Common.Model.API;
using PD = VMS.DV.PD.Scripting;

namespace EpidEvaluationStandalone.ViewModels
{
    public class OpenPatientViewModel : INotifyPropertyChanged
    {
        private readonly ESAPI.Application appESAPI;
        private readonly PD.Application appPD;
        private string patientId = string.Empty;
        private PatientViewModel selectedPatient;        

        public OpenPatientViewModel()
        {
            SearchCommand = new RelayCommand(SearchPatients);
            OpenCommand = new RelayCommand(OpenPatient, () => SelectedPatient != null);
        }

        public OpenPatientViewModel(ESAPI.Application appESAPI, PD.Application appPD) : this()
        {
            this.appESAPI = appESAPI;
            this.appPD = appPD;
        }

        public event EventHandler PatientOpened;
        public event EventHandler PatientClosed;

        /// <summary>
        /// Text entered in the Patient ID field.
        /// </summary>
        public string PatientId
        {
            get => patientId;
            set
            {
                if (patientId == value)
                    return;

                patientId = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Patients matching the entered Patient ID.
        /// </summary>
        public ObservableCollection<PatientViewModel> MatchingPatients { get; } = new ObservableCollection<PatientViewModel>();

        /// <summary>
        /// Patient selected in the ListBox.
        /// </summary>
        public PatientViewModel SelectedPatient
        {
            get => selectedPatient;
            set
            {
                if (selectedPatient == value)
                    return;

                selectedPatient = value;
                OnPropertyChanged();

                if (OpenCommand is RelayCommand command)
                    command.RaiseCanExecuteChanged();
            }
        }

        public ICommand SearchCommand { get; }

        public ICommand OpenCommand { get; }

        private void SearchPatients()
        {
            MatchingPatients.Clear();

            if (string.IsNullOrWhiteSpace(PatientId) || PatientId.Length < 3 || appESAPI == null)
                return;

            var matches = appESAPI.PatientSummaries.Where(x => x.Id.IndexOf(PatientId, StringComparison.OrdinalIgnoreCase) != -1).Select(x => new PatientViewModel(x));
            foreach(var match in matches) 
                MatchingPatients.Add(match);
        }

        private void OpenPatient()
        {
            if (SelectedPatient == null)
                return;

            try
            {
                ESAPI.Patient patientESAPI = appESAPI.OpenPatientById(SelectedPatient.PatientId);
                PD.Patient patientPD = appPD.OpenPatientById(SelectedPatient.PatientId);

                PatientOpened?.Invoke(this, EventArgs.Empty);

                MainWindow mainWindow = new MainWindow(patientPD, patientESAPI);
                mainWindow.Closed += MainWindow_Closed;
                mainWindow.Show();

                PatientOpened?.Invoke(this, EventArgs.Empty);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }            
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            PatientClosed?.Invoke(this, EventArgs.Empty);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class PatientViewModel
    {
        public PatientViewModel(ESAPI.PatientSummary patientSummary)
        {
            PatientId = patientSummary.Id;
            FirstName = patientSummary.FirstName;
            LastName = patientSummary.LastName;
        }

        public string PatientId { get; private set; }
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action execute;
        private readonly Func<bool> canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
            this.canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return canExecute?.Invoke() ?? true;
        }

        public void Execute(object parameter)
        {
            execute();
        }

        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}