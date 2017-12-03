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

    using Acoustics.Shared;
    using Acoustics.Test.TestHelpers;
    using Acoustics.Tools.Wav;

    using global::AudioAnalysisTools.DSP.Seewave;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SeewaveTests
    {
        private static WavReader wavReader;
        private static SeewaveTestCase[] seewaveTestCases;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            // calculate indices
            var recordingPath = PathHelper.ResolveAsset("whip bird2.wav");
            wavReader = new WavReader(recordingPath);

            // find expected data
            var dataFile = PathHelper.ResolveAsset("seewave_data.json");
            seewaveTestCases = Json.Deserialise<SeewaveTestCase[]>(dataFile);
        }

        public class SeewaveTestCase
        {
            public string SeewaveVersion { get; set; }

            public string TestFile { get; set; }

            public string Command { get; set; }

            public double[] Result { get; set; }
        }

        [TestMethod]
        public void TestEnvelopeFunction()
        {
            var cases = seewaveTestCases.Where(x => x.Command.StartsWith("env"));

//            Seewave.Envelope()
        }


        [TestMethod]
        public void TestHilbertFunction()
        {
            var testCase = seewaveTestCases.Single(x => x.Command.StartsWith("Im(hilbert"));

            var actual = Seewave.Hilbert(wavReader.Samples);

            AssertFirstHundred(testCase.Result, actual);
        }

        private static void AssertFirstHundred(double[] expected, double[] actual, double epsilon = 0.0001)
        {
            Assert.IsNotNull(actual);

            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(expected[i], actual[i], epsilon, $"Elements did not match at index {i}");
            }
        }
    }
}
