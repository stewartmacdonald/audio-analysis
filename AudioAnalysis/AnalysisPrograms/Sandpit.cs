﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

using TowseyLib;
using AudioAnalysisTools;

namespace AnalysisPrograms
{
    class Sandpit
    {

        public const int RESAMPLE_RATE = 17640;
        public const string imageViewer = @"C:\Windows\system32\mspaint.exe";



        public static void Dev(string[] args)
        {

            //SET VERBOSITY
            DateTime tStart = DateTime.Now;
            Log.Verbosity = 1;
            Log.WriteLine("# Start Time = " + tStart.ToString());

            // experiments with Sobel ridge detector
            if (true)
            {
                //string wavFilePath = @"C:\SensorNetworks\WavFiles\LewinsRail\BAC2_20071008-085040.wav";
                string wavFilePath = @"C:\SensorNetworks\WavFiles\SunshineCoast\DM420036_min407.wav";
                string outputDir = @"C:\SensorNetworks\Output\Test";
                string imageFname = "test3.png";
                string annotatedImageFname = "SC8_annotatedTEST.png";
                double magnitudeThreshold = 6.0; // of ridge hieght above neighbours
                double intensityThreshold = 5.0; // dB

                //var testImage = (Bitmap)(Image.FromFile(imagePath));
                var recording = new AudioRecording(wavFilePath);
                var config = new SonogramConfig { NoiseReductionType = NoiseReductionType.STANDARD, WindowOverlap = 0.5 };
                var spectrogram = new SpectralSonogram(config, recording.GetWavReader());
                Plot scores = null; 
                double eventThreshold = 0.5; // dummy variable - not used
                List<AcousticEvent> list = null;
                Image image = DrawSonogram(spectrogram, scores, list, eventThreshold);
                string imagePath = Path.Combine(outputDir, imageFname);
                image.Save(imagePath, ImageFormat.Png);

                Bitmap bmp = (Bitmap)image;

                double[,] matrix = MatrixTools.MatrixRotate90Anticlockwise(spectrogram.Data);

                List<PointOfInterest> poiList = new List<PointOfInterest>();
                double secondsScale = spectrogram.Configuration.GetFrameOffset(recording.SampleRate);
                var timeScale = TimeSpan.FromTicks((long)(secondsScale * TimeSpan.TicksPerSecond));
                double herzScale = spectrogram.FBinWidth;
                double freqBinCount = spectrogram.Configuration.FreqBinCount;
                int ridgeLength = 9; // dimension of NxN matrix to use for ridge detection - must be odd number
                int halfLength = ridgeLength / 2;

                int rows = matrix.GetLength(0);
                int cols = matrix.GetLength(1);
                for (int r = halfLength; r < rows - halfLength; r++)
                {
                    for (int c = halfLength; c < cols - halfLength; c++)
                    {
                        var subM = MatrixTools.Submatrix(matrix, r - halfLength, c - halfLength, r + halfLength, c + halfLength); // extract NxN submatrix
                        double magnitude, direction;
                        bool isRidge = false;
                        TowseyLib.ImageTools.SobelRidgeDetection(subM, out isRidge, out magnitude, out direction);
                        if (isRidge && (magnitude > magnitudeThreshold)) 
                        {
                            Point point = new Point(c, r);
                            //var poi = new PointOfInterest(point);
                            TimeSpan time = TimeSpan.FromSeconds(c * secondsScale);
                            double herz = (freqBinCount-r -1) * herzScale;
                            var poi = new PointOfInterest(time, herz);
                            poi.Point = point;
                            poi.RidgeOrientation = direction;
                            poi.OrientationCategory = (int)Math.Round((direction * 8) / Math.PI);
                            poi.RidgeMagnitude = magnitude;
                            poi.Intensity = matrix[r, c];
                            poi.TimeScale = timeScale;
                            poi.HerzScale = herzScale;
                            poi.IsLocalMaximum = MatrixTools.CentreIsLocalMaximum(subM, magnitudeThreshold + 2.0); // local max must stick out!
                            poiList.Add(poi);
                        }
                        //c++;
                    }
                    //r++;
                }

                //PointOfInterest.RemoveLowIntensityPOIs(poiList, intensityThreshold);

                PointOfInterest.PruneSingletons(poiList, rows, cols);
                //PointOfInterest.PruneDoublets(poiList, rows, cols);
                poiList = PointOfInterest.PruneAdjacentTracks(poiList, rows, cols);

                foreach (PointOfInterest poi in poiList)
                {
                    poi.DrawColor = Color.Crimson;
                    bool multiPixel = false;
                    //poi.DrawPoint(bmp, (int)freqBinCount, multiPixel);
                    poi.DrawOrientationPoint(bmp, (int)freqBinCount);

                    // draw local max
                    //poi.DrawColor = Color.Cyan;
                    //poi.DrawLocalMax(bmp, (int)freqBinCount);
                }

                imagePath = Path.Combine(outputDir, annotatedImageFname);
                image.Save(imagePath, ImageFormat.Png);
                FileInfo fiImage = new FileInfo(imagePath);
                if (fiImage.Exists)
                {
                    TowseyLib.ProcessRunner process = new TowseyLib.ProcessRunner(imageViewer);
                    process.Run(imagePath, outputDir);
                }

            } // experiments with Sobel ridge detector




            Log.WriteLine("# Finished!");
            Console.ReadLine();
            System.Environment.Exit(666);
        } // Dev()


        public static Image DrawSonogram(BaseSonogram sonogram, Plot scores, List<AcousticEvent> poi, double eventThreshold)
        {
            bool doHighlightSubband = false; bool add1kHzLines = true;
            Image_MultiTrack image = new Image_MultiTrack(sonogram.GetImage(doHighlightSubband, add1kHzLines));

            //System.Drawing.Image img = sonogram.GetImage(doHighlightSubband, add1kHzLines);
            //img.Save(@"C:\SensorNetworks\temp\testimage1.png", System.Drawing.Imaging.ImageFormat.Png);

            //Image_MultiTrack image = new Image_MultiTrack(img);
            image.AddTrack(Image_Track.GetTimeTrack(sonogram.Duration, sonogram.FramesPerSecond));
            image.AddTrack(Image_Track.GetSegmentationTrack(sonogram));
            if (scores != null) image.AddTrack(Image_Track.GetNamedScoreTrack(scores.data, 0.0, 1.0, scores.threshold, scores.title));
            if ((poi != null) && (poi.Count > 0))
                image.AddEvents(poi, sonogram.NyquistFrequency, sonogram.Configuration.FreqBinCount, sonogram.FramesPerSecond);
            return image.GetImage();
        } //DrawSonogram()


    } // class Sandpit

}