using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMS.CA.Scripting;
using ESAPI = VMS.TPS.Common.Model.API;
using PD = VMS.DV.PD.Scripting;

namespace EpidEvaluation.ViewModels
{
    public class MainViewModel
    {
        public ObservableCollection<PatientItemViewModel> PatientItems { get; set; }
        public ObservableCollection<PortalDoseEvaluationViewModel> PortalDoseEvaluationItems { get; set; }
        public MainViewModel(PD.Patient patient, ESAPI.Patient patientEclipse)
        {
            PatientItems = new ObservableCollection<PatientItemViewModel>();
            PatientItemViewModel rootPatient = new PatientItemViewModel(patient);
            foreach (Course course in patient.Courses)
            {
                ESAPI.Course courseEclipse = patientEclipse?.Courses.FirstOrDefault(x => x.Id == course.Id);

                PatientItemViewModel childCourse = new PatientItemViewModel(course);                
                foreach(PlanSetup planSetup in course.PlanSetups)
                {
                    ESAPI.ExternalPlanSetup planEclipse = courseEclipse?.ExternalPlanSetups.FirstOrDefault(x => x.Id == planSetup.Id);
                    int[] beamNumbersInTreatmentOrder = planEclipse.BeamsInTreatmentOrder.Where(x=>!x.IsSetupField && (x.BeamTechnique == VMS.TPS.Common.Model.Types.BeamTechnique.Static || x.BeamTechnique== VMS.TPS.Common.Model.Types.BeamTechnique.MLC)).Select(x => x.BeamNumber).ToArray();
                    
                    PatientItemViewModel childPlan = new PatientItemViewModel(planSetup);
                    foreach(int beamNumber in beamNumbersInTreatmentOrder)
                    {
                        if (planSetup.Beams.FirstOrDefault(x => x.BeamNumber == beamNumber) is Beam beam)
                        {
                            PatientItemViewModel childBeam = new PatientItemViewModel(beam);
                            foreach (ProjectionImage projectionImage in beam.FieldImages)
                            {
                                if (projectionImage.Frames.FirstOrDefault() is Frame firstFrame && firstFrame.DisplayUnit == "CU")
                                {
                                    ESAPI.Beam esapiBeam = patientEclipse?.Courses.FirstOrDefault(x => x.Id == course.Id)?.ExternalPlanSetups.FirstOrDefault(x => x.Id == planSetup.Id)?.Beams.FirstOrDefault(x => x.Id == beam.Id);
                                    childBeam.Children.Add(new PatientItemViewModel(projectionImage, esapiBeam));
                                }
                            }
                            childPlan.Children.Add(childBeam);
                        }
                    }
                    childCourse.Children.Add(childPlan);
                }
                rootPatient.Children.Add(childCourse);
            }
            PatientItems.Add(rootPatient);

            PortalDoseEvaluationItems = new ObservableCollection<PortalDoseEvaluationViewModel>();
        }

        public MainViewModel(PD.Patient patient, ESAPI.ExternalPlanSetup planEclipse)
        {
            PatientItems = new ObservableCollection<PatientItemViewModel>();

            PlanSetup planSetup = patient?.Courses?.FirstOrDefault(x => x.Id == planEclipse.Course.Id)?.PlanSetups?.FirstOrDefault(x => x.Id == planEclipse.Id);
            int[] beamNumbersInTreatmentOrder = planEclipse.BeamsInTreatmentOrder.Where(x => !x.IsSetupField && (x.BeamTechnique == VMS.TPS.Common.Model.Types.BeamTechnique.Static || x.BeamTechnique == VMS.TPS.Common.Model.Types.BeamTechnique.MLC)).Select(x => x.BeamNumber).ToArray();

            PatientItemViewModel rootPlan = new PatientItemViewModel(planSetup);
            foreach (int beamNumber in beamNumbersInTreatmentOrder)
            {
                if (planSetup.Beams.FirstOrDefault(x => x.BeamNumber == beamNumber) is Beam beam)
                {
                    PatientItemViewModel childBeam = new PatientItemViewModel(beam);
                    foreach (ProjectionImage projectionImage in beam.FieldImages)
                    {
                        if (projectionImage.Frames.FirstOrDefault() is Frame firstFrame && firstFrame.DisplayUnit == "CU")
                        {
                            ESAPI.Beam esapiBeam = planEclipse?.Beams.FirstOrDefault(x => x.Id == beam.Id);
                            childBeam.Children.Add(new PatientItemViewModel(projectionImage, esapiBeam));
                        }
                    }
                    rootPlan.Children.Add(childBeam);
                }
            }

            PatientItems.Add(rootPlan);

            PortalDoseEvaluationItems = new ObservableCollection<PortalDoseEvaluationViewModel>();
        }
    }
}
