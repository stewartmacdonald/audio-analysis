// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RecognizerResults.cs" company="QutBioacoustics">
//   All code in this file and all associated files are the copyright of the QUT Bioacoustics Research Group (formally MQUTeR).
// </copyright>
// <summary>
//   Defines the RecognizerResults type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace AnalysisPrograms.Recognizers.Base
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;

    using AnalysisBase.ResultBases;

    using AudioAnalysisTools;

    using TowseyLibrary;

    public class RecognizerResults
    {
        private List<Plot> plots;

        #region Public Properties

        public RecognizerResults()
        {
            this.Plots = new List<Plot>();
        }

        public List<AcousticEvent> Events { get; set; }

        public double[,] Hits { get; set; }

        public BaseSonogram Sonogram { get; set; }

        /// <summary>
        /// Currently used to return a score track image that can be appended to a **high resolution indices image**.
        /// </summary>
        public Image ScoreTrack { get; set; }

        /// <summary>
        /// Gets or sets a list of plots.
        /// Used by the multi recognizer
        /// </summary>
        public List<Plot> Plots
        {
            get
            {
                return this.plots;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value), "Cannot be set to null");
                }

                this.plots = value;
            }
        }

        #endregion
    }
}