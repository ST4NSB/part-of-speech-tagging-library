﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NLP
{
    public class TextNormalization
    {
        public static double MinMaxNormalization(double value, double newMax, double newMin, double oldMax = 1.0d, double oldMin = 0.0d)
        {
            return (double)(((value - oldMin) * (newMax - newMin)) / (oldMax - oldMin)) + newMin;
        }

        public static List<Tokenizer.WordTag> CleanDataPipeline(List<Tokenizer.WordTag> words, bool toLowerTxt = true)
        {
            List<Tokenizer.WordTag> newWords = new List<Tokenizer.WordTag>();
            foreach (var sw in words)
            {
                string tsw = EliminateDigitsFromWord(sw.word);
                if (!string.IsNullOrEmpty(tsw))
                {
                    if (toLowerTxt)
                        tsw = ToLowerCaseNormalization(tsw);
                    //tsw = EliminateApostrophe(tsw);
                    newWords.Add(new Tokenizer.WordTag(tsw, sw.tag));
                }
            }
            return newWords;
        }

        private static bool IsStopWord(string word)
        {
            string[] stopWords = { "``", "\"", "\'", "''", "(", ")", "[", "]", "{", "}" };
            foreach (var sword in stopWords)
                if (word.Equals(sword))
                    return true;
            return false;
        }

        private static string EliminateDotFromWord(string word)
        {
            string newstring = word.Replace(".", string.Empty);
            return newstring;
        }

        private static string EliminateDigitsFromWord(string word)
        {
            if (!word.Any(char.IsDigit))
                return word;
            else
            {
                string output = Regex.Replace(word, @"[\d-]", string.Empty);
                //int count = Regex.Matches(output, @"[a-zA-z]").Count;
                var count = output.Count(char.IsLetter);
                if (count >= 3) // verifies if has at least 3 letters left
                    return output;
                return string.Empty;
            }
        }

        private static string ToLowerCaseNormalization(string word)
        {
            return word.ToLower();
        }

        private static string EliminateApostrophe(string word)
        {
            var output = word.Replace("\'", string.Empty);
            return output;
        }
    }
}
