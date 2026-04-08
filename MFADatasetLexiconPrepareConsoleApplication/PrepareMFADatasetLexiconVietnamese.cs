using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MFADatasetLexiconPrepareConsoleApplication
{
    public static class PrepareMFADatasetLexiconVietnamese
    {
        /// <summary>
        /// Làm sạch văn bản theo chuẩn MFA
        /// </summary>
        public static string CleanTextForMfa(string text)
        {
            // 1. Chuẩn hóa Unicode về NFC (Tránh bẫy tổ hợp dấu)
            text = text.Normalize(NormalizationForm.FormC);

            // 2. Chuyển toàn bộ về chữ thường
            text = text.ToLowerInvariant();

            // 3. Xóa mọi dấu câu (chỉ giữ lại chữ cái, số và khoảng trắng)
            // \w bao gồm tất cả các chữ cái có dấu của Tiếng Việt/Dao và số
            text = Regex.Replace(text, @"[^\w\s]", " ");

            // 4. Xóa khoảng trắng thừa
            text = Regex.Replace(text, @"\s+", " ").Trim();

            return text;
        }

        /// <summary>
        /// Tách từ thành các âm vị (Grapheme-to-Phoneme)
        /// </summary>
        public static string WordToPhonemes(string word)
        {
            // Danh sách phụ âm ghép tiếng Việt và ngoại lai (xếp từ dài đến ngắn)
            string[] consonantClusters = {
                "ngh",
                "str", "sch", "shr", "spl", "spr",
                "ch", "gh", "gi", "kh", "ng", "nh", "ph", "qu", "th", "tr",
                "dz", "br", "bl", "cl", "cr", "dr", "fl", "fr", "gl", "gr",
                "pl", "pr", "sc", "sh", "sk", "sl", "sm", "sn", "sp", "st", "sw"
            };

            string remaining = word;
            List<string> phonemes = new List<string>();

            while (remaining.Length > 0)
            {
                bool matched = false;
                foreach (string cluster in consonantClusters)
                {
                    if (remaining.StartsWith(cluster))
                    {
                        phonemes.Add(cluster);
                        remaining = remaining.Substring(cluster.Length);
                        matched = true;
                        break;
                    }
                }

                if (!matched)
                {
                    // Lấy ký tự đầu tiên
                    phonemes.Add(remaining[0].ToString());
                    remaining = remaining.Substring(1);
                }
            }

            return string.Join(" ", phonemes);
        }

        /// <summary>
        /// Hàm main thực thi toàn bộ quy trình
        /// </summary>
        public static void PrepareMfaDataset(string inputDir, string outputDir, string lexiconPath)
        {
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // Sử dụng HashSet để lưu trữ từ vựng duy nhất (tương đương set() trong Python)
            HashSet<string> uniqueWords = new HashSet<string>();

            Console.WriteLine($"Đang xử lý dữ liệu từ: {inputDir}...");

            // Cấu hình UTF-8 không có Byte Order Mark (BOM) để MFA đọc file không bị lỗi
            Encoding utf8WithoutBom = new UTF8Encoding(false);

            // Duyệt qua tất cả các file trong thư mục đầu vào
            foreach (string inputPath in Directory.GetFiles(inputDir))
            {
                string filename = Path.GetFileName(inputPath);
                string outputPath = Path.Combine(outputDir, filename);

                if (filename.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    // Đọc và làm sạch Text
                    string content = File.ReadAllText(inputPath, Encoding.UTF8);
                    string cleaned = CleanTextForMfa(content);

                    // Cập nhật kho từ vựng
                    string[] words = cleaned.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string word in words)
                    {
                        uniqueWords.Add(word);
                    }

                    // Ghi file Text sạch ra output
                    File.WriteAllText(outputPath, cleaned, utf8WithoutBom);
                }
                else if (filename.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                {
                    // Copy file Audio (.wav) sang output
                    File.Copy(inputPath, outputPath, overwrite: true);
                }
            }

            Console.WriteLine("Đang tạo file Lexicon...");

            // Tạo file Lexicon với các thẻ bắt buộc
            List<string> lexiconLines = new List<string> { "<SIL>\tSIL", "<UNK>\tSPN" };

            // OrderBy tương đương với sorted() của Python
            foreach (string word in uniqueWords.OrderBy(w => w))
            {
                string phonemes = WordToPhonemes(word);
                lexiconLines.Add($"{word}\t{phonemes}");
            }

            // Tạo thư mục chứa file lexicon nếu chưa có
            string lexiconDir = Path.GetDirectoryName(lexiconPath);
            if (!string.IsNullOrEmpty(lexiconDir) && !Directory.Exists(lexiconDir))
            {
                Directory.CreateDirectory(lexiconDir);
            }

            // Ghi file
            File.WriteAllLines(lexiconPath, lexiconLines, utf8WithoutBom);

            Console.WriteLine("\n" + new string('=', 50));
            Console.WriteLine("✅ THÀNH CÔNG! Đã dọn dẹp sạch sẽ dữ liệu.");
            Console.WriteLine($"📁 Dữ liệu sạch lưu tại : {outputDir}");
            Console.WriteLine($"📕 Lexicon ({uniqueWords.Count} từ) lưu tại : {lexiconPath}");
            Console.WriteLine(new string('=', 50));
            Console.WriteLine("🚀 BƯỚC TIẾP THEO: Hãy mở Terminal/CMD và chạy câu lệnh sau:\n");
            Console.WriteLine($"mfa align {outputDir} {lexiconPath} vietnamese_mfa ./output_textgrids --clean");
        }

    }
}
