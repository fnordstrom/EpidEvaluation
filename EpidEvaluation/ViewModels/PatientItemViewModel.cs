using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using VMS.CA.Scripting;
using VMS.DV.PD.Scripting;
using ESAPI = VMS.TPS.Common.Model.API;

namespace EpidEvaluation.ViewModels
{
    public class PatientItemViewModel: INotifyPropertyChanged
    {
        private bool isExpanded;
        private bool isSelected;
        private readonly PortalDoseImage portalDoseImage;

        public PatientItemViewModel(Patient patient) : this(patient.Id, patient, true)  { }
        public PatientItemViewModel(Course course) : this(course.Id, course, true) { }
        public PatientItemViewModel(PlanSetup planSetup) : this(planSetup.Id, planSetup) { }
        public PatientItemViewModel(Beam beam) : this(beam.Id, beam) { }
        public PatientItemViewModel(ProjectionImage projectionImage, ESAPI.Beam esapiBeam) : this($"{projectionImage.Id}{(projectionImage.Frames.Count() ==1 ? string.Empty : $"{projectionImage.Frames.Count()} frames")}", projectionImage) 
        {            
            portalDoseImage = new PortalDoseImage(projectionImage, esapiBeam);
        }
        private PatientItemViewModel(string name, object pdObject, bool isExpanded = false)
        {
            Name = name;            
            PDObject = pdObject;
            Children = new ObservableCollection<PatientItemViewModel>();
            this.isExpanded = isExpanded;
        }        

        public string Name { get; private set; }
        public object PDObject { get; private set; }
        public IEnumerable<PortalDoseImage> PortalDoseImages 
        {
            get { return portalDoseImage != null ? new PortalDoseImage[] { portalDoseImage }.AsEnumerable() : GetPortalDoseImages(Children); }
        }
        public ObservableCollection<PatientItemViewModel> Children { get; private set; }

        private List<PortalDoseImage> GetPortalDoseImages(IEnumerable<PatientItemViewModel> children)
        {
            List<PortalDoseImage> portalDoseImages = new List<PortalDoseImage>();
            foreach(PatientItemViewModel child in children)
            {
                if(child.portalDoseImage != null)
                    portalDoseImages.Add(child.portalDoseImage);
                if (child.Children != null)
                {
                    List<PortalDoseImage> childImages = GetPortalDoseImages(child.Children);
                    portalDoseImages.AddRange(childImages);
                }                
            }
            return portalDoseImages;
        }

        public bool IsExpanded
        {
            get => isExpanded;
            set
            {
                if (isExpanded != value)
                {
                    isExpanded = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (isSelected != value)
                {
                    isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
