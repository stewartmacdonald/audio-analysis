﻿namespace AnalysisPrograms
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Acoustics.Shared;
    using Production;
    using log4net;
    using PowerArgs;
    using QutBioacosutics.Xie;
    using TowseyLibrary;

    public static class XiesAnalysis
    {
        [CustomDetailedDescription]
        [CustomDescription]
        public class Arguments
        {

            [ArgDescription("The source audio file to operate on")]
            [Production.ArgExistingFile()]
            public FileInfo Source { get; set; }

            [ArgDescription("The path to the config file")]
            [Production.ArgExistingFile()]
            [ArgRequired]
            public FileInfo Config { get; set; }

            public static string Description()
            {
                return "Jie Xie's workspace for his research. Mainly stuff to do with frogs.";
            }

            public static string AdditionalNotes()
            {
                return "The majority of the options for this analysis are in the config file or are build constants";
            }

        }

        private static readonly ILog Log = LogManager.GetLogger(typeof(XiesAnalysis));

        [Obsolete("See https://github.com/QutBioacoustics/audio-analysis/issues/134")]
        internal static Arguments Dev()
        {
            throw new NotImplementedException();
        }

        public static void Execute(Arguments arguments)
        {
            if (arguments == null)
            {
                arguments = Dev();
            }

            Log.Info("Xie Start");

            // load configuration
            dynamic configuration = Yaml.Deserialise(arguments.Config);

            Main.Entry(configuration, arguments.Source);
        }
    }
}
