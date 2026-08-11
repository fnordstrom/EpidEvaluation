using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ESAPI = VMS.TPS.Common.Model.API;
using ESAPI_Types = VMS.TPS.Common.Model.Types;
using PD = VMS.CA.Scripting;

namespace EpidEvaluation.ViewModels
{
    public class PortalDoseEvaluationViewModel : INotifyPropertyChanged
    {
        private readonly PortalDoseImage portalDoseImage;

        public PortalDoseEvaluationViewModel(PortalDoseImage portalDoseImage)
        {
            this.portalDoseImage = portalDoseImage;

            ImageClickedCommand = new RelayCommand<MouseButtonEventArgs>(OnImageClicked);
            ImageSizeChangedCommand = new RelayCommand<Image>(OnImageSizeChanged);

            if (portalDoseImage?.Prediction is PortalDosePrediction prediction)
            {
                if (!prediction.CalculationPointSet && prediction.IsPointOutsideField())
                    prediction.MovePointInsideField();
                else
                    prediction.Calculate();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string Name { get { return portalDoseImage?.Id ?? "-"; } }
        public string BeamInfo 
        { 
            get 
            { 
                List<string> info = new List<string>
                {
                    portalDoseImage?.EnergyModeDisplayName ?? "-"
                };
                if(!string.IsNullOrEmpty(portalDoseImage?.WedgeId))
                    info.Add(portalDoseImage.WedgeId.Replace("EDW", string.Empty));
                info.Add($"{(portalDoseImage?.Meterset ?? double.NaN):F1} MU");

                return string.Join(", ", info);
            } 
        }
        public double MU { get { return portalDoseImage?.Meterset ?? double.NaN; } }
        public string Date { get { return portalDoseImage?.CreationDateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-"; } }
        public string BeamId { get { return portalDoseImage?.BeamId ?? "-"; } }
        public Visibility InvalidDetectorPositionMessageVisibility { get { return (portalDoseImage?.IsDetectorPositionValid() ?? true) ? Visibility.Hidden : Visibility.Visible; } }
        public double Measured { get { return portalDoseImage?.Prediction?.Measured ?? double.NaN; } }
        public double Predicted { get { return portalDoseImage?.Prediction?.Predicted ?? double.NaN; } }
        public double Deviation { get { return portalDoseImage?.Prediction?.Deviation ?? double.NaN; } }
        public Brush Background 
        {
            get 
            {
                if(!portalDoseImage.IsDetectorPositionValid())
                    return Brushes.Red;
                else if (Math.Abs(Deviation) <= 5.0)
                    return Brushes.LightGreen;
                else if (Math.Abs(Deviation) <= 7.0)
                    return Brushes.Yellow;
                else
                    return  Brushes.Pink; 
            } 
        }

        public PortalDoseImage PortalDoseImage { get { return portalDoseImage; } }
        public BitmapSource Bitmap { get { return portalDoseImage?.Bitmap; } }
        public ICommand ImageClickedCommand { get; }
        public ICommand ImageSizeChangedCommand { get; }
        public Point CalculationPointImagePosition
        {
            get
            {
                double scalingX = imageSize.Width / portalDoseImage.XSize;
                double scalingY = imageSize.Height / portalDoseImage.YSize;
                Point pointIndex = portalDoseImage?.Prediction?.CalculationPointIndex ?? new Point(double.NaN, double.NaN);

                return new Point(pointIndex.X * scalingX, pointIndex.Y * scalingY);
            }
            set
            {                
                double scalingX = portalDoseImage.XSize / imageSize.Width;
                double scalingY = portalDoseImage.YSize / imageSize.Height;
                Point pointIndex = new Point(value.X * scalingX, value.Y * scalingY);

                if(portalDoseImage?.Prediction is PortalDosePrediction prediction)
                prediction.CalculationPointIndex = pointIndex;   
                
                OnPropertyChanged();
                OnPropertyChanged("Measured");
                OnPropertyChanged("Predicted");
                OnPropertyChanged("Deviation");
                OnPropertyChanged("Background");
            }
        }
        private Size imageSize;
        private void OnImageClicked(MouseButtonEventArgs e)
        {
            if (e.Source is Image image)
                CalculationPointImagePosition = e.GetPosition(image);
        }

        private void OnImageSizeChanged(Image image)
        {
            imageSize = new Size(image.ActualWidth, image.ActualHeight);
            OnPropertyChanged("CalculationPointImagePosition");
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }        
    }
}
