﻿// <copyright file="PcaWhiteningTest.cs" company="QutEcoacoustics">
// All code in this file and all associated files are the copyright and property of the QUT Ecoacoustics Research Group (formerly MQUTeR, and formerly QUT Bioacoustics Research Group).
// </copyright>

namespace Acoustics.Test.AudioAnalysisTools.DSP
{
    using global::AudioAnalysisTools.DSP;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Collections.Generic;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Accord.Statistics.Analysis;
    using Accord.Math;
    using global::AudioAnalysisTools.StandardSpectrograms;
    using global::AudioAnalysisTools.WavTools;
    using global::TowseyLibrary;
    using TestHelpers;

    [TestClass]
    public class PcaWhiteningTest
    {

        private DirectoryInfo outputDirectory;

        [TestInitialize]

        public void Setup()
        {
            this.outputDirectory = PathHelper.GetTempDir();
        }

        [TestCleanup]

        public void Cleanup()
        {
            PathHelper.DeleteTempDir(this.outputDirectory);
        }

        [TestMethod]
        public void TestPcaWhitening()
        {
            var outputDir = this.outputDirectory;
            var resultDir = PathHelper.ResolveAssetPath("C:\\Users\\kholghim\\Mahnoosh\\PcaWhitening");
            var outputLinScaImagePath = Path.Combine(resultDir, "LinearFreqScaleSpectrogram.png");
            var outputAmpSpecImagePath = Path.Combine(resultDir, "AmplitudeSpectrogram.png");
            var outputNormAmpImagePath = Path.Combine(resultDir, "NormAmplitudeSpectrogram.png");
            var outputMelImagePath = Path.Combine(resultDir, "MelScaleSpectrogram.png");
            var outputNormMelImagePath = Path.Combine(resultDir, "NormalizedMelScaleSpectrogram.png");
            var outputNoiseReducedImagePath = Path.Combine(
                resultDir,
                "NoiseReducedSpectrogram.png");
            var outputWhitenedSpectrogramPath = Path.Combine(resultDir, "WhitenedSpectrogram.png");
            var outputReSpecImagePath = Path.Combine(resultDir, "ReconstrcutedSpectrogram.png");
            var projectionMatrixPath = Path.Combine(resultDir, "ProjectionMatrix");
            var recordingPath = PathHelper.ResolveAsset("Recordings", "BAC2_20071008-085040.wav"); //    "PcaWhitening", "20160705_064611_22069.wav"
            var recording = new AudioRecording(recordingPath);

            // GENERATE AMPLITUDE SPECTROGRAM
            var fst = FreqScaleType.Linear;
            var freqScale = new FrequencyScale(fst);

            /*
            var sonoConfig = new SonogramConfig
            {
                WindowSize = freqScale.WindowSize,
                WindowOverlap = 0.75,
                SourceFName = recording.BaseName,
                NoiseReductionType = NoiseReductionType.None,
                NoiseReductionParameter = 0.0,
            };
            */

            var sonoConfig = new SonogramConfig
            {
                WindowSize = freqScale.FinalBinCount * 2,
                WindowOverlap = 0.2,
                SourceFName = recording.BaseName,
                NoiseReductionType = NoiseReductionType.None,
                NoiseReductionParameter = 0.0,
            };

            //var sonogram = new SpectrogramStandard(sonoConfig, recording.WavReader);
            var amplitudeSpectrogram = new AmplitudeSonogram(sonoConfig, recording.WavReader);

            //sonogram.Configuration.WindowSize = freqScale.WindowSize;
            amplitudeSpectrogram.Configuration.WindowSize = freqScale.WindowSize;

            //var image = sonogram.GetImageFullyAnnotated(sonogram.GetImage(), "SPECTROGRAM: " + fst.ToString(), freqScale.GridLineLocations);
            var image = amplitudeSpectrogram.GetImageFullyAnnotated(amplitudeSpectrogram.GetImage(), "SPECTROGRAM: " + fst.ToString(), freqScale.GridLineLocations);
            //image.Save(outputLinScaImagePath, ImageFormat.Png);
            image.Save(outputAmpSpecImagePath, ImageFormat.Png);

            //CONVERT TO MEL SCALE
            //var filterBankSize = 200; // according to Dan Stowell's paper, this should be 40!

            //var MFB = MFCCStuff.MelFilterBank(sonogram.Data, filterBankSize, recording.Nyquist, 0, recording.Nyquist);
            //var MFB = MFCCStuff.MelFilterBank(sonogram.Data, filterBankSize, recording.Nyquist, 0, recording.Nyquist);

            //var melImage = amplitudeSpectrogram.GetImageFullyAnnotated(amplitudeSpectrogram.GetImage(), "MELSPECTROGRAM: " + fst.ToString(), freqScale.GridLineLocations);
            //sonogram.Data = MFB;
            //var melImage = sonogram.GetImageFullyAnnotated(sonogram.GetImage(), "MELSPECTROGRAM: " + fst.ToString(), freqScale.GridLineLocations);
            //melImage.Save(outputMelImagePath, ImageFormat.Png);

            // DO RMS NORMALIZATION
            amplitudeSpectrogram.Data = PcaWhitening.RmsNormalization(amplitudeSpectrogram.Data);
            var normImage = amplitudeSpectrogram.GetImageFullyAnnotated(amplitudeSpectrogram.GetImage(), "NORMAmplitudeSPECTROGRAM: " + fst.ToString(), freqScale.GridLineLocations);
            //normMelImage.Save(outputNormMelImagePath, ImageFormat.Png);
            normImage.Save(outputNormAmpImagePath, ImageFormat.Png);

            // CONVERT NORMALIZED AMPLITUDE SPECTROGRAM TO dB SPECTROGRAM
            //var sonogram = new SpectrogramStandard(sonoConfig, amplitudeSpectrogram.Data);
            var sonogram = new SpectrogramStandard(amplitudeSpectrogram);
            //sonogram.Configuration.WindowSize = freqScale.WindowSize;
            var standImage = sonogram.GetImageFullyAnnotated(sonogram.GetImage(), "SPECTROGRAM: " + fst.ToString(), freqScale.GridLineLocations);
            standImage.Save(outputLinScaImagePath, ImageFormat.Png);

            /*
            // DO NOISE REDUCTION
            var dataMatrix = SNR.NoiseReduce_Standard(sonogram.Data);
            sonogram.Data = dataMatrix;
            var noiseReducedImage = sonogram.GetImageFullyAnnotated(sonogram.GetImage(), "NOISEREDUCEDSPECTROGRAM: " + fst.ToString(), freqScale.GridLineLocations);
            noiseReducedImage.Save(outputNoiseReducedImagePath, ImageFormat.Png);
            */

            // Do Patch Sampling
            int patchWidth = 16; //32; //
            int patchHeight = 16; //32; //
            int noOfRandomPatches = 500; //1000; // 1500; //
            int rows = sonogram.Data.GetLength(0); //3247
            int cols = sonogram.Data.GetLength(1); //256
            /*
            //int numberOfPatches = (rows / patchHeight) * (cols / patchWidth);
            //var sequentialPatches = PatchSampling.GetPatches(sonogram.Data, patchWidth, patchHeight, (rows / patchHeight) * (cols / patchWidth), "sequential");
            var randomPatches = PatchSampling.GetPatches(sonogram.Data, patchWidth, patchHeight, 1500, "random");
            //double[,] sequentialPatchMatrix = sequentialPatches.ToMatrix();
            double[,] randomPatchMatrix = randomPatches.ToMatrix();

            // DO UNIT TESTING ONLY FOR PCAWHITENING
            //var expected = new double[100, 100];
            //var actual = PcaWhitening.Whitening(sonogram.Data);
            //var actual = PcaWhitening.Whitening(sequentialPatchMatrix);
            var actual = PcaWhitening.Whitening(randomPatchMatrix);

            //when using a matrix of random patches, we need to output the projection matrix
            //that is used to transform the data into the new feature subspace.
            //in Accord.net, this matrix is called "ComponentVectors", which its columns contain the
            //principle components, known as Eigenvectors.
            double[,] projectionMatrix = actual.Item1;

            //write the projection matrix to disk
            //MatrixTools.WriteMatrix2File(projectionMatrix, resultDir);
            FileTools.WriteMatrix2File(projectionMatrix, projectionMatrixPath);
            */

            //+++++++++++++++++++++++++++++++++++++++++++++++++Exp1: reconstructing the spectrogram from sequential patches and the projection matrix obtained from random patches
            /*
            double[,] reconstructedSpec = PcaWhitening.ReconstructSpectrogram(projectionMatrix, sequentialPatchMatrix, actual.Item3, actual.Item4);

            //int cols = sonogram.Data.GetLength(1);
            sonogram.Data = PatchSampling.ConvertPatches(reconstructedSpec, patchWidth, patchHeight, cols);

            var respecImage = sonogram.GetImageFullyAnnotated(sonogram.GetImage(), "RECONSTRUCTEDSPECTROGRAM: " + fst.ToString(), freqScale.GridLineLocations);
            respecImage.Save(outputReSpecImagePath, ImageFormat.Png);
            */
            //+++++++++++++++++++++++++++++++++++++++++++++++++Exp1

            /*
            //+++++++++++++++++++++++++++++++++++++++++++++++++Exp2: Apply the projection matrix to sequential patches from another recording
            var recording2Path = PathHelper.ResolveAsset("Recordings", "BAC2_20071008-085040.wav");
            var recording2 = new AudioRecording(recording2Path);
            var fst2 = FreqScaleType.Linear;
            var freqScale2 = new FrequencyScale(fst2);
            var sonoConfig2 = new SonogramConfig
            {
                WindowSize = freqScale2.FinalBinCount * 2,
                WindowOverlap = 0.2,
                SourceFName = recording2.BaseName,
                NoiseReductionType = NoiseReductionType.None,
                NoiseReductionParameter = 0.0,
            };
            var amplitudeSpectrogram2 = new AmplitudeSonogram(sonoConfig2, recording2.WavReader);
            amplitudeSpectrogram2.Configuration.WindowSize = freqScale2.WindowSize;
            var image2 = amplitudeSpectrogram2.GetImageFullyAnnotated(amplitudeSpectrogram2.GetImage(), "SPECTROGRAM: " + fst2.ToString(), freqScale2.GridLineLocations);
            image2.Save(outputAmpSpecImagePath, ImageFormat.Png);
            amplitudeSpectrogram2.Data = PcaWhitening.RmsNormalization(amplitudeSpectrogram2.Data);
            var sonogram2 = new SpectrogramStandard(amplitudeSpectrogram2);
            var standImage2 = sonogram2.GetImageFullyAnnotated(sonogram2.GetImage(), "SPECTROGRAM: " + fst2.ToString(), freqScale2.GridLineLocations);
            standImage2.Save(outputLinScaImagePath, ImageFormat.Png);
            //int patchWidth2 = 16; //32; //
            //int patchHeight2 = 16; //32; //
            int rows2 = sonogram2.Data.GetLength(0); //3247
            int cols2 = sonogram2.Data.GetLength(1); //256
            var sequentialPatches2 = PatchSampling.GetPatches(sonogram2.Data, patchWidth, patchHeight, (rows2 / patchHeight) * (cols2 / patchWidth), "sequential");
            //var randomPatches = PatchSampling.GetPatches(sonogram.Data, patchWidth, patchHeight, 4000, "random");
            double[,] sequentialPatchMatrix2 = sequentialPatches2.ToMatrix();
            //double[,] randomPatchMatrix = randomPatches.ToMatrix();
            double[,] reconstructedSpec2 = PcaWhitening.ReconstructSpectrogram(projectionMatrix, sequentialPatchMatrix2, actual.Item3, actual.Item4);
            sonogram2.Data = PatchSampling.ConvertPatches(reconstructedSpec2, patchWidth2, patchHeight2, cols2);
            var respecImage2 = sonogram2.GetImageFullyAnnotated(sonogram2.GetImage(), "RECONSTRUCTEDSPECTROGRAM: " + fst2.ToString(), freqScale2.GridLineLocations);
            respecImage2.Save(outputReSpecImagePath, ImageFormat.Png);
            //+++++++++++++++++++++++++++++++++++++++++++++++++Exp2
            */

            //+++++++++++++++++++++++++++++++++++++++++++++++++Exp3: different freq bands, different source-target, same patch size

            //First: creating 3 matrices from 3 different freq bands of the source spectrogram
            List<double[,]> allSubmatrices = PatchSampling.GetFreqBandMatrices(sonogram.Data);
            double[][,] matrices = allSubmatrices.ToArray();

            //Second: creating 3 projection matrices from 3 different group of random patches
            //obtained from 3 different freq bands of the source spectrogram
            List<double[,]> projectionMatrices = new List<double[,]>();
            List<double[,]> eigenVectors = new List<double[,]>();
            List<int> noOfComponents = new List<int>();

            //+++++++++++++++++++++++++++++++++++++++++++++++++Exp4: different freq bands, different source-target, different patch size
            int freqBandIndex = 0;
            while (freqBandIndex < allSubmatrices.Count)
            {
                if (freqBandIndex == 0)  //lower band: patch size is 32-by-8
                {
                    int lowPatchWidth = 8;
                    int lowPatchHeight = 32;
                    var randomPatches = PatchSampling.GetPatches(matrices[freqBandIndex], lowPatchWidth, lowPatchHeight, noOfRandomPatches, "random").ToMatrix();
                    var actual = PcaWhitening.Whitening(randomPatches);
                    projectionMatrices.Add(actual.Item1);
                    eigenVectors.Add(actual.Item3);
                    noOfComponents.Add(actual.Item4);
                    freqBandIndex++;
                }
                else
                {
                    if (freqBandIndex == 1)  //mid band: patch size is 16-by-16
                    {
                        var randomPatches = PatchSampling.GetPatches(matrices[freqBandIndex], patchWidth, patchHeight, noOfRandomPatches, "random").ToMatrix();
                        var actual = PcaWhitening.Whitening(randomPatches);
                        projectionMatrices.Add(actual.Item1);
                        eigenVectors.Add(actual.Item3);
                        noOfComponents.Add(actual.Item4);
                        freqBandIndex++;
                    }
                    else
                    {
                        if (freqBandIndex == 2) //upper band: patch size is 8-by-32
                        {
                            int upPatchWidth = 32;
                            int upPatchHeight = 8;
                            var randomPatches = PatchSampling.GetPatches(matrices[freqBandIndex], upPatchWidth, upPatchHeight, noOfRandomPatches, "random").ToMatrix();
                            var actual = PcaWhitening.Whitening(randomPatches);
                            projectionMatrices.Add(actual.Item1);
                            eigenVectors.Add(actual.Item3);
                            noOfComponents.Add(actual.Item4);
                            freqBandIndex++;
                        }
                    }
                }
            }
            //+++++++++++++++++++++++++++++++++++++++++++++++++Exp4: different freq bands, different source-target, different patch size
            /*
            for (int i = 0; i < allSubmatrices.Count; i++)
            {
                var randomPatches = PatchSampling.GetPatches(matrices[i], patchWidth, patchHeight, noOfRandomPatches, "random").ToMatrix();
                var actual = PcaWhitening.Whitening(randomPatches);
                projectionMatrices.Add(actual.Item1);
                eigenVectors.Add(actual.Item3);
                noOfComponents.Add(actual.Item4);
            }
            */

            //Third: divide the target spectrogram into 3 submatrices with different freq bands.
            //divide each submatrix into sequential patches
            var recording2Path = PathHelper.ResolveAsset("PcaWhitening", "20160705_064611_22069.wav"); //  "Recordings", "BAC2_20071008-085040.wav"  
            var recording2 = new AudioRecording(recording2Path);
            var fst2 = FreqScaleType.Linear;
            var freqScale2 = new FrequencyScale(fst2);
            var sonoConfig2 = new SonogramConfig
            {
                WindowSize = freqScale2.FinalBinCount * 2,
                WindowOverlap = 0.2,
                SourceFName = recording2.BaseName,
                NoiseReductionType = NoiseReductionType.None,
                NoiseReductionParameter = 0.0,
            };
            var amplitudeSpectrogram2 = new AmplitudeSonogram(sonoConfig2, recording2.WavReader);
            amplitudeSpectrogram2.Configuration.WindowSize = freqScale2.WindowSize;
            //var image2 = amplitudeSpectrogram2.GetImageFullyAnnotated(amplitudeSpectrogram2.GetImage(), "SPECTROGRAM: " + fst2.ToString(), freqScale2.GridLineLocations);
            //image2.Save(outputAmpSpecImagePath, ImageFormat.Png);
            amplitudeSpectrogram2.Data = PcaWhitening.RmsNormalization(amplitudeSpectrogram2.Data);
            var sonogram2 = new SpectrogramStandard(amplitudeSpectrogram2);
            //var standImage2 = sonogram2.GetImageFullyAnnotated(sonogram2.GetImage(), "SPECTROGRAM: " + fst2.ToString(), freqScale2.GridLineLocations);
            //standImage2.Save(outputLinScaImagePath, ImageFormat.Png);
            //int patchWidth2 = 16; //32; //
            //int patchHeight2 = 16; //32; //
            int rows2 = sonogram2.Data.GetLength(0); //3247
            //int cols2 = sonogram2.Data.GetLength(1); //256
            List<double[,]> allSubmatrices2 = PatchSampling.GetFreqBandMatrices(sonogram2.Data);
            //double[][,] matrices2 = allSubmatrices2.ToArray();

            //Forth: Reconstruct the source matrix with projection matrices
            List<double[,]> clearedSubmat = new List<double[,]>();

            //+++++++++++++++++++++++++++++++++++++++++++++++++Exp4: different freq bands, different source-target, different patch size
            freqBandIndex = 0;
            while (freqBandIndex < allSubmatrices2.Count)
            {
                if (freqBandIndex == 0)  //lower band: patch size is 32-by-8
                {
                    int lowPatchWidth = 8;
                    int lowPatchHeight = 32;
                    var sequentialPatches = PatchSampling.GetPatches(allSubmatrices2.ToArray()[freqBandIndex], lowPatchWidth, lowPatchHeight, (rows2 / lowPatchHeight) * (allSubmatrices2.ToArray()[freqBandIndex].GetLength(1) / lowPatchWidth), "sequential");
                    double[,] reconstructedSpec2 = PcaWhitening.ReconstructSpectrogram(projectionMatrices.ToArray()[freqBandIndex], sequentialPatches.ToMatrix(), eigenVectors.ToArray()[freqBandIndex], noOfComponents.ToArray()[freqBandIndex]);
                    clearedSubmat.Add(PatchSampling.ConvertPatches(reconstructedSpec2, lowPatchWidth, lowPatchHeight, allSubmatrices2.ToArray()[freqBandIndex].GetLength(1)));
                    freqBandIndex++;
                }
                else
                {
                    if (freqBandIndex == 1)  //mid band: patch size is 16-by-16
                    {
                        var sequentialPatches = PatchSampling.GetPatches(allSubmatrices2.ToArray()[freqBandIndex], patchWidth, patchHeight, (rows2 / patchHeight) * (allSubmatrices2.ToArray()[freqBandIndex].GetLength(1) / patchWidth), "sequential");
                        double[,] reconstructedSpec2 = PcaWhitening.ReconstructSpectrogram(projectionMatrices.ToArray()[freqBandIndex], sequentialPatches.ToMatrix(), eigenVectors.ToArray()[freqBandIndex], noOfComponents.ToArray()[freqBandIndex]);
                        clearedSubmat.Add(PatchSampling.ConvertPatches(reconstructedSpec2, patchWidth, patchHeight, allSubmatrices2.ToArray()[freqBandIndex].GetLength(1)));
                        freqBandIndex++;
                    }
                    else
                    {
                        if (freqBandIndex == 2) //upper band: patch size is 8-by-32
                        {
                            int upPatchWidth = 32;
                            int upPatchHeight = 8;
                            var sequentialPatches = PatchSampling.GetPatches(allSubmatrices2.ToArray()[freqBandIndex], upPatchWidth, upPatchHeight, (rows2 / upPatchHeight) * (allSubmatrices2.ToArray()[freqBandIndex].GetLength(1) / upPatchWidth), "sequential");
                            double[,] reconstructedSpec2 = PcaWhitening.ReconstructSpectrogram(projectionMatrices.ToArray()[freqBandIndex], sequentialPatches.ToMatrix(), eigenVectors.ToArray()[freqBandIndex], noOfComponents.ToArray()[freqBandIndex]);
                            clearedSubmat.Add(PatchSampling.ConvertPatches(reconstructedSpec2, upPatchWidth, upPatchHeight, allSubmatrices2.ToArray()[freqBandIndex].GetLength(1)));
                            freqBandIndex++;
                        }
                    }
                }
            }
            //+++++++++++++++++++++++++++++++++++++++++++++++++Exp4: different freq bands, different source-target, different patch size

            /*
            for (int i = 0; i < allSubmatrices2.Count; i++)
            {
                var sequentialPatches = PatchSampling.GetPatches(allSubmatrices2.ToArray()[i], patchWidth, patchHeight, (rows2 / patchHeight) * (allSubmatrices2.ToArray()[i].GetLength(1) / patchWidth), "sequential");
                double[,] reconstructedSpec2 = PcaWhitening.ReconstructSpectrogram(projectionMatrices.ToArray()[i], sequentialPatches.ToMatrix(), eigenVectors.ToArray()[i], noOfComponents.ToArray()[i]);
                clearedSubmat.Add(PatchSampling.ConvertPatches(reconstructedSpec2, patchWidth, patchHeight, allSubmatrices2.ToArray()[i].GetLength(1)));
            }
            */

            sonogram2.Data = PatchSampling.ConcatFreqBandMatrices(clearedSubmat);
            var respecImage2 = sonogram2.GetImageFullyAnnotated(sonogram2.GetImage(), "RECONSTRUCTEDSPECTROGRAM: " + fst2.ToString(), freqScale2.GridLineLocations);
            respecImage2.Save(outputReSpecImagePath, ImageFormat.Png);
            //+++++++++++++++++++++++++++++++++++++++++++++++++Exp3
            /*
            //we need the width (#columns) of the original spectrogram to reconstruct the matrix from patches
            //int cols = sonogram.Data.GetLength(1);
            //sonogram.Data = actual;
            sonogram.Data = PatchSampling.ConvertPatches(actual.Item2, patchWidth, patchHeight, cols);

            //ImageTools.DrawMatrix(actual, outputWhitenedSpectrogramPath);

            //var whitenedImage = sonogram.GetImageFullyAnnotated(sonogram.GetImage(), "WHITENEDSPECTROGRAM: " + fst.ToString(), freqScale.GridLineLocations);
            var whitenedImage = sonogram.GetImageFullyAnnotated(sonogram.GetImage(), "WHITENEDSPECTROGRAM: " + fst.ToString(), freqScale.GridLineLocations);
            whitenedImage.Save(outputWhitenedSpectrogramPath, ImageFormat.Png);

            //Assert.AreEqual(expected, actual);
            */

            /*
            double[][] data =
            {
                new[] { 2.5,  2.4 },
                new[] { 0.5,  0.7 },
                new[] { 2.2,  2.9 },
                new[] { 1.9,  2.2 },
                new[] { 3.1,  3.0 },
                new[] { 2.3,  2.7 },
                new[] { 2.0,  1.6 },
                new[] { 1.0,  1.1 },
                new[] { 1.5,  1.6 },
                new[] { 1.1,  0.9 }
            };
            var pca = new PrincipalComponentAnalysis()
            {
                Method = PrincipalComponentMethod.Center,
                Whiten = true,
                //ExplainedVariance = 0.1,
            };
            pca.Learn(data);
            var i = pca.Eigenvalues;
            var j = pca.NumberOfOutputs;
            double[][] output1 = pca.Transform(data);

            pca.ExplainedVariance = 0.7;
            double[][] output2 = pca.Transform(data);
            var k = pca.NumberOfInputs;
            */
        }
    }
}