// <copyright file="Seewave.cs" company="QutEcoacoustics">
// All code in this file and all associated files are the copyright and property of the QUT Ecoacoustics Research Group (formerly MQUTeR, and formerly QUT Bioacoustics Research Group).
// </copyright>

namespace AudioAnalysisTools.DSP.Seewave
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Accord.Math;

    /// <summary>
    /// A collection of algorithms implemented from the R Seewave package by Jerome Sueur.
    /// </summary>
    /// <remarks>
    /// See https://github.com/cran/seewave/commit/97501b30e1475710511d4a147d14c17b2d4ca96c for source implementation.
    /// </remarks>
    public class Seewave
    {
        public static void Envelope(double[] samples, EnvelopeType envelopeType = EnvelopeType.Hilbert, ISignalSmoother smoother = null, bool normalize = false)
        {
            // Input
            var signalLength = samples.Length;


            switch (envelopeType)
            {
                case EnvelopeType.Hilbert:

                    break;
                case EnvelopeType.Absolute:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(envelopeType), envelopeType, null);
            }
        }

        public static double[] Hilbert(double[] samples)
        {
            var result = samples.Copy();
            HilbertTransform.FHT(result, FourierTransform.Direction.Forward);

            return result;
        }
    }

    public enum EnvelopeType
    {
        Hilbert,
        Absolute,
    }

    public interface ISignalSmoother
    {
        double[] Modify(double[] input);
    }

    public class MeanSlidingWindow : ISignalSmoother
    {
        private readonly double x;
        private readonly double y;

        public MeanSlidingWindow(double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        public double[] Modify(double[] input)
        {
            throw new NotImplementedException();
        }
    }

    public class KernelSmoother : ISignalSmoother
    {
       public KernelSmoother()
        {
        }

        public double[] Modify(double[] input)
        {
            throw new NotImplementedException();
        }
    }

    public class SumSmoother : ISignalSmoother
    {
        private readonly double x;

        public SumSmoother(double x)
        {
            this.x = x;
        }

        public double[] Modify(double[] input)
        {
            throw new NotImplementedException();
        }
    }
}
