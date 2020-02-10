﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NLP
{
    public class HMMTagger
    {
        public List<EmissionModel> EmissionFreq;
        public Dictionary<string, int> UnigramFreq = new Dictionary<string, int>();
        public Dictionary<Tuple<string, string>, int> BigramTransition;
        public Dictionary<Tuple<string, string, string>, int> TrigramTransition;

        public List<EmissionProbabilisticModel> EmissionProbabilities;
        public Dictionary<Tuple<string, string>, double> BigramTransitionProbabilities;

        private Stopwatch TrainingTime;

        public HMMTagger() { }

        public HMMTagger(
            List<EmissionModel> EmissionFreq,
            Dictionary<string, int> UnigramFreq,
            Dictionary<Tuple<string, string>, int> BigramTransition,
            Dictionary<Tuple<string, string, string>, int> TrigramTransition)
        {
            this.EmissionFreq = EmissionFreq;
            this.UnigramFreq = UnigramFreq;
            this.BigramTransition = BigramTransition;
            this.TrigramTransition = TrigramTransition;
        }

        /// <summary>
        /// The Model struct definition (Word - Dic[Tag, Tag_Frequency]), eg. (The, [at, 1]) 
        /// </summary>
        public class EmissionModel
        {
            public string Word;
            public Dictionary<string, int> TagFreq;
            public EmissionModel()
            {
                this.TagFreq = new Dictionary<string, int>();
            }
        }

        public class EmissionProbabilisticModel
        {
            public string Word;
            public Dictionary<string, double> TagFreq;
            public EmissionProbabilisticModel()
            {
                this.TagFreq = new Dictionary<string, double>();
            }
        }

        /// <summary>
        /// Function that creates the Emission & Transition Matrix
        /// </summary>
        /// <param name="wordsInput">List of words - tag, eg. The - at)</param>
        public void TrainModel(List<Tokenizer.WordTag> wordsInput, string model = "bigram")
        {
            this.TrainingTime = new Stopwatch();
            this.TrainingTime.Start();

            this.CalculateEmissionAndTransitionOccurrences(wordsInput);
            this.CalculateBigramOccurences(wordsInput); // calculated automatically
            if (model.Equals("trigram"))
                this.CalculateTrigramOccurences(wordsInput);

            this.TrainingTime.Stop();
        }

        public void CalculateProbabilitiesForTestFiles(List<Tokenizer.WordTag> testWords, string model = "bigram")
        {
            this.EmissionProbabilities = new List<EmissionProbabilisticModel>();
            this.BigramTransitionProbabilities = new Dictionary<Tuple<string, string>, double>();

            // emission stage
            foreach (var tw in testWords)
            {
                HMMTagger.EmissionModel wmFind = EmissionFreq.Find(x => x.Word == tw.word);
                EmissionProbabilisticModel wFind = EmissionProbabilities.Find(x => x.Word == tw.word);
                if (wmFind != null && wFind == null)
                {
                    EmissionProbabilisticModel epModel = new EmissionProbabilisticModel();
                    epModel.Word = wmFind.Word;
                    foreach (var tf in wmFind.TagFreq)
                    {
                        int cti = this.UnigramFreq.FirstOrDefault(x => x.Key == tf.Key).Value;
                        float pwiti = (float)tf.Value / cti; // Emission probability: p(wi/ti) = C(ti, wi) / C(ti)
                        epModel.TagFreq.Add(tf.Key, pwiti);
                    }
                    this.EmissionProbabilities.Add(epModel);
                }
            }

            // transition stage
            foreach (var tuple in this.BigramTransition)
            {
                var cti = this.UnigramFreq.FirstOrDefault(x => x.Key.Equals(tuple.Key.Item1)).Value;
                float pti = (float)tuple.Value / cti; // Transition probability: p(ti|ti-1) = C(ti-1, ti) / C(ti-1)
                this.BigramTransitionProbabilities.Add(tuple.Key, pti);

            }

            if (model.Equals("trigram"))
            {
                // TODO: add condition later for tri-gram
            }
        }


        private void CalculateEmissionAndTransitionOccurrences(List<Tokenizer.WordTag> wordsInput)
        {
            this.EmissionFreq = new List<EmissionModel>();
            foreach (var w in wordsInput)
            {
                EmissionModel wmFind = EmissionFreq.Find(x => x.Word == w.word);
                if (wmFind == null)
                {
                    EmissionModel wModel = new EmissionModel();
                    wModel.Word = w.word;
                    wModel.TagFreq.Add(w.tag, 1);
                    this.AddTagToUnigramOccurences(w.tag);
                    this.EmissionFreq.Add(wModel);
                }
                else
                {
                    var tag = wmFind.TagFreq.FirstOrDefault(x => x.Key == w.tag);
                    if (tag.Key == null)
                    {
                        this.AddTagToUnigramOccurences(w.tag);
                        wmFind.TagFreq.Add(w.tag, 1);
                    }
                    else
                    {
                        this.AddTagToUnigramOccurences(w.tag);
                        wmFind.TagFreq[tag.Key] += 1;
                    }
                }
            }
        }

        private void AddTagToUnigramOccurences(string wordTag)
        {
            var tag = this.UnigramFreq.FirstOrDefault(x => x.Key == wordTag);
            if (tag.Key == null)
            {
                this.UnigramFreq.Add(wordTag, 1);
            }
            else
            {
                this.UnigramFreq[tag.Key] += 1;
            }
        }

        private void CalculateBigramOccurences(List<Tokenizer.WordTag> wordsInput)
        {
            this.BigramTransition = new Dictionary<Tuple<string, string>, int>();
            bool firstFileChecked = false;
            for (int i = -1; i < wordsInput.Count - 1; i++)
            {
                if (!firstFileChecked)
                {
                    this.BigramTransition.Add(new Tuple<string, string>(".", wordsInput[i + 1].tag), 1);
                    firstFileChecked = true;
                    continue;
                }

                var tuple = new Tuple<string, string>(wordsInput[i].tag, wordsInput[i + 1].tag);
                var tag = this.BigramTransition.FirstOrDefault(x => x.Key.Equals(tuple));
                if (tag.Key == null)
                {
                    this.BigramTransition.Add(tuple, 1);
                }
                else
                {
                    this.BigramTransition[tag.Key] += 1;
                }
            }
        }

        private void CalculateTrigramOccurences(List<Tokenizer.WordTag> wordsInput)
        {
            this.TrigramTransition = new Dictionary<Tuple<string, string, string>, int>();
            bool firstFileChecked = false;
            for (int i = -1; i < wordsInput.Count - 2; i++)
            {
                if (!firstFileChecked)
                {
                    this.TrigramTransition.Add(new Tuple<string, string, string>(".", wordsInput[i + 1].tag, wordsInput[i + 2].tag), 1);
                    firstFileChecked = true;
                    continue;
                }

                var tuple = new Tuple<string, string, string>(wordsInput[i].tag, wordsInput[i + 1].tag, wordsInput[i + 2].tag);

                if (tuple.Item2.Equals("."))
                    continue;
                
                var tag = this.TrigramTransition.FirstOrDefault(x => x.Key.Equals(tuple));
                if (tag.Key == null)
                {
                    this.TrigramTransition.Add(tuple, 1);
                }
                else
                {
                    this.TrigramTransition[tag.Key] += 1;
                }
            }
        }

        /// <summary>
        /// Method that returns the elapsed time of trained model(Emission + Transition) (ms)
        /// </summary>
        public long GetTrainingTimeMs()
        {
            return this.TrainingTime.ElapsedMilliseconds;
        }

    }
}