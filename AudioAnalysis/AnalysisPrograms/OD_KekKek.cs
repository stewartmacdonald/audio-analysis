﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using TowseyLib;
using AudioAnalysisTools;

namespace AnalysisPrograms
{

    //Here is link to wiki page containing info about how to write Analysis techniques
    //https://wiki.qut.edu.au/display/mquter/Audio+Analysis+Processing+Architecture

    //HERE ARE COMMAND LINE ARGUMENTS TO PLACE IN START OPTIONS - PROPERTIES PAGE,  debug command line
    //for LEWIN's RAIL
    // ID, recording, template.zip, working directory.
    //kekkek C:\SensorNetworks\WavFiles\LewinsRail\BAC2_20071008-075040.wav C:\SensorNetworks\Templates\Template_2\Template2.zip  C:\SensorNetworks\Output\LewinsRail\
    class OD_KekKek
    {

        //Keys to recognise identifiers in PARAMETERS - INI file. 
        public static string key_MIN_HZ = "MIN_FREQ";
        public static string key_MAX_HZ = "MAX_FREQ";
        public static string key_FRAME_OVERLAP = "FRAME_OVERLAP";
        public static string key_DCT_DURATION = "DCT_DURATION";
        public static string key_MIN_OSCIL_FREQ = "MIN_OSCIL_FREQ";
        public static string key_MAX_OSCIL_FREQ = "MAX_OSCIL_FREQ";
        public static string key_MIN_AMPLITUDE = "MIN_AMPLITUDE";
        public static string key_MIN_DURATION = "MIN_DURATION";
        public static string key_MAX_DURATION = "MAX_DURATION";
        public static string key_EVENT_THRESHOLD = "EVENT_THRESHOLD";
        public static string key_DRAW_SONOGRAMS = "DRAW_SONOGRAMS";

        public static string eventsFile = "events.txt"; 



        public static void Dev(string[] args)
        {
            string title = "# DETECTING LEWIN's RAIL Kek-Kek USING MFCCs and OD";
            string date = "# DATE AND TIME: " + DateTime.Now;
            Log.WriteLine(title);
            Log.WriteLine(date);

            Log.Verbosity = 1;
            CheckArguments(args);


            string recordingPath    = args[0];
            string templatePath     = args[1];
            string workingDirectory = args[2];
            string recordingFN      = Path.GetFileName(recordingPath);
            string outputDir        = workingDirectory;
            string opFName          = "events.txt"; ;
            string opPath           = outputDir + opFName;
            string templateFN       = Path.GetFileNameWithoutExtension(templatePath);
            
            Log.WriteLine("# Recording:     " + recordingFN);
            Log.WriteLine("# Working Dir:   " + workingDirectory);
            Log.WriteLine("# Output folder: " + outputDir);
            FileTools.WriteTextFile(opPath, date + "\n# Scanning recording for Lewin's Rail Kek Kek\n# Recording file: " + recordingFN);


            //A: SHIFT TEMPLATE TO WORKING DIRECTORY AND UNZIP IF NOT ALREADY DONE
            string templateName = Path.GetFileNameWithoutExtension(templatePath);
            //create the working directory if it does not exist
            if (!Directory.Exists(workingDirectory)) Directory.CreateDirectory(workingDirectory);
            string newTemplateDir = workingDirectory + templateName;
            //if (!Directory.Exists(newTemplateDir)) 
                ZipUnzip.UnZip(newTemplateDir, templatePath, true);

            //B: INI CONFIG and CREATE DIRECTORY STRUCTURE
            Log.WriteLine("# Init CONFIG and creating directory structure");
            Log.WriteLine("# New Template Dir: " + newTemplateDir);
            //READ PARAMETER VALUES FROM INI FILE
            //HTKConfig htkConfig = new HTKConfig(workingDirectory, templateName);
            //Log.WriteLine("\tDATA  =" + htkConfig.DataDir);
            //Log.WriteLine("\tRESULT=" + htkConfig.ResultsDir);
            string iniPath = workingDirectory + templateFN + "\\" + templateFN + ".txt";
            //NEXT LINE IS A TEMPORARY FIX
            AppendNewParams(iniPath);

            var config = new Configuration(iniPath);
            Dictionary<string, string> dict = config.GetTable();
            Dictionary<string, string>.KeyCollection keys = dict.Keys;

            int minHz = Int32.Parse(dict[key_MIN_HZ]);
            int maxHz = Int32.Parse(dict[key_MAX_HZ]);
            double frameOverlap = Double.Parse(dict[key_FRAME_OVERLAP]);
            double dctDuration = Double.Parse(dict[key_DCT_DURATION]);     //duration of DCT in seconds 
            int minOscilFreq = Int32.Parse(dict[key_MIN_OSCIL_FREQ]); //ignore oscillations below this threshold freq
            int maxOscilFreq = Int32.Parse(dict[key_MAX_OSCIL_FREQ]);    //ignore oscillations above this threshold freq
            double minAmplitude = Double.Parse(dict[key_MIN_AMPLITUDE]);    //minimum acceptable value of a DCT coefficient
            double eventThreshold = Double.Parse(dict[key_EVENT_THRESHOLD]);
            double minDuration = Double.Parse(dict[key_MIN_DURATION]);     //min duration of event in seconds 
            double maxDuration = Double.Parse(dict[key_MAX_DURATION]);     //max duration of event in seconds 
            int DRAW_SONOGRAMS = Int32.Parse(dict[key_DRAW_SONOGRAMS]);    //options to draw sonogram

            Log.WriteIfVerbose("Freq band: {0} Hz - {1} Hz.)", minHz, maxHz);
            Log.WriteIfVerbose("Oscill bounds: " + minOscilFreq + " - " + maxOscilFreq + " Hz");
            Log.WriteIfVerbose("minAmplitude = " + minAmplitude);
            Log.WriteIfVerbose("Duration bounds: " + minDuration + " - " + maxDuration + " seconds");

            //#############################################################################################################################################
            var results = Execute_CallDetect(recordingPath, minHz, maxHz, frameOverlap, dctDuration, minOscilFreq, maxOscilFreq, minAmplitude,
                                                eventThreshold, minDuration, maxDuration);
            Log.WriteLine("# Finished detecting Lewin's Rail calls.");
            //#############################################################################################################################################

            var sonogram = results.Item1;
            var hits = results.Item2;
            var scores = results.Item3;
            var predictedEvents = results.Item4;
            Log.WriteLine("# Event Count = " + predictedEvents.Count());

            //write event count to results file.            
            WriteEventsInfo2TextFile(predictedEvents, opPath);

            if (DRAW_SONOGRAMS == 2)
            {
                string imagePath = outputDir + Path.GetFileNameWithoutExtension(recordingPath) + ".png";
                DrawSonogram(sonogram, imagePath, hits, scores, predictedEvents, eventThreshold);
            }
            else
                if ((DRAW_SONOGRAMS == 1) && (predictedEvents.Count > 0))
                {
                    string imagePath = outputDir + Path.GetFileNameWithoutExtension(recordingPath) + ".png";
                    DrawSonogram(sonogram, imagePath, hits, scores, predictedEvents, eventThreshold);
                }

            Log.WriteLine("# Finished recording:- " + Path.GetFileName(recordingPath));
            Console.ReadLine();
        } //Dev()


        public static System.Tuple<BaseSonogram, Double[,], double[], List<AcousticEvent>> Execute_CallDetect(string wavPath,
            int minHz, int maxHz, double frameOverlap, double dctDuration, int minOscilFreq, int maxOscilFreq, double minAmplitude,
            double eventThreshold, double minDuration, double maxDuration)
        {
            //i: GET RECORDING
            AudioRecording recording = new AudioRecording(wavPath);
            if (recording.SampleRate != 22050) recording.ConvertSampleRate22kHz();
            int sr = recording.SampleRate;

            //ii: MAKE SONOGRAM
            Log.WriteLine("Start sonogram.");
            SonogramConfig sonoConfig = new SonogramConfig(); //default values config
            sonoConfig.WindowOverlap = frameOverlap;
            sonoConfig.SourceFName = recording.FileName;
            BaseSonogram sonogram = new SpectralSonogram(sonoConfig, recording.GetWavReader());
            recording.Dispose();
            Log.WriteLine("Signal: Duration={0}, Sample Rate={1}", sonogram.Duration, sr);
            Log.WriteLine("Frames: Size={0}, Count={1}, Duration={2:f1}ms, Overlap={5:f0}%, Offset={3:f1}ms, Frames/s={4:f1}",
                                       sonogram.Configuration.WindowSize, sonogram.FrameCount, (sonogram.FrameDuration * 1000),
                                      (sonogram.FrameOffset * 1000), sonogram.FramesPerSecond, frameOverlap);
            int binCount = (int)(maxHz / sonogram.FBinWidth) - (int)(minHz / sonogram.FBinWidth) + 1;
            Log.WriteIfVerbose("Freq band: {0} Hz - {1} Hz. (Freq bin count = {2})", minHz, maxHz, binCount);

            Log.WriteIfVerbose("DctDuration=" + dctDuration + "sec.  (# frames=" + (int)Math.Round(dctDuration * sonogram.FramesPerSecond) + ")");
            Log.WriteIfVerbose("EventThreshold=" + eventThreshold);
            Log.WriteLine("Start OD event detection");

            //iii: DETECT OSCILLATIONS
            List<AcousticEvent> predictedEvents;  //predefinition of results event list
            double[] scores;                      //predefinition of score array
            Double[,] hits;                       //predefinition of hits matrix - to superimpose on sonogram image
            OscillationAnalysis.Execute((SpectralSonogram)sonogram, minHz, maxHz, dctDuration, minOscilFreq, maxOscilFreq,
                                         minAmplitude, eventThreshold, minDuration, maxDuration, out scores, out predictedEvents, out hits);

            return System.Tuple.Create(sonogram, hits, scores, predictedEvents);

        }//end CaneToadRecogniser


