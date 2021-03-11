using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Zotero.Test.NetCore
{
    public class AbstractStyler
    {
        public static string StyleAbstract(string originAbs)
        {
            if (originAbs == null) return null;
            var punctuation = originAbs.Where(char.IsPunctuation).Distinct().ToArray();
            var wordsStats = originAbs.Split()
                .Select(_ => _.Trim(punctuation))
                .Select(_ => _.ToLower())
                .Where(IsLexicalWord)
                .GroupBy(_ => _)
                .Select(_ => new { word = _.Key, num = _.Count() })
                .Where(_ => _.num > 1);
            var colors = GetColors(wordsStats.Count()).Select(ToHex).ToArray();
            var wordStyle = wordsStats
                .OrderByDescending(_ => _.num)
                .Select((_, i) => new { _.word, color = colors[i] })
                .ToArray();

            var words = originAbs.Split();
            var wordList = new List<string>();
            foreach (var word in words)
            {
                var pureWord = word.Trim(punctuation);//.ToLower();
                var lowWord = pureWord.ToLower();
                if (IsNonLexicalWord(lowWord))
                {
                    wordList.Add($"<font color=\"#bbb\">{word}</font>");
                }
                else
                {
                    var style = wordStyle.FirstOrDefault(_ => _.word == lowWord);
                    if (style == null)
                    {
                        wordList.Add($"<font color=\"#999\">{word}</font>");
                    }
                    else
                    {
                        wordList.Add($"<font color=\"{style.color}\">{word}</font>");
                    }
                }
            }

            return string.Join(" ", wordList);
        }
        private static IEnumerable<Color> GetColors(int count)
        {
            var baseColors = new[] { "44696C", "A35F0C", "B8254C", "7F233C", "120E0C", }
                .Select(GetColorFromHex)
                .ToArray();
            var targetColor = GetColorFromHex("999");
            var number = baseColors.Length;
            var steps = (int)Math.Ceiling((double)count / number);
            var colors = baseColors.Select(_ => GetGradients(_, targetColor, steps).ToArray()).ToArray();
            for (int i = 0; i < count; i++)
            {
                var m = i % number;
                var n = i / number;
                yield return colors[m][n];
            }
        }

        private static string ToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

        private static Color GetColorFromHex(string hex) => ColorTranslator.FromHtml("#" + hex);

        private static IEnumerable<Color> GetGradients(Color start, Color end, int steps)
        {
            if (steps == 1)
            {
                yield return start;
                yield break;
            }

            int stepA = ((end.A - start.A) / (steps - 1));
            int stepR = ((end.R - start.R) / (steps - 1));
            int stepG = ((end.G - start.G) / (steps - 1));
            int stepB = ((end.B - start.B) / (steps - 1));

            for (int i = 0; i < steps; i++)
            {
                yield return Color.FromArgb(start.A + (stepA * i),
                                            start.R + (stepR * i),
                                            start.G + (stepG * i),
                                            start.B + (stepB * i));
            }
        }

        private static readonly string[] NonLexicalWords = new[]  {
            "to",
            "got",
            "is",
            "have",
            "and",
            "although",
            "or",
            "that",
            "when",
            "while",
            "a",
            "either",
            "more",
            "much",
            "neither",
            "my",
            "that",
            "the",
            "as",
            "no",
            "nor",
            "not",
            "at",
            "between",
            "in",
            "of",
            "without",
            "I",
            "you",
            "he",
            "she",
            "it",
            "we",
            "they",
            "anybody",
            "one",
        };
        private static bool IsLexicalWord(string word) => !IsNonLexicalWord(word);
        private static bool IsNonLexicalWord(string word) => NonLexicalWords.Contains(word);
    }
}
