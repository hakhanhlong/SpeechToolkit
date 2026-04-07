using Azure.Core;
using Google.GenAI.Types;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text;

namespace TextTranslateDataPreparingConsoleApplication
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            var handler = new HttpClientHandler();
            var httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMinutes(10) // Tăng lên 10 phút để thoải mái chạy local
            };


            // 1. Cấu hình cơ bản
            string inputFilePath = @"C:\AILAB\DATASET\GigaSpeech2\data\vithai_small\train_small_1.tsv";
            string outputFilePath = @"C:\AILAB\DATASET\GigaSpeech2\data\vithai_small\train_vithaismall.tsv";
            string targetLanguage = "dân tộc Thái (Thái đen)"; // Ngôn ngữ đích
            //string modelId = "gemma4:31b"; // Thay đổi theo model bạn đang chạy trên Ollama
            string modelId = "translategemma:27b"; // Thay đổi theo model bạn đang chạy trên Ollama
            string ollamaEndpoint = "http://localhost:11434/v1"; // Endpoint tương thích chuẩn OpenAI của Ollama

            // Tạo file input mẫu nếu chưa tồn tại
            if (!System.IO.File.Exists(inputFilePath))
            {
                await System.IO.File.WriteAllLinesAsync(inputFilePath, new[] {
                    "Hello, how are you today?",
                    "Artificial Intelligence is changing the world.",
                    "Semantic Kernel makes building AI agents very easy."
                });
                Console.WriteLine($"[Info] Created sample {inputFilePath}");
            }

            // 2. Khởi tạo Semantic Kernel
            // Sử dụng OpenAIChatCompletion trỏ về local Ollama để đảm bảo tính ổn định cao nhất
            var builder = Kernel.CreateBuilder();
            builder.AddOpenAIChatCompletion(
                modelId: modelId,
                apiKey: "no-key-required", // Ollama không yêu cầu API Key
                endpoint: new Uri(ollamaEndpoint),
                httpClient: httpClient
            );


            //builder.AddGoogleAIGeminiChatCompletion("gemma-4-31b-it", apiKey: "AIzaSyAjRuYwTxy2GWdUsn8Qo1aFP796KXXQIJw", httpClient: httpClient);
            var kernel = builder.Build();

            // 3. Khai báo Prompt template
            //            string promptTemplate = @"
            //You are an expert linguist and professional translator.
            //Translate the following text into {{$targetLanguage}}.

            //Rules:
            //1. Return ONLY the completely translated text.
            //2. DO NOT add any explanations, notes, or conversational filler.
            //3. Maintain the original tone, context, and formatting of the text.
            //4. If the text is empty or un-translatable, return it exactly as is.

            //Text to translate:
            //{{$input}}";

            //string promptTemplate = @"
            //    You are a professional Vietnamese (vi) to {{$targetLanguage}} translator. 
            //    Your goal is to accurately convey the meaning and nuances of the original Vietnamese text while adhering to dân tộc Thái (Thái đen) grammar, vocabulary, and cultural sensitivities.
            //    Produce only the dân tộc Thái (Thái đen) translation, without any additional explanations or commentary. Please translate the following Vietnamese text into dân tộc Thái (Thái đen):

            //    {{$input}}

            //    Rules:
            //    1. Return ONLY the completely translated text.
            //    2. DO NOT add any explanations, notes, or conversational filler.
            //    3. Maintain the original tone, context, content and formatting of the text.
            //    4. If the text is empty or un-translatable, return it exactly as is.
            //";

            string promptTemplate = @"
                Bạn là một dịch giả chuyên nghiệp Tiếng Việt (vi) sang tiếng  dân tộc Thái (Thái Đen). Mục tiêu của bạn là truyền tải chính xác ý nghĩa và sắc thái của văn bản gốc tiếng Việt, 
                đồng thời tuân thủ ngữ pháp, từ vựng và sự nhạy cảm về văn hóa của tiếng  dân tộc Thái (Thái Đen).

                Chỉ dịch sang tiếng dân tộc Thái (Thái Đen), không kèm theo bất kỳ lời giải thích hoặc bình luận nào. Vui lòng dịch đoạn văn Tiếng Việt sau sang tiếng dân tộc Thái (Thái Đen):

                {{$input}}

                Quy tắc:
                1. Chỉ trả về văn bản đã được dịch hoàn chỉnh.
                2. KHÔNG thêm bất kỳ lời giải thích, ghi chú hoặc đoạn hội thoại nào.
                3. Giữ nguyên giọng điệu, ngữ cảnh, nội dung và định dạng gốc của văn bản.
                4. Nếu văn bản trống hoặc không thể dịch được, hãy trả về chính xác như vậy.
                5. Không phải dịch sang tiếng Thái Lan hay Thái cổ
            ";

            //string promptTemplate = @"
            //    Dịch sang ngôn ngữ dân tộc Thái (Thái đen): {{$input}}
            //";






            var translateFunction = kernel.CreateFunctionFromPrompt(promptTemplate);

            Console.WriteLine($"[Info] Starting translation to {targetLanguage} using model '{modelId}'...");

            // 4. Đọc, xử lý và ghi file từng dòng (Stream processing)
            try
            {
                using var reader = new StreamReader(inputFilePath);
                using var writer = new StreamWriter(outputFilePath, append: false);

                string? line;
                int lineNumber = 1;

                while ((line = await reader.ReadLineAsync()) != null)
                {                    
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        await writer.WriteLineAsync(); // Giữ nguyên dòng trống
                        continue;
                    }

                    Console.Write($"Translating line {lineNumber}... ");
                    //var executionSettings = new OpenAIPromptExecutionSettings
                    //{
                    //    Temperature = 0.0, // Ép tính nhất quán, giảm khả năng tự sinh thinking dài dòng
                    //    TopP = 0.1
                    //};
                    //// Gọi Semantic Kernel
                    //var arguments = new KernelArguments(executionSettings)
                    //{
                    //    { "input", line },
                    //    //{ "targetLanguage", targetLanguage }
                    //};

                    var kernelArguments = new KernelArguments(new GeminiPromptExecutionSettings
                    {
                        //ThinkingConfig = new GeminiThinkingConfig
                        //{
                        //    ThinkingLevel = "high"
                        //},
                        Temperature = 0.2,
                        ToolCallBehavior = null

                    })
                    {
                        { "input", line },
                        { "targetLanguage", targetLanguage }
                    };


                    var result = await kernel.InvokeAsync(translateFunction, kernelArguments);
                    string translatedText = result.GetValue<string>()?.Trim() ?? "NO_VALUE";

                    // Ghi kết quả vào file output
                    await writer.WriteLineAsync(translatedText.ToUpper());

                    Console.WriteLine($"Done. {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")} => {translatedText.ToUpper()}");
                    lineNumber++;
                }

                Console.WriteLine($"\n[Success] Translation completed. Results saved to {outputFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[Error] An error occurred: {ex.Message}");
            }
        }
    }
}
