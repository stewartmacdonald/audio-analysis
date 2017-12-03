// <copyright file="SeewaveTests.cs" company="QutEcoacoustics">
// All code in this file and all associated files are the copyright and property of the QUT Ecoacoustics Research Group (formerly MQUTeR, and formerly QUT Bioacoustics Research Group).
// </copyright>

namespace Acoustics.Test.AudioAnalysisTools.DSP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Acoustics.Test.TestHelpers;
    using Acoustics.Tools.Wav;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    public class SeewaveTests
    {
        private static WavReader wavReader;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            // calculate indices
            var recordingPath = PathHelper.ResolveAsset("whip bird.wav");
            wavReader = new WavReader(recordingPath);
        }

        [DataTestMethod()]
        public void TestEnvelopeFunction()
        {
            
        }

    }
}