        public static Template_CCAuto ReadSerialisedFile2Template(string serialPath)
        {
            //Log.WriteLine("\nA: READ serialised template from file: " + serialPath);
            var serializedData = FileTools.ReadSerialisedObject(serialPath);
            var template = QutSensors.Shared.Utilities.BinaryDeserialize(serializedData) as Template_CCAuto;
            template.mode = Mode.UNDEFINED;
            return template;
        }


        static void DrawSonogram(BaseSonogram sonogram, string path, double[,] hits, double[] scores, List<AcousticEvent> predictedEvents, double eventThreshold)
        {
            Log.WriteLine("# Start to draw image of sonogram.");
            bool doHighlightSubband = false; bool add1kHzLines = true;
            double maxScore = 50.0; //assumed max posisble oscillations per second

            using (System.Drawing.Image img = sonogram.GetImage(doHighlightSubband, add1kHzLines))
            using (Image_MultiTrack image = new Image_MultiTrack(img))
            {
                //img.Save(@"C:\SensorNetworks\WavFiles\temp1\testimage1.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                image.AddTrack(Image_Track.GetTimeTrack(sonogram.Duration));
                image.AddTrack(Image_Track.GetSegmentationTrack(sonogram));
                image.AddTrack(Image_Track.GetScoreTrack(scores, 0.0, 1.0, eventThreshold));
                image.AddSuperimposedMatrix(hits, maxScore);
                image.AddEvents(predictedEvents);
                image.Save(path);
            }
        }


        static void WriteEventsInfo2TextFile(List<AcousticEvent> predictedEvents, string path)
        {
            StringBuilder sb = new StringBuilder("# EVENT COUNT = " + predictedEvents.Count() + "\n");
            AcousticEvent.WriteEvents(predictedEvents, ref sb);
            sb.Append("#############################################################################");
            FileTools.Append2TextFile(path, sb.ToString());
        }


        /// <summary>
        /// JUST A TEMPORARY METHOD WHILE SETTING UP THIS CLASS
        /// These params will be used to detect a kek-kek in the output file from MFCCs 
        /// </summary>
        private static void AppendNewParams(string iniPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# duration of DCT in seconds");
            sb.AppendLine("DCT_DURATION=2.0");
            sb.AppendLine("# ignore oscillation rates below the min & above the max threshold OSCILLATIONS PER SECOND");
            sb.AppendLine("MIN_OSCIL_FREQ=4");
            sb.AppendLine("MAX_OSCIL_FREQ=6");
            sb.AppendLine("# minimum acceptable value of a DCT coefficient");
            sb.AppendLine("MIN_AMPLITUDE=0.4");
            sb.AppendLine("# Minimum duration for the length of a true event.");
            sb.AppendLine("MIN_DURATION=2.0");
            sb.AppendLine("# Maximum duration for the length of a true event.");
            sb.AppendLine("MAX_DURATION=10.0");
            sb.AppendLine("# Event threshold - use this to determin FP / FN trade-off for events.");
            sb.AppendLine("EVENT_THRESHOLD=0.20");
            sb.AppendLine("# save a sonogram for each recording that contained a hit");
            sb.AppendLine("DRAW_SONOGRAMS=2");
            FileTools.Append2TextFile(iniPath, sb.ToString());
        }


        private static void CheckArguments(string[] args)
        {
            if (args.Length < 3)
            {
                Log.WriteLine("NUMBER OF COMMAND LINE ARGUMENTS = {0}", args.Length);
                foreach (string arg in args) Log.WriteLine(arg + "  ");
                Log.WriteLine("YOU REQUIRE {0} COMMAND LINE ARGUMENTS\n", 3);
                Usage();
            }
            CheckPaths(args);
        }

        /// <summary>
        /// this method checks for the existence of the two files whose paths are expected as first two arguments of the command line.
        /// </summary>
        /// <param name="args"></param>
        private static void CheckPaths(string[] args)
        {
            if (!File.Exists(args[0]))
            {
                Console.WriteLine("Cannot find recording file <" + args[0] + ">");
                Console.WriteLine("Press <ENTER> key to exit.");
                Console.ReadLine();
                System.Environment.Exit(1);
            }
            if (!File.Exists(args[1]))
            {
                Console.WriteLine("Cannot find initialisation file: <" + args[1] + ">");
                Usage();
                Console.WriteLine("Press <ENTER> key to exit.");
                Console.ReadLine();
                System.Environment.Exit(1);
            }
        }


        private static void Usage()
        {
            Console.WriteLine("INCORRECT COMMAND LINE.");
            Console.WriteLine("USAGE:");
            Console.WriteLine("KekKek_MFCC_OD.exe recordingPath iniPath outputFileName");
            Console.WriteLine("where:");
            Console.WriteLine("recordingFileName:-(string) The path of the audio file to be processed.");
            Console.WriteLine("iniPath:-          (string) The path of the ini file containing all required parameters.");
            Console.WriteLine("outputFileName:-   (string) The name of the output file.");
            Console.WriteLine("                            By default, the output dir is that containing the ini file.");
            Console.WriteLine("");
            Console.WriteLine("\nPress <ENTER> key to exit.");
            Console.ReadLine();
            System.Environment.Exit(1);
        }

    }
}