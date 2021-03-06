﻿// <copyright file="Statistics.cs" company="QutEcoacoustics">
// All code in this file and all associated files are the copyright and property of the QUT Ecoacoustics Research Group (formerly MQUTeR, and formerly QUT Bioacoustics Research Group).
// </copyright>

namespace TowseyLibrary
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// consists of a set of static methods to do elementary statistics
    /// NOTE: Much of stats to do with a normal distribution is in the NormDist CLASS.
    /// </summary>
    public class Statistics
    {
        public static double GetMedian(double[] v)
        {
            Tuple<int[], double[]> tuple = DataTools.SortArray(v);
            double median = tuple.Item2[v.Length / 2];
            return median;
        }

        /// <summary>
        /// Analyses an array of events or hits, represented by a binary of matrix.
        /// Assumes a Poisson distribution
        /// Returns an array of Z-scores indicating the probability at any time or frame that the number of hits occuring
        /// in the window centred on that point could have occured by chance.
        /// </summary>
        /// <param name="hits"></param>
        /// <param name="window"></param>
        /// <param name="thresholdZ"></param>
        /// <param name="thresholdCount"></param>
        /// <returns></returns>
        public static void AnalyseClustersOfHits(int[] hits, int window, double thresholdZ, int thresholdCount,
                                                out double[] zScores, out double expectedHits, out double sd)
        {
            int frameCount = hits.Length;
            int hitCount = DataTools.CountPositives(hits);

            expectedHits = (double)hitCount * window / frameCount;
            sd = Math.Sqrt(expectedHits); //assume Poisson Distribution

            //LoggedConsole.WriteLine("hitCount="+hitCount+"  expectedHits = " + expectedHits + "+/-" + sd+"  thresholdSum="+thresholdSum);
            int offset = (int)(window * 0.5); //assign score to position in window
            int sum = 0;
            for (int i = 0; i < window; i++)
            {
                if (hits[i] > 0)
                {
                    sum++;  //set up the song window
                }
            }

            //now calculate z-scores for the number of syllable hits in a window
            zScores = new double[frameCount];
            for (int i = window; i < frameCount; i++)
            {
                if (sum < thresholdCount)
                {
                    zScores[i - offset] = -10.0;  //not enough hits to constitute a cluster - set ascore to neg value
                }
                else
                {
                    zScores[i - offset] = (sum - expectedHits) / sd;
                }

                sum = sum - hits[i - window] + hits[i]; //move the songwindow
            }
        }

        public static string tStatisticAndSignificance(double m1, double sd1, int count1,
                                        double m2, double sd2, int count2)
        {
            double t = tStatistic(m1, sd1, count1, m2, sd2, count2);
            int df = count1 + count2 - 2;
            double p = tStatisticAlpha(Math.Abs(t), df);
            StringBuilder sb = new StringBuilder("t=" + t.ToString("F3"));
            sb.Append(" p=" + p.ToString("F4"));
            if (p == 0.050)
            {
                sb.Append("*");
            }

            if (p == 0.025)
            {
                sb.Append("*");
            }

            if (p == 0.010)
            {
                sb.Append("**");
            }

            if (p == 0.005)
            {
                sb.Append("**");
            }

            if (p <= 0.001)
            {
                sb.Append("***");
            }

            return sb.ToString();
        }

        /**
         * Calculates the t-statistic.
         * t(df) = (m1-m2)/SE
         * where SE^2 = s^2(m+n)/(mn)
         * where s^2 = estimated variance = ((ColumnLeft-1)v1 + (ColumnRight-1)v2)/(ColumnLeft+ColumnRight-2)
         * where v1 = sd1^2 and v2 =  sd2^2
         * To calculate ASSUME df = ColumnLeft+ColumnRight-2 = infinity
         * @param m1
         * @param sd1
         * @param count1
         * @param m2
         * @param sd2
         * @param count2
         * @return
         */
        public static double tStatistic(double m1, double sd1, int count1,
                                        double m2, double sd2, int count2)
        {
            double v1 = sd1 * sd1;
            double v2 = sd2 * sd2;
            int df = count1 + count2 - 2;
            double v = (((count1 - 1) * v1) + ((count2 - 1) * v2)) / df;
            double SEsquared = v * (count1 + count2) / (count1 * count2);
            double t = (m1 - m2) / Math.Sqrt(SEsquared);
            return t;
        }

        public static double tStatisticAlpha(double t, int df)
        {
            double[] table_df_inf = { 0.25, 0.51, 0.67, 0.85, 1.05, 1.282, 1.645, 1.96, 2.326, 2.576, 3.09, 3.291 };
            double[] table_df_15 = { 0.26, 0.53, 0.69, 0.87, 1.07, 1.341, 1.753, 2.13, 2.602, 2.947, 3.73, 4.073 };
            double[] alpha = { 0.40, 0.30, 0.25, 0.20, 0.15, 0.10, 0.05, 0.025, 0.01, 0.005, 0.001, 0.0005 };
            double[] tTable = table_df_inf;
            if (df <= 15)
            {
                tTable = table_df_15;
            }

            double p = 0.5;
            int size = alpha.Length - 1;
            double t2 = 4.0;
            double p2 = 0.0001;

            // first check if t exceeds the max in table
            if (t > tTable[size])
            {
                return alpha[size];
            }

            for (int i = size; i >= 0; i--)
            {
                if (t > tTable[i])
                {
                    double t1 = tTable[i];
                    if (i < size)
                    {
                        t2 = tTable[i + 1];
                    }

                    double p1 = alpha[i];
                    if (i < size)
                    {
                        p2 = alpha[i + 1];
                    }

                    double slope = (p2 - p1) / (t2 - t1);
                    p = (slope * (t - t1)) + p1;
                    break;
                }
            }

            return p;
        }

        public static double[] bayesBoundary(int countC1, double meanC1, double sdC1,
                               int countC2, double meanC2, double sdC2)
        {
            double lnRatio = Math.Log(countC1 / (double)countC2);
            double sqrMean1 = meanC1 * meanC1;
            double sqrMean2 = meanC2 * meanC2;
            double sqrSD1 = sdC1 * sdC1;
            double sqrSD2 = sdC2 * sdC2;

            double A = ((1 / sqrSD2) - (1 / sqrSD1)) / 2;
            double B = (meanC2 / sqrSD2) - (meanC1 / sqrSD1);
            B = -B;
            double C = Math.Log(sdC2 / sdC1) + lnRatio;
            C = C + (sqrMean2 / 2 / sqrSD2) - (sqrMean1 / 2 / sqrSD1);
            double[] ob = quadraticRoots(A, B, C);
            return ob;
        }

        public static double[] quadraticRoots(double A, double B, double C)
        {
            int signB = 1;
            if (B < 0.0)
            {
                signB = -1;
            }

            double sqrt = Math.Sqrt((B * B) - (4 * A * C));
            double Q = -0.5 * (B + (signB * sqrt));

            double[] roots = new double[2];
            roots[0] = Q / A;
            roots[1] = C / Q;
            return roots;
        }

        public static double[] CreateInverseProbabilityDistribution(int length)
        {
            double[] distribution = new double[length];
            for (int i = 0; i < length; i++)
            {
                distribution[i] = 1 / (double)(i + 1);
            }

            //for (int i = 0; i < length; i++) distribution[i] = 1 / (double)((i + 1) * (i + 1));
            //double sum = 0;
            //for (int i = 0; i < length; i++) sum += distribution[i];
            //Console.WriteLine("pre-sum = {0:f3}", sum);
            distribution = DataTools.Normalise2Probabilites(distribution);
            return distribution;
        }

        public static double[] CreateQuadraticProbabilityDistribution(int length)
        {
            double[] distribution = new double[length];
            for (int i = 0; i < length; i++)
            {
                distribution[i] = i * i;
            }

            //double sum = 0;
            //for (int i = 0; i < length; i++) sum += distribution[i];
            //Console.WriteLine("pre-sum = {0:f3}", sum);
            distribution = DataTools.Normalise2Probabilites(distribution);
            distribution = DataTools.reverseArray(distribution);
            return distribution;
        }

        public static Tuple<int[], int[]> RandomSamplingUsingProbabilityDistribution(int distributionlength, int sampleCount, int seed)
        {
            double[] distribution = CreateInverseProbabilityDistribution(distributionlength);

            //double[] distribution = Statistics.CreateQuadraticProbabilityDistribution(distributionlength);
            // double sum = distribution.Sum();
            // Console.WriteLine("post-sum = {0:f3}", sum);
            distribution = DataTools.ConvertProbabilityDistribution2CummulativeProbabilites(distribution);

            // sum = distribution.Sum();
            // Console.WriteLine("cumm-sum = {0:f3}", sum);
            // Console.WriteLine("cumm-sum = {0:f3}", distribution[length-1]);

            // double refValue = 0.65;
            // int lowerIndex = 0;
            // int upperIndex = 99;
            // int location = DataTools.WhichSideOfCentre(distribution, refValue, lowerIndex, upperIndex);
            // Console.WriteLine("location = " + location);

            int[] samples = DataTools.SampleArrayRandomlyWithoutReplacementUsingProbabilityDistribution(distribution, sampleCount, seed);

            //for (int i = 0; i < sampleCount; i++) Console.WriteLine("s"+i+ "    " + samples[i]);
            Tuple<int[], int[]> tuple = DataTools.SortArray(samples);
            int[] sortedSamples = tuple.Item2;
            return Tuple.Create(samples, sortedSamples);
        }

        public static void main(string[] args)
        {
            /*
                int   countC1  = 4000000;
                    double meanC1  = -8.7;
                    double sdC1    =  4.6;
                    int    countC2 = 4000;
                    double meanC2  =  8.0;
                    double sdC2    =  2.7;
                double bb[] = bayesBoundary(countC1, meanC1, sdC1, countC2, meanC2, sdC2);
                LoggedConsole.WriteLine("boundary 1="+bb[0]+"  boundary 2="+bb[1]);
            */

            /*
                double[] roots = NormalDist.quadraticRoots(6, -13, 6);
                    LoggedConsole.WriteLine("root1="+roots[0]+"  root2="+roots[1]);
            */

            //calculating the t-statistic.
            double av1 = 16.78;
            double sd1 = 0.6788;
            int count1 = 50;
            double av2 = 17.00;
            double sd2 = 0.782;
            int count2 = 50;
            double t = tStatistic(av1, sd1, count1, av2, sd2, count2);
            LoggedConsole.WriteLine("t=" + t);
            int df = count1 + count2 - 2;
            LoggedConsole.WriteLine("alpha=" + tStatisticAlpha(Math.Abs(t), df));

            //calculate a lot of t-statistics taken from two files.
            /*    String dir = "D:\\Bioinformatics\\Data\\Chlamydia_trachomatis\\TSSpredictionsSarahEML2\\";
            //    String f1  = dir+"earlyGenesPostVirgilTStats.txt";
                String f1  = dir+"middleGenesPostVirgilTStats.txt";
                String f2  = dir+"allGenesPostVirgilTStats.txt";
                String op  = dir+"earlyGenesPostVirgilTStatsOutput.txt";
                int count1 = 28;
                int count2 = 798;
                Vector v1 = FileUtilities.ReadFile2Vector(f1);
                Vector v2 = FileUtilities.ReadFile2Vector(f2);
                BufferedWriter bw = FileUtilities.getBufferedWriter(op);
                for(int i=0;i<1000;i++)
                { String line1av = (String)v1.get(i);
                  String line2av = (String)v2.get(i);
                  if(line1av.startsWith("#")) continue;
                  if(line2av.startsWith("#")) continue;
                  i++;
                  String line1sd = (String)v1.get(i);
                  String line2sd = (String)v2.get(i);

                  //get the values
                  String[] av1array = line1av.split(" +");
                  String[] av2array = line2av.split(" +");
                  String[] sd1array = line1sd.split(" +");
                  String[] sd2array = line2sd.split(" +");

                  String title = av1array[0];
                  LoggedConsole.WriteLine(line1av);
                  LoggedConsole.WriteLine(line2av);
                  for(int n=1;n<av1array.mapLength;n++)
                  {
                    double m1  = Double.parseDouble(av1array[n]);
                    double sd1 = Double.parseDouble(sd1array[n]);
                    double m2  = Double.parseDouble(av2array[n]);
                    double sd2 = Double.parseDouble(sd2array[n]);
                    LoggedConsole.WriteLine(tStatistic(m1,sd1,count1,m2,sd2,count2));
                    try{
                      bw.write(title+" "+m1+" "+m2+" "+tStatistic(m1,sd1,count1,m2,sd2,count2));
                    } catch(Exception e)
                    {
                      LoggedConsole.WriteLine(e);
                    }
                  }

                  if((i+1)>=v1.size()) break;
                }
            */

            /*
                int   countC1  = 4000000;
                    double meanC1  = -8.7;
                    double sdC1    =  4.6;
                    int    countC2 = 4000;
                    double meanC2  =  8.0;
                    double sdC2    =  2.7;
                double bb[] = bayesBoundary(countC1, meanC1, sdC1, countC2, meanC2, sdC2);
                LoggedConsole.WriteLine("boundary 1="+bb[0]+"  boundary 2="+bb[1]);
            */

            LoggedConsole.WriteLine("FINISHED");
        }//end MAIN()
    }//end class
}
