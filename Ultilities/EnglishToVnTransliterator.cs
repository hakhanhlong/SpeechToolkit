using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ultilities
{
    /// <summary>
    /// English to Vietnamese Transliterator.
    /// Converts English words to Vietnamese phonetic transliteration using a rule-based approach.
    /// </summary>
    public static class EnglishToVnTransliterator
    {
        private static readonly List<(Regex Pattern, string Replacement)> HighPriorityRules;
        private static readonly List<(Regex Pattern, string Replacement)> EndingRules;
        private static readonly List<(Regex Pattern, string Replacement)> GeneralRules;

        private const string Vowels = "aeiouăâêôơưáàảãạắằẳẵặấầẩẫậéèẻẽẹếềểễệíìỉĩịóòỏõọốồổỗộớờởỡợúùủũụứừửữựýỳỷỹỵ";
        private static readonly Regex SyllablePattern;
        private static readonly Regex ConsonantYPattern = new Regex(@"([bcdfghjklmnpqrstvwxz])y", RegexOptions.Compiled);
        private static readonly Regex YEndPattern = new Regex(@"y$", RegexOptions.Compiled);
        private static readonly Regex DoubleConsonantPattern = new Regex(@"([brlptdgmnckxsvfzjwqh])\1+", RegexOptions.Compiled);

        private static readonly HashSet<string> ValidConsonantPairs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ch", "th", "ph", "sh", "ng", "tr", "nh", "gh", "kh"
        };

        private static readonly HashSet<char> Consonants = new HashSet<char>("bcdfghjklmnpqrstvwxz".ToCharArray());
        private static readonly HashSet<char> ValidEndings = new HashSet<char>("ptcmngs".ToCharArray());

        static EnglishToVnTransliterator()
        {
            // Regex tách âm tiết động dựa trên chuỗi Vowels
            SyllablePattern = new Regex($@"([^{Vowels}]*[{Vowels}]+[ptcmngs]?(?![{Vowels}]))", RegexOptions.Compiled);

            // ==========================================
            // 1. HIGH PRIORITY RULES
            // ==========================================
            HighPriorityRules = new List<(Regex, string)>
            {
                // Special word endings
                (new Regex(@"tion$"), "ân"), (new Regex(@"sion$"), "ân"),
                (new Regex(@"age$"), "ây"), (new Regex(@"ing$"), "ing"),
                (new Regex(@"ture$"), "chờ"), (new Regex(@"cial$"), "xô"), (new Regex(@"tial$"), "xô"),

                // Complex vowel combinations
                (new Regex(@"aught"), "ót"), (new Regex(@"ought"), "ót"), (new Regex(@"ound"), "ao"),
                (new Regex(@"ight"), "ai"), (new Regex(@"eigh"), "ây"), (new Regex(@"ough"), "ao"),

                // Initial consonant clusters
                (new Regex(@"\bst(?!r)"), "t"), (new Regex(@"\bstr"), "tr"),
                (new Regex(@"\bsch"), "c"), (new Regex(@"\bsc(?=h)"), "c"),
                (new Regex(@"\bsc|\bsk"), "c"), (new Regex(@"\bsp"), "p"),
                (new Regex(@"\btr"), "tr"), (new Regex(@"\bbr"), "r"),
                (new Regex(@"\bcr|\bpr|\bgr|\bdr|\bfr"), "r"),
                (new Regex(@"\bbl|\bcl|\bsl|\bpl"), "l"), (new Regex(@"\bfl"), "ph"),

                // Double consonants
                (new Regex(@"ck"), "c"), (new Regex(@"sh"), "s"), (new Regex(@"ch"), "ch"),
                (new Regex(@"th"), "th"), (new Regex(@"ph"), "ph"), (new Regex(@"wh"), "q"),
                (new Regex(@"qu"), "q"), (new Regex(@"kn"), "n"), (new Regex(@"wr"), "r")
            };

            // ==========================================
            // 2. ENDING RULES
            // ==========================================
            EndingRules = new List<(Regex, string)>
            {
                (new Regex(@"le$"), "ồ"),
                
                // Vowel + consonant endings
                (new Regex(@"ook$"), "úc"), (new Regex(@"ood$"), "út"), (new Regex(@"ool$"), "un"),
                (new Regex(@"oom$"), "um"), (new Regex(@"oon$"), "un"), (new Regex(@"oot$"), "út"),
                (new Regex(@"iend$"), "en"), (new Regex(@"end$"), "en"), (new Regex(@"eau$"), "iu"),
                (new Regex(@"ail$"), "ain"), (new Regex(@"ain$"), "ain"), (new Regex(@"ait$"), "ât"),
                (new Regex(@"oat$"), "ốt"), (new Regex(@"oad$"), "ốt"), (new Regex(@"oal$"), "ôn"),
                (new Regex(@"eep$"), "íp"), (new Regex(@"eet$"), "ít"), (new Regex(@"eel$"), "in"),

                // -TCH endings
                (new Regex(@"atch$"), "át"), (new Regex(@"etch$"), "éch"), (new Regex(@"itch$"), "ích"),
                (new Regex(@"otch$"), "ốt"), (new Regex(@"utch$"), "út"),

                // -DGE endings
                (new Regex(@"edge$"), "ét"), (new Regex(@"idge$"), "ít"), (new Regex(@"odge$"), "ót"), (new Regex(@"udge$"), "út"),

                // -CK/-K endings
                (new Regex(@"ack$"), "ác"), (new Regex(@"eck$"), "éc"), (new Regex(@"ick$"), "ích"),
                (new Regex(@"ock$"), "óc"), (new Regex(@"uck$"), "úc"),

                // -SH endings
                (new Regex(@"ash$"), "át"), (new Regex(@"esh$"), "ét"), (new Regex(@"ish$"), "ít"),
                (new Regex(@"osh$"), "ốt"), (new Regex(@"ush$"), "út"),

                // -TH endings
                (new Regex(@"ath$"), "át"), (new Regex(@"eth$"), "ét"), (new Regex(@"ith$"), "ít"),
                (new Regex(@"oth$"), "ót"), (new Regex(@"uth$"), "út"),

                // -TE endings (silent E)
                (new Regex(@"ate$"), "ây"), (new Regex(@"ete$"), "ét"), (new Regex(@"ite$"), "ai"),
                (new Regex(@"ote$"), "ốt"), (new Regex(@"ute$"), "út"),

                // -DE endings
                (new Regex(@"ade$"), "ây"), (new Regex(@"ede$"), "ét"), (new Regex(@"ide$"), "ai"),
                (new Regex(@"ode$"), "ốt"), (new Regex(@"ude$"), "út"),

                // Silent-E endings
                (new Regex(@"ake$"), "ây"), (new Regex(@"ame$"), "am"), (new Regex(@"ane$"), "an"),
                (new Regex(@"ape$"), "ếp"), (new Regex(@"eke$"), "ét"), (new Regex(@"eme$"), "êm"),
                (new Regex(@"ene$"), "en"), (new Regex(@"ike$"), "íc"), (new Regex(@"ime$"), "am"),
                (new Regex(@"ine$"), "ai"), (new Regex(@"oke$"), "ốc"), (new Regex(@"ome$"), "om"),
                (new Regex(@"one$"), "oăn"), (new Regex(@"uke$"), "ấc"), (new Regex(@"ume$"), "uym"),
                (new Regex(@"une$"), "uyn"),

                // -SE endings
                (new Regex(@"ase$"), "ây"), (new Regex(@"ise$"), "ai"), (new Regex(@"ose$"), "âu"),

                // -LL endings
                (new Regex(@"all$"), "âu"), (new Regex(@"ell$"), "eo"), (new Regex(@"ill$"), "iu"),
                (new Regex(@"oll$"), "ôn"), (new Regex(@"ull$"), "un"),

                // -NG endings
                (new Regex(@"ang$"), "ang"), (new Regex(@"eng$"), "ing"), (new Regex(@"ong$"), "ong"), (new Regex(@"ung$"), "âng"),

                // Complex vowel endings
                (new Regex(@"air$"), "e"), (new Regex(@"ear$"), "ia"), (new Regex(@"ire$"), "ai"),
                (new Regex(@"ure$"), "iu"), (new Regex(@"our$"), "ao"), (new Regex(@"ore$"), "o"),
                (new Regex(@"ound$"), "ao"), (new Regex(@"ight$"), "ai"), (new Regex(@"aught$"), "ót"),
                (new Regex(@"ought$"), "ót"), (new Regex(@"eigh$"), "ây"), (new Regex(@"ork$"), "ót"),

                // Double vowel endings
                (new Regex(@"ee$"), "i"), (new Regex(@"ea$"), "i"), (new Regex(@"oo$"), "u"),
                (new Regex(@"oa$"), "oa"), (new Regex(@"oe$"), "oe"), (new Regex(@"ai$"), "ai"),
                (new Regex(@"ay$"), "ay"), (new Regex(@"au$"), "au"), (new Regex(@"aw$"), "â"),
                (new Regex(@"ei$"), "ây"), (new Regex(@"ey$"), "ây"), (new Regex(@"oi$"), "oi"),
                (new Regex(@"oy$"), "oi"), (new Regex(@"ou$"), "u"), (new Regex(@"ow$"), "ô"),
                (new Regex(@"ue$"), "ue"), (new Regex(@"ui$"), "ui"), (new Regex(@"ie$"), "ai"),
                (new Regex(@"eu$"), "iu"),

                // -R endings
                (new Regex(@"ar$"), "a"), (new Regex(@"er$"), "ơ"), (new Regex(@"ir$"), "ơ"),
                (new Regex(@"or$"), "o"), (new Regex(@"ur$"), "ơ"),

                // -L endings
                (new Regex(@"al$"), "an"), (new Regex(@"el$"), "eo"), (new Regex(@"il$"), "iu"),
                (new Regex(@"ol$"), "ôn"), (new Regex(@"ul$"), "un"),

                // Basic closed syllable endings
                (new Regex(@"ab$"), "áp"), (new Regex(@"ad$"), "át"), (new Regex(@"ag$"), "ác"),
                (new Regex(@"ak$"), "át"), (new Regex(@"ap$"), "áp"), (new Regex(@"at$"), "át"),
                (new Regex(@"eb$"), "ép"), (new Regex(@"ed$"), "ét"), (new Regex(@"eg$"), "ét"),
                (new Regex(@"ek$"), "éc"), (new Regex(@"ep$"), "ép"), (new Regex(@"et$"), "ét"),
                (new Regex(@"ib$"), "íp"), (new Regex(@"id$"), "ít"), (new Regex(@"ig$"), "íc"),
                (new Regex(@"ik$"), "íc"), (new Regex(@"ip$"), "íp"), (new Regex(@"it$"), "ít"),
                (new Regex(@"ob$"), "óp"), (new Regex(@"od$"), "ót"), (new Regex(@"og$"), "óc"),
                (new Regex(@"ok$"), "óc"), (new Regex(@"op$"), "óp"), (new Regex(@"ot$"), "ót"),
                (new Regex(@"ub$"), "úp"), (new Regex(@"ud$"), "út"), (new Regex(@"ug$"), "úc"),
                (new Regex(@"uk$"), "úc"), (new Regex(@"up$"), "úp"), (new Regex(@"ut$"), "út"),

                // -M/-N endings
                (new Regex(@"am$"), "am"), (new Regex(@"an$"), "an"), (new Regex(@"em$"), "em"),
                (new Regex(@"en$"), "en"), (new Regex(@"im$"), "im"), (new Regex(@"in$"), "in"),
                (new Regex(@"om$"), "om"), (new Regex(@"on$"), "on"), (new Regex(@"um$"), "âm"),
                (new Regex(@"un$"), "ân"),

                // -S endings
                (new Regex(@"as$"), "ẹt"), (new Regex(@"es$"), "ẹt"), (new Regex(@"is$"), "ít"),
                (new Regex(@"os$"), "ọt"), (new Regex(@"us$"), "ợt"),

                // Double vowels
                (new Regex(@"aa$"), "a"), (new Regex(@"ii$"), "i"), (new Regex(@"uu$"), "u")
            };

            // ==========================================
            // 3. GENERAL RULES
            // ==========================================
            GeneralRules = new List<(Regex, string)>
            {
                (new Regex(@"j"), "d"), (new Regex(@"z"), "d"), (new Regex(@"w"), "u"),
                (new Regex(@"x"), "x"), (new Regex(@"v"), "v"), (new Regex(@"f"), "ph"),
                (new Regex(@"s"), "x"), (new Regex(@"c"), "k"), (new Regex(@"q"), "ku"),
                (new Regex(@"a"), "a"), (new Regex(@"e"), "e"), (new Regex(@"i"), "i"),
                (new Regex(@"o"), "o"), (new Regex(@"u"), "u")
            };

            // Bổ sung RegexOptions.Compiled cho các pattern nội bộ trong các List
            CompileRules(HighPriorityRules);
            CompileRules(EndingRules);
            CompileRules(GeneralRules);
        }

        private static void CompileRules(List<(Regex, string)> rules)
        {
            for (int i = 0; i < rules.Count; i++)
            {
                var rule = rules[i];
                if ((rule.Item1.Options & RegexOptions.Compiled) == 0)
                {
                    rules[i] = (new Regex(rule.Item1.ToString(), rule.Item1.Options | RegexOptions.Compiled), rule.Item2);
                }
            }
        }

        private static string ApplyRules(string w, List<(Regex Pattern, string Replacement)> rules)
        {
            foreach (var rule in rules)
            {
                w = rule.Pattern.Replace(w, rule.Replacement);
            }
            return w;
        }

        private static string CleanConsonantClusters(string p)
        {
            // Thay thế phụ âm lặp (vd: pp -> p) -> Tương đương r'\1' trong Python
            p = DoubleConsonantPattern.Replace(p, "$1");

            var result = new System.Text.StringBuilder();
            int i = 0;
            while (i < p.Length)
            {
                if (i < p.Length - 1 && Consonants.Contains(p[i]) && Consonants.Contains(p[i + 1]))
                {
                    string pair = p.Substring(i, 2);
                    if (ValidConsonantPairs.Contains(pair))
                    {
                        result.Append(pair);
                        i += 2;
                    }
                    else
                    {
                        result.Append(p[i + 1]);
                        i += 2;
                    }
                }
                else
                {
                    result.Append(p[i]);
                    i += 1;
                }
            }
            return result.ToString();
        }

        private static string ApplyCkRule(string p)
        {
            if (p.StartsWith("ch") || p.StartsWith("th") || p.StartsWith("ph") || p.StartsWith("sh"))
                return p;

            if (p.StartsWith("k") || p.StartsWith("c"))
            {
                if (p.Length > 1)
                {
                    char nextChar = p[1];
                    bool useK = nextChar == 'i' || nextChar == 'e' || nextChar == 'y';
                    return (useK ? "k" : "c") + p.Substring(1);
                }
            }
            return p;
        }

        private static string FilterEnding(string p)
        {
            if (p.Length > 1 && !Vowels.Contains(p[p.Length - 1].ToString()))
            {
                char last = p[p.Length - 1];
                if (!ValidEndings.Contains(last))
                {
                    if (last == 'l') return p.Substring(0, p.Length - 1) + "n";
                    return p.Substring(0, p.Length - 1);
                }
            }
            return p;
        }

        private static string ProcessSyllable(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            s = s.Trim();
            if (string.IsNullOrEmpty(s)) return string.Empty;

            if (s.StartsWith("y")) s = "d" + s.Substring(1);

            s = ApplyRules(s, HighPriorityRules);
            s = ApplyRules(s, EndingRules);
            s = ApplyRules(s, GeneralRules);

            s = ConsonantYPattern.Replace(s, "$1i");
            s = YEndPattern.Replace(s, "i");

            s = CleanConsonantClusters(s);
            s = ApplyCkRule(s);
            s = FilterEnding(s);

            return s;
        }

        private static string EnglishToVietnamese(string word)
        {
            if (string.IsNullOrWhiteSpace(word)) return string.Empty;

            string w = word.ToLowerInvariant().Trim();

            if (w.StartsWith("y")) w = "d" + w.Substring(1);
            if (w.StartsWith("d")) w = "đ" + w.Substring(1);

            w = ApplyRules(w, HighPriorityRules);
            w = ApplyRules(w, EndingRules);
            w = ApplyRules(w, GeneralRules);

            w = ConsonantYPattern.Replace(w, "$1i");
            w = YEndPattern.Replace(w, "i");

            var parts = SyllablePattern.Matches(w).Cast<Match>().Select(m => m.Groups[1].Value).ToList();
            if (parts.Count == 0) return w;

            var finalParts = parts.Select(ProcessSyllable).Where(p => !string.IsNullOrEmpty(p)).ToList();
            return string.Join("-", finalParts);
        }

        /// <summary>
        /// Transliterate a word from English to Vietnamese.
        /// If the word is already Vietnamese, returns it unchanged.
        /// </summary>
        public static string TransliterateWord(string word)
        {
            if (string.IsNullOrWhiteSpace(word)) return word ?? string.Empty;

            // Gọi class VnLanguageDetectorUtility đã port ở bước trước
            if (VnLanguageDetectorUtility.IsVietnameseWord(word))
            {
                return word;
            }

            return EnglishToVietnamese(word);
        }
    }
}
