﻿// <copyright file="Cluster.cs" company="QutEcoacoustics">
// All code in this file and all associated files are the copyright and property of the QUT Ecoacoustics Research Group (formerly MQUTeR, and formerly QUT Bioacoustics Research Group).
// </copyright>

namespace NeuralNets
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using TowseyLibrary;

    public class Cluster
    {
        public int Size
        {
            get { return this.Vectors.Count; }
        }

        public List<double[]> Vectors { get; private set; } //members  of the cluster

        public double[] Centroid { get; private set; } //centroid of the cluster

        /// <summary>
        /// CONSTRUCTOR
        /// Init with list of vectors
        /// </summary>
        /// <param name="list"></param>
        public Cluster(List<double[]> list)
        {
            this.Vectors = list;
        }

        /// <summary>
        /// CONSTRUCTOR
        /// Init with centroid
        /// </summary>
        /// <param name="list"></param>
        public Cluster(double[] vector)
        {
            this.Centroid = vector;
            this.Vectors = new List<double[]>();
        }

        public double[] CalculateCentroid()
        {
            int vCount = this.Vectors.Count;
            if (vCount == 0)
            {
                return null;

                //throw new Exception("Cluster.Centroid(): count = " + vCount);
            }

            int featureCount = this.Vectors[0].Length;

            //accumulate the vectors into an averaged feature vector
            double[] avVector = new double[featureCount];
            for (int i = 0; i < vCount; i++)
            {
                for (int j = 0; j < featureCount; j++)
                {
                    avVector[j] += this.Vectors[i][j]; //sum the Vectors
                }
            }

            for (int i = 0; i < featureCount; i++)
            {
                avVector[i] = avVector[i] / vCount; //average Vectors
            }

            this.Centroid = avVector;

            return avVector;
        }

        public void SetCentroid(double[] vector)
        {
            this.Centroid = new double[vector.Length];
            for (int i = 0; i < vector.Length; i++)
            {
                this.Centroid[i] = vector[i];
            }
        }

        public void ResetMembers()
        {
            this.Vectors = new List<double[]>();
        }

        /// <summary>
        /// calculate euclidian distance of vector from centroid
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public double DistanceFromCentroid( double[] vector)
        {
            double dist = 0.0;
            for (int i = 0; i < vector.Length; i++)
            {
                dist += Math.Pow(vector[i] - this.Centroid[i], 2);
            }

            return Math.Sqrt(dist);
        }
    } //end class Cluster
}
