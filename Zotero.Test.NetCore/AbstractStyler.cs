using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Zotero.Test.NetCore
{
    public class AbstractStyler
    {
        public static string StyleAbstract(string originAbs, string[] highlights = null)
        {
            if (originAbs == null) return null;
            highlights ??= Array.Empty<string>();
            var punctuation = originAbs.Where(char.IsPunctuation).Distinct().ToArray();
            var wordsStats = originAbs.Split()
                .Select(_ => _.Trim(punctuation))
                .Select(_ => _.ToLower())
                .Where(IsLexicalWord)
                .GroupBy(_ => _)
                .Select(_ => new { word = _.Key, num = _.Count() })
                .Where(_ => _.num > 1);
            var colors = GetColors(wordsStats.Count()).ToArray();
            var wordStyle = wordsStats
                .OrderByDescending(_ => _.num)
                .Select((_, i) => new { _.word, color = colors[i] })
                .ToArray();

            var hlpunc = string.Join("", highlights).Where(char.IsPunctuation).Distinct().ToArray();
            var hls = highlights.Select(hl => hl.Split().Select(_ => _.Trim(punctuation).ToLower()).ToArray());

            var words = originAbs.Split();
            var pureWords = originAbs.Split().Select(_ => _.Trim(punctuation).ToLower()).ToArray();
            var wordList = new List<string>();
            foreach (var word in words)
            {
                var pureWord = word.Trim(punctuation);//.ToLower();
                var lowWord = pureWord.ToLower();
                var resultWord = word;
                if (resultWord.EndsWith(".")) resultWord = resultWord.Replace(".", ".◾");

                if (IsNonLexicalWord(lowWord))
                {
                    wordList.Add(WrapFontColor(resultWord, GetColorFromHex("ddd")));
                }
                else
                {
                    var style = wordStyle.FirstOrDefault(_ => _.word == lowWord);
                    if (style == null)
                    {
                        wordList.Add(WrapFontColor(resultWord, GetColorFromHex("aaa")));
                    }
                    else
                    {
                        wordList.Add(WrapFontColor(resultWord, style.color));
                    }
                }
            }

            // highlight
            foreach (var hl in hls)
            {
                var idxs = FindStartingIndices(pureWords, hl);
                foreach (var idx in idxs)
                {
                    wordList[idx] = "<span class=\"highlight\">" + wordList[idx];
                    wordList[idx + hl.Length - 1] += "</span>";
                }
            }

            return string.Join(" ", wordList);
        }
        private static IEnumerable<Color> GetColors(int count)
        {
            var baseColors = new[] { "36133D", "10293F", "2C6829", "8F7D19", "AB4822", }
            //var baseColors = new[] { "44696C", "A35F0C", "B8254C", "7F233C", "120E0C", }
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

        // adapted from https://stackoverflow.com/a/1780481
        private static IEnumerable<int> FindStartingIndices(string[] arr, string[] subArr)
        {
            IEnumerable<int> index = Enumerable.Range(0, arr.Length - subArr.Length + 1);
            for (int i = 0; i < subArr.Length; i++)
            {
                index = index.Where(n => arr[n + i] == subArr[i]).ToArray();
            }
            return index;
        }
        private static readonly string[] NonLexicalWords = new[] { "the", "of", "and", "to", "a", "in", "for", "is", "on", "that", "by", "this", "with", "i", "you", "it", "not", "or", "be", "are", "from", "at", "as", "your", "all", "have", "new", "more", "an", "was", "we", "will", "home", "can", "us", "about", "if", "page", "my", "has", "free", "but", "our", "one", "other", "do", "no", "information", "time", "they", "site", "he", "up", "may", "what", "which", "their", "news", "out", "use", "any", "there", "see", "only", "so", "his", "when", "contact", "here", "business", "who", "web", "also", "now", "help", "get", "pm", "view", "online", "c", "e", "first", "am", "been", "would", "how", "were", "me", "s", "services", "some", "these", "click", "its", "like", "service", "x", "than", "find", "price", "date", "back", "top", "people", "had", "list", "name", "just", "over", "state", "year", "day", "into", "email", "two", "health", "n", "world", "re", "next", "used", "go", "b", "work", "last", "most", "products", "music", "buy", "make", "them", "should", "product", "post", "her", "city", "t", "add", "policy", "number", "such", "please", "available", "copyright", "support", "message", "after", "best", "software", "then", "jan", "good", "video", "well", "d", "where", "info", "rights", "public", "books", "high", "school", "through", "m", "each", "links", "she", "review", "years", "order", "very", "privacy", "book", "items", "company", "r", "read", "group", "sex", "need", "many", "user", "said", "de", "does", "set", "under", "general", "research", "university", "january", "mail", "full", "map", "reviews", "program", "life" };
        private static bool IsLexicalWord(string word) => !IsNonLexicalWord(word);
        private static bool IsNonLexicalWord(string word) => NonLexicalWords.Contains(word);

        private static string WrapFontColor(string content, Color color)
            => $"<font color=\"{ToHex(color)}\">{content}</font>";

        private static string WrapStyle(string content, string style)
            => $"<span style=\"{style}\">{content}</span>";
    }
}
