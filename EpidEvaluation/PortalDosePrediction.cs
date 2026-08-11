using System;
using System.Linq;
using System.Windows;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace EpidEvaluation
{
    public class PortalDosePrediction
    {
        private static readonly BeamData[] allBeamData = BeamData.All;
        
        private readonly PortalDoseImage portalDoseImage;
        private readonly BeamData beamData;

        private readonly double jawPositionY1 = double.NaN;
        private readonly double jawPositionY2 = double.NaN;

        private Point calculationPoint = new Point();
        
        /// <summary>
        /// Portal dose prediction in one point using analytical methods
        /// </summary>
        /// <param name="beam">ESAPI Beam object associated with the portal image</param>
        /// <param name="portalDoseImage">Portal dose image</param>
        public PortalDosePrediction(Beam beam, PortalDoseImage portalDoseImage) 
        { 
            this.portalDoseImage = portalDoseImage;

            if (beam != null)
            {
                CollimatorRotation = beam.CollimatorRotation;

                if (beam?.ControlPoints?.FirstOrDefault()?.JawPositions is VRect<double> jawPositions)
                {
                    jawPositionY1 = jawPositions.Y1;
                    jawPositionY2 = jawPositions.Y2;
                }

                (CollimatorX, CollimatorY) = GetFieldSize(beam);
                (FieldSizeX, FieldSizeY) = GetFieldSize(beam, true);
            }

            // Get the beam data for the selected beam
            if (beam != null && GetNominalBeamEnergy() is int nominalBeamEnergy)            
                beamData = allBeamData.FirstOrDefault(x => x.NominalBeamEnergy == nominalBeamEnergy);
        }
        
        /// <summary>
        /// Get or set the position of calculation point in EPID geometry
        /// </summary>
        public Point CalculationPoint { get { return calculationPoint; } set { calculationPoint = value; CalculationPointSet = true; Calculate(); } }
        /// <summary>
        /// Get or set the position of calculation point based on the voxel index in the EPID geometry
        /// </summary>
        public Point CalculationPointIndex { get { return MathTools.CoordinateToIndex(portalDoseImage, CalculationPoint); } set { CalculationPoint = MathTools.IndexToCoordinate(portalDoseImage, value); } }
        /// <summary>
        /// Get whether the calculation point has been explicitly set or if the default position is being used
        /// </summary>
        public bool CalculationPointSet { get; private set; }

        /// <summary>
        /// Get the predicted value for the selected point
        /// </summary>        
        public double Predicted { get; private set; } = double.NaN;
        /// <summary>
        /// Get the measured value for the selected point
        /// </summary>
        public double Measured { get; private set; } = double.NaN;
        /// <summary>
        /// Get the deviation in percent between measured and calculated value for the selected point
        /// </summary>
        public double Deviation { get; private set; } = double.NaN;
        /// <summary>
        /// Get the collimator rotation (deg)
        /// </summary>
        public double CollimatorRotation { get;private set; } = double.NaN;
        /// <summary>
        /// Get the collimator opening in X (mm)
        /// </summary>
        public double CollimatorX { get; private set; } = double.NaN;
        /// <summary>
        /// Get the collimator opening in Y (mm)
        /// </summary>
        public double CollimatorY { get; private set; } = double.NaN;
        /// <summary>
        /// Get the field size in X (mm)
        /// </summary>
        public double FieldSizeX { get; private set; } = double.NaN;
        /// <summary>
        /// Get the field size in Y (mm)
        /// </summary>
        public double FieldSizeY { get; private set; } = double.NaN;

        /// <summary>
        /// Get the enhanced dynamic wedge factor
        /// </summary>
        public double EDWF { get; private set; } = double.NaN;
        /// <summary>
        /// Get the beam intensity at calculation point
        /// </summary>
        public double Intensity { get; private set; } = double.NaN; 
        /// <summary>
        /// Get the output factor
        /// </summary>
        public double OutputFactor { get; private set; } = double.NaN;
        /// <summary>
        /// Get the output factor given by the collimators
        /// </summary>
        public double OutputFactorCollimator { get; private set; } = double.NaN;
        /// <summary>
        /// Get the output factor given by the field size
        /// </summary>
        public double OutputFactorField { get; private set; } = double.NaN;

        /// <summary>
        /// Extract the nominal beam energy from EnergyModeDisplayName
        /// </summary>
        /// <returns>The nominal beam energy (0 if fail)</returns>
        private int GetNominalBeamEnergy()
        {
            int nominalBeamEnergy = 0;

            // Extract nominal beam energy from energy mode display name
            // In Portal Dosimetry API, display name doesn't contain fluence mode (e.g. FFF)
            if (portalDoseImage?.EnergyModeDisplayName is string energyMode)
            {
                energyMode = energyMode.Replace("X", string.Empty);
                int.TryParse(energyMode, out nominalBeamEnergy);
            }

            return nominalBeamEnergy;
        }

        /// <summary>
        /// Transform from EPID coordinate to beam coordinate by applying collimator rotation
        /// </summary>
        /// <param name="epidPoint"></param>
        /// <returns>Point in beam geometry</returns>
        private Point EPIDToBeam(Point epidPoint)
        {
            if (double.IsNaN(CollimatorRotation))
                return new Point(double.NaN, double.NaN);
            else
                return MathTools.Rotate(epidPoint, -CollimatorRotation);
        }

        /// <summary>
        /// Predict portal dose value
        /// </summary>
        public void Calculate()
        {                        
            EDWF = CalculateEDWF();
            Intensity = CalculateIntensity();
            OutputFactorCollimator = CalculateOutputFactor(CollimatorX, CollimatorY);
            OutputFactorField = CalculateOutputFactor(FieldSizeX, FieldSizeY);
            OutputFactor = 0.5 * (OutputFactorCollimator + OutputFactorField); // Mean of output factors
            
            double mu = portalDoseImage?.Meterset ?? double.NaN;
            
            Predicted = mu * EDWF * Intensity * OutputFactor;
            Measured = DetermineMeasuredValue();
            Deviation = 100.0 * (Measured - Predicted) / Predicted;
        }
        
        /// <summary>
        /// Determine if the selected point is outside the field (i.e. value < 25% of maximum)
        /// </summary>
        /// <returns>True if outside</returns>
        public bool IsPointOutsideField()
        {
            return DetermineMeasuredValue() < 0.25 * (portalDoseImage?.MaxCUValue ?? 0.0);
        }

        /// <summary>
        /// Move the calculation point to inside the field (image values < 5% of max is considered outside)
        /// </summary>
        public void MovePointInsideField()
        {
            if (portalDoseImage != null)
            {
                portalDoseImage.ExtractVoxelData();

                (int bestX, int bestY) = MathTools.FindFarthestFromLowValue(portalDoseImage.Voxels, (int)(0.05 * portalDoseImage.MaxVoxelValue));
                CalculationPointIndex = new Point(bestX, bestY);
            }
        }

        /// <summary>
        /// Move the calculation point to isocenter
        /// </summary>
        public void MovePointToIso()
        {
            CalculationPoint = new Point(0, 0);
        }

        /// <summary>
        /// Determine the measured value in the selected point
        /// </summary>
        private double DetermineMeasuredValue()
        {
            if (portalDoseImage == null)
                return double.NaN;
            else
            {
                Point calculationPointIndex = CalculationPointIndex;
                int xindex = (int)Math.Round(calculationPointIndex.X);
                int yindex = (int)Math.Round(calculationPointIndex.Y);
                return portalDoseImage.GetCU(xindex, yindex);
            }
        }

        /// <summary>
        /// Get the field size (X, Y)
        /// </summary>
        /// <param name="mlc">False = collimator opening is returned. True = the size of the opening is calculated using area and the Y-opening defined by MLC and jaws</param>
        /// <returns>The field size</returns>
        private (double X, double Y) GetFieldSize(Beam beam, bool mlc = false)
        {
            if (beam?.ControlPoints.FirstOrDefault() is ControlPoint controlPoint)
            {
                double[] leafBoundaries = LeafGeometry.GetLeafBoundaries(beam);

                if (leafBoundaries == null || !mlc)
                {
                    double fieldWidth = controlPoint.JawPositions.X2 - controlPoint.JawPositions.X1;
                    double fieldHeight = controlPoint.JawPositions.Y2 - controlPoint.JawPositions.Y1;
                    return (fieldWidth, fieldHeight);
                }
                else
                {
                    var jaws = controlPoint.JawPositions;

                    double x1 = jaws.X1;
                    double x2 = jaws.X2;
                    double y1 = jaws.Y1;
                    double y2 = jaws.Y2;

                    int firstOpen = -1;
                    int lastOpen = -1;

                    float[,] leaves = controlPoint.LeafPositions;

                    double area = 0.0;

                    int nPairs = leaves.GetLength(1);

                    for (int i = 0; i < nPairs; i++)
                    {
                        double bottom = leafBoundaries[i];
                        double top = leafBoundaries[i + 1];

                        double yBottom = Math.Max(bottom, y1);
                        double yTop = Math.Min(top, y2);

                        double left = Math.Max(leaves[0, i], x1);
                        double right = Math.Min(leaves[1, i], x2);

                        double width = Math.Max(0.0, right - left);

                        if (width >= 1.0)
                        {
                            if (firstOpen < 0)
                                firstOpen = i;

                            lastOpen = i;
                        }

                        if (yTop <= yBottom)
                            continue;

                        area += width * (yTop - yBottom);
                    }

                    double fieldBottom = Math.Max(leafBoundaries[firstOpen], y1);
                    double fieldTop = Math.Min(leafBoundaries[lastOpen + 1], y2);

                    double fieldHeight = Math.Max(0.0, fieldTop - fieldBottom);
                    double fieldWidth = area / fieldHeight;

                    return (fieldWidth, fieldHeight);
                }
            }
            else
                return (double.NaN, double.NaN);
        }

        /// <summary>
        /// Calculate Enhanced Dynamic Wedge Factor (EDWF)
        /// According to Kuperman 2005 
        /// https://pubmed.ncbi.nlm.nih.gov/15984676/
        /// </summary>
        /// <returns>The EDWF</returns>
        private double CalculateEDWF()
        {
            if (portalDoseImage == null || beamData == null)
                return double.NaN;
            else if (string.IsNullOrEmpty(portalDoseImage.WedgeId))
                return 1.0;

            double calculationPointY = EPIDToBeam(CalculationPoint).Y;

            double teta = portalDoseImage.WedgeAngle;
            double Y, Y_FIX, Y_MI;

            if (portalDoseImage.WedgeDirection == 180) // OUT
            {                
                Y_MI = -jawPositionY2 * 0.1;
                Y_FIX = -jawPositionY1 * 0.1;
                Y = -calculationPointY * 0.1;
            }
            else if (portalDoseImage.WedgeDirection == 0) // IN
            {
                Y_MI = jawPositionY1 * 0.1;
                Y_FIX = jawPositionY2 * 0.1;
                Y = calculationPointY * 0.1;
            }
            else
                return double.NaN;

            double Y_MF = Y_FIX - 0.5;
            double A = 1 - Math.Tan(teta * (Math.PI / 180)) / Math.Tan(60 * (Math.PI / 180));
            double B = Math.Tan(teta * (Math.PI / 180)) / Math.Tan(60 * (Math.PI / 180));

            double Y_Prime = Y + beamData.Lambda * (Y_FIX - Y_MI) + beamData.Mu * ((Y_FIX + Y_MI) / 2 - Y);

            return (A * MathTools.Interpolate(beamData.GSTT, 0) + B * MathTools.Interpolate(beamData.GSTT, Y_Prime)) / (A * MathTools.Interpolate(beamData.GSTT, 0) + B * MathTools.Interpolate(beamData.GSTT, Y_MF));
        }

        /// <summary>
        /// Determine beam intensity from lookup table
        /// </summary>
        /// <returns>The intensity factor</returns>
        private double CalculateIntensity()
        {
            if (beamData == null)
                return double.NaN;
            // Radial position
            double pos = Math.Sqrt(calculationPoint.X * calculationPoint.X + calculationPoint.Y * calculationPoint.Y);
            return MathTools.Interpolate(beamData.Intensity, pos) / MathTools.Interpolate(beamData.Intensity, 0.0);
        }

        /// <summary>
        /// Determine output factor from lookup table
        /// </summary>
        /// <param name="fieldSizeX">Field size in X (mm)</param>
        /// <param name="fieldSizeY">Field size in X (mm)</param>
        /// <returns>The output factor</returns>
        private double CalculateOutputFactor(double fieldSizeX, double fieldSizeY)
        {
            if (beamData == null)
                return double.NaN;
            return (MathTools.Interpolate(beamData.OF, fieldSizeX, fieldSizeY) / MathTools.Interpolate(beamData.OF, 100, 100));
        }
    }
}