using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EpidEvaluation
{
    internal class BeamData
    {
        public BeamData(int energy)
        {
            NominalBeamEnergy = energy;

            if (energy == 6)
            {         
                // From Kupermann 2005
                Lambda = 0.0077;
                Mu = 0.1028;

                GSTT = ReadTable("6X_GSTT"); // From Varian Enhanced Dyunamic Wedge Implementation Guide
                OF = ReadTable("6X_OF"); // Reference Beam Data
                Intensity = ReadTable("6X_Intensity"); // Reference Beam Data
            }
            else if(energy == 10)
            {
                GSTT = ReadTable("10X_GSTT"); // From Varian Enhanced Dyunamic Wedge Implementation Guide
                OF = ReadTable("10X_OF"); // Reference Beam Data
                Intensity = ReadTable("10X_Intensity"); // Reference Beam Data
            }
            else if (energy == 15)
            {
                // From Interpolated from Kupermann 2005
                Lambda = 0.0047;
                Mu = 0.08338;

                GSTT = ReadTable("15X_GSTT"); // From Varian Enhanced Dyunamic Wedge Implementation Guide
                OF = ReadTable("15X_OF"); // Reference Beam Data
                Intensity = ReadTable("15X_Intensity"); // Reference Beam Data
            }
        }

        public static BeamData[] All { get { return new BeamData[] { new BeamData(6), new BeamData(15) }; } }

        public int NominalBeamEnergy { get; private set; }
        public double[,] GSTT { get; private set; }
        public double Lambda { get; private set; }
        public double Mu { get; private set; }
        public double[,] OF { get; private set; }
        public double[,] Intensity { get; private set; }

        private double[,] ReadTable(string data)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            string resourceName = $"{assembly.GetName().Name}.BeamData.{data}";
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new InvalidOperationException("Embedded resource '" + resourceName + "' not found.");

                using (StreamReader reader = new StreamReader(stream))
                {
                    List<double[]> rows = new List<double[]>();

                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();

                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        double[] values = line
                            .Split('\t')
                            .Select(s => double.Parse(s, CultureInfo.InvariantCulture))
                            .ToArray();

                        rows.Add(values);
                    }

                    if (rows.Count == 0)
                        return new double[0, 0];

                    int rowCount = rows.Count;
                    int colCount = rows[0].Length;

                    double[,] result = new double[rowCount, colCount];

                    for (int i = 0; i < rowCount; i++)
                    {
                        if (rows[i].Length != colCount)
                            throw new FormatException($"Row {i} has {rows[i].Length} columns; expected {colCount}");

                        for (int j = 0; j < colCount; j++)
                            result[i, j] = rows[i][j];
                    }

                    return result;
                }
            }
        }
    }
}
