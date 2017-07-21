using System;
using System.Collections.Generic; 

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
    }
}