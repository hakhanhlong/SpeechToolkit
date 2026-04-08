using NAudio.Wave;

namespace SpeechAudioNoiseGateConsoleApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            // ================= CẤU HÌNH =================
            string inputFolder = @"/mnt/d/SPEECH_DATAPREPARING/audio_1_clean";
            string outputFolder = @"/mnt/d/SPEECH_DATAPREPARING/audio_1_clean/thu_muc_da_cat_sach_tieng_on";

            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            // Duyệt qua tất cả các file .wav để lọc tiếng
            string[] wavFiles = Directory.GetFiles(inputFolder, "*.wav");

            foreach (var inPath in wavFiles)
            {
                string filename = Path.GetFileName(inPath);
                string outPath = Path.Combine(outputFolder, filename);

                // Ngưỡng -35 dBFS: Số càng âm (vd -45) thì cổng càng dễ dãi. 
                // Số càng gần 0 (vd -20) thì cổng càng khắt khe, chỉ tiếng hét thật to mới lọt qua.
                ApplyNoiseGate(inPath, outPath, thresholdDbfs: -28, chunkSizeMs: 10);
            }

            Console.WriteLine("✨ Đã xử lý xong toàn bộ thư mục!");
        }

        /// <summary>
        /// Quét qua file audio, đoạn nào có âm lượng nhỏ hơn thresholdDbfs 
        /// sẽ bị ép thành khoảng lặng tuyệt đối (0 âm thanh).
        /// Giữ nguyên độ dài tổng thể của file gốc.
        /// </summary>
        public static void ApplyNoiseGate(string inputWav, string outputWav, double thresholdDbfs = -35, int chunkSizeMs = 10)
        {
            Console.WriteLine($"Đang xử lý file: {Path.GetFileName(inputWav)}...");

            try
            {
                // AudioFileReader tự động chuẩn hóa mọi giá trị âm thanh về kiểu float (từ -1.0 đến 1.0)
                using (var reader = new AudioFileReader(inputWav))
                {
                    // Giữ nguyên sample rate và channels, nhưng chuyển về định dạng 16-bit tiêu chuẩn khi xuất file
                    var outFormat = new WaveFormat(reader.WaveFormat.SampleRate, 16, reader.WaveFormat.Channels);

                    using (var writer = new WaveFileWriter(outputWav, outFormat))
                    {
                        // Tính toán số lượng mẫu (samples) cho mỗi khoảng thời gian cắt (chunk)
                        int samplesPerChunk = (int)(reader.WaveFormat.SampleRate * reader.WaveFormat.Channels * (chunkSizeMs / 1000.0));
                        float[] buffer = new float[samplesPerChunk];
                        int samplesRead;

                        // Đọc file theo từng chunk nhỏ
                        while ((samplesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            // 1. Tính toán RMS (Root Mean Square) để đo mức năng lượng của đoạn âm thanh
                            double sumSquares = 0;
                            for (int i = 0; i < samplesRead; i++)
                            {
                                sumSquares += buffer[i] * buffer[i];
                            }

                            double rms = Math.Sqrt(sumSquares / samplesRead);

                            // 2. Chuyển đổi RMS sang dBFS (Decibels relative to Full Scale)
                            // Tránh lỗi Log10(0) khi rms = 0 bằng cách gán sẵn bằng mức âm vô cực (gần im lặng)
                            double dbfs = rms > 0 ? 20 * Math.Log10(rms) : -100.0;

                            // 3. Kiểm tra cường độ âm thanh
                            if (dbfs < thresholdDbfs)
                            {
                                // Coi là tiếng ồn/thở -> Xóa toàn bộ dữ liệu trong buffer (ép thành số 0 - im lặng tuyệt đối)
                                Array.Clear(buffer, 0, samplesRead);
                            }

                            // 4. Ghi buffer (đã giữ nguyên hoặc đã làm im lặng) vào file xuất
                            // Phương thức WriteSamples sẽ tự động convert mảng float ngược về 16-bit theo cấu hình của 'outFormat'
                            writer.WriteSamples(buffer, 0, samplesRead);
                        }
                    }
                }
                Console.WriteLine($"✅ Đã lưu file làm sạch tại: {outputWav}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi khi xử lý file {Path.GetFileName(inputWav)}: {ex.Message}");
            }
        }
    }
}
