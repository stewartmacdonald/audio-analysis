﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DrawLongDurationSpectrograms.cs" company="QutEcoacoustics">
// All code in this file and all associated files are the copyright and property of the QUT Ecoacoustics Research Group (formerly MQUTeR, and formerly QUT Bioacoustics Research Group).
// </copyright>
// <summary>
//   Defines the DrawLongDurationSpectrograms type.
//
// Action code for this analysis = ColourSpectrogram
// Activity Codes for other tasks to do with spectrograms and audio files:
//
// audio2csv - Calls AnalyseLongRecording.Execute(): Outputs acoustic indices and LD false-colour spectrograms.
// audio2sonogram - Calls AnalysisPrograms.Audio2Sonogram.Main(): Produces a sonogram from an audio file - EITHER custom OR via SOX.Generates multiple spectrogram images and oscilllations info
// indicescsv2image - Calls DrawSummaryIndexTracks.Main(): Input csv file of summary indices. Outputs a tracks image.
// colourspectrogram - Calls DrawLongDurationSpectrograms.Execute():  Produces LD spectrograms from matrices of indices.
// zoomingspectrograms - Calls DrawZoomingSpectrograms.Execute():  Produces LD spectrograms on different time scales.
// differencespectrogram - Calls DifferenceSpectrogram.Execute():  Produces Long duration difference spectrograms
//
// audiofilecheck - Writes information about audio files to a csv file.
// snr - Calls SnrAnalysis.Execute():  Calculates signal to noise ratio.
// audiocutter - Cuts audio into segments of desired length and format
// createfoursonograms
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace AnalysisPrograms
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Acoustics.Shared;
    using Acoustics.Shared.Csv;
    using AnalyseLongRecordings;
    using AudioAnalysisTools;
    using AudioAnalysisTools.Indices;
    using AudioAnalysisTools.LongDurationSpectrograms;
    using AudioAnalysisTools.LongDurationSpectrograms.Zooming;
    using AudioAnalysisTools.StandardSpectrograms;
    using log4net;
    using McMaster.Extensions.CommandLineUtils;
    using Production;
    using Production.Arguments;
    using Production.Validation;
    using TowseyLibrary;

    using Zio;

    /// <summary>
    /// First argument on command line to call this action is "ColourSpectrogram"
    /// Activity Codes for other tasks to do with spectrograms and audio files:
    ///
    /// audio2csv - Calls AnalyseLongRecording.Execute(): Outputs acoustic indices and LD false-colour spectrograms.
    /// audio2sonogram - Calls AnalysisPrograms.Audio2Sonogram.Main(): Produces a sonogram from an audio file - EITHER custom OR via SOX.Generates multiple spectrogram images and oscilllations info
    /// indicescsv2image - Calls DrawSummaryIndexTracks.Main(): Input csv file of summary indices. Outputs a tracks image.
    /// colourspectrogram - Calls DrawLongDurationSpectrograms.Execute():  Produces LD spectrograms from matrices of indices.
    /// zoomingspectrograms - Calls DrawZoomingSpectrograms.Execute():  Produces LD spectrograms on different time scales.
    /// differencespectrogram - Calls DifferenceSpectrogram.Execute():  Produces Long duration difference spectrograms
    ///
    /// audiofilecheck - Writes information about audio files to a csv file.
    /// snr - Calls SnrAnalysis.Execute():  Calculates signal to noise ratio.
    /// audiocutter - Cuts audio into segments of desired length and format
    /// createfoursonograms
    /// </summary>
    public static class DrawLongDurationSpectrograms
    {
        private static readonly ILog Log = LogManager.GetLogger(nameof(DrawLongDurationSpectrograms));

        public const string CommandName = "DrawLongDurationSpectrograms";

        [Command(
            CommandName,
            Description = "[BETA] Produces long-duration, false-colour spectrograms from matrices of spectral indices.")]
        public class Arguments : SubCommandBase
        {
            [Option(Description = "Directory where the input data is located.")]
            [DirectoryExists]
            [LegalFilePath]
            public string InputDataDirectory { get; set; }

            [Option(Description = "Directory where the output is to go.")]
            [DirectoryExistsOrCreate(createIfNotExists: true)]
            [LegalFilePath]
            public string OutputDirectory { get; set; }

            [Option(
                Description = "User specified file containing a list of indices and their properties.",
                ShortName = "ip")]
            [ExistingFile(Extension = ".yml")]
            [LegalFilePath]
            public string IndexPropertiesConfig { get; set; }

            [Option(
                ShortName = "fcs",
                Description = "Config file specifying directory containing indices.csv files and other parameters.")]
            [ExistingFile(Extension = ".yml")]
            [LegalFilePath]
            public string FalseColourSpectrogramConfig { get; set; }

            public string ColourMap1 { get; set; }

            public string ColourMap2 { get; set; }

            public TimeSpan TemporalScale { get; set; }

            public override Task<int> Execute(CommandLineApplication app)
            {
                DrawLongDurationSpectrograms.Execute(this);
                return this.Ok();
            }
        }

        public static void Execute(Arguments arguments)
        {
            if (arguments == null)
            {
                throw new NoDeveloperMethodException();
            }

            string date = "# DATE AND TIME: " + DateTime.Now;
            LoggedConsole.WriteLine("# DRAW LONG DURATION SPECTROGRAMS DERIVED FROM CSV FILES OF SPECTRAL INDICES OBTAINED FROM AN AUDIO RECORDING");
            LoggedConsole.WriteLine(date);
            LoggedConsole.WriteLine("# Spectrogram Config      file: " + arguments.FalseColourSpectrogramConfig);
            LoggedConsole.WriteLine("# Index Properties Config file: " + arguments.IndexPropertiesConfig);
            LoggedConsole.WriteLine();

            (FileEntry indexGenerationDataFile, FileEntry indexDistributionsFile) =
                ZoomParameters.CheckNeededFilesExist(arguments.InputDataDirectory.ToDirectoryEntry());

            var indexGenerationData = Json.Deserialize<IndexGenerationData>(indexGenerationDataFile);

            // spectral distribution statistics is required only when calcualting difference spectrograms.
            Dictionary<string, IndexDistributions.SpectralStats> indexDistributionsData = null;
            if (indexDistributionsFile != null && indexDistributionsFile.Exists)
            {
                indexDistributionsData = IndexDistributions.Deserialize(indexDistributionsFile);
            }

            // this config can be found in IndexGenerationData. If config argument not specified, simply take it from icd file
            LdSpectrogramConfig config;
            if (arguments.FalseColourSpectrogramConfig == null)
            {
                config = indexGenerationData.LongDurationSpectrogramConfig;
            }
            else
            {
                config = LdSpectrogramConfig.ReadYamlToConfig(arguments.FalseColourSpectrogramConfig.ToFileInfo());
            }

            string originalBaseName;
            string[] otherSegments;
            string analysisTag;
            FilenameHelpers.ParseAnalysisFileName(indexGenerationDataFile, out originalBaseName, out analysisTag, out otherSegments);

            // CHECK FOR ERROR SEGMENTS - get zero signal array
            var input = arguments.InputDataDirectory.ToDirectoryInfo();
            var csvFile = new FileInfo(Path.Combine(input.FullName, originalBaseName + "__Towsey.Acoustic.Indices.csv"));

            //Dictionary<string, double[]> summaryIndices = CsvTools.ReadCSVFile2Dictionary(csvFile.FullName);
            //var summaryIndices = Csv.ReadFromCsv<Dictionary<string, double[]>>(csvFile);
            var summaryIndices = Csv.ReadFromCsv<SummaryIndexValues>(csvFile);

            var indexErrors = GapsAndJoins.DataIntegrityCheckForZeroSignal(summaryIndices);

            //config.IndexCalculationDuration = TimeSpan.FromSeconds(1.0);
            //config.XAxisTicInterval = TimeSpan.FromSeconds(60.0);
            //config.IndexCalculationDuration = TimeSpan.FromSeconds(60.0);
            //config.XAxisTicInterval = TimeSpan.FromSeconds(3600.0);
            LDSpectrogramRGB.DrawSpectrogramsFromSpectralIndices(
                inputDirectory: input,
                outputDirectory: arguments.OutputDirectory.ToDirectoryInfo(),
                ldSpectrogramConfig: config,
                indexPropertiesConfigPath: arguments.IndexPropertiesConfig.ToFileInfo(),
                indexGenerationData: indexGenerationData,
                basename: originalBaseName,
                analysisType: AcousticIndices.TowseyAcoustic,
                indexSpectrograms: null,
                indexStatistics: indexDistributionsData,
                segmentErrors: indexErrors,
                imageChrome: false.ToImageChrome());

            Log.Success("Draw Long Duration Spectrograms complete!");
        }

        /// <summary>
        /// The integer returned from this method is the count of time-frames in the spectrogram.
        /// </summary>
        public static int DrawAggregatedSpectrograms(Arguments arguments, string fileStem, Dictionary<string, double[,]> spectra = null)
        {
            // note: the spectra are oriented as per visual orientation, i.e. xAxis = time frames
            var keys = spectra.Keys.ToArray();
            int frameCount = spectra[keys[0]].GetLength(1);
            double spectrogramScale = 0.1;
            TimeSpan timeScale = TimeSpan.FromSeconds(spectrogramScale);
            DirectoryInfo outputDirectory = arguments.OutputDirectory.ToDirectoryInfo();

            Image combinedImage = DrawGrayScaleSpectrograms(arguments, fileStem, timeScale, spectra);
            string fileName = Path.Combine(outputDirectory.FullName, fileStem + ".CombinedGreyScale.png");
            combinedImage.Save(fileName);

            // Draw False-colour Spectrograms
            combinedImage = DrawFalseColourSpectrograms(fileStem, timeScale, arguments.IndexPropertiesConfig.ToFileInfo(), spectra);
            fileName = Path.Combine(outputDirectory.FullName, fileStem + ".TwoMaps.png");
            combinedImage.Save(fileName);
            return frameCount;
        } // method DrawAggregatedSpectrograms()

        public static Image DrawGrayScaleSpectrograms(Arguments arguments, string fileStem, TimeSpan dataScale, Dictionary<string, double[,]> spectra = null)
        {
            int sampleRate = 22050;
            int frameWidth = 512;

            //double backgroundFilter = 0.0; // 0.0 means small values are removed.
            double backgroundFilter = 0.75;  // 0.75 means small values are accentuated.
            string analysisType = AcousticIndices.TowseyAcoustic;
            string[] keys = LDSpectrogramRGB.GetArrayOfAvailableKeys();

            //LoggedConsole.WriteLine("# Spectrogram Config      file: " + arguments.SpectrogramConfigPath);
            //LoggedConsole.WriteLine("# Index Properties Config file: " + arguments.IndexPropertiesConfig);
            var inputDirectory = arguments.InputDataDirectory;
            Dictionary<string, IndexProperties> indexProperties = IndexProperties.GetIndexProperties(arguments.IndexPropertiesConfig.ToFileInfo());

            if (spectra == null)
            {
                //C:\SensorNetworks\Output\BIRD50\Training\ID0001\Towsey.Acoustic\ID0001__Towsey.Acoustic.ACI
                spectra = IndexMatrices.ReadSpectralIndices(inputDirectory.ToDirectoryInfo(), fileStem, analysisType, keys);
            }

            // note: the spectra are oriented as per visual orientation, i.e. xAxis = time frames
            //int frameCount = spectra[keys[0]].GetLength(1);
            var cs1 = new LDSpectrogramRGB(minuteOffset: TimeSpan.Zero, xScale: dataScale, sampleRate: sampleRate, frameWidth: frameWidth, colourMap: null)
            {
                FileName = fileStem,
                BackgroundFilter = backgroundFilter,
                IndexCalculationDuration = dataScale,
            };

            cs1.SetSpectralIndexProperties(indexProperties); // set the relevant dictionary of index properties
            cs1.SpectrogramMatrices = spectra;
            if (cs1.GetCountOfSpectrogramMatrices() == 0)
            {
                LoggedConsole.WriteLine("WARNING:  " + fileStem + ":   No spectrogram matrices in the dictionary. Spectrogram files do not exist?");
                return null;
            }

            List<Image> list = new List<Image>();
            Font stringFont = new Font("Arial", 14);

            foreach (string key in keys)
            {
                var image = cs1.DrawGreyscaleSpectrogramOfIndex(key);

                int width = 70;
                int height = image.Height;
                var label = new Bitmap(width, height);
                var g1 = Graphics.FromImage(label);
                g1.Clear(Color.Gray);
                g1.DrawString(key, stringFont, Brushes.Black, new PointF(4, 30));
                g1.DrawLine(new Pen(Color.Black), 0, 0, width, 0); //draw upper boundary
                g1.DrawLine(new Pen(Color.Black), 0, 1, width, 1); //draw upper boundary

                Image[] imagearray = { label, image };
                var labelledImage = ImageTools.CombineImagesInLine(imagearray);
                list.Add(labelledImage);
            } //foreach key

            var combinedImage = ImageTools.CombineImagesVertically(list.ToArray());
            return combinedImage;
        } // method DrawGrayScaleSpectrograms()

        public static Image DrawFalseColourSpectrograms(Arguments args, string fileStem, Dictionary<string, double[,]> spectra = null)
        {
            //DirectoryInfo inputDirectory = args.InputDataDirectory;
            FileInfo indexPropertiesConfig = args.IndexPropertiesConfig.ToFileInfo();
            Dictionary<string, IndexProperties> indexProperties = IndexProperties.GetIndexProperties(indexPropertiesConfig);
            return DrawFalseColourSpectrograms(args, fileStem, indexProperties, spectra);
        }

        /// <summary>
        /// Draws two false colour spectrograms using a default set of arguments
        /// </summary>
        public static Image DrawFalseColourSpectrograms(string fileStem, TimeSpan dataScale, FileInfo indexPropertiesConfig, Dictionary<string, double[,]> spectra = null)
        {
            // read in index properties and create a new entry for "PHN"
            Dictionary<string, IndexProperties> indexProperties = IndexProperties.GetIndexProperties(indexPropertiesConfig);

            var args = new Arguments();

            // args.InputDataDirectory = new DirectoryInfo(Path.Combine(outputDirectory.FullName, recording.BaseName + ".csv")),
            // args.OutputDirectory = new DirectoryInfo(outputDirectory.FullName + @"/SpectrogramImages");
            args.FalseColourSpectrogramConfig = null;
            args.IndexPropertiesConfig = indexPropertiesConfig.FullName;
            args.ColourMap1 = LDSpectrogramRGB.DefaultColorMap1;
            args.ColourMap2 = LDSpectrogramRGB.DefaultColorMap2;
            args.TemporalScale = dataScale;

            return DrawFalseColourSpectrograms(args, fileStem, indexProperties, spectra);
        }

        public static Image DrawFalseColourSpectrograms(Arguments args, string fileStem, Dictionary<string, IndexProperties> indexProperties, Dictionary<string, double[,]> spectra = null)
        {
            // note: the spectra are oriented as per visual orientation, i.e. xAxis = time framesDictionary<string, Int16>.KeyCollection keys = AuthorList.Keys
            // string[] keys = spectra.Keys.ToCommaSeparatedList().Split(',');
            // int frameCount = spectra[keys[0]].GetLength(1);

            int sampleRate = 22050;
            int frameWidth = 512;
            double backgroundFilter = 0.75;  // 0.75 means small values are accentuated.
            var minuteOffset = TimeSpan.Zero;
            var dataScale = args.TemporalScale;
            string colourMap = args.ColourMap1 ?? LDSpectrogramRGB.DefaultColorMap1;
            var cs1 = new LDSpectrogramRGB(minuteOffset, dataScale, sampleRate, frameWidth, colourMap)
            {
                FileName = fileStem,
                BackgroundFilter = backgroundFilter,
                IndexCalculationDuration = dataScale,
            };
            cs1.SetSpectralIndexProperties(indexProperties); // set the relevant dictionary of index properties
            cs1.SpectrogramMatrices = spectra;

            var image1 = cs1.DrawFalseColourSpectrogramChromeless("NEGATIVE", colourMap);
            var fullDuration = TimeSpan.FromSeconds(image1.Width * dataScale.TotalSeconds);

            string title = fileStem;
            var titleImage = LDSpectrogramRGB.DrawTitleBarOfFalseColourSpectrogram(title, image1.Width);
            int trackHeight = 20;
            var timeScale = ImageTrack.DrawTimeRelativeTrack(fullDuration, image1.Width, trackHeight);

            colourMap = args.ColourMap2 ?? LDSpectrogramRGB.DefaultColorMap2;
            var image2 = cs1.DrawFalseColourSpectrogramChromeless("NEGATIVE", colourMap);
            var list = new List<Image> { titleImage, image1, timeScale, image2 };
            var combinedImage = ImageTools.CombineImagesVertically(list.ToArray());
            return combinedImage;
        }

        /// <summary>
        /// The integer returned from this method is the number of seconds duration of the spectrogram.
        /// Note that this method is called only when spectrogramScale = 0.1
        /// </summary>
        public static int DrawRidgeSpectrograms(Arguments arguments, string fileStem, Dictionary<string, double[,]> spectra = null)
        {
            //LoggedConsole.WriteLine("# Spectrogram Config      file: " + arguments.SpectrogramConfigPath);
            //LoggedConsole.WriteLine("# Index Properties Config file: " + arguments.IndexPropertiesConfig);
            var inputDirectory = arguments.InputDataDirectory;
            var outputDirectory = arguments.OutputDirectory;
            var indexPropertiesConfig = arguments.IndexPropertiesConfig;
            double spectrogramScale = 0.1;

            // var dataScale = TimeSpan.FromSeconds(spectrogramScale);

            // draw the spectrogram images
            var labelledImage = DrawRidgeSpectrograms(inputDirectory.ToDirectoryInfo(), indexPropertiesConfig.ToFileInfo(), fileStem, spectrogramScale, spectra = null);

            // combine and save
            string fileName = Path.Combine(outputDirectory.ToDirectoryInfo().FullName, fileStem + ".Ridges.png");
            labelledImage.Save(fileName);
            return (int)Math.Round(labelledImage.Width * spectrogramScale);
        } // method DrawRidgeSpectrograms()

        public static Image DrawRidgeSpectrograms(DirectoryInfo inputDirectory, FileInfo ipConfig, string fileStem, double scale, Dictionary<string, double[,]> spectra = null)
        {
            string analysisType = AcousticIndices.TowseyAcoustic;

            //double backgroundFilter = 0.0; // 0.0 means small values are removed.
            double backgroundFilter = 0.75;  // 0.75 means small values are accentuated.
            var dataScale = TimeSpan.FromSeconds(scale);

            Dictionary<string, IndexProperties> indexProperties = IndexProperties.GetIndexProperties(ipConfig);
            string[] keys = SpectralPeakTracks.GetDefaultRidgeKeys();

            // read the csv files of the indices in keys array
            if (spectra == null)
            {
                //C:\SensorNetworks\Output\BIRD50\Training\ID0001\Towsey.Acoustic\ID0001__Towsey.Acoustic.ACI
                spectra = IndexMatrices.ReadSpectralIndices(inputDirectory, fileStem, analysisType, keys);
            }

            var cs1 = new LDSpectrogramRGB(minuteOffset: TimeSpan.Zero, xScale: dataScale, sampleRate: 22050, frameWidth: 512, colourMap: null)
            {
                FileName = fileStem,
                BackgroundFilter = backgroundFilter,
                IndexCalculationDuration = dataScale,
            };

            // set the relevant dictionary of index properties
            cs1.SetSpectralIndexProperties(indexProperties);
            cs1.SpectrogramMatrices = spectra;
            if (cs1.GetCountOfSpectrogramMatrices() == 0)
            {
                LoggedConsole.WriteLine("WARNING:  " + fileStem + ":   No spectrogram matrices in the dictionary. Spectrogram files do not exist?");
                return null;
            }
            else if (cs1.GetCountOfSpectrogramMatrices() < keys.Length)
            {
                LoggedConsole.WriteLine("WARNING:  " + fileStem + ":   Missing indices in the dictionary. Some files do not exist?");
                return null;
            }

            Font stringFont = new Font("Tahoma", 8);

            // constants for labels
            Brush[] brush = { Brushes.Blue, Brushes.Green, Brushes.Red, Brushes.Orange, Brushes.Purple };
            Color[] color = { Color.Blue, Color.Green, Color.Red, Color.Orange, Color.Purple };
            int labelYvalue = 3;
            int labelIndex = 0;
            Bitmap ridges = null;
            Graphics g2 = null;

            foreach (string key in keys)
            {
                Bitmap greyScaleImage = (Bitmap)cs1.DrawGreyscaleSpectrogramOfIndex(key);
                var pixelWidth = greyScaleImage.Width;

                int height = greyScaleImage.Height;
                if (ridges == null)
                {
                    ridges = new Bitmap(pixelWidth, height);
                    g2 = Graphics.FromImage(ridges);
                    g2.Clear(Color.White);
                }

                g2.DrawString(key, stringFont, brush[labelIndex], new PointF(0, labelYvalue));
                labelYvalue += 10;

                //g1.DrawLine(new Pen(Color.Black), 0, 0, width, 0);//draw upper boundary
                //g1.DrawLine(new Pen(Color.Black), 0, 1, width, 1);//draw upper boundary

                // transfer greyscale image to colour image
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < pixelWidth; x++)
                    {
                        var col = greyScaleImage.GetPixel(x, y);
                        if (col.G < 150)
                        {
                            ridges.SetPixel(x, y, color[labelIndex]);
                        }
                    }
                }

                labelIndex += 1;
            } //foreach key

            return ridges;
        } // method DrawRidgeSpectrograms()

        /*
        public static void CreatePhnIndex(Dictionary<string, IndexProperties> indexProperties, Dictionary<string, double[,]> spectra)
        {
            string newKey = "PHN";
            if (!spectra.ContainsKey(newKey))
            {
                // create a composite index from three related indices - take the max
                // Assume that the values are comparable so that max is meaningful.
                double[,] phnIndex = CreateNewCompositeIndex(spectra, "RHZ-RPS-RNG");

                // Name the index PHN because it is composite of Positive, Horiz and Negative ridge values.
                spectra.Add(newKey, phnIndex);
            }

            if (!indexProperties.ContainsKey(newKey))
            {
                IndexProperties phnProperties = new IndexProperties();
                phnProperties.Key = newKey;
                phnProperties.Name = newKey;
                phnProperties.NormMin = 2.0;
                phnProperties.NormMax = 10.0;
                phnProperties.CalculateNormMin = false;
                phnProperties.CalculateNormMax = false;
                indexProperties.Add(newKey, phnProperties);
            }
        }

        public static double[,] CreateNewCompositeIndex(Dictionary<string, double[,]> spectra, string sourceFeatures)
        {
            string[] keys = sourceFeatures.Split('-');

            int rowCount = spectra[keys[0]].GetLength(0);
            int colCount = spectra[keys[0]].GetLength(1);
            double[,] compositeIndex = new double[rowCount, colCount];

            for (int row = 0; row < rowCount; row++)
            {
                for (int col = 0; col < colCount; col++)
                {
                    double value = 0.0;
                    for (int i = 0; i < keys.Length; i++)
                    {
                        if (value < (spectra[keys[i]])[row, col])
                        {
                            value = spectra[keys[i]][row, col];
                        }
                    }

                    compositeIndex[row, col] = value;
                }
            }

            return compositeIndex;
        }
        */
    }
}
