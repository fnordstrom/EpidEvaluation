using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.API;

namespace EpidEvaluation
{
    internal class LeafGeometry
    {
        public static double[] GetLeafBoundaries(Beam beam)
        {
            double[] boundaries;
            string model = beam?.MLC?.Model;

            switch (model)
            {
                case "Millennium 120":
                    boundaries = LeafGeometry.Millennium120();
                    break;

                case "High Definition 120":
                    boundaries = LeafGeometry.HD120();
                    break;

                default:
                    boundaries = null;
                    break;
            }

            return boundaries;
        }

        private static double[] Millennium120()
        {
            List<double> bounds = new List<double>();

            double y = -200.0;

            bounds.Add(y);

            // 10 outer leaves (10 mm)
            for (int i = 0; i < 10; i++)
            {
                y += 10.0;
                bounds.Add(y);
            }

            // 40 inner leaves (5 mm)
            for (int i = 0; i < 40; i++)
            {
                y += 5.0;
                bounds.Add(y);
            }

            // 10 outer leaves (10 mm)
            for (int i = 0; i < 10; i++)
            {
                y += 10.0;
                bounds.Add(y);
            }

            return bounds.ToArray();
        }

        private static double[] HD120()
        {
            List<double> bounds = new List<double>();

            double y = -110.0;

            bounds.Add(y);

            // 14 outer leaves (5 mm)
            for (int i = 0; i < 14; i++)
            {
                y += 5.0;
                bounds.Add(y);
            }

            // 32 central leaves (2.5 mm)
            for (int i = 0; i < 32; i++)
            {
                y += 2.5;
                bounds.Add(y);
            }

            // 14 outer leaves (5 mm)
            for (int i = 0; i < 14; i++)
            {
                y += 5.0;
                bounds.Add(y);
            }

            return bounds.ToArray();
        }
    }
}
