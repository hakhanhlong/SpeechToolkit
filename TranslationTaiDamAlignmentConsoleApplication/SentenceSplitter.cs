using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TranslationTaiDamAlignmentConsoleApplication
{
    public class SentenceSplitter
    {
        public static List<string> SplitIntoSentences(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new List<string>();

            // 1. Tiền xử lý: Xóa bớt khoảng trắng thừa và ký tự xuống dòng lộn xộn
            text = Regex.Replace(text, @"\s+", " ").Trim();

            // 2. Danh sách các từ viết tắt phổ biến trong Tiếng Việt không được cắt
            // Thêm các từ viết tắt tiếng Thái Đen vào đây nếu có
            string[] abbreviations = { "TP", "GS", "TS", "ThS", "PGS", "BS", "Mr", "Mrs", "v.v", "St", "Q", "H", "P" };

            // Chuyển danh sách thành chuỗi regex (vd: TP|GS|TS)
            string abbrevPattern = string.Join("|", abbreviations);

            // 3. Mẫu Regex tách câu thần thánh:
            // Giải thích:
            // (?<!\b(?:TP|GS|TS|...)) : Phía trước KHÔNG PHẢI là từ viết tắt
            // (?<=[.!?]+[""']?)       : Phía trước LÀ dấu câu (.!?) có thể kèm dấu nháy đóng (")
            // \s+                     : Theo sau là một hoặc nhiều khoảng trắng
            // (?=[\p{Lu}\p{M}\"\'\-]) : Phía sau LÀ một chữ cái viết hoa (\p{Lu}), hoặc dấu nháy, dấu gạch ngang
            string pattern = $@"(?<!\b(?:{abbrevPattern}))(?<=[.!?]+[""']?)\s+(?=[\p{{Lu}}\p{{M}}\""\'\-])";

            // 4. Thực hiện tách câu
            var rawSentences = Regex.Split(text, pattern);

            // 5. Làm sạch kết quả
            var result = rawSentences
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .ToList();

            return result;
        }
    }
}
