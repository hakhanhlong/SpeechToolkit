using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ultilities
{
    // <summary>
    /// Detect if a word is Vietnamese based on structure and character analysis.
    /// Ported from Python/JS implementation.
    /// </summary>
    public class VnLanguageDetector
    {
        // Sử dụng RegexOptions.Compiled để tăng tốc độ kiểm tra nếu xử lý văn bản dài
        private static readonly Regex VnAccentRegex = new Regex(
            @"[àáảãạăằắẳẵặâầấẩẫậèéẻẽẹêềếểễệìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵđ]",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        private static readonly Regex EnSpecialCharsRegex = new Regex(
            @"[fwzj]",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        // Regex tách âm tiết: [Phụ âm đầu] + [Nguyên âm] + [Phụ âm cuối]
        private static readonly Regex SyllableRegex = new Regex(
            @"^([^ueoaiy]*)([ueoaiy]+)([^ueoaiy]*)$",
            RegexOptions.Compiled // Chuỗi đã được ToLower() trước khi match nên không cần IgnoreCase ở đây
        );

        private static readonly Regex BadVowelsRegex = new Regex(
            @"ee|oo|ea|ae|ie",
            RegexOptions.Compiled
        );

        // Sử dụng HashSet để lookup với tốc độ O(1) thay vì Array/List (O(n))
        private static readonly HashSet<string> VnOnsets = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "b", "c", "d", "đ", "g", "h", "k", "l", "m", "n", "p", "q", "r", "s", "t", "v", "x",
            "ch", "gh", "gi", "kh", "ng", "nh", "ph", "qu", "th", "tr"
        };

        private static readonly HashSet<string> VnEndings = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "p", "t", "c", "m", "n", "ng", "ch", "nh"
        };

        private static readonly HashSet<string> AllowedVowelExceptions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "oa", "oe", "ua", "uy"
        };

        public bool IsVietnameseWord(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
            {
                return false;
            }

            string w = word.Trim().ToLowerInvariant();

            // 1. Nếu chứa dấu thanh tiếng Việt hoặc chữ 'đ/Đ' -> Chắc chắn là tiếng Việt
            if (VnAccentRegex.IsMatch(w))
            {
                return true;
            }

            // 2. Nếu chứa các ký tự ngoại lai đặc trưng của tiếng Anh -> Không phải tiếng Việt
            if (EnSpecialCharsRegex.IsMatch(w))
            {
                return false;
            }

            // 3. Tách cấu trúc từ (Syllable)
            Match match = SyllableRegex.Match(w);
            if (!match.Success)
            {
                return false;
            }

            string onset = match.Groups[1].Value;
            string vowel = match.Groups[2].Value;
            string ending = match.Groups[3].Value;

            // 4. Kiểm tra phụ âm đầu hợp lệ
            if (!string.IsNullOrEmpty(onset) && !VnOnsets.Contains(onset))
            {
                return false;
            }

            // 5. Kiểm tra phụ âm cuối hợp lệ
            if (!string.IsNullOrEmpty(ending) && !VnEndings.Contains(ending))
            {
                return false;
            }

            // 6. Kiểm tra các cụm nguyên âm thường gặp trong tiếng Anh nhưng không có trong tiếng Việt
            if (BadVowelsRegex.IsMatch(vowel))
            {
                if (!AllowedVowelExceptions.Contains(vowel))
                {
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Static wrapper mô phỏng lại behavior module-level function của Python.
    /// </summary>
    public static class VnLanguageDetectorUtility
    {
        private static readonly VnLanguageDetector _detector = new VnLanguageDetector();

        /// <summary>
        /// Check if a word is Vietnamese.
        /// </summary>
        /// <param name="word">The word to check</param>
        /// <returns>True if it is a Vietnamese word, False otherwise.</returns>
        public static bool IsVietnameseWord(string word)
        {
            return _detector.IsVietnameseWord(word);
        }
    }
}
