using System;
using System.Collections.Generic;

using CSML;

namespace WordSupport
{
    public static class Utilities
    {
        public static string GetTrainingCorpusDirectory()
        {
            return GetResourcesDirectory() + "Training Corpus\\";
        }
        public static string GetResourcesDirectory()
        {
            if (AppDomain.CurrentDomain.BaseDirectory.ToLower().EndsWith("debug") ||
                AppDomain.CurrentDomain.BaseDirectory.ToLower().EndsWith("release"))
            {
                return AppDomain.CurrentDomain.BaseDirectory.ToLower() + "\\..\\..\\..\\Resources\\";
            }
            else
            {
                return AppDomain.CurrentDomain.BaseDirectory.ToLower() + "\\Resources\\";
            }
        }

        public static double CalculateTFAdjustedForDocLength(Dictionary<string, WordStats>  wordList, string targetTerm)
        { 
            double retVal = 0;

            int N = wordList[targetTerm].Count;
            int sum = 0;

            foreach (KeyValuePair<string, WordStats> kvp in wordList)
            {
                sum += kvp.Value.Count;
            }

            return retVal = (double)N / (double)sum;
        }


        public static double[,] CalcSim(double[,] MatrixTFIDF, int N)
        {
            double[,] retVal = new double[N, N];
            Matrix A;
            Matrix B;

            for (int i = 0; i < N; i++)
            {
                for (int j = i; j < N; j++)
                {
                    if (j == i)
                    { retVal[i, j] = 1; continue; }

                    string temp = "";
                    string temp2 = "";
                    for (int k = 0; k < MatrixTFIDF.GetLength(1); k++)
                    {
                        temp += MatrixTFIDF[i, k].ToString() + ",";
                        temp2 += MatrixTFIDF[j, k].ToString() + ",";
                    }
                    temp = temp.Remove(temp.Length - 1);
                    temp2 = temp2.Remove(temp2.Length - 1);
                    A = new Matrix(temp);
                    B = new Matrix(temp2);

                    retVal[i, j] = Sim(A, B);
                }
            }

            return retVal;
        }

        /// <summary>
        /// A.B / ||A|| ||B||
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static double Sim(Matrix a, Matrix b)
        {
            double retVal = 0;

            double top = Matrix.Dot(a, b);

            double Adot = Math.Sqrt(Matrix.Dot(a, a));
            double Bdot = Math.Sqrt(Matrix.Dot(b, b));

            double bot = Math.Sqrt(Matrix.Dot(a, a)) * Math.Sqrt(Matrix.Dot(b, b));

            return retVal = top / bot;
        }
    }
}