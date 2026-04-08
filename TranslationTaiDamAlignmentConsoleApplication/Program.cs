using BlingFire;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TranslationTaiDamAlignmentConsoleApplication
{
    // 1. Cấu trúc dữ liệu
    public class InputSentence
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("tai_dam")]
        public string TaiDamText { get; set; }
    }

    public class AlignedTranslation
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("tai_dam")]
        public string TaiDam { get; set; }

        [JsonPropertyName("english")]
        public string English { get; set; }

        [JsonPropertyName("vietnamese")]
        public string Vietnamese { get; set; }
    }

    class Program
    {
        // Cấu hình tham số
        const int BATCH_SIZE = 2; // Xử lý 15 câu mỗi lần gọi API
        const int MAX_RETRIES = 3; // Thử lại tối đa 3 lần nếu LLM trả về lỗi JSON
        const string OUTPUT_CSV_PATH = "TaiDam_Parallel_Dataset.csv";

       
        

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            //string modelId = "translategemma:27b"; // Thay đổi theo model bạn đang chạy trên Ollama
            string modelId = "translategemma:27b"; // Thay đổi theo model bạn đang chạy trên Ollama
            string ollamaEndpoint = "http://localhost:11434/v1"; // Endpoint tương thích chuẩn OpenAI của Ollama

            var handler = new HttpClientHandler();
            var httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMinutes(10) // Tăng lên 10 phút để thoải mái chạy local
            };

            var builder = Kernel.CreateBuilder();

            
            builder.AddOpenAIChatCompletion(
                modelId: modelId,
                apiKey: "no-key-required", // Ollama không yêu cầu API Key
                endpoint: new Uri(ollamaEndpoint),
                httpClient: httpClient
            );

            var kernel = builder.Build();

            // Giả lập dữ liệu đầu vào (Trong thực tế bạn sẽ đọc từ file txt/db)
            //var inputSentences = GenerateSampleData(50);

            string longText = """
                Luật xỏm pành, tứm ten san điều khòng Luật BHYT, mi 11 mú côn đảy Quỹ BHYT khừn 100% ngơn pày khám dà bệnh nặp té mự 1/1/2026, chảnh thí xương lằng: Cốc khoẹ lỏ sĩ quàn quần đội dần mương Việt Nàm, quần nhần chuyền nghiệp đàng dệt vịa; sĩ quàn, hạ sĩ quàn chuyền nghiệp cánh sĩ quàn, hạ sĩ quàn chuyền mồn kỹ thuật đàng dệt vịa cuồng chủm mú cồng àn dần mương; côn dệt nả vịa cơ yếu hưởng lường xương quần nhần. Thứ 2, sĩ quàn, hạ sĩ quàn quần đội dần mương đàng dệt vịa; hạ sĩ quàn, chiến sĩ nghĩa vụ cồng àn dần mương; học viền quần đội, học viền cồng àn, học viền cơ yếu đảy hưởng ngơn choi dừa vịa kìn dú lỏ côn Việt Nàm. Thứ 3, lỏ học viền quần đội, học viền cồng àn, học viền cơ yếu đảy hưởng ngơn choi dừa vịa kìn dú lỏ côn mương nọk. Thứ 4, lỏ học viền ép sĩ quàn dự bị té 3 bườn táo khửn hê chôm hặp BHXH, BHYT. Thứ 5, lỏ dần quần thường trực. Thứ 6, lỏ Côn mi cồng cắp cách mạng toi luông tặt pùn khòng Pháp lệnh nhọng nho côn mi cồng cắp cách mạng, cựu chiến bình. Thứ 7, lỏ làn nọi cỏng 6 pì. Thứ 8, lỏ tay hươn liệt sĩ, côn mi cồng liệng đù liệt sĩ toi luông tặt pùn khòng Pháp lệnh nhọng nho côn mi cồng cắp cách mạng. Thứ 9, lỏ chua hươn cặt, pi nọng chựa nọi côn lỏ chua hươn áo cặt đàng kìn dú nẳng pưng xã, bản phổng pi nọng chựa nọi côn cánh phổng xùng đìn pu, pi nọng chựa nọi côn đàng kìn dú nẳng phổng kình tế xã hội nhăng pọ lài dạk cha, côn dần đàng kìn dú nẳng pưng xã đòn bể. Thứ 10, lỏ côn đàng đảy hưởng ngơn choi dừa hạng bườn; côn đàng đảy hưởng ngơn choi dừa liệng đù hạng bườn toi luông tặt pùn khòng pháp luật mi nuống pan; côn đàng đảy hưởng ngơn dệt lang hạng bườn lẹo dú cuồng mú côn đảy hưởng luông choi dừa khòng bản mương. Thứ 11, lỏ côn thảu ké té tục 75 pì táo khửn đàng đảy hưởng ngơn choi dừa chơ lộm tài hạng bườn; côn tục té 70 – 75 pì lỏ chua hươn cặt cánh đàng đảy hưởng ngơn choi dừa chơ lộm tài hạng bườn. Cắp vịa nho xùng mức choi dừa, khày quảng mú côn đảy hưởng, chính sách BHYT té pì 2026 cọm pày xú vịa dón xìa ngơn tiên khòng côn ma khám dà bệnh. Luông pùn tặt ók lỏ dón số ngơn tiên khám dà bệnh khòng côn dần lông nhăng cỏng 30% cuồng dan té pì 2028 – 2030. Toi luông nhẳn danh khòng Quốc hội cánh chínhphủ, vịa khày quảng quyên lợi BHYT té pì 2026 lỏ bón pâng hẳư dan hầng hy mưa nả; nghiền cứu xỏm pành mức nộp BHYT chọp xồm cắp khù ngai đì khòng côn dần, xòng ma xứp tàm nho xùng mức đảy choi dừa; khày quảng tứm pưng tàng dà, kỹ thuật; dệt thử bấng khừn ngơn dịch vụ hỏng non dà bệnh, khám nhẳn danh pưng tàng bệnh lâng pọ, hựt họt vịa tênh lài dần mương chôm hặp BHYT toi luông tẹt tiêng.
                """;

            var inputSentences = SplitIntoSentences(longText);

            var finalResults = new List<AlignedTranslation>();

            Console.WriteLine($"Bắt đầu xử lý tổng cộng {inputSentences.Count} câu...");

            // 2. Chia lô dữ liệu (Batching)
            var batches = inputSentences.Chunk(BATCH_SIZE).ToList();

            for (int i = 0; i < batches.Count; i++)
            {
                Console.WriteLine($"\nĐang xử lý Lô {i + 1}/{batches.Count} ({batches[i].Length} câu)...");

                var batchResult = await ProcessBatchWithRetryAsync(kernel, batches[i].ToList());

                if (batchResult != null && batchResult.Count > 0)
                {
                    finalResults.AddRange(batchResult);
                    Console.WriteLine($"✅ Hoàn thành Lô {i + 1}. Tổng số câu đã dịch: {finalResults.Count}");
                }
                else
                {
                    Console.WriteLine($"❌ Lô {i + 1} thất bại hoàn toàn sau {MAX_RETRIES} lần thử.");
                }

                // Nghỉ một chút giữa các batch để tránh lỗi Rate Limit (429 Too Many Requests)
                await Task.Delay(2000);
            }

            // 3. Xuất kết quả ra file CSV
            ExportToCsv(finalResults, OUTPUT_CSV_PATH);
        }

        /// <summary>
        /// Xử lý một lô dữ liệu và có cơ chế Retry tự động nếu JSON bị lỗi
        /// </summary>
        static async Task<List<AlignedTranslation>> ProcessBatchWithRetryAsync(Kernel kernel, List<InputSentence> batch)
        {
            string jsonInput = JsonSerializer.Serialize(batch);
            string promptTemplate = @"
Bạn là chuyên gia ngôn ngữ Tai Dam (Thái Đen), Tiếng Anh và Tiếng Việt.
Dịch danh sách các câu tiếng Tai Dam sau sang Tiếng Anh, sau đó dịch từ bản Tiếng Anh sang Tiếng Việt để sát nghĩa nhất.

Dữ liệu đầu vào:
{{$input}}

YÊU CẦU BẮT BUỘC:
- Trả về DUY NHẤT một mảng JSON hợp lệ.
- Không có text giải thích, không dùng markdown ```json.
- Giữ nguyên không thay đổi các câu tiếng Tai Dam (Thái Đen)
- Mỗi object có 4 keys: ""id"", ""tai_dam"", ""english"", ""vietnamese"".
";

            var executionSettings = new OpenAIPromptExecutionSettings
            {
                Temperature = 1.0, // Ép tính nhất quán, giảm khả năng tự sinh thinking dài dòng
                TopP = 0.95
            };

            var arguments = new KernelArguments(executionSettings) { { "input", jsonInput } };

            for (int attempt = 1; attempt <= MAX_RETRIES; attempt++)
            {
                try
                {
                    var result = await kernel.InvokePromptAsync(promptTemplate, arguments);
                    string responseText = result.ToString().Trim();

                    // Dọn dẹp chuỗi trả về
                    if (responseText.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
                    {
                        responseText = responseText.Replace("```json", "").Replace("```", "").Trim();
                    }

                    var alignedResults = JsonSerializer.Deserialize<List<AlignedTranslation>>(responseText);

                    // Nếu parse thành công và số lượng khớp, trả về ngay
                    if (alignedResults != null && alignedResults.Count == batch.Count)
                    {
                        return alignedResults;
                    }
                }
                catch (JsonException)
                {
                    Console.WriteLine($"   [Cảnh báo] Lỗi parse JSON ở lần thử {attempt}/{MAX_RETRIES}. LLM trả về sai định dạng. Đang thử lại...");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   [Cảnh báo] Lỗi kết nối ở lần thử {attempt}: {ex.Message}");
                }
            }

            return new List<AlignedTranslation>(); // Trả về list rỗng nếu thất bại hoàn toàn
        }

        /// <summary>
        /// Xuất danh sách ra file CSV chuẩn, xử lý an toàn các ký tự đặc biệt
        /// </summary>
        static void ExportToCsv(List<AlignedTranslation> data, string filePath)
        {
            Console.WriteLine($"\nĐang xuất dữ liệu ra file: {filePath}...");

            // Sử dụng UTF8 có BOM để Excel mở không bị lỗi font tiếng Việt/Tai Dam
            using (var writer = new StreamWriter(filePath, false, new UTF8Encoding(true)))
            {
                // Viết Header
                writer.WriteLine("ID,TaiDam,English,Vietnamese");

                foreach (var item in data)
                {
                    var id = EscapeCsvField(item.Id);
                    var taiDam = EscapeCsvField(item.TaiDam);
                    var english = EscapeCsvField(item.English);
                    var vietnamese = EscapeCsvField(item.Vietnamese);

                    writer.WriteLine($"{id},{taiDam},{english},{vietnamese}");
                }
            }
            Console.WriteLine("✅ Xuất CSV thành công!");
        }

        /// <summary>
        /// Hàm bọc nội dung CSV an toàn (Xử lý dấu phẩy, ngoặc kép, xuống dòng trong câu dịch)
        /// </summary>
        static string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field)) return "";

            // Nếu chuỗi có chứa dấu phẩy, ngoặc kép hoặc ký tự xuống dòng
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
            {
                // Nhân đôi dấu ngoặc kép hiện có
                field = field.Replace("\"", "\"\"");
                // Bọc toàn bộ chuỗi trong ngoặc kép
                field = $"\"{field}\"";
            }
            return field;
        }

        /// <summary>
        /// Tạo dữ liệu giả lập để test
        /// </summary>
        static List<InputSentence> GenerateSampleData(int count)
        {
            var list = new List<InputSentence>();
            for (int i = 1; i <= count; i++)
            {
                list.Add(new InputSentence
                {
                    Id = $"TD_{i:D4}",
                    TaiDamText = $"Câu tiếng Thái đen mẫu số {i}"
                });
            }
            return list;
        }

        static List<InputSentence> SplitIntoSentences(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new List<InputSentence>();

            //// 1. Tiền xử lý: Xóa bớt khoảng trắng thừa và ký tự xuống dòng lộn xộn
            //text = Regex.Replace(text, @"\s+", " ").Trim();

            //// 2. Danh sách các từ viết tắt phổ biến trong Tiếng Việt không được cắt
            //// Thêm các từ viết tắt tiếng Thái Đen vào đây nếu có
            //string[] abbreviations = { "TP", "GS", "TS", "ThS", "PGS", "BS", "Mr", "Mrs", "v.v", "St", "Q", "H", "P" };

            //// Chuyển danh sách thành chuỗi regex (vd: TP|GS|TS)
            //string abbrevPattern = string.Join("|", abbreviations);

            //// 3. Mẫu Regex tách câu thần thánh:
            //// Giải thích:
            //// (?<!\b(?:TP|GS|TS|...)) : Phía trước KHÔNG PHẢI là từ viết tắt
            //// (?<=[.!?]+[""']?)       : Phía trước LÀ dấu câu (.!?) có thể kèm dấu nháy đóng (")
            //// \s+                     : Theo sau là một hoặc nhiều khoảng trắng
            //// (?=[\p{Lu}\p{M}\"\'\-]) : Phía sau LÀ một chữ cái viết hoa (\p{Lu}), hoặc dấu nháy, dấu gạch ngang
            //string pattern = $@"(?<!\b(?:{abbrevPattern}))(?<=[.!?]+[""']?)\s+(?=[\p{{Lu}}\p{{M}}\""\'\-])";

            //// 4. Thực hiện tách câu
            //var rawSentences = Regex.Split(text, pattern);

            // BlingFire có hàm GetSentences cực kỳ tối ưu
            // Nó sử dụng một mô hình máy học nhỏ (State Machine) bên dưới để nhận diện ranh giới câu
            string[] rawSentences = BlingFireUtils.GetSentences(text).ToArray();

            // 5. Làm sạch kết quả
            var result = rawSentences
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .ToList();

            var list = new List<InputSentence>();
            for (int i = 0; i < result.Count; i++)
            {
                list.Add(new InputSentence
                {
                    Id = $"TD_{i:D4}",
                    TaiDamText = $"{result[i]}"
                });
            }
            return list;
        }

    }
}
