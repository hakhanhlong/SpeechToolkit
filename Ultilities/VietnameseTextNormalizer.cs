using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ultilities
{
    public static class VietnameseTextNormalizer
    {
        
      
            // 1. Bộ từ điển chuẩn hóa vị trí dấu thanh
        private static readonly Dictionary<string, string> ToneNormalizationMap = new Dictionary<string, string>
        {
            {"oà", "òa"}, {"oá", "óa"}, {"oả", "ỏa"}, {"oã", "õa"}, {"oạ", "ọa"},
            {"oè", "òe"}, {"oé", "óe"}, {"oẻ", "ỏe"}, {"oẽ", "õe"}, {"oẹ", "ọe"},
            {"uỳ", "ùy"}, {"uý", "úy"}, {"uỷ", "ủy"}, {"uỹ", "ũy"}, {"uỵ", "ụy"},
            {"Oà", "Òa"}, {"Oá", "Óa"}, {"Oả", "Ỏa"}, {"Oã", "Õa"}, {"Oạ", "Ọa"},
            {"Oè", "Òe"}, {"Oé", "Óe"}, {"Oẻ", "Ỏe"}, {"Oẽ", "Õe"}, {"Oẹ", "Ọe"},
            {"Uỳ", "Ùy"}, {"Uý", "Úy"}, {"Uỷ", "Ủy"}, {"Uỹ", "Ũy"}, {"Uỵ", "Ụy"}
        };

            // 2. Bộ từ điển viết tắt (Có thể cấu hình thêm)
        private static readonly Dictionary<string, string> Abbreviations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"THPT", "Trung học Phổ thông"},
            {"THCS", "Trung học Cơ sở"},
            {"UBND", "Ủy ban nhân dân"},
            {"HĐND", "Hội đồng nhân dân"},
            {"TW", "Trung ương"},
            {"TP", "Thành phố"},
            {"VN", "Việt Nam"},
            {"MTTQ","Mặt trận tổ quốc"},
            // ==========================================
            // TỔ CHỨC CHÍNH TRỊ - HÀNH CHÍNH - NHÀ NƯỚC
            // ==========================================
            {"CHXHCNVN", "Cộng hòa Xã hội chủ nghĩa Việt Nam"},
            {"ĐCS", "Đảng Cộng sản"},
            {"BCH", "Ban Chấp hành"},
            {"BCT", "Bộ Chính trị"}, // Lưu ý: Đôi khi có nghĩa là Bộ Công Thương tùy ngữ cảnh
            {"BBT", "Ban Bí thư"},
            {"ĐBQH", "Đại biểu Quốc hội"},
            {"QĐND", "Quân đội nhân dân"},
            {"CAND", "Công an nhân dân"},
            {"CSGT", "Cảnh sát giao thông"},
            {"PCCC", "Phòng cháy chữa cháy"},
            {"BHXH", "Bảo hiểm xã hội"},
            {"BHYT", "Bảo hiểm y tế"},
            {"BHTN", "Bảo hiểm thất nghiệp"},

            // ==========================================
            // CÁC BỘ, BAN, NGÀNH
            // ==========================================
            {"BQP", "Bộ Quốc phòng"},
            {"BCA", "Bộ Công an"},
            {"BNG", "Bộ Ngoại giao"},
            //{"BTC", "Bộ Tài chính"},
            {"BTP", "Bộ Tư pháp"},
            {"BKHĐT", "Bộ Kế hoạch và Đầu tư"},
            {"BYT", "Bộ Y tế"},
            {"GDĐT", "Giáo dục và Đào tạo"}, // Trên báo chí thường viết liền hoặc có dấu &
            {"GD&ĐT", "Giáo dục và Đào tạo"},
            {"GTVT", "Giao thông Vận tải"},
            {"NNPTNT", "Nông nghiệp và Phát triển nông thôn"},
            {"NN&PTNT", "Nông nghiệp và Phát triển nông thôn"},
            {"LĐTBXH", "Lao động, Thương binh và Xã hội"},
            {"LĐTB&XH", "Lao động, Thương binh và Xã hội"},
            {"VHTTDL", "Văn hóa, Thể thao và Du lịch"},
            {"TTTT", "Thông tin và Truyền thông"},
            {"TNMT", "Tài nguyên và Môi trường"},
            {"TN&MT", "Tài nguyên và Môi trường"},
            {"NHNN", "Ngân hàng Nhà nước"},
            {"TAND", "Tòa án nhân dân"},
            {"VKSND", "Viện kiểm sát nhân dân"},

            // ==========================================
            // GIÁO DỤC VÀ ĐÀO TẠO
            // ==========================================
            // {"TH", "Tiểu học"}, // Rất dễ nhầm với Truyền hình, nên cân nhắc khi dùng tự động
            {"GDTX", "Giáo dục Thường xuyên"},
            {"ĐH", "Đại học"},
            {"CĐ", "Cao đẳng"},
            {"TC", "Trung cấp"},
            {"ThS", "Thạc sĩ"},
            {"TS", "Tiến sĩ"},
            {"PGS", "Phó Giáo sư"},
            {"GS", "Giáo sư"},
            {"SV", "Sinh viên"},
            {"HS", "Học sinh"},
            {"HSSV", "Học sinh sinh viên"},
            {"BGH", "Ban Giám hiệu"},

            // ==========================================
            // KINH TẾ - KINH DOANH - DOANH NGHIỆP
            // ==========================================
            {"CTCP", "Công ty Cổ phần"},
            {"TNHH", "Trách nhiệm hữu hạn"},
            {"MTV", "Một thành viên"},
            {"DNNN", "Doanh nghiệp Nhà nước"},
            {"HTX", "Hợp tác xã"},
            {"KCN", "Khu công nghiệp"},
            {"KCX", "Khu chế xuất"},
            {"KKT", "Khu kinh tế"},
            {"BĐS", "Bất động sản"},
            {"TMCP", "Thương mại cổ phần"},
            {"XNK", "Xuất nhập khẩu"},
            {"FDI", "Đầu tư trực tiếp nước ngoài"},
            {"GDP", "Tổng sản phẩm quốc nội"},
            {"CPI", "Chỉ số giá tiêu dùng"},

            // ==========================================
            // Y TẾ - SỨC KHỎE
            // ==========================================
            {"BV", "Bệnh viện"},
            {"BS", "Bác sĩ"},
            {"PK", "Phòng khám"},
            {"TYT", "Trạm y tế"},
            {"TBYT", "Thiết bị y tế"},
            {"VSATTP", "Vệ sinh an toàn thực phẩm"},
            {"WHO", "Tổ chức Y tế Thế giới"},

            // ==========================================
            // TỔ CHỨC ĐOÀN THỂ - XÃ HỘI
            // ==========================================
            {"ĐTN", "Đoàn Thanh niên"},
            {"HLHPN", "Hội Liên hiệp Phụ nữ"},
            {"TLĐLĐ", "Tổng Liên đoàn Lao động"},
            {"HCTĐ", "Hội Chữ thập đỏ"},
            {"HND", "Hội Nông dân"},
            {"CCB", "Cựu chiến binh"},

            // ==========================================
            // ĐƠN VỊ HÀNH CHÍNH & ĐỊA DANH
            // ==========================================
            {"TX", "Thị xã"},
            //{"TT", "Thị trấn"},
            {"TPHCM", "Thành phố Hồ Chí Minh"},
            {"TP.HCM", "Thành phố Hồ Chí Minh"},
            {"HN", "Hà Nội"},
            {"ĐBSCL", "Đồng bằng sông Cửu Long"},
            {"ĐB", "Đồng bằng"},
            {"KTTĐ", "Kinh tế trọng điểm"},

            // ==========================================
            // VĂN BẢN PHÁP QUY
            // ==========================================
            {"NĐ", "Nghị định"},
            {"NQ", "Nghị quyết"},
            {"TT", "Thông tư"}, // Dễ nhầm với Thị trấn, tùy data của bạn để cấu hình
            {"QĐ", "Quyết định"},
            {"CV", "Công văn"},
            {"CH", "Cộng hòa"},
            {"XHCN", "Xã hội chủ nghĩa"},

            // ==========================================
            // KHOA HỌC - CÔNG NGHỆ - KHÁC
            // ==========================================
            {"CNTT", "Công nghệ thông tin"},                        
            {"AI", "Trí tuệ nhân tạo"},
            {"P.", "Phường"}, // Các từ có dấu chấm thường gặp
            {"Q.", "Quận"},
            {"H.", "Huyện"},
            {"X.", "Xã"},

            // ==========================================
            // TÀI CHÍNH - NGÂN HÀNG - CHỨNG KHOÁN
            // ==========================================
            {"NHTM", "Ngân hàng thương mại"},
            {"NHTMCP", "Ngân hàng thương mại cổ phần"},
            {"HĐQT", "Hội đồng quản trị"},
            {"ĐHĐCĐ", "Đại hội đồng cổ đông"},
            {"BCTC", "Báo cáo tài chính"},
            {"LNTT", "Lợi nhuận trước thuế"},
            {"LNST", "Lợi nhuận sau thuế"},
            {"TCTD", "Tổ chức tín dụng"},
            {"DNNVV", "Doanh nghiệp nhỏ và vừa"},
            {"SMEs", "Doanh nghiệp nhỏ và vừa"},
            {"CSH", "Chủ sở hữu"},
            {"CK", "Chứng khoán"},
            {"UBCKNN", "Ủy ban Chứng khoán Nhà nước"},
            {"GDCK", "Giao dịch chứng khoán"},

            // ==========================================
            // AN NINH - PHÁP LUẬT - TƯ PHÁP
            // ==========================================
            {"CQCSĐT", "Cơ quan Cảnh sát điều tra"},
            {"HĐXX", "Hội đồng xét xử"},
            {"BLHS", "Bộ luật Hình sự"},
            {"BLDS", "Bộ luật Dân sự"},
            {"CSKV", "Cảnh sát khu vực"},
            {"CSHS", "Cảnh sát hình sự"},
            {"CSKT", "Cảnh sát kinh tế"},
            {"ANM", "An ninh mạng"},
            {"GPLX", "Giấy phép lái xe"},
            {"CMND", "Chứng minh nhân dân"},
            {"CCCD", "Căn cước công dân"},
            {"THA", "Thi hành án"},

            // ==========================================
            // GIAO THÔNG - XÂY DỰNG - BẤT ĐỘNG SẢN
            // ==========================================
            {"GPMB", "Giải phóng mặt bằng"},
            {"KĐT", "Khu đô thị"},
            {"KDC", "Khu dân cư"},
            {"NƠXH", "Nhà ở xã hội"},
            {"CC", "Chung cư"},
            {"QL", "Quốc lộ"},
            {"TL", "Tỉnh lộ"},
            {"BOT", "Trạm thu phí Bê Ô Tê"}, // Đọc theo phiên âm tiếng Việt trên đài
            {"PTGT", "Phương tiện giao thông"},
            {"ĐS", "Đường sắt"},
            {"ĐSCT", "Đường sắt cao tốc"},

            // ==========================================
            // Y TẾ - GIÁO DỤC (Mở rộng)
            // ==========================================
            {"KCB", "Khám chữa bệnh"},
            {"TTYT", "Trung tâm y tế"},
            {"HSCC", "Hồi sức cấp cứu"},
            {"BGDĐT", "Bộ Giáo dục và Đào tạo"},
            {"KTX", "Ký túc xá"},
            {"TKB", "Thời khóa biểu"},
            {"GV", "Giáo viên"},
            {"GVCN", "Giáo viên chủ nhiệm"},
            {"HK", "Học kỳ"},
            {"NCS", "Nghiên cứu sinh"},
            {"PGS.TS", "Phó Giáo sư Tiến sĩ"}, // Thường xuất hiện liền nhau
            {"GS.TS", "Giáo sư Tiến sĩ"},

            // ==========================================
            // VĂN HÓA - XÃ HỘI - TRUYỀN THÔNG
            // ==========================================
            {"MXH", "Mạng xã hội"},
            {"CLB", "Câu lạc bộ"},
            {"BTC", "Ban tổ chức"},
            {"BGK", "Ban giám khảo"},
            {"NXB", "Nhà xuất bản"},
            {"BTV", "Biên tập viên"},
            {"KTV", "Kỹ thuật viên"},
            {"PTV", "Phát thanh viên"},
            {"TTXVN", "Thông tấn xã Việt Nam"},
            {"ĐTHVN", "Đài Truyền hình Việt Nam"},
            {"VOV", "Đài Tiếng nói Việt Nam"},
            {"VTV", "Đài Truyền hình Việt Nam"},
            {"UBMTTQ", "Ủy ban Mặt trận Tổ quốc"},

            // ==========================================
            // TỪ VIẾT TẮT THÔNG DỤNG KHÁC
            // ==========================================
            {"STT", "Số thứ tự"},
            {"SĐT", "Số điện thoại"},
            {"TDP", "Tổ dân phố"},
            {"KP", "Khu phố"},
            {"CSKH", "Chăm sóc khách hàng"},
            {"ATTT", "An toàn thông tin"},
            {"CSDL", "Cơ sở dữ liệu"},
            {"Kính gửi", "K/g"},
            {"CN", "Công nhân"},

            // ==========================================
            // CÔNG NGHỆ - KỸ THUẬT - SỐ HÓA
            // ==========================================
            
            {"KHCN", "Khoa học Công nghệ"},
            {"KHKT", "Khoa học Kỹ thuật"},
            {"CĐS", "Chuyển đổi số"},
            {"TMĐT", "Thương mại điện tử"},
            {"HTTT", "Hệ thống thông tin"},
            {"PTTM", "Phân tích tỉ mỉ"},
            {"ĐTVT", "Điện tử Viễn thông"},

            // ==========================================
            // NÔNG NGHIỆP - MÔI TRƯỜNG
            // ==========================================
            
            {"PCTT", "Phòng chống thiên tai"},
            {"TKNL", "Tiết kiệm năng lượng"},
            {"BĐKH", "Biến đổi khí hậu"},
            {"BVMT", "Bảo vệ môi trường"},
            {"Hợp tác xã", "HTX"},

            // ==========================================
            // THỂ THAO - GIẢI TRÍ
            // ==========================================
            {"VĐV", "Vận động viên"},
            {"HLV", "Huấn luyện viên"},
            {"LĐBĐ", "Liên đoàn bóng đá"},
            {"SEA Games", "Đại hội Thể thao Đông Nam Á"},
            {"HCV", "Huy chương Vàng"},
            {"HCB", "Huy chương Bạc"},
            {"HCĐ", "Huy chương Đồng"}



        };

            // ==========================================
            // CÁC HÀM XỬ LÝ CHÍNH
            // ==========================================

            /// <summary>
            /// Hàm chuẩn hóa văn bản cơ bản (Unicode, dấu thanh, khoảng trắng)
            /// </summary>
            public static string Normalize(string text, bool removeDiacritics = false)
            {
                if (string.IsNullOrWhiteSpace(text)) return string.Empty;

                text = text.Normalize(NormalizationForm.FormC);
                text = StandardizeTonePlacement(text);
                text = CleanSpacesAndPunctuation(text);

                if (removeDiacritics) text = RemoveVietnameseSigns(text);

                return text.Trim();
            }

            /// <summary>
            /// Hàm chuẩn hóa chuyên sâu cho STT / TTS (Bao gồm đọc số, đơn vị, viết tắt)
            /// </summary>
            public static string NormalizeForSpeech(string text)
            {
                if (string.IsNullOrWhiteSpace(text)) return string.Empty;

                // Bước 1: Chuẩn hóa nền tảng (Unicode, dấu thanh)
                text = Normalize(text);

                // Bước 2: Xử lý khoảng thời gian/số liệu (VD: 1946-2026 -> 1946 đến 2026)
                text = Regex.Replace(text, @"(?<=\d)\s*-\s*(?=\d)", " đến ");

                // Bước 3: Xử lý đơn vị đo lường (Bắt buộc phải đứng sau số)
                text = Regex.Replace(text, @"(?<=\d)\s*m²", " mét vuông");
                text = Regex.Replace(text, @"(?<=\d)\s*m³", " mét khối");
                text = Regex.Replace(text, @"(?<=\d)\s*m\b", " mét", RegexOptions.IgnoreCase);
                text = Regex.Replace(text, @"(?<=\d)\s*km\b", " ki lô mét", RegexOptions.IgnoreCase);
                text = Regex.Replace(text, @"(?<=\d)\s*kg\b", " ki lô gam", RegexOptions.IgnoreCase);
                text = Regex.Replace(text, @"(?<=\d)\s*%\b", " phần trăm");

                // Bước 4: Mở rộng từ viết tắt
                foreach (var kvp in Abbreviations)
                {
                    text = Regex.Replace(text, $@"\b{kvp.Key}\b", kvp.Value);
                }

                // Bước 5: Đọc số thành chữ tiếng Việt (Bao gồm số có dấu chấm hàng nghìn như 7.000)
                text = Regex.Replace(text, @"\b\d{1,3}(?:\.\d{3})*\b|\b\d+\b", match =>
                {
                    string numStr = match.Value.Replace(".", ""); // Xóa dấu chấm phân cách
                    if (long.TryParse(numStr, out long number))
                    {
                        string words = NumberToWords(number);
                        // Giữ nguyên viết hoa chữ cái đầu nếu cần
                        return char.ToUpper(words[0]) + words.Substring(1);
                    }
                    return match.Value;
                });

                // Bước 6: Dọn dẹp khoảng trắng thừa có thể sinh ra trong quá trình chuyển đổi
                text = Regex.Replace(text, @"[ \t]+", " ");

                return text.Trim();
            }

            // ==========================================
            // CÁC HÀM HỖ TRỢ XỬ LÝ SỐ VÀ KÝ TỰ
            // ==========================================

            private static string StandardizeTonePlacement(string text)
            {
                foreach (var pair in ToneNormalizationMap)
                {
                    text = text.Replace(pair.Key, pair.Value);
                }
                return text;
            }

            private static string CleanSpacesAndPunctuation(string text)
            {
                text = Regex.Replace(text, @"[ \t]+", " ");
                text = Regex.Replace(text, @"\s+([\,\.\;\:\!\?])", "$1");
                text = Regex.Replace(text, @"([\,\.\;\:\!\?])([^\s\”\”\)])", "$1 $2");
                return text;
            }

            private static string RemoveVietnameseSigns(string text)
            {
                string normalizedString = text.Normalize(NormalizationForm.FormD);
                StringBuilder stringBuilder = new StringBuilder();

                foreach (char c in normalizedString)
                {
                    if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    {
                        stringBuilder.Append(c);
                    }
                }

                return stringBuilder.ToString().Normalize(NormalizationForm.FormC)
                                    .Replace('đ', 'd').Replace('Đ', 'D')
                                    .Replace("‘", "'").Replace("’", "'").Replace("“", "\"").Replace("”", "\"");
            }

            /// <summary>
            /// Thuật toán đọc số tiếng Việt cực kỳ chặt chẽ (Xử lý mươi, mốt, lăm, tư, lẻ)
            /// </summary>
            private static string NumberToWords(long number)
            {
                if (number == 0) return "không";

                // Ngoại lệ theo thói quen đọc khẩu ngữ nhanh (Tùy chọn)
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

                    if (group > 0 || (number > 0 && unitIndex == 0)) // Đọc cả nhóm 000 nếu nó nằm ở đuôi tỷ
                    {
                        bool readHundred = number > 0; // Nếu có hàng nghìn/triệu phía trước, bắt buộc đọc hàng trăm
                        string groupText = ReadGroupOfThree(group, digits, readHundred);
                        if (!string.IsNullOrEmpty(groupText))
                        {
                            result = $"{groupText} {units[unitIndex]} {result}".Trim();
                        }
                    }
                    unitIndex++;
                }

                return result.Replace("  ", " ").Trim();
            }

            private static string ReadGroupOfThree(int group, string[] digits, bool readHundred)
            {
                int hundred = group / 100;
                int ten = (group % 100) / 10;
                int unit = group % 10;

                if (group == 0 && !readHundred) return "";
                if (group == 0 && readHundred) return "không trăm";

                string result = "";

                // Đọc hàng trăm
                if (readHundred || hundred > 0)
                {
                    result += $"{digits[hundred]} trăm ";
                }

                // Đọc hàng chục
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

                // Đọc hàng đơn vị
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
