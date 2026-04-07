using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ultilities
{
    public class VietnameseTextProcessor
    {
        private static readonly Dictionary<string, string> Digits = new Dictionary<string, string>
        {
            {"0", "không"}, {"1", "một"}, {"2", "hai"}, {"3", "ba"}, {"4", "bốn"},
            {"5", "năm"}, {"6", "sáu"}, {"7", "bảy"}, {"8", "tám"}, {"9", "chín"}
        };

        private static readonly Dictionary<string, string> Teens = new Dictionary<string, string>
        {
            {"10", "mười"}, {"11", "mười một"}, {"12", "mười hai"}, {"13", "mười ba"},
            {"14", "mười bốn"}, {"15", "mười lăm"}, {"16", "mười sáu"}, {"17", "mười bảy"},
            {"18", "mười tám"}, {"19", "mười chín"}
        };

        private static readonly Dictionary<string, string> Tens = new Dictionary<string, string>
        {
            {"2", "hai mươi"}, {"3", "ba mươi"}, {"4", "bốn mươi"}, {"5", "năm mươi"},
            {"6", "sáu mươi"}, {"7", "bảy mươi"}, {"8", "tám mươi"}, {"9", "chín mươi"}
        };

        private static readonly Dictionary<string, string> UnitMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Length
            {"cm", "xăng-ti-mét"}, {"mm", "mi-li-mét"}, {"km", "ki-lô-mét"},
            {"dm", "đề-xi-mét"}, {"hm", "héc-tô-mét"}, {"dam", "đề-ca-mét"},
            {"m", "mét"}, {"inch", "in"},
            // Weight
            {"kg", "ki-lô-gam"}, {"mg", "mi-li-gam"}, {"g", "gam"},
            {"t", "tấn"}, {"tấn", "tấn"}, {"yến", "yến"}, {"lạng", "lạng"},
            // Volume
            {"ml", "mi-li-lít"}, {"l", "lít"}, {"lít", "lít"},
            // Area
            {"m²", "mét vuông"}, {"m2", "mét vuông"},
            {"km²", "ki-lô-mét vuông"}, {"km2", "ki-lô-mét vuông"},
            {"ha", "héc-ta"},
            {"cm²", "xăng-ti-mét vuông"}, {"cm2", "xăng-ti-mét vuông"},
            // Cubic
            {"m³", "mét khối"}, {"m3", "mét khối"},
            {"cm³", "xăng-ti-mét khối"}, {"cm3", "xăng-ti-mét khối"},
            {"km³", "ki-lô-mét khối"}, {"km3", "ki-lô-mét khối"},
            // Time
            {"s", "giây"}, {"sec", "giây"}, {"min", "phút"},
            {"h", "giờ"}, {"hr", "giờ"}, {"hrs", "giờ"},
            // Speed
            {"km/h", "ki-lô-mét trên giờ"}, {"kmh", "ki-lô-mét trên giờ"},
            {"m/s", "mét trên giây"}, {"ms", "mét trên giây"},
            {"mm/h", "mi-li-mét trên giờ"}, {"cm/s", "xăng-ti-mét trên giây"},
            // Temperature
            {"°C", "độ C"}, {"°F", "độ F"}, {"°K", "độ K"},
            {"°R", "độ R"}, {"°Re", "độ Re"}, {"°Ro", "độ Ro"},
            {"°N", "độ N"}, {"°D", "độ D"}
        };

        private static readonly Dictionary<string, string> OrdinalMap = new Dictionary<string, string>
        {
            {"1", "nhất"}, {"2", "hai"}, {"3", "ba"}, {"4", "tư"}, {"5", "năm"},
            {"6", "sáu"}, {"7", "bảy"}, {"8", "tám"}, {"9", "chín"}, {"10", "mười"}
        };

        private static readonly Dictionary<char, int> RomanValues = new Dictionary<char, int>
        {
            {'I', 1}, {'V', 5}, {'X', 10}, {'L', 50}, {'C', 100}
        };

        // --- Regex Patterns ---
        // Sử dụng \p{Cs} để match toàn bộ các Emoji thuộc chuẩn Surrogate Pairs
        // Kết hợp với các dải unicode 16-bit của các biểu tượng cũ
        private static readonly Regex EmojiPattern = new Regex(
            @"[\u2600-\u26FF\u2700-\u27BF\u238C-\u2454\u20D0-\u20FF\uFE0F\u200D]|\p{Cs}",
            RegexOptions.Compiled);

        private static readonly Regex ThousandSepPattern = new Regex(@"(\d{1,3}(?:\.\d{3})+)(?=\s|$|[^\d.,])", RegexOptions.Compiled);
        private static readonly Regex DecimalPattern = new Regex(@"(\d+),(\d+)(?=\s|$|[^\d,])", RegexOptions.Compiled);
        private static readonly Regex PercentageRangePattern = new Regex(@"(\d+)\s*[-–—]\s*(\d+)\s*%", RegexOptions.Compiled);
        private static readonly Regex PercentageDecimalPattern = new Regex(@"(\d+),(\d+)\s*%", RegexOptions.Compiled);
        private static readonly Regex PercentagePattern = new Regex(@"(\d+)\s*%", RegexOptions.Compiled);
        private static readonly Regex StandaloneNumberPattern = new Regex(@"\b\d+\b", RegexOptions.Compiled);
        private static readonly Regex WhitespacePattern = new Regex(@"\s+", RegexOptions.Compiled);

        private static readonly Regex TimeHmsPattern = new Regex(@"(\d{1,2}):(\d{2})(?::(\d{2}))?", RegexOptions.Compiled);
        private static readonly Regex TimeHhmmPattern = new Regex(@"(\d{1,2})h(\d{2})(?![a-zà-ỹ])", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex TimeHPattern = new Regex(@"(\d{1,2})h(?![a-zà-ỹ\d])", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex TimeGioPhutPattern = new Regex(@"(\d+)\s*giờ\s*(\d+)\s*phút", RegexOptions.Compiled);
        private static readonly Regex TimeGioPattern = new Regex(@"(\d+)\s*giờ(?!\s*\d)", RegexOptions.Compiled);

        private static readonly Regex DateNgayRangePattern = new Regex(@"ngày\s+(\d{1,2})\s*[-–—]\s*(\d{1,2})\s*[/-]\s*(\d{1,2})(?:\s*[/-]\s*(\d{4}))?", RegexOptions.Compiled);
        private static readonly Regex DateRangePattern = new Regex(@"(\d{1,2})\s*[-–—]\s*(\d{1,2})\s*[/-]\s*(\d{1,2})(?:\s*[/-]\s*(\d{4}))?", RegexOptions.Compiled);
        private static readonly Regex MonthRangePattern = new Regex(@"(\d{1,2})\s*[-–—]\s*(\d{1,2})\s*[/-]\s*(\d{4})", RegexOptions.Compiled);
        private static readonly Regex DateSinhPattern = new Regex(@"(Sinh|sinh)\s+ngày\s+(\d{1,2})[/-](\d{1,2})[/-](\d{4})", RegexOptions.Compiled);
        private static readonly Regex DateFullPattern = new Regex(@"(\d{1,2})[/-](\d{1,2})[/-](\d{4})", RegexOptions.Compiled);
        private static readonly Regex DateMonthYearPattern = new Regex(@"(?:tháng\s+)?(\d{1,2})\s*[/-]\s*(\d{4})(?![\/-]\d)", RegexOptions.Compiled);
        private static readonly Regex DateDayMonthPattern = new Regex(@"(\d{1,2})\s*[/-]\s*(\d{1,2})(?![\/-]\d)(?!\d+\s*%)", RegexOptions.Compiled);
        private static readonly Regex DateXThangYPattern = new Regex(@"(\d+)\s*tháng\s*(\d+)", RegexOptions.Compiled);
        private static readonly Regex DateThangXPattern = new Regex(@"tháng\s*(\d+)", RegexOptions.Compiled);
        private static readonly Regex DateNgayXPattern = new Regex(@"ngày\s*(\d+)", RegexOptions.Compiled);

        private static readonly Regex CurrencyVndPattern1 = new Regex(@"(\d+(?:,\d+)?)\s*(?:đồng|VND|vnđ)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex CurrencyVndPattern2 = new Regex(@"(\d+(?:,\d+)?)đ(?![a-zà-ỹ])", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex CurrencyUsdPattern1 = new Regex(@"\$\s*(\d+(?:,\d+)?)", RegexOptions.Compiled);
        private static readonly Regex CurrencyUsdPattern2 = new Regex(@"(\d+(?:,\d+)?)\s*(?:USD|\$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex YearRangePattern = new Regex(@"(\d{4})\s*[-–—]\s*(\d{4})", RegexOptions.Compiled);
        private static readonly Regex OrdinalPattern = new Regex(@"(thứ|lần|bước|phần|chương|tập|số)\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex PhoneVnPattern = new Regex(@"0\d{9,10}", RegexOptions.Compiled);
        private static readonly Regex PhoneIntlPattern = new Regex(@"\+84\d{9,10}", RegexOptions.Compiled);

        private static readonly Regex RomanNumeralPattern = new Regex(@"\b([IVXLC]{2,})\b", RegexOptions.Compiled);

        private static readonly Regex AddressKeywordPattern = new Regex(@"(số|nhà|đường|hẻm|ngõ|ngách|kiệt|phố)\s+(\d+(?:/\d+)+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex Address3PartPattern = new Regex(@"\b(\d+)/(\d+)/(\d{1,3})\b", RegexOptions.Compiled);
        private static readonly Regex AddressBigNumPattern = new Regex(@"\b(\d{3,})/(\d+)\b", RegexOptions.Compiled);

        private readonly List<(string Unit, Regex Pattern)> _unitPatterns;

        public VietnameseTextProcessor()
        {
            _unitPatterns = CompileUnitPatterns();
        }

        private List<(string, Regex)> CompileUnitPatterns()
        {
            var sortedUnits = UnitMap.Keys.OrderByDescending(k => k.Length).ToList();
            var patterns = new List<(string, Regex)>();

            foreach (var unit in sortedUnits)
            {
                string escaped = Regex.Escape(unit);
                Regex pattern;
                if (unit.Length == 1)
                {
                    pattern = new Regex($@"(\d+)\s*{escaped}(?!\s*[a-zA-Zà-ỹ])(?=\s*[^a-zA-Zà-ỹ]|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
                }
                else
                {
                    pattern = new Regex($@"(\d+)\s*{escaped}(?=\s|[^\w]|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
                }
                patterns.Add((unit, pattern));
            }
            return patterns;
        }

        public string NumberToWords(string numStr)
        {
            numStr = Regex.Replace(numStr, "^0+", "");
            if (string.IsNullOrEmpty(numStr)) numStr = "0";

            if (numStr.StartsWith("-"))
            {
                return "âm " + NumberToWords(numStr.Substring(1));
            }

            if (!long.TryParse(numStr, out long num))
            {
                return numStr;
            }

            if (num == 0) return "không";
            if (num < 10) return Digits[num.ToString()];
            if (num < 20) return Teens[num.ToString()];

            if (num < 100)
            {
                long tens = num / 10;
                long units = num % 10;
                if (units == 0) return Tens[tens.ToString()];
                if (units == 1) return Tens[tens.ToString()] + " mốt";
                if (units == 4) return Tens[tens.ToString()] + " tư";
                if (units == 5) return Tens[tens.ToString()] + " lăm";
                return Tens[tens.ToString()] + " " + Digits[units.ToString()];
            }
            if (num < 1000)
            {
                long hundreds = num / 100;
                long remainder = num % 100;
                string result = Digits[hundreds.ToString()] + " trăm";
                if (remainder == 0) return result;
                if (remainder < 10) return result + " lẻ " + Digits[remainder.ToString()];
                return result + " " + NumberToWords(remainder.ToString());
            }
            if (num < 1000000)
            {
                long thousands = num / 1000;
                long remainder = num % 1000;
                string result = NumberToWords(thousands.ToString()) + " nghìn";
                if (remainder == 0) return result;
                if (remainder < 100)
                {
                    if (remainder < 10) return result + " không trăm lẻ " + Digits[remainder.ToString()];
                    return result + " không trăm " + NumberToWords(remainder.ToString());
                }
                return result + " " + NumberToWords(remainder.ToString());
            }
            if (num < 1000000000)
            {
                long millions = num / 1000000;
                long remainder = num % 1000000;
                string result = NumberToWords(millions.ToString()) + " triệu";
                if (remainder == 0) return result;
                if (remainder < 100)
                {
                    if (remainder < 10) return result + " không trăm lẻ " + Digits[remainder.ToString()];
                    return result + " không trăm " + NumberToWords(remainder.ToString());
                }
                return result + " " + NumberToWords(remainder.ToString());
            }
            if (num < 1000000000000)
            {
                long billions = num / 1000000000;
                long remainder = num % 1000000000;
                string result = NumberToWords(billions.ToString()) + " tỷ";
                if (remainder == 0) return result;
                if (remainder < 100)
                {
                    if (remainder < 10) return result + " không trăm lẻ " + Digits[remainder.ToString()];
                    return result + " không trăm " + NumberToWords(remainder.ToString());
                }
                return result + " " + NumberToWords(remainder.ToString());
            }

            return string.Join(" ", numStr.Select(d => Digits.ContainsKey(d.ToString()) ? Digits[d.ToString()] : d.ToString()));
        }

        public string RemoveThousandSeparators(string text)
        {
            return ThousandSepPattern.Replace(text, m => m.Value.Replace(".", ""));
        }

        public string ConvertDecimal(string text)
        {
            return DecimalPattern.Replace(text, m =>
            {
                string integerPart = m.Groups[1].Value;
                string decimalPart = m.Groups[2].Value;
                string decimalWords = NumberToWords(Regex.Replace(decimalPart, "^0+", "") == "" ? "0" : Regex.Replace(decimalPart, "^0+", ""));
                return $"{NumberToWords(integerPart)} phẩy {decimalWords}";
            });
        }

        public string ConvertPercentage(string text)
        {
            text = PercentageRangePattern.Replace(text, m =>
                $"{NumberToWords(m.Groups[1].Value)} đến {NumberToWords(m.Groups[2].Value)} phần trăm");

            text = PercentageDecimalPattern.Replace(text, m =>
            {
                string integerPart = m.Groups[1].Value;
                string decimalPart = m.Groups[2].Value;
                string decimalWords = NumberToWords(Regex.Replace(decimalPart, "^0+", "") == "" ? "0" : Regex.Replace(decimalPart, "^0+", ""));
                return $"{NumberToWords(integerPart)} phẩy {decimalWords} phần trăm";
            });

            return PercentagePattern.Replace(text, m => NumberToWords(m.Groups[1].Value) + " phần trăm");
        }

        public string ConvertCurrency(string text)
        {
            MatchEvaluator replaceVnd = m => NumberToWords(m.Groups[1].Value.Replace(",", "")) + " đồng";
            text = CurrencyVndPattern1.Replace(text, replaceVnd);
            text = CurrencyVndPattern2.Replace(text, replaceVnd);

            MatchEvaluator replaceUsd = m => NumberToWords(m.Groups[1].Value.Replace(",", "")) + " đô la";
            text = CurrencyUsdPattern1.Replace(text, replaceUsd);
            text = CurrencyUsdPattern2.Replace(text, replaceUsd);

            return text;
        }

        public string ConvertTime(string text)
        {
            text = TimeHmsPattern.Replace(text, m =>
            {
                string result = NumberToWords(m.Groups[1].Value) + " giờ";
                if (!string.IsNullOrEmpty(m.Groups[2].Value) && m.Groups[2].Value != "00")
                    result += " " + NumberToWords(m.Groups[2].Value) + " phút";
                if (m.Groups[3].Success && !string.IsNullOrEmpty(m.Groups[3].Value) && m.Groups[3].Value != "00")
                    result += " " + NumberToWords(m.Groups[3].Value) + " giây";
                return result;
            });

            text = TimeHhmmPattern.Replace(text, m =>
            {
                int h = int.Parse(m.Groups[1].Value);
                int minute = int.Parse(m.Groups[2].Value);
                if (h >= 0 && h <= 23 && minute >= 0 && minute <= 59)
                    return NumberToWords(m.Groups[1].Value) + " giờ " + NumberToWords(m.Groups[2].Value);
                return m.Value;
            });

            text = TimeHPattern.Replace(text, m =>
            {
                int h = int.Parse(m.Groups[1].Value);
                if (h >= 0 && h <= 23)
                    return NumberToWords(m.Groups[1].Value) + " giờ";
                return m.Value;
            });

            text = TimeGioPhutPattern.Replace(text, m => NumberToWords(m.Groups[1].Value) + " giờ " + NumberToWords(m.Groups[2].Value) + " phút");
            text = TimeGioPattern.Replace(text, m => NumberToWords(m.Groups[1].Value) + " giờ");

            return text;
        }

        public string ConvertYearRange(string text)
        {
            return YearRangePattern.Replace(text, m => NumberToWords(m.Groups[1].Value) + " đến " + NumberToWords(m.Groups[2].Value));
        }

        private bool IsValidDate(string day, string month, string year = null)
        {
            if (!int.TryParse(day, out int d) || !int.TryParse(month, out int m)) return false;
            if (year != null)
            {
                if (!int.TryParse(year, out int y)) return false;
                return d >= 1 && d <= 31 && m >= 1 && m <= 12 && y >= 1000 && y <= 9999;
            }
            return d >= 1 && d <= 31 && m >= 1 && m <= 12;
        }

        private bool IsValidMonth(string month)
        {
            if (!int.TryParse(month, out int m)) return false;
            return m >= 1 && m <= 12;
        }

        public string ConvertDate(string text)
        {
            text = DateNgayRangePattern.Replace(text, m =>
            {
                string y = m.Groups[4].Success ? m.Groups[4].Value : null;
                if (IsValidDate(m.Groups[1].Value, m.Groups[3].Value, y) && IsValidDate(m.Groups[2].Value, m.Groups[3].Value, y))
                {
                    string res = $"ngày {NumberToWords(m.Groups[1].Value)} đến {NumberToWords(m.Groups[2].Value)} tháng {NumberToWords(m.Groups[3].Value)}";
                    if (y != null) res += $" năm {NumberToWords(y)}";
                    return res;
                }
                return m.Value;
            });

            text = DateRangePattern.Replace(text, m =>
            {
                string y = m.Groups[4].Success ? m.Groups[4].Value : null;
                if (IsValidDate(m.Groups[1].Value, m.Groups[3].Value, y) && IsValidDate(m.Groups[2].Value, m.Groups[3].Value, y))
                {
                    string res = $"{NumberToWords(m.Groups[1].Value)} đến {NumberToWords(m.Groups[2].Value)} tháng {NumberToWords(m.Groups[3].Value)}";
                    if (y != null) res += $" năm {NumberToWords(y)}";
                    return res;
                }
                return m.Value;
            });

            text = MonthRangePattern.Replace(text, m =>
            {
                if (IsValidMonth(m.Groups[1].Value) && IsValidMonth(m.Groups[2].Value))
                {
                    int y = int.Parse(m.Groups[3].Value);
                    if (y >= 1000 && y <= 9999)
                        return $"tháng {NumberToWords(m.Groups[1].Value)} đến tháng {NumberToWords(m.Groups[2].Value)} năm {NumberToWords(m.Groups[3].Value)}";
                }
                return m.Value;
            });

            text = DateSinhPattern.Replace(text, m => IsValidDate(m.Groups[2].Value, m.Groups[3].Value, m.Groups[4].Value)
                ? $"{m.Groups[1].Value} ngày {NumberToWords(m.Groups[2].Value)} tháng {NumberToWords(m.Groups[3].Value)} năm {NumberToWords(m.Groups[4].Value)}"
                : m.Value);

            text = DateFullPattern.Replace(text, m => IsValidDate(m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value)
                ? $"ngày {NumberToWords(m.Groups[1].Value)} tháng {NumberToWords(m.Groups[2].Value)} năm {NumberToWords(m.Groups[3].Value)}"
                : m.Value);

            text = DateMonthYearPattern.Replace(text, m =>
            {
                int mVal = int.Parse(m.Groups[1].Value);
                int yVal = int.Parse(m.Groups[2].Value);
                return (mVal >= 1 && mVal <= 12 && yVal >= 1000 && yVal <= 9999)
                    ? $"tháng {NumberToWords(m.Groups[1].Value)} năm {NumberToWords(m.Groups[2].Value)}" : m.Value;
            });

            text = DateDayMonthPattern.Replace(text, m => IsValidDate(m.Groups[1].Value, m.Groups[2].Value)
                ? $"{NumberToWords(m.Groups[1].Value)} tháng {NumberToWords(m.Groups[2].Value)}" : m.Value);

            text = DateXThangYPattern.Replace(text, m => IsValidDate(m.Groups[1].Value, m.Groups[2].Value)
                ? $"ngày {NumberToWords(m.Groups[1].Value)} tháng {NumberToWords(m.Groups[2].Value)}" : m.Value);

            text = DateThangXPattern.Replace(text, m => IsValidMonth(m.Groups[1].Value) ? "tháng " + NumberToWords(m.Groups[1].Value) : m.Value);
            text = DateNgayXPattern.Replace(text, m => (int.Parse(m.Groups[1].Value) >= 1 && int.Parse(m.Groups[1].Value) <= 31) ? "ngày " + NumberToWords(m.Groups[1].Value) : m.Value);

            return text;
        }

        public string ConvertOrdinal(string text)
        {
            return OrdinalPattern.Replace(text, m =>
            {
                string num = m.Groups[2].Value;
                return m.Groups[1].Value + " " + (OrdinalMap.ContainsKey(num) ? OrdinalMap[num] : NumberToWords(num));
            });
        }

        public string ConvertPhoneNumber(string text)
        {
            MatchEvaluator replacePhone = m =>
            {
                var digits = Regex.Matches(m.Value, @"\d").Cast<Match>().Select(match => match.Value);
                return string.Join(" ", digits.Select(d => Digits.ContainsKey(d) ? Digits[d] : d));
            };

            text = PhoneVnPattern.Replace(text, replacePhone);
            text = PhoneIntlPattern.Replace(text, replacePhone);
            return text;
        }

        public string ConvertMeasurementUnits(string text)
        {
            foreach (var mapping in _unitPatterns)
            {
                text = mapping.Pattern.Replace(text, m => m.Groups[1].Value + " " + UnitMap[mapping.Unit]);
            }
            return text;
        }

        private int RomanToInt(string s)
        {
            if (string.IsNullOrEmpty(s) || !s.All(c => RomanValues.ContainsKey(c))) return -1;

            int total = 0;
            int prev = 0;
            for (int i = s.Length - 1; i >= 0; i--)
            {
                int val = RomanValues[s[i]];
                if (val < prev) total -= val;
                else total += val;
                prev = val;
            }
            return total;
        }

        public string ConvertRomanNumerals(string text)
        {
            return RomanNumeralPattern.Replace(text, m =>
            {
                int value = RomanToInt(m.Groups[1].Value);
                if (value >= 1 && value < 100) return NumberToWords(value.ToString());
                return m.Value;
            });
        }

        private string ReadAddressParts(string partsStr)
        {
            var parts = partsStr.Split('/');
            return string.Join(" trên ", parts.Select(NumberToWords));
        }

        public string ConvertAddressNumber(string text)
        {
            text = AddressKeywordPattern.Replace(text, m => m.Groups[1].Value + " " + ReadAddressParts(m.Groups[2].Value));
            text = Address3PartPattern.Replace(text, m => ReadAddressParts($"{m.Groups[1].Value}/{m.Groups[2].Value}/{m.Groups[3].Value}"));
            text = AddressBigNumPattern.Replace(text, m => ReadAddressParts($"{m.Groups[1].Value}/{m.Groups[2].Value}"));
            return text;
        }

        public string ConvertStandaloneNumbers(string text)
        {
            return StandaloneNumberPattern.Replace(text, m => NumberToWords(m.Value));
        }

        public string RemoveSpecialChars(string text)
        {
            text = text.Replace("&", " và ")
                       .Replace("@", " a còng ")
                       .Replace("#", " thăng ")
                       .Replace("*", "")
                       .Replace("_", " ")
                       .Replace("~", "")
                       .Replace("`", "")
                       .Replace("^", "");

            text = Regex.Replace(text, @"https?://\S+", "");
            text = Regex.Replace(text, @"www\.\S+", "");
            text = Regex.Replace(text, @"\S+@\S+\.\S+", "");
            return text;
        }

        public string NormalizePunctuation(string text)
        {
            text = Regex.Replace(text, @"[""„‟”""]", "\""); // Bao gồm cả ngoặc kép thông minh nếu có
            text = Regex.Replace(text, @"['‚‛‘’]", "'");
            text = Regex.Replace(text, @"[–—−]", "-");
            text = Regex.Replace(text, @"\.{3,}", "...");
            text = text.Replace("…", "...");
            text = Regex.Replace(text, @"([!?.]){2,}", "$1");
            return text;
        }

        public string CleanTextForTTS(string text)
        {
            text = EmojiPattern.Replace(text, "");
            text = Regex.Replace(text, @"[\\()¯""”""]", "");
            text = Regex.Replace(text, @"\s—", ".");
            text = Regex.Replace(text, @"\b_\b", " ");
            text = Regex.Replace(text, @"(?<!\d)-(?!\d)", " ");
            text = Regex.Replace(text, @"[^\u0000-\u024F\u1E00-\u1EFF]", "");
            return text.Trim();
        }

        /// <summary>
        /// Main function to process Vietnamese text for TTS.
        /// Applies all normalization steps in the correct order matching the source.
        /// </summary>
        public string ProcessVietnameseText(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            // Step 1: Normalize Unicode
            text = text.Normalize(NormalizationForm.FormC);

            // Step 2: Remove special characters
            text = RemoveSpecialChars(text);

            // Step 3: Normalize punctuation
            text = NormalizePunctuation(text);

            // Step 4: Clean text
            text = CleanTextForTTS(text);

            // Step 5: Convert address numbers
            text = ConvertAddressNumber(text);

            // Step 6: Convert year ranges
            text = ConvertYearRange(text);

            // Step 7: Convert percentage ranges
            text = PercentageRangePattern.Replace(text, m => $"{NumberToWords(m.Groups[1].Value)} đến {NumberToWords(m.Groups[2].Value)} phần trăm");

            // Step 8: Convert dates
            text = ConvertDate(text);

            // Step 9: Convert times
            text = ConvertTime(text);

            // Step 10: Convert ordinals
            text = ConvertOrdinal(text);

            // Step 11: Remove thousand separators
            text = RemoveThousandSeparators(text);

            // Step 12: Convert currency
            text = ConvertCurrency(text);

            // Step 13: Convert percentages
            text = ConvertPercentage(text);

            // Step 14: Convert phone numbers
            text = ConvertPhoneNumber(text);

            // Step 15: Convert decimals
            text = ConvertDecimal(text);

            // Step 16: Convert measurement units
            text = ConvertMeasurementUnits(text);

            // Step 17: Convert Roman numerals
            text = ConvertRomanNumerals(text);

            // Step 18: Convert remaining standalone numbers
            text = ConvertStandaloneNumbers(text);

            // Step 19: Clean whitespace
            text = WhitespacePattern.Replace(text, " ").Trim();

            return text;
        }
    }
}
