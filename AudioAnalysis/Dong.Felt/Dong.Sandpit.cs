﻿namespace Dong.Felt
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.IO;
    using System.Drawing;
    using TowseyLib;
    using AudioAnalysisTools;
    using AudioAnalysisTools.Sonogram;
    using System.Drawing.Imaging;
    using log4net;
    using AnalysisBase;
    using Representations;
    using Dong.Felt.Configuration;
    using Dong.Felt.Preprocessing;
    using Dong.Felt.ResultsOutput;

    public class DongSandpit
    {
        public const int RESAMPLE_RATE = 17640;
        public const string imageViewer = @"C:\Windows\system32\mspaint.exe";  // why we need this?

        public static void Play()
        {
            //SET VERBOSITY
            DateTime tStart = DateTime.Now;
            Log.Verbosity = 1;
            Log.WriteLine("# Start Time = " + tStart.ToString());
            /// experiments with similarity search with ridgeNeighbourhoodRepresentation.
            if (true)
            {

                var config = new SonogramConfig { NoiseReductionType = NoiseReductionType.STANDARD, WindowOverlap = 0.5 };
                var ridgeConfig = new RidgeDetectionConfiguration
                {
                    RidgeDetectionmMagnitudeThreshold = 6.5,
                    RidgeMatrixLength = 5,
                    FilterRidgeMatrixLength = 7,
                    MinimumNumberInRidgeInMatrix = 3
                };
                var neighbourhoodLength = 9;

                string inputDirectory = @"C:\XUEYAN\PHD research work\New Datasets\6.Grey Fantail1\Grey Fantail1-Training";
                string queryInputDirectory = @"C:\XUEYAN\PHD research work\New Datasets\6.Grey Fantail1\Query";
                string querycsvFileName = "SE_SE727_20101014-051200-0515-0516-Grey Fantail1.csv";
                string querycsvFilePath = Path.Combine(queryInputDirectory, querycsvFileName);
                string queryaudioFileName = "SE_SE727_20101014-051200-0515-0516-Grey Fantail1.wav";
                string queryaudioFilePath = Path.Combine(queryInputDirectory, queryaudioFileName);
                string outputDirectory = @"C:\XUEYAN\PHD research work\New Datasets\6.Grey Fantail1\RepresentationResults";
                string csvOutputFileName = "candidates region representation.csv";
                string csvOutputPath = Path.Combine(outputDirectory, csvOutputFileName);
                string matchedCandidateFileName = "matched candidates.csv";
                string matchedCandidateOutputPath = Path.Combine(outputDirectory, matchedCandidateFileName);
                var rank = 10;
                MatchingBatchProcess(querycsvFilePath, queryaudioFilePath, inputDirectory, neighbourhoodLength,
                    ridgeConfig, config, csvOutputPath, matchedCandidateOutputPath, rank);
                var csvfile = @"C:\XUEYAN\PHD research work\New Datasets\6.Grey Fantail1\RepresentationResults\matched candidates.csv";
                var file = new FileInfo(csvfile);
                var candidates = CSVResults.CsvToCandidatesList(file);
                string segmentOutputDirectory = @"C:\XUEYAN\PHD research work\New Datasets\6.Grey Fantail1\SegmentOutput";
                if (candidates != null)
                {
                    for (int i = 0; i < candidates.Count(); i++)
                    {
                        var outPutFileName = i + 1 + ".wav";
                        var outPutFilePath = Path.Combine(segmentOutputDirectory, outPutFileName);
                        OutputResults.AudioSegmentBasedCandidates(candidates[i], outPutFilePath.ToFileInfo());
                    }
                }
                var listString = new List<string>();
                listString.Add("Q");
                for (int i = 0; i < rank; i++)
                {
                    int temp = i + 1;
                    listString.Add(temp.ToString());
                }
                var query = new Candidates();
                candidates.Insert(0, query);
                var imageArray = DrawingSpectrogramsFromAudios(segmentOutputDirectory, config, listString, candidates).ToArray();
                var imageResult = ImageAnalysisTools.CombineImagesHorizontally(imageArray);
                var imageOutputName = "Combined image.png";
                var imagePath = Path.Combine(segmentOutputDirectory, imageOutputName);
                imageResult.Save(imagePath, ImageFormat.Png);
                
                /// Single experiment. 
                /// FilePathSetting
                //string inputDirectory = @"C:\XUEYAN\PHD research work\New Datasets\13.Silvereye3\Silvereye3-Training";
                //string outputDirectory = @"C:\XUEYAN\PHD research work\New Datasets\13.Silvereye3\RepresentationResults";
                //string audioFileName = "NEJB_NE465_20101017-050500-050600-Silvereye3.wav";
                //string wavFilePath = Path.Combine(inputDirectory, audioFileName);
                //string imageFileName = Path.ChangeExtension(audioFileName, "-NH-9.png");
                //string imagePath = Path.Combine(outputDirectory, imageFileName);
                //string annotatedImageFileName = Path.ChangeExtension(audioFileName, "-annotate.png");
                //string annotatedImagePath = Path.Combine(outputDirectory, annotatedImageFileName);
                //string nhRepresentationCsvFileName = Path.ChangeExtension(audioFileName, "nh-9-nhRepresentation.csv");
                //string nhRepresentationCsvPath = Path.Combine(outputDirectory, nhRepresentationCsvFileName);
                //string nhRegionCsvFileName = Path.ChangeExtension(audioFileName, "nh-9-regionRepresentation.csv");
                //string nhRegionCsvPath = Path.Combine(outputDirectory, nhRegionCsvFileName);

                ///// Read audio files into spectrogram.
                //var config = new SonogramConfig { NoiseReductionType = NoiseReductionType.STANDARD, WindowOverlap = 0.5 };
                //var spectrogram = Preprocessing.AudioPreprosessing.AudioToSpectrogram(config, wavFilePath);

                ///// spectrogramConfiguration setting
                //var secondToMillionSecondUnit = 1000;
                //var spectrogramConfig = new SpectrogramConfiguration
                //{
                //    FrequencyScale = spectrogram.FBinWidth,
                //    TimeScale = (spectrogram.FrameDuration - spectrogram.FrameOffset) * secondToMillionSecondUnit,
                //    NyquistFrequency = spectrogram.NyquistFrequency
                //};
                /////Ridge detection experiment
                //var ridgeConfig = new RidgeDetectionConfiguration
                //{
                //    RidgeDetectionmMagnitudeThreshold = 6.5,
                //    RidgeMatrixLength = 5,
                //    FilterRidgeMatrixLength = 7,
                //    MinimumNumberInRidgeInMatrix = 3
                //};

                ///// Read Liang's spectrogram.Data
                ////string fileName = "2Liang_spectro.csv";
                ////string csvPath = Path.Combine(outputDirectory, fileName);
                ////var lines = File.ReadAllLines(csvPath).Select(i => i.Split(','));
                ////var header = lines.Take(1).ToList();
                ////var lines1 = lines.Skip(1);
                ////var index = 0;
                ////var rows = 256;
                ////var columns = 5161;
                ////var array = new double[rows * columns];
                ////var matrix = new double[rows, columns];

                ////foreach (var csvRow in lines1)
                ////{
                ////    array[index++] = double.Parse(csvRow[1]);
                ////}

                ////for (int i = 0; i < rows; i++)
                ////{
                ////    for (int j = 0; j < columns; j++)
                ////    {
                ////        matrix[i, j] = array[i + j * rows];
                ////    }
                ////}

                ///// Change my spectrogram.Data into Liang's. 
                ////var spectrogramDataRows = spectrogram.Data.GetLength(0);
                ////var spectrogramDataColumns = spectrogram.Data.GetLength(1);
                ////for (int row = 0; row < spectrogramDataRows; row++)
                ////{
                ////    for (int col = 0; col < spectrogramDataColumns; col++)
                ////    {
                ////        spectrogram.Data[row, col] = 0.0;
                ////    }
                ////}

                /// spectrogram drawing setting
                //var scores = new List<double>();
                //scores.Add(1.0);
                //var acousticEventlist = new List<AcousticEvent>();
                //var poiList = new List<PointOfInterest>();
                //double eventThreshold = 0.5; // dummy variable - not used                               
                //Image image = ImageAnalysisTools.DrawSonogram(spectrogram, scores, acousticEventlist, eventThreshold, null);
                ////image.Save(imagePath, ImageFormat.Png);

                //var ridges = POISelection.PostRidgeDetection(spectrogram, ridgeConfig);
                //Bitmap bmp = (Bitmap)image;
                //foreach (PointOfInterest poi in ridges)
                //{
                //    //poi.DrawPoint(bmp, (int)freqBinCount, multiPixel);
                //    poi.DrawOrientationPoint(bmp, (int)spectrogram.Configuration.FreqBinCount);
                //    //poi.DrawRefinedOrientationPoint(bmp, (int)spectrogram.Configuration.FreqBinCount);
                //}

                /////Output poiList to CSV
                ////string fileName = "NW_NW273_20101013-051200-0513-0514-Brown Cuckoo-dove1-before refine direction.csv";
                ////string csvPath = Path.Combine(outputDirectory, fileName);
                ////CSVResults.PointOfInterestListToCSV(ridges, csvPath, wavFilePath);  

                ///// Read Liang's spectrogram data from csv file               
                ////// each region should have same nhCount, here we just get it from the first region item. 
                ////var dataOutputFile = @"C:\XUEYAN\DICTA Conference data\Spectrogram data for Toad.csv";
                ////var audioFilePath = "DM420008_262m_00s__264m_00s - Faint Toad.wav";
                ////results.Add(new List<string>() { "FileName", "rowIndex", "colIndex", "value"});
                ////for (int i = 0; i < matrix.GetLength(0); i++)
                ////{
                ////    for (int j = 0; j < matrix.GetLength(1); j++)
                ////    {
                ////        results.Add(new List<string>() { audioFilePath, i.ToString(), j.ToString(),matrix[i,j].ToString()});
                ////    }           
                ////}
                ////File.WriteAllLines(dataOutputFile, results.Select((IEnumerable<string> i) => { return string.Join(",", i); }));

                ///// Read the spectrogram.data into csv for Liang. 
                ////var result = new List<List<string>>();
                ////result.Add(new List<string>() { "FileName", "Value" });
                ////string fileName = "SE_SE727_20101014-074900-075000";
                ////string csvPath = Path.Combine(outputDirectory, fileName + ".csv");   
                ////for (int rowIndex = 0; rowIndex < rows; rowIndex++)
                ////{
                ////    for (int colIndex = 0; colIndex < cols; colIndex++)
                ////    {
                ////        result.Add(new List<string>() { fileName, matrix[rowIndex, colIndex].ToString() });
                ////    }
                ////}
                ////File.WriteAllLines(csvPath, result.Select((IEnumerable<string> i) => { return string.Join(",", i); }));

                //var rows = spectrogram.Data.GetLength(1) - 1;  // Have to minus the graphical device context line. 
                //var cols = spectrogram.Data.GetLength(0);
                //var nhRepresentationList = RidgeDescriptionNeighbourhoodRepresentation.FromAudioFilePointOfInterestList(ridges, rows, cols, neighbourhoodLength, spectrogramConfig);
                //var NormalizedNhRepresentationList = StatisticalAnalysis.NormalizeProperties(nhRepresentationList);
                //var file = new FileInfo(nhRepresentationCsvPath);
                //CSVResults.NhRepresentationListToCSV(file, NormalizedNhRepresentationList);

                ///// Read query          
                //var queryCsvFilePath = @"C:\XUEYAN\PHD research work\New Datasets\19.Torresian Crow\Query\SE_SE727_20101016-055700-055800-Torresian Crow.csv"; ;
                //var csvfile = new FileInfo(queryCsvFilePath);
                //var queryInfo = CSVResults.CsvToAcousticEvent(csvfile);
                //var nhFrequencyRange = neighbourhoodLength * spectrogram.FBinWidth;
                //var nhCountInRow = (int)(spectrogram.NyquistFrequency / nhFrequencyRange);
                //if (spectrogram.NyquistFrequency % nhFrequencyRange == 0)
                //{
                //    nhCountInRow--;
                //}
                //var nhCountInColumn = (int)(spectrogram.FrameCount / neighbourhoodLength);
                //if (spectrogram.FrameCount % neighbourhoodLength == 0)
                //{
                //    nhCountInColumn--;
                //}
                //var query = new Query(queryInfo.MaxFreq, queryInfo.MinFreq, queryInfo.TimeStart, queryInfo.TimeEnd, neighbourhoodLength, nhCountInRow, spectrogramConfig);


                ///// get query representation
                //var queryRegionRepresentation = Indexing.ExtractQueryRegionRepresentationFromAudioNhRepresentations(query, nhRepresentationList, nhCountInRow, nhCountInColumn, wavFilePath);
                ///// Write query representation into csv.
                //var CSVResultDirectory = @"C:\XUEYAN\PHD research work\New Datasets\19.Torresian Crow\RepresentationResults";
                ////var csvFileName1 = "SE_SE727_20101016-055700-055800-Torresian Crow-RegionRepresentation-neighbourhood-9.csv";
                ////string csvPath1 = Path.Combine(CSVResultDirectory, csvFileName1);
                ////var queryRegionRepresentationfile = new FileInfo(csvPath1);
                ////var file1 = new FileInfo(csvPath1);
                ////CSVResults.RegionRepresentationListToCSV(file1, queryRegionRepresentation);

                ///// get region representation for an audio file
                //var regionRepresentation = Indexing.RegionRepresentationFromAudioNhRepresentations(queryRegionRepresentation, nhRepresentationList, nhCountInRow, nhCountInColumn, wavFilePath, neighbourhoodLength, spectrogramConfig);

                ///// get the candidates from region representation list.
                //var candidatesRegionRepresentaion = Indexing.ExtractCandidatesRegionRepresentationFromRegionRepresntations(queryRegionRepresentation, regionRepresentation);

                ///// output candidatesRegionRepresentation
                ////var csvFileName2 = "SE_SE727_20101013-051800-051900-Shining Bronze-cuckoo1-candidates-regionRepresentation.csv";
                ////string csvPath2 = Path.Combine(CSVResultDirectory, csvFileName2);
                ////var file2 = new FileInfo(csvPath2);
                ////CSVResults.RegionRepresentationListToCSV(file2, candidatesRegionRepresentaion);

                ///// calculate the distance between candidates and query.
                //var weight1 = 0.3;
                //var weight2 = 0.7;
                //var candidateList = Indexing.DistanceCalculation(queryRegionRepresentation, candidatesRegionRepresentaion, weight1, weight2);
                ////var similarityScoreList = Indexing.DistanceListToSimilarityScoreList(candidateList);

                ///// write the similarity score into csv file.       
                //var candidateCsvFileName = "SE_SE727_20101016-055700-055800-Torresian Crow-candidates.csv";
                //var candidateOutputFilePath = Path.Combine(CSVResultDirectory, candidateCsvFileName);
                //var candidatefile = new FileInfo(candidateOutputFilePath);
                //CSVResults.CandidateListToCSV(candidatefile, candidateList); 

                ///reconstruct the spectrogram.
                //var gr = Graphics.FromImage(bmp);
                //foreach (var nh in nhRepresentationList)
                //foreach (var nh in normalisedNhRepresentationList)
                //{
                //    RidgeDescriptionNeighbourhoodRepresentation.RidgeNeighbourhoodRepresentationToImage(gr, nh);
                //}
                //image = (Image)bmp;
                //bmp.Save(imagePath);

                /// To get the similairty score and get the ranking. 
                //var rank = 10;
                //var itemList = (from l in listOfPositions
                //                orderby l.Item1 ascending
                //                select l);
                //var finalListOfPositions = new List<Tuple<double, List<RidgeNeighbourhoodFeatureVector>>>();
                //for (int i = 0; i < rank; i++)
                //{
                //    finalListOfPositions.Add(new Tuple<double, List<RidgeNeighbourhoodFeatureVector>>(itemList.ElementAt(i).Item1, itemList.ElementAt(i).Item2));
                //}
                //var finalListOfPositions = listOfPositions.GetRange(0, rank);
                //var times = queryFeatureVector.Count();
                //var filterfinalListOfPositions = FilterOutOverlappedEvents(finalListOfPositions, searchFrameStep, times);   
                //var similarityScoreVector = StatisticalAnalysis.SimilarityScoreListToVector(similarityScoreList);


                //var rank = 8;
                //distanceList.Sort();
                //var finalAcousticEvents = new List<AcousticEvent>();

                //    //foreach (var p in similarityScoreList)
                //    //{
                //    //    var frequencyRange = query.nhCountInRow * spectrogram.FBinWidth * neighbourhoodLength;
                //    //    var maxFrequency = p.Item3 + frequencyRange;
                //    //    var millisecondToSecondTransUnit = 1000;
                //    //    finalAcousticEvents.Add(new AcousticEvent(p.Item2 / millisecondToSecondTransUnit, query.duration / millisecondToSecondTransUnit, p.Item3, maxFrequency));
                //    //}
                //var filterOverlappedEvents = FilterOutOverlappedEvents(finalAcousticEvents);
                //var similarityScore = StatisticalAnalysis.ConvertDistanceToPercentageSimilarityScore(Indexing.DistanceScoreFromAudioRegionVectorRepresentation(queryRegionRepresentation, candidatesVector));

                ///Read the acoustic events from csv files.  
                //acousticEventlist = CSVResults.CsvToAcousticEvent(file);
                /// output events image
                //imagePath = Path.Combine(outputDirectory, annotatedImageFileName);

                /// to save the ridge detection spectrogram. 
                //image = (Image)bmp;
                //image.Save(annotatedImagePath);

                /// to save the annotated spectrogram. 
                //image = DrawSonogram(spectrogram, scores, finalAcousticEvents, eventThreshold, ridges);
                //image.Save(imagePath, ImageFormat.Png);
            }
        } // Dev()      

        // This function still needs to be considered. 
        public static List<PointOfInterest> ShowupPoiInsideBox(List<PointOfInterest> filterPoiList, List<PointOfInterest> finalPoiList, int rowsCount, int colsCount)
        {
            var Matrix = PointOfInterest.TransferPOIsToMatrix(filterPoiList, rowsCount, colsCount);
            var result = new PointOfInterest[rowsCount, colsCount];
            for (int row = 0; row < rowsCount; row++)
            {
                for (int col = 0; col < colsCount; col++)
                {
                    if (Matrix[row, col] == null) continue;
                    else
                    {
                        foreach (var fpoi in finalPoiList)
                        {
                            if (row == fpoi.Point.Y && col == fpoi.Point.X)
                            {
                                for (int i = 0; i < 11; i++)
                                {
                                    for (int j = 0; j < 11; j++)
                                    {
                                        if (StatisticalAnalysis.checkBoundary(row + i, col + j, rowsCount, colsCount))
                                        {
                                            result[row + i, col + j] = Matrix[row + i, col + j];
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return PointOfInterest.TransferPOIMatrix2List(result);
        }

        public static List<AcousticEvent> FilterOutOverlappedEvents(List<AcousticEvent> listOfEvents)
        {
            var result = new List<AcousticEvent>();
            for (int i = 0; i < listOfEvents.Count; i++)
            {
                for (int j = i + 1; j < listOfEvents.Count; j++)
                {
                    var timePosition1 = listOfEvents[i].TimeStart;
                    var timePosition2 = listOfEvents[j].TimeStart;

                    var positionDifference = Math.Abs(timePosition1 - timePosition2);
                    if (positionDifference <= listOfEvents[i].Duration)
                    {
                        listOfEvents.Remove(listOfEvents[i]);
                        j--;
                    }
                }
            }
            result = listOfEvents;
            return result;
        }

        public static List<Tuple<double, double, double>> OutputTopRank(List<List<Tuple<double, double, double>>> similarityScoreTupleList, int rank)
        {
            var result = new List<Tuple<double, double, double>>();
            var count = similarityScoreTupleList.Count;
            // result = similarityScoreTupleList[count - 1];
            for (int i = 1; i <= rank; i++)
            {
                var subListCount = similarityScoreTupleList[count - i].Count;
                for (int j = 0; j < subListCount; j++)
                {
                    if (similarityScoreTupleList[count - i][j].Item1 > 0.7)
                    {
                        result.Add(similarityScoreTupleList[count - i][j]);
                    }
                }
            }
            return result;
        }

        public static List<double> SeperateSimilarityScoreFromTuple(List<Tuple<double, double, double>> tuple)
        {
            var result = new List<double>();
            foreach (var t in tuple)
            {
                result.Add(t.Item1);
            }
            return result;
        }

        public static void MatchingBatchProcess(string queryCsvFilePath, string queryAudioFilePath, string trainingWavFileDirectory, int neighbourhoodLength,
            RidgeDetectionConfiguration ridgeConfig, SonogramConfig config, string regionPresentOutputCSVPath,
            string candidateLocationOutputFile, int rank)
        {
            if (Directory.Exists(trainingWavFileDirectory))
            {
                var audioFiles = Directory.GetFiles(trainingWavFileDirectory, @"*.wav", SearchOption.TopDirectoryOnly);
                var audioFilesCount = audioFiles.Count();
                /// To save all the candidates       
                var candidatesRegionList = new List<RegionRerepresentation>();
                var spectrogram = AudioPreprosessing.AudioToSpectrogram(config, queryAudioFilePath);
                var secondToMillionSecondUnit = 1000;
                var spectrogramConfig = new SpectrogramConfiguration
                    {
                        FrequencyScale = spectrogram.FBinWidth,
                        TimeScale = (spectrogram.FrameDuration - spectrogram.FrameOffset) * secondToMillionSecondUnit,
                        NyquistFrequency = spectrogram.NyquistFrequency
                    };
                var ridges = POISelection.PostRidgeDetection(spectrogram, ridgeConfig);
                var rows = spectrogram.Data.GetLength(1) - 1;  // Have to minus the graphical device context line. 
                var cols = spectrogram.Data.GetLength(0);
                var ridgeNhRepresentationList = RidgeDescriptionNeighbourhoodRepresentation.FromAudioFilePointOfInterestList(ridges, rows, cols, neighbourhoodLength, spectrogramConfig);
                var NormalizedNhRepresentationList = StatisticalAnalysis.NormalizeProperties(ridgeNhRepresentationList);
                /// 1. Read the query csv file by parsing the queryCsvFilePath
                var queryCsvFile = new FileInfo(queryCsvFilePath);
                var query = Query.QueryRepresentationFromQueryInfo(queryCsvFile, neighbourhoodLength, spectrogram, spectrogramConfig);
                var queryRepresentation = Indexing.ExtractQueryRegionRepresentationFromAudioNhRepresentations(query, neighbourhoodLength, NormalizedNhRepresentationList,
                    queryAudioFilePath, spectrogram);
                // regionRepresentation 
                for (int i = 0; i < audioFilesCount; i++)
                {
                    /// 2. Read the candidates 
                    var candidateSpectrogram = AudioPreprosessing.AudioToSpectrogram(config, audioFiles[i]);
                    var candidateRidges = POISelection.PostRidgeDetection(candidateSpectrogram, ridgeConfig);
                    var rows1 = candidateSpectrogram.Data.GetLength(1) - 1;
                    var cols1 = candidateSpectrogram.Data.GetLength(0);
                    var candidateRidgeNhRepresentationList = RidgeDescriptionNeighbourhoodRepresentation.FromAudioFilePointOfInterestList(candidateRidges, rows1, cols1, neighbourhoodLength, spectrogramConfig);
                    var CanNormalizedNhRepresentationList = StatisticalAnalysis.NormalizeProperties(candidateRidgeNhRepresentationList);
                    var regionRepresentation = Indexing.RegionRepresentationFromAudioNhRepresentations(queryRepresentation, CanNormalizedNhRepresentationList,
                        audioFiles[i], neighbourhoodLength, spectrogramConfig, candidateSpectrogram);
                    var candidatesRepresentation = Indexing.ExtractCandidatesRegionRepresentationFromRegionRepresntations(queryRepresentation, regionRepresentation);
                    foreach (var c in candidatesRepresentation)
                    {
                        candidatesRegionList.Add(c);
                    }
                }
                var outputFile = new FileInfo(regionPresentOutputCSVPath);
                CSVResults.RegionRepresentationListToCSV(outputFile, candidatesRegionList);

                ///3. Ranking the candidates - calculate the distance and output the matched acoustic events.
                var weight1 = 1;
                var weight2 = 1;
                /// To calculate the distance
                var candidateDistanceList = Indexing.WeightedEuclideanDistCalculation(queryRepresentation, candidatesRegionList, weight1, weight2);
                var normalizedCanList = StatisticalAnalysis.NormalizeCandidateDistance(candidateDistanceList);
                var simiScoreCandidatesList = StatisticalAnalysis.ConvertDistanceToSimilarityScore(normalizedCanList);
                var candidateLocationFile = new FileInfo(candidateLocationOutputFile);
                /// To save all matched acoustic events
                //var acousticEventList = new List<AcousticEvent>();
                var candidateList = new List<Candidates>();
                simiScoreCandidatesList = simiScoreCandidatesList.OrderByDescending(x => x.Score).ToList();
                if (simiScoreCandidatesList.Count != 0)
                {
                    for (int i = 0; i < rank; i++)
                    {
                        candidateList.Add(simiScoreCandidatesList[i]);
                    }
                }
                //var drawSpectrogramConfig = new DrawSpectrogramConfiguration
                //{
                //    Score = new List<double>(),
                //    AcousticEventList = acousticEventList,
                //    PoiList = new List<PointOfInterest>(),
                //    EventThreshold = 0.5,
                //};
                CSVResults.CandidateListToCSV(candidateLocationFile, candidateList);
            }
        }

        public static List<Image> DrawingSpectrogramsFromAudios(string audioFileDirectory, SonogramConfig config, List<string> s,
            List<Candidates> candidates)
        {
            var result = new List<Image>();
            if (Directory.Exists(audioFileDirectory))
            {
                var audioFiles = Directory.GetFiles(audioFileDirectory, @"*.wav", SearchOption.TopDirectoryOnly);
                var audioFilesCount = audioFiles.Count();
                for (int i = 0; i < audioFilesCount; i++)
                {
                    var spectrogram = AudioPreprosessing.AudioToSpectrogram(config, audioFiles[i]);
                    var scores = new List<double>();
                    scores.Add(1.0);
                    var acousticEventlist = new List<AcousticEvent>();
                    var poiList = new List<PointOfInterest>();
                    double eventThreshold = 0.5; // dummy variable - not used                               
                    Image image = ImageAnalysisTools.DrawSonogram(spectrogram, scores, acousticEventlist, eventThreshold, null);
                    var improvedImage = ImageAnalysisTools.DrawImageLeftIndicator(image, s[i]);                   
                    var finalImage = ImageAnalysisTools.DrawFileName(improvedImage, candidates[i]);
                    result.Add(finalImage);        
                }
            }
            return result;
        }

    } // class dong.sandpit
}
