using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ultilities
{
    public class VietnameseNormalizer
    {
        private static readonly Regex WordBoundaryRegex = new Regex(@"[\w\u00C0-\u1EFF]+", RegexOptions.Compiled);

        // Uppercase code pattern: 2+ chars, starts with uppercase letter, only uppercase + digits
        private static readonly Regex UppercaseCodePattern = new Regex(@"\b([A-Z][A-Z0-9]+)\b", RegexOptions.Compiled);

        private static readonly Dictionary<char, string> LetterNames = new Dictionary<char, string>
        {
            {'A', "a"}, {'B', "bê"}, {'C', "xê"}, {'D', "đê"}, {'E', "ê"},
            {'F', "ép"}, {'G', "giê"}, {'H', "hát"}, {'I', "i"}, {'J', "giây"},
            {'K', "ca"}, {'L', "e-lờ"}, {'M', "em"}, {'N', "en"}, {'O', "o"},
            {'P', "pê"}, {'Q', "cu"}, {'R', "e-rờ"}, {'S', "ét"}, {'T', "tê"},
            {'U', "u"}, {'V', "vê"}, {'W', "vê kép"}, {'X', "ích"}, {'Y', "i"},
            {'Z', "dét"}
        };

        private readonly VietnameseTextProcessor _processor;
        private readonly bool _enableTransliteration;
        private readonly string _dataDir;

        private Dictionary<string, string> _acronymMap;
        private Dictionary<string, string> _nonVietnameseMap;
        private Dictionary<string, string> _replacements;

        public VietnameseNormalizer(
            string acronymsPath = null,
            string nonVietnameseWordsPath = null,
            string dataDir = null,
            bool enableTransliteration = true)
        {
            _processor = new VietnameseTextProcessor();
            _enableTransliteration = enableTransliteration;

            if (!string.IsNullOrEmpty(dataDir))
            {
                _dataDir = dataDir;
            }
            else
            {
                // Mặc định thư mục 'data' nằm cùng cấp với file thực thi
                _dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            }

            _acronymMap = LoadAcronyms(acronymsPath);
            _nonVietnameseMap = LoadNonVietnameseWords(nonVietnameseWordsPath);

            BuildReplacementDict();
        }

        private Dictionary<string, string> LoadAcronyms(string customPath = null)
        {
            var acronymMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string pathToLoad = customPath ?? Path.Combine(_dataDir, "acronyms.csv");

            if (File.Exists(pathToLoad))
            {
                try
                {
                    var rows = ReadCsv(pathToLoad);
                    foreach (var row in rows)
                    {
                        string acronym = GetValueOrDefault(row, "acronym", "word").Trim().ToLowerInvariant();
                        string transliteration = GetValueOrDefault(row, "transliteration", "vietnamese_pronunciation").Trim();

                        if (!string.IsNullOrEmpty(acronym) && !string.IsNullOrEmpty(transliteration))
                        {
                            acronymMap[acronym] = transliteration;
                        }
                    }
                }
                catch { /* Ignore errors just like Python logic */ }
            }

            // Mặc dù Dictionary không có thứ tự, ta dùng nó với Regex/Từ điển tĩnh nên tốc độ là O(1)
            return acronymMap;
        }

        private Dictionary<string, string> LoadNonVietnameseWords(string customPath = null)
        {
            var wordMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string pathToLoad = customPath;

            if (string.IsNullOrEmpty(pathToLoad))
            {
                string path1 = Path.Combine(_dataDir, "non-vietnamese-words.csv");
                string path2 = Path.Combine(_dataDir, "non-vietnamese-words-20k.csv");
                if (File.Exists(path1)) pathToLoad = path1;
                else if (File.Exists(path2)) pathToLoad = path2;
            }

            if (!string.IsNullOrEmpty(pathToLoad) && File.Exists(pathToLoad))
            {
                try
                {
                    var rows = ReadCsv(pathToLoad);
                    foreach (var row in rows)
                    {
                        string word = GetValueOrDefault(row, "word", "original").Trim().ToLowerInvariant();
                        string pronunciation = GetValueOrDefault(row, "vietnamese_pronunciation", "transliteration").Trim();

                        if (!string.IsNullOrEmpty(word) && !string.IsNullOrEmpty(pronunciation))
                        {
                            wordMap[word] = pronunciation;
                        }
                    }
                }
                catch { /* Ignore errors */ }
            }

            return wordMap;
        }

        private void BuildReplacementDict()
        {
            _replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in _nonVietnameseMap)
            {
                _replacements[kvp.Key.ToLowerInvariant()] = kvp.Value;
            }
        }

        private string SpellOutCode(string code)
        {
            var parts = new List<string>();
            int i = 0;
            while (i < code.Length)
            {
                if (char.IsDigit(code[i]))
                {
                    int j = i;
                    while (j < code.Length && char.IsDigit(code[j]))
                    {
                        j++;
                    }
                    string numStr = code.Substring(i, j - i);
                    parts.Add(_processor.NumberToWords(numStr));
                    i = j;
                }
                else if (char.IsLetter(code[i]))
                {
                    char upperChar = char.ToUpperInvariant(code[i]);
                    if (LetterNames.ContainsKey(upperChar))
                    {
                        parts.Add(LetterNames[upperChar]);
                    }
                    i++;
                }
                else
                {
                    i++;
                }
            }
            return string.Join(" ", parts);
        }

        private string HandleUppercaseCodes(string text)
        {
            return UppercaseCodePattern.Replace(text, match =>
            {
                string code = match.Groups[1].Value;
                string codeLower = code.ToLowerInvariant();

                if (_acronymMap.ContainsKey(codeLower))
                {
                    return _acronymMap[codeLower];
                }
                return SpellOutCode(code);
            });
        }

        private string ApplyTransliteration(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            var processedWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var replacementsToMake = new List<(string Original, string Transliterated)>();

            foreach (Match match in WordBoundaryRegex.Matches(text))
            {
                string word = match.Value;
                string wordLower = word.ToLowerInvariant();

                if (processedWords.Contains(wordLower)) continue;
                processedWords.Add(wordLower);

                if (_replacements.ContainsKey(wordLower)) continue;

                if (VnLanguageDetectorUtility.IsVietnameseWord(word) || VnLanguageDetectorUtility.IsVietnameseWord(wordLower))
                    continue;

                if (word.Length <= 1) continue;

                string transliterated = EnglishToVnTransliterator.TransliterateWord(word);
                if (transliterated != word)
                {
                    replacementsToMake.Add((word, transliterated));
                }
            }

            // Apply replacements using lookaround to strictly match boundaries
            foreach (var item in replacementsToMake)
            {
                string escaped = Regex.Escape(item.Original);
                // Regex: Match từ nếu phía trước và sau nó không phải là ký tự chữ hoặc dấu tiếng Việt
                string patternStr = $@"(?<=^|[^\w\u00C0-\u1EFF]){escaped}(?=[^\w\u00C0-\u1EFF]|$)";

                text = Regex.Replace(text, patternStr, m =>
                {
                    string matchedWord = m.Value;
                    if (matchedWord.Length > 0 && char.IsUpper(matchedWord[0]))
                    {
                        if (item.Transliterated.Length > 1)
                            return char.ToUpperInvariant(item.Transliterated[0]) + item.Transliterated.Substring(1);
                        return item.Transliterated.ToUpperInvariant();
                    }
                    return item.Transliterated;
                }, RegexOptions.IgnoreCase);
            }

            return text;
        }

        public string Normalize(
            string text,
            bool enablePreprocessing = true,
            bool? enableTransliteration = null)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            string normalized;

            if (enablePreprocessing)
            {
                normalized = _processor.ProcessVietnameseText(text);
            }
            else
            {
                normalized = text.Normalize(NormalizationForm.FormC);
                normalized = Regex.Replace(normalized, @"\s+", " ").Trim();
            }

            normalized = HandleUppercaseCodes(normalized);
            normalized = normalized.ToLowerInvariant();

            // Replace words using dictionary (fast lookup)
            if (_replacements.Count > 0)
            {
                normalized = Regex.Replace(normalized, @"\b\w+\b", match =>
                {
                    string word = match.Value;
                    string wordLower = word.ToLowerInvariant();
                    if (_replacements.ContainsKey(wordLower))
                    {
                        return _replacements[wordLower];
                    }
                    return word;
                });
            }

            bool shouldTransliterate = enableTransliteration ?? _enableTransliteration;
            if (shouldTransliterate)
            {
                normalized = ApplyTransliteration(normalized);
            }

            normalized = Regex.Replace(normalized, @"\s+", " ").Trim();
            return normalized;
        }

        public void ReloadDictionaries(string acronymsPath = null, string nonVietnameseWordsPath = null)
        {
            _acronymMap = LoadAcronyms(acronymsPath);
            _nonVietnameseMap = LoadNonVietnameseWords(nonVietnameseWordsPath);
            BuildReplacementDict();
        }

        // --- Helper Methods cho đọc CSV ---
        private string GetValueOrDefault(Dictionary<string, string> row, string key1, string key2)
        {
            if (row.TryGetValue(key1, out string val) && !string.IsNullOrEmpty(val)) return val;
            if (row.TryGetValue(key2, out val) && !string.IsNullOrEmpty(val)) return val;
            return string.Empty;
        }

        private List<Dictionary<string, string>> ReadCsv(string filePath)
        {
            var result = new List<Dictionary<string, string>>();
            var lines = File.ReadAllLines(filePath);
            if (lines.Length == 0) return result;

            var headers = ParseCsvLine(lines[0]);

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                var parts = ParseCsvLine(lines[i]);
                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                for (int j = 0; j < headers.Count && j < parts.Count; j++)
                {
                    row[headers[j]] = parts[j];
                }
                result.Add(row);
            }
            return result;
        }

        /// <summary>
        /// Hàm hỗ trợ parse CSV cơ bản xử lý được các dấu ngoặc kép bọc chuỗi
        /// </summary>
        private List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var currentField = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '\"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentField.ToString().Trim(' ', '\"'));
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }
            result.Add(currentField.ToString().Trim(' ', '\"'));
            return result;
        }
    }
}
