using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using WordSupport;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            List<WordSupport.WordRelations> foundRelatedWords = new List<WordSupport.WordRelations>();

            var a = new WordSupport.WordRelations("umpire", null);
            var b = new WordSupport.WordRelations("referee", null);

            foundRelatedWords.Add(a);
            foundRelatedWords.Add(b);

            b.RelatedWords.Add(a.WordCategorizer.ToLower(), a.RelatedWords);
            a.RelatedWords.Add(b.WordCategorizer.ToLower(), b.RelatedWords);
        }

        [TestMethod]
        public void TestPdfToText()
        {
            IPdfExtraction pClass = new Pdf();

            pClass.ExtractText(Utilities.GetTrainingCorpusDirectory() + "A cell_based model of hemostasis.pdf", Utilities.GetTrainingCorpusDirectory() + @"A cell_based model of hemostasis.txt");
        }

        [TestMethod]
        public void ConvertTrainingCorpusFiles()
        {
            foreach (var pdfFIle in new System.IO.DirectoryInfo(Utilities.GetTrainingCorpusDirectory()).EnumerateFiles("*.pdf"))
            {
                IPdfExtraction pClass = new Pdf();

                if(System.IO.File.Exists(pdfFIle.Name + ".txt."))
                    System.IO.File.Delete(pdfFIle.Name + ".txt.");
                
                pClass.ExtractText(pdfFIle.FullName, Utilities.GetTrainingCorpusDirectory() + pdfFIle.Name + ".txt.");

            }
        }

        [TestMethod]
        public void TestWordExtractionFromTxt()
        {
            WordExtraction we = new WordExtraction();
            we.ExtractWords("test.txt");
        }

        [TestMethod]
        public void PopulateCommonWordsFromTxt()
        {
            Sanitizing sant = new Sanitizing();

            sant.PopulateCommonWords(Utilities.GetResourcesDirectory() + "CommonWords.txt");
        }

        [TestMethod]
        public void ExtractFromTxtPopulateCommonSanitizeCommon()
        {
            WordExtraction we = new WordExtraction();
            we.ExtractWords(Utilities.GetTrainingCorpusDirectory() + "test.txt");

            we.SetUpSantization(Utilities.GetResourcesDirectory() + "CommonWords.txt");

            we.RemoveCommonGarbageWords();
        }

        [TestMethod]
        public void TryHunspellRun()
        {
            var hRunner = new CorpusKeywordManager();
             hRunner.Run(Utilities.GetTrainingCorpusDirectory(), Utilities.GetResourcesDirectory(), AppDomain.CurrentDomain.BaseDirectory);

            //hRunner.SimilarityBetweenQueryAndDocuments(new List<string> { "azure"},)
        }
    }
}
