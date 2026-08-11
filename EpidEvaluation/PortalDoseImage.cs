using System;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using VMS.CA.Scripting;
using VMS.TPS.Common.Model.API;
using ESAPI = VMS.TPS.Common.Model.API;

namespace EpidEvaluation
{
    public class PortalDoseImage
    {
        private const int DEFAULT_SIZE = 1190;

        private readonly Frame frame;
        private ushort[,] voxels;
        private BitmapSource bitmap;
        
        public PortalDoseImage(ProjectionImage projectionImage, ESAPI.Beam beam)
        {
            if(projectionImage!=null)
            {
                Id = projectionImage?.Id;
                CreationDateTime = projectionImage?.CreationDateTime;
            }

            frame = projectionImage?.Frames?.FirstOrDefault();
            if (frame != null)
            {
                XSize = frame.XSize;
                YSize = frame.YSize;
                XRes = frame.XRes;
                YRes = frame.YRes;
                Origin = new Point3D(frame.Origin.x, frame.Origin.y, frame.Origin.z);
                VoxelToDisplayValueScaling = frame.VoxelToDisplayValue(1);
            }            
                        
            BeamId = beam?.Id;
            EnergyModeDisplayName = beam?.EnergyModeDisplayName;
            Wedge wedge = beam?.Wedges?.FirstOrDefault();
            WedgeId = wedge?.Id;
            WedgeAngle = wedge?.WedgeAngle ?? double.NaN;
            WedgeDirection = wedge?.Direction ?? double.NaN;
            Meterset = beam?.Meterset.Value ?? double.NaN;

            Prediction = new PortalDosePrediction(beam, this);
        }

        public PortalDosePrediction Prediction { get; private set; }

        public string Id { get; private set; }
        public DateTime? CreationDateTime { get;private set; }

        public string BeamId { get; private set; }
        public string EnergyModeDisplayName { get; private set; }
        public string WedgeId { get; private set; }
        public double WedgeAngle { get; private set; }
        public double WedgeDirection { get; private set; }
        public double Meterset { get; private set; }
        
        public int XSize { get; private set; }
        public int YSize { get; private set; }
        public double XRes { get; private set; }
        public double YRes { get; private set; }
        public Point3D? Origin { get; private set; }
        public double VoxelToDisplayValueScaling { get; private set; }

        public ushort[,] Voxels { get { return voxels; } }
        public ushort MaxVoxelValue { get; private set; } = 0;
        public double MaxCUValue { get { return VoxelToDisplayValueScaling * MaxVoxelValue; } }

        /// <summary>
        /// Determine if the EPID was aquired in the valid position (Not yet implemented)
        /// </summary>
        /// <returns>True if valid</returns>
        public bool IsDetectorPositionValid()
        {
            return true;
        }

        public void ExtractVoxelData()
        {
            if (voxels == null && frame != null)
            {
                voxels = new ushort[frame.XSize, frame.YSize];
                frame?.GetVoxels(0, voxels);
                MaxVoxelValue = voxels.Cast<ushort>().Max();                
            }
        }
        
        public double GetCU(int xindex, int yindex)
        {
            if (frame == null || xindex < 0 || xindex >= XSize || yindex < 0 || yindex >= YSize)
                return double.NaN;
            
            ExtractVoxelData();
            if (voxels == null)
                return double.NaN;

            return VoxelToDisplayValueScaling * voxels[xindex,yindex];
        }

        public BitmapSource Bitmap
        {
            get
            {
                if (bitmap == null)
                    bitmap = CreateBitmap();
                return bitmap;
            }
        }

        private BitmapSource CreateBitmap()
        {
            ExtractVoxelData();

            int width = XSize == 0 ? DEFAULT_SIZE : XSize;
            int height = YSize == 0 ? DEFAULT_SIZE : YSize;            

            PixelFormat pixelFormat = PixelFormats.Bgr32;
            int bytesPerPixel = (pixelFormat.BitsPerPixel + 7) / 8;
            int stride = width * bytesPerPixel;
            byte[] pixels = new byte[stride * height];

            if (voxels != null)
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                    {
                        byte value = (byte)Math.Round(255.0 * voxels[x, y] / MaxVoxelValue);
                        Color color = ColorMap.Get(value);
                        int index = y * stride + x * bytesPerPixel;

                        pixels[index + 3] = 255; // alpha
                        pixels[index + 2] = color.R; // R
                        pixels[index + 1] = color.G; // G
                        pixels[index] = color.B;     // B
                    }

            return BitmapSource.Create(width, height, 96, 96, pixelFormat, null, pixels, stride);
        }
    }
}
