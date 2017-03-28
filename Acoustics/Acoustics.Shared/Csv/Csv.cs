﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Csv.cs" company="QutBioacoustics">
//   All code in this file and all associated files are the copyright and property of the QUT Ecoacoustics Research Group (formerly MQUTeR, and formerly QUT Bioacoustics Research Group).
// </copyright>
// <summary>
//   Generic methods for reading and writing Csv file.
//   *** DO NOT CHANGE THIS CLASS UNLESS INSTRUCTED TOO ***
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Acoustics.Shared.Csv
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Contracts;
    using CsvHelper;
    using CsvHelper.Configuration;
    using CsvHelper.TypeConversion;
    using Fasterflect;

    /// <summary>
    /// Generic methods for reading and writing Csv file.
    /// .
    /// *** DO NOT CHANGE THIS CLASS UNLESS INSTRUCTED TOO ***
    /// </summary>
    public static class Csv
    {
        private static readonly List<CsvClassMap> ClassMapsToRegister = new List<CsvClassMap>();

        static Csv()
        {
            RegisterAnalysisProgramsTypeConverters();

            // Find all of our custom class maps
            var type = typeof(CsvClassMap);
            var classMapTypes =
                AppDomain.CurrentDomain.GetAssemblies()
                         .Where(
                             a =>
                                 {
                                     var assemblyCompanyAttribute = (AssemblyCompanyAttribute)
                                                                    a.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false).FirstOrDefault();
                                     return assemblyCompanyAttribute != null && assemblyCompanyAttribute.Company == "QUT";
                                 })
                         .SelectMany(s => s.GetTypes())
                         .Where(type.IsAssignableFrom);

            // initialise and store
            foreach (var classMapType in classMapTypes)
            {
                var instance = classMapType.CreateInstance() as CsvClassMap;
                ClassMapsToRegister.Add(instance);
            }
        }

        public static void RegisterAnalysisProgramsTypeConverters()
        {
            // This is a manually maintained method
            TypeConverterFactory.AddConverter<ISet<Point>>(new CsvSetPointConverter());

        }

        public static CsvConfiguration DefaultConfiguration
        {
            get
            {
                // change the defaults here if you want
                var settings = new CsvConfiguration()
                                   {
                                       HasHeaderRecord = true,
                                   };
                foreach (var classMap in ClassMapsToRegister)
                {
                    settings.RegisterClassMap(classMap);
                }

                return settings;
            }
        }

        /// <summary>
        /// Serialize results to CSV - if you want the concrete type to be serialized you need to ensure it is downcast before using this method.
        /// </summary>
        /// <typeparam name="T">The type to serialize.</typeparam>
        /// <param name="destination">The file to create.</param>
        /// <param name="results"></param>
        public static void WriteToCsv<T>(FileInfo destination, IEnumerable<T> results)
        {
            Contract.Requires(destination != null);

            // using CSV Helper
            using (var stream = destination.CreateText())
            {
                var writer = new CsvWriter(stream, DefaultConfiguration);
                writer.WriteRecords(results);
            }
        }


        /// <summary>
        /// This has not been tested yet! Contact anthony if you have problems.
        /// IMPORTANT NOTE:
        /// If I get an exception, how do I tell what line the exception is on?
        /// There is a lot of information held in Exception.Data["CsvHelper"]
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static IEnumerable<T> ReadFromCsv<T>(FileInfo source, bool throwOnMissingField = true)
        {
            Contract.Requires(source != null);

            // using CSV Helper
            using (var stream = source.OpenText())
            {
                var configuration = DefaultConfiguration;
                configuration.WillThrowOnMissingField = false;
                var reader = new CsvReader(stream, configuration);

                IEnumerable<T> results = reader.GetRecords<T>();
                foreach (var result in results)
                {
                    yield return result;
                }
            }
        }

        #region matrix/readers writers

        private static void EncodeMatrixInner<T>(this CsvWriter writer, MatrixMapper<T> matrix, bool includeRowIndex)
        {
            int columns = matrix.Columns;

            // write header
            if (includeRowIndex)
            {
                writer.WriteField("Index");
            }

            for (int i = 0; i < columns; i++)
            {
                writer.WriteField("c" + i.ToString("000000"));
            }

            writer.NextRecord();

            // write rows
            foreach (var i in matrix)
            {
                writer.WriteField(i);
                for (int j = 0; j < columns; j++)
                {
                    writer.WriteField(matrix[i, j]);
                }

                writer.NextRecord();
            }
        }

        private static List<T[]> DecodeMatrix<T>(this CsvReader reader, bool includeRowIndex, out int rowCount,
            out int columnCount)
        {
            // read header
            if (!reader.Read())
            {
                rowCount = 0;
                columnCount = 0;
                return new List<T[]>();
            }

            var headers = reader.FieldHeaders;
            if (includeRowIndex && headers[0] != "Index")
            {
                throw new CsvHelperException("Expected an index header and there was none");
            }
            if (!includeRowIndex && headers[0] == "Index")
            {
                throw new CsvHelperException("Did not expect an index header and there was one");
            }

            var rowIndex = includeRowIndex ? 1 : 0;
            columnCount = headers.Length - rowIndex;
            var csvRows = new List<T[]>(1440);

            rowCount = 0;
            do
            {
                var row = new T[columnCount];

                for (int i = rowIndex; i <= columnCount; i++)
                {
                    row[i - rowIndex] = reader.GetField<T>(i);
                }

                csvRows.Add(row);
                rowCount++;
            }
            while (reader.Read());

            return csvRows;
        }

        private static T[,] DecodeMatrix<T>(this CsvReader reader, TwoDimensionalArray dimensionality,
            bool includeRowIndex)
        {
            int rowCount;
            int columnCount;
            var csvRows = DecodeMatrix<T>(reader, includeRowIndex, out rowCount, out columnCount);

            var result = dimensionality == TwoDimensionalArray.RowMajor
                ? new T[rowCount, columnCount]
                : new T[columnCount, rowCount];

            for (int i = 0; i < csvRows.Count; i++)
            {
                var row = csvRows[i];
                for (int j = 0; j < row.Length; j++)
                {
                    switch (dimensionality)
                    {
                        case TwoDimensionalArray.RowMajor:
                            result[i, j] = row[j];
                            break;
                        case TwoDimensionalArray.ColumnMajor:
                            result[j, i] = row[j];
                            break;
                        case TwoDimensionalArray.ColumnMajorFlipped:
                            result[columnCount - 1 - j, i] = row[j];
                            break;
                        default:
                            throw new NotImplementedException("Other dimensionalities not implemented");
                    }
                }
            }
            return result;
        }


        public static void WriteMatrixToCsv<T>(FileInfo destination, T[,] matrix, TwoDimensionalArray dimensionality = TwoDimensionalArray.RowMajor)
        {
            Contract.Requires(destination != null);

            // not tested!
            using (var stream = destination.CreateText())
            {
                var writer = new CsvWriter(stream, DefaultConfiguration);

                var transformedMatrix = new TwoDimArrayMapper<T>(matrix, dimensionality);

                EncodeMatrixInner(writer, transformedMatrix, true);
            }
        }

        public static T[,] ReadMatrixFromCsv<T>(FileInfo source, TwoDimensionalArray dimensionality = TwoDimensionalArray.RowMajor)
        {
            Contract.Requires(source != null);

            // not tested!
            using (var stream = source.OpenText())
            {
                var reader = new CsvReader(stream, DefaultConfiguration);

                return reader.DecodeMatrix<T>(dimensionality, true);
            }
        }

        public static void WriteMatrixToCsv<T>(FileInfo destination, IEnumerable<T[]> matrix)
        {
            Contract.Requires(destination != null);

            // not tested!
            using (var stream = destination.CreateText())
            {
                var writer = new CsvWriter(stream, DefaultConfiguration);

                var transformedMatrix = new EnumerableMapper<T>(matrix);

                EncodeMatrixInner(writer, transformedMatrix, true);
            }
        }

        public static IEnumerable<T[]> ReadMatrixFromCsv<T>(FileInfo source)
        {
            Contract.Requires(source != null);

            // not tested!
            List<T[]> matrix;
            using (var stream = new StreamReader(source.FullName))
            {
                var reader = new CsvReader(stream, DefaultConfiguration);

                int rowCount;
                int columnCount;
                matrix = reader.DecodeMatrix<T>(true, out rowCount, out columnCount);
            }

            return matrix;
        }

        public static void WriteMatrixToCsv<T, U>(FileInfo destination, IEnumerable<U> matrix, Func<U, T[]> selector)
        {
            Contract.Requires(destination != null);

            using (var stream = destination.CreateText())
            {
                var writer = new CsvWriter(stream, DefaultConfiguration);

                var transformedMatrix = new ObjectArrayMapper<U, T>(matrix, selector);

                EncodeMatrixInner(writer, transformedMatrix, true);
            }
        }

        #endregion
    }
}