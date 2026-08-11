using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.API;

namespace EpidEvaluation.ViewModels
{
    internal class PredictionViewModel
    {
        public PredictionViewModel(PortalDoseImage portalDoseImage)
        {
            PortalDosePrediction prediction = portalDoseImage?.Prediction;

            BeamId = portalDoseImage?.BeamId ?? "-";
            
            Energy = portalDoseImage?.EnergyModeDisplayName ?? "-";
            Wedge = portalDoseImage?.WedgeId ?? "-";
            Meterset = portalDoseImage?.Meterset ?? double.NaN;

            Collimator = $"{prediction.CollimatorX * 0.1:F1}x{prediction.CollimatorY * 0.1:F1} cm";
            FieldSize = $"{prediction.FieldSizeX * 0.1:F1}x{prediction.FieldSizeY * 0.1:F1} cm";

            EDWF = prediction?.EDWF ?? double.NaN;
            Intensity = prediction?.Intensity ?? double.NaN;
            OutputFactor = prediction?.OutputFactor ?? double.NaN;
            OutputFactorCollimator = prediction?.OutputFactorCollimator ?? double.NaN;
            OutputFactorField = prediction?.OutputFactorField ?? double.NaN;
            Measured = prediction?.Measured ?? double.NaN;
            Predicted = prediction?.Predicted ?? double.NaN;
            Deviation = prediction?.Deviation ?? double.NaN;
        }

        public string BeamId { get; private set; }
        public string Energy { get; private set; }
        public string Wedge { get; private set; }
        public double Meterset { get; private set; }
        public string Collimator { get; private set; }
        public string FieldSize { get; private set; }
        public double EDWF { get; private set; }
        public double Intensity { get; private set; }
        public double OutputFactor { get; private set; }
        public double OutputFactorCollimator { get; private set; }
        public double OutputFactorField { get; private set; }
        public double Measured { get; private set; }
        public double Predicted { get; private set; }
        public double Deviation { get; private set; }
    }
}
