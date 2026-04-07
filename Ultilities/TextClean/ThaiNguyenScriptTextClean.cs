using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ultilities.TextClean
{
    public class ThaiNguyenScriptTextClean
    {
        // Từ điển chứa các từ viết tắt cần chuyển đổi
        private static readonly Dictionary<string, string> Abbreviations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"THPT", "Trung Học Phổ Thông"},
            {"HĐND", "Hội đồng nhân dân"},
            {"UBND", "Ủy ban nhân dân"}
            // Thêm các từ viết tắt khác vào đây...
        };

        public static string CleanScript(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText)) return string.Empty;

            string text = rawText;

            // 1. Xóa các nhãn người nói
            text = Regex.Replace(text, @"(?im)^(MC\d*|TĐ|Thưa \.\.\.)\s*:\s*", "");

            // 2. Xóa các thành phần trong ngoặc đơn không được phát âm
            // Lưu ý: Nếu bạn muốn giữ lại năm trong ngoặc (VD: (1946-2026)) để đọc, bạn cần vô hiệu hóa dòng này.
            text = Regex.Replace(text, @"\(.*?\)", "");

            // 3. Xóa các ghi chú âm thanh và kỹ thuật
            string[] technicalKeywords = {
                @"^Nhạc hiệu.*",
                @"^Nhạc cắt.*",
                @"^Nhạc mục.*",
                @"^Chấu:.*",
                @"^T/h:.*",
                @"^CM XDĐ.*",
                @"^KỊCH BẢN CHI TIẾT.*"
            };
            foreach (var keyword in technicalKeywords)
            {
                text = Regex.Replace(text, keyword, "", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            }

            // 4. Xóa các dấu gạch đầu dòng
            text = Regex.Replace(text, @"(?m)^\s*-\s*", "");

            // --- BẮT ĐẦU CÁC YÊU CẦU MỚI (CHỮ VIẾT TẮT, ĐƠN VỊ, SỐ) ---

            // A. Xử lý khoảng (Ví dụ: 1946-2026 -> 1946 đến 2026)
            text = Regex.Replace(text, @"(?<=\d)\s*-\s*(?=\d)", " đến ");

            // B. Xử lý đơn vị đo lường (Ví dụ: m² -> mét vuông)
            text = Regex.Replace(text, @"(?<=\d)\s*m²", " mét vuông");
            text = Regex.Replace(text, @"(?<=\d)\s*m3", " mét khối");

            // C. Chuyển đổi chữ viết tắt
            foreach (var kvp in Abbreviations)
            {
                text = Regex.Replace(text, $@"\b{kvp.Key}\b", kvp.Value);
            }

            // D. Chuyển đổi số sang chữ (Ví dụ: 7.000 -> Bẩy nghìn)
            // Nhận diện số nguyên và số có dấu chấm phân cách hàng nghìn (VD: 7.000, 2026)
            text = Regex.Replace(text, @"\b\d{1,3}(?:\.\d{3})*\b|\b\d+\b", match =>
            {
                string numStr = match.Value.Replace(".", ""); // Bỏ dấu chấm để xử lý
                if (long.TryParse(numStr, out long number))
                {
                    string words = ConvertNumberToVietnamese(number);
                    // Viết hoa chữ cái đầu tiên của số nếu cần thiết
                    return char.ToUpper(words[0]) + words.Substring(1);
                }
                return match.Value;
            });

            // --- KẾT THÚC CÁC YÊU CẦU MỚI ---

            // 5. Xóa các tên tác giả/phóng viên đứng độc lập ở cuối tin
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            var cleanedLines = new List<string>();
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                // Đếm số từ trong dòng (tương đương len(line.split()) trong Python)
                var wordCount = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;

                if (wordCount > 3 || string.IsNullOrEmpty(trimmed))
                {
                    cleanedLines.Add(trimmed);
                }
            }
            text = string.Join("\n", cleanedLines);

            // 6. Chuẩn hóa khoảng trắng và dòng trống
            text = Regex.Replace(text, @"[ \t]+", " ");
            text = Regex.Replace(text, @"\n\s*\n", "\n");

            return text.Trim();

        }

        // Hàm hỗ trợ chuyển đổi số sang chữ tiếng Việt
        private static string ConvertNumberToVietnamese(long number)
        {
            if (number == 0) return "không";

            // Xử lý ngoại lệ theo yêu cầu ngữ điệu cụ thể của bạn (Khẩu ngữ)
            if (number == 2026) return "hai nghìn không hai sáu";
            if (number == 1946) return "một nghìn chín trăm bốn sáu";

            string[] units = { "", "nghìn", "triệu", "tỷ", "nghìn tỷ", "triệu tỷ" };
            string[] digits = { "không", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín" };

            string result = "";
            int unitIndex = 0;

            while (number > 0)
            {
                int group = (int)(number % 1000);
                number /= 1000;

                if (group > 0)
                {
                    string groupText = ReadGroupOfThree(group, digits, number > 0);
                    result = $"{groupText} {units[unitIndex]} {result}".Trim();
                }
                unitIndex++;
            }

            return result.Trim();
        }

        private static string ReadGroupOfThree(int group, string[] digits, bool readHundred)
        {
            int hundred = group / 100;
            int ten = (group % 100) / 10;
            int unit = group % 10;

            string result = "";

            if (readHundred || hundred > 0)
            {
                result += $"{digits[hundred]} trăm ";
            }

            if (ten == 0 && unit > 0 && (readHundred || hundred > 0))
            {
                result += "lẻ ";
            }
            else if (ten == 1)
            {
                result += "mười ";
            }
            else if (ten > 1)
            {
                result += $"{digits[ten]} mươi ";
            }

            if (unit > 0)
            {
                if (ten > 1 && unit == 1) result += "mốt";
                else if (ten > 0 && unit == 5) result += "lăm";
                else if (ten > 1 && unit == 4) result += "tư";
                else result += digits[unit];
            }

            return result.Trim();
        }

    }
}
