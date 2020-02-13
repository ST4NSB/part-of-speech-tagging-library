﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NLP;

namespace PostAppConsole
{
    class Program
    {
        static string LoadAndReadFolderFiles(string folderName)
        {
            string path = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + "\\" + folderName;
            Console.WriteLine("Read File Path: [" + path + "]");
            string text = FileReader.GetAllTextFromDirectoryAsString(path);
            return text;
        }

        static void WriteToTxtFile(string folderName, string fileName, string jsonFile)
        {
            string path = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + "\\" + folderName + "\\" + fileName;
            Console.WriteLine("Write File Path: [" + path + "]");
            if (!File.Exists(path))
            {
                using (StreamWriter sw = File.CreateText(path))
                {
                    sw.Write(jsonFile);
                    sw.Dispose();
                }
            }
            else Console.WriteLine("Couldn't write to file (File already exists)!");
        }

        static void Main(string[] args)
        {
            const string BrownfolderTrain = "Brown Corpus\\1_Train", BrownfolderTest = "Brown Corpus\\2_Test",
                demoFileTrain = "demo files\\train", demoFileTest = "demo files\\test";

            var text = LoadAndReadFolderFiles(BrownfolderTrain);
            var oldWords = Tokenizer.SeparateTagFromWord(Tokenizer.WordTokenizeCorpus(text));

            var words = SpeechPart.GetNewHierarchicTags(oldWords);
            words = TextNormalization.Pipeline(words);


            Console.WriteLine("Done with loading and creating tokens!");
            HMMTagger tagger = new HMMTagger();
            tagger.TrainModel(words);
            Console.WriteLine("Done with training MODEL!");

            //foreach (var model in tagger.EmissionFreq)
            //{
            //    Console.WriteLine(model.Word);
            //    foreach (var item in model.TagFreq)
            //    {
            //        Console.WriteLine("     " + item.Key + " -> " + item.Value);
            //    }
            //}
            //foreach (var item in tagger.UnigramFreq)
            //    Console.WriteLine(item.Key + " -> " + item.Value);
            //foreach (var item in tagger.BigramTransition)
            //    Console.WriteLine(item.Key + " -> " + item.Value);
            //foreach (var item in tagger.TrigramTransition)
            //    Console.WriteLine(item.Key + " -> " + item.Value);

            Console.WriteLine("Duration of training model: " + tagger.GetTrainingTimeMs() + " ms!");
            //// WriteToTxtFile("Trained Files", "SVM_trained_file.json", JsonConvert.SerializeObject(tagger.EmissionFreq));

            Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            var textTest = LoadAndReadFolderFiles(BrownfolderTest);
            var oldWordsTest = Tokenizer.SeparateTagFromWord(Tokenizer.WordTokenizeCorpus(textTest));
            var wordsTest = SpeechPart.GetNewHierarchicTags(oldWordsTest);
            wordsTest = TextNormalization.Pipeline(wordsTest);

            wordsTest = tagger.EliminateDuplicateSequenceOfEndOfSentenceTags(wordsTest);
            tagger.CalculateProbabilitiesForTestFiles(wordsTest, model: "trigram");
            Decoder decoder = new Decoder(tagger.EmissionProbabilities, tagger.UnigramProbabilities, tagger.BigramTransitionProbabilities, tagger.TrigramTransitionProbabilities);
            Console.WriteLine("\nInterpolation: " + tagger.DeletedInterpolation());

            decoder.SetLambdaValues(tagger.DeletedInterpolation());

            decoder.ViterbiDecoding(wordsTest, model: "trigram", mode: "backward");
            tagger.EliminateAllEndOfSentenceTags(wordsTest);

            //decoder = new Decoder();
            //const string deftag = "NULL";
            //decoder.PredictedTags = new List<string>();
            //foreach (var tw in wordsTest)
            //{
            //    var modelMax = tagger.EmissionFreq.Find(x => x.Word == tw.word);
            //    if (modelMax != null)
            //    {
            //        string maxTag = modelMax.TagFreq.OrderByDescending(x => x.Value).FirstOrDefault().Key;
            //        if (maxTag != ".")
            //            decoder.PredictedTags.Add(maxTag);
            //        else decoder.PredictedTags.Add(deftag);
            //    }
            //    else decoder.PredictedTags.Add(deftag); // NULL / NN
            //}

            //foreach (var item in decoder.EmissionProbabilities)
            //{
            //    Console.WriteLine(item.Word);
            //    foreach (var item2 in item.TagFreq)
            //        Console.WriteLine("\t" + item2.Key + " -> " + item2.Value);
            //}
            //foreach (var item in decoder.UnigramProbabilities)
            //    Console.WriteLine("UNI: " + item.Key + "->" + item.Value);
            //foreach (var item in decoder.BigramTransitionProbabilities)
            //    Console.WriteLine("BI: " + item.Key + " -> " + item.Value);
            //foreach (var item in decoder.TrigramTransitionProbabilities)
            //    Console.WriteLine("TRI: " + item.Key + " -> " + item.Value);

            //foreach (var item in decoder.PredictedTags)
            //    Console.Write(item + " ");

            Console.WriteLine("\nDuration of Viterbi Decoding: " + decoder.GetViterbiDecodingTime() + " ms!\n");

            Console.WriteLine("testwords: " + wordsTest.Count + " , predwords: " + decoder.PredictedTags.Count);
            Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");

            Evaluation eval = new Evaluation();
            eval.CreateSupervizedEvaluationsMatrix(wordsTest, decoder.PredictedTags, fbeta: 1);
            Console.WriteLine("TAG\t\tACCURACY\t\tPRECISION\t\tRECALL\t\t\tF1-SCORE");
            var fullMatrix = eval.GetFullClassificationMatrix();
            for (int i = 0; i < eval.GetFullMatrixLineLength(); i++)
            {
                for (int j = 0; j < eval.GetFullMatrixColLength(); j++)
                    Console.Write(fullMatrix[i][j] + "\t\t");
                Console.WriteLine();
            }

            Console.WriteLine("\nAccuracy: " + eval.GetSimpleAccuracy(wordsTest, decoder.PredictedTags));

            Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");



            ////using (System.IO.StreamWriter file = new System.IO.StreamWriter("bigram_for_back.csv"))
            ////{
            ////    file.WriteLine("Word,Real Tag,Prediction Tag");
            ////    for (int i = 0; i < wordsTest.Count; i++) 
            ////    {
            ////        file.WriteLine("\"" + wordsTest[i].word + "\"," + wordsTest[i].tag + "," + decoder.PredictedTags[i]);
            ////    }
            ////}

        }
    }
}
