using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace EpidEvaluation
{
    internal static class ColorMap
    {
        private static readonly Color[] JetColors = CreateJet(256);

        public static Color Get(byte value)
        {
            return JetColors[value];
        }

        private static Color[] CreateJet(int count)
        {
            var colors = new Color[count];

            for (int i = 0; i < count; i++)
            {
                double x = i / (double)(count - 1);

                byte r, g, b;

                if (x < 0.25)
                {
                    r = 0;
                    g = (byte)(x * 4 * 255);
                    b = 255;
                }
                else if (x < 0.5)
                {
                    r = 0;
                    g = 255;
                    b = (byte)((0.5 - x) * 4 * 255);
                }
                else if (x < 0.75)
                {
                    r = (byte)((x - 0.5) * 4 * 255);
                    g = 255;
                    b = 0;
                }
                else
                {
                    r = 255;
                    g = (byte)((1.0 - x) * 4 * 255);
                    b = 0;
                }

                colors[i] = Color.FromRgb(r, g, b);
            }

            return colors;
        }
    }
}
