using System;
using System.Windows;

namespace EpidEvaluation
{
    internal static class MathTools
    {
        /// <summary>
        /// 1D linear interpolation
        /// </summary>
        /// <param name="table">(2,N) dimensional data (X=first column, Y=second column)</param>
        /// <param name="x">X-value</param>
        /// <returns>Interpolated Y-value</returns>
        public static double Interpolate(double[,] table, double x)
        {
            int rows = table.GetLength(0);

            if (rows < 2 || table.GetLength(1) != 2)
                return double.NaN;

            // Outside range
            if (x < table[0, 0] || x > table[rows - 1, 0])
                return double.NaN;

            // Exact match with first point
            if (x == table[0, 0])
                return table[0, 1];

            for (int i = 1; i < rows; i++)
            {
                double x0 = table[i - 1, 0];
                double y0 = table[i - 1, 1];
                double x1 = table[i, 0];
                double y1 = table[i, 1];

                // Exact match
                if (x == x1)
                    return y1;

                if (x < x1)
                {
                    // Linear interpolation
                    return y0 + (x - x0) * (y1 - y0) / (x1 - x0);
                }
            }

            return double.NaN;
        }

        /// <summary>
        /// 2D linear interpolation
        /// </summary>
        /// <param name="table">(N,M) dimensional data (X=first row, Y=first column)</param>
        /// <param name="x">X-Value</param>
        /// <param name="y">T-Value</param>
        /// <returns>Interpolated value</returns>
        public static double Interpolate(double[,] table, double x, double y)
        {
            int rows = table.GetLength(0);
            int cols = table.GetLength(1);

            // Find X interval
            int ix = -1;
            for (int i = 1; i < cols - 1; i++)
            {
                if (x >= table[0, i] && x <= table[0, i + 1])
                {
                    ix = i;
                    break;
                }
            }

            // Find Y interval
            int iy = -1;
            for (int i = 1; i < rows - 1; i++)
            {
                if (y >= table[i, 0] && y <= table[i + 1, 0])
                {
                    iy = i;
                    break;
                }
            }

            if (ix == -1 || iy == -1)
                return double.NaN;

            double x1 = table[0, ix];
            double x2 = table[0, ix + 1];
            double y1 = table[iy, 0];
            double y2 = table[iy + 1, 0];

            double q11 = table[iy, ix];
            double q21 = table[iy, ix + 1];
            double q12 = table[iy + 1, ix];
            double q22 = table[iy + 1, ix + 1];

            double tx = (x - x1) / (x2 - x1);
            double ty = (y - y1) / (y2 - y1);

            return q11 * (1 - tx) * (1 - ty)
                 + q21 * tx * (1 - ty)
                 + q12 * (1 - tx) * ty
                 + q22 * tx * ty;
        }

        /// <summary>
        /// Find the index of the point with farthest distance from a value below threshold using fast two-pass chamfer distance transform
        /// </summary>
        /// <param name="data">Image data</param>
        /// <param name="thresholdValue">Threashold value</param>
        /// <returns>The index of the point with farthest distance from a value below threshold</returns>
        public static (int x, int y) FindFarthestFromLowValue(ushort[,] data, int thresholdValue)
        {
            int width = data.GetLength(0);
            int height = data.GetLength(1);

            // Find max
            ushort max = 0;
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (data[x, y] > max)
                        max = data[x, y];

            // Distance map
            int[,] dist = new int[width, height];
            int inf = int.MaxValue / 4;

            // Initialize
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    dist[x, y] = data[x, y] < thresholdValue ? 0 : inf;
                }
            }

            // Forward pass
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int d = dist[x, y];

                    if (x > 0)
                        d = Math.Min(d, dist[x - 1, y] + 1);

                    if (y > 0)
                        d = Math.Min(d, dist[x, y - 1] + 1);

                    if (x > 0 && y > 0)
                        d = Math.Min(d, dist[x - 1, y - 1] + 1);

                    dist[x, y] = d;
                }
            }

            // Backward pass
            for (int x = width - 1; x >= 0; x--)
            {
                for (int y = height - 1; y >= 0; y--)
                {
                    int d = dist[x, y];

                    if (x < width - 1)
                        d = Math.Min(d, dist[x + 1, y] + 1);

                    if (y < height - 1)
                        d = Math.Min(d, dist[x, y + 1] + 1);

                    if (x < width - 1 && y < height - 1)
                        d = Math.Min(d, dist[x + 1, y + 1] + 1);

                    dist[x, y] = d;
                }
            }

            // Find maximum distance
            int maxDist = -1;
            (int x, int y) result = (-1, -1);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (dist[x, y] > maxDist)
                    {
                        maxDist = dist[x, y];
                        result = (x, y);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Rotate point around origo
        /// </summary>
        /// <param name="point">The point to rotate</param>
        /// <param name="angle">The angle in degrees</param>
        /// <returns>Rotated point</returns>
        public static Point Rotate(Point point, double angle)
        {
            double angleRadians = angle * Math.PI / 180.0;

            double cos = Math.Cos(angleRadians);
            double sin = Math.Sin(angleRadians);

            return new Point(point.X * cos - point.Y * sin, point.X * sin + point.Y * cos);
        }

        /// <summary>
        /// Transform from point index to point coordinate
        /// </summary>
        /// <param name="portalDoseImage">The portal dose image</param>
        /// <param name="index">Index of the point</param>
        /// <param name="index"></param>
        /// <returns>Coordinate of the point</returns>
        public static Point IndexToCoordinate(PortalDoseImage portalDoseImage, Point index)
        {
            if (portalDoseImage == null)
                return new Point(double.NaN, double.NaN);

            double positionX = -(portalDoseImage.XSize - 1) * portalDoseImage.XRes * 0.5;
            double positionY = (portalDoseImage.YSize - 1) * portalDoseImage.YRes * 0.5;

            double coordinateX = positionX + index.X * portalDoseImage.XRes;
            double coordinateY = positionY - index.Y * portalDoseImage.YRes;

            return new Point(coordinateX, coordinateY);
        }

        /// <summary>
        /// Transform from point coordinate to point index
        /// </summary>
        /// <param name="portalDoseImage">The portal dose image</param>
        /// <param name="coordinate">Coordinate of the point</param>
        /// <returns>Index of the point</returns>
        public static Point CoordinateToIndex(PortalDoseImage portalDoseImage, Point coordinate)
        {
            if(portalDoseImage==null)
                return new Point(double.NaN, double.NaN);

            double positionX = -(portalDoseImage.XSize - 1) * portalDoseImage.XRes * 0.5;
            double positionY = (portalDoseImage.YSize - 1) * portalDoseImage.YRes * 0.5;

            double indexX = (coordinate.X - positionX) / portalDoseImage.XRes;
            double indexY = -(coordinate.Y - positionY) / portalDoseImage.YRes;

            return new Point(indexX, indexY);
        }
    }
}