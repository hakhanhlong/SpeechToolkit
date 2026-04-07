using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ultilities;
using Ultilities.TextClean;

namespace SpeechToTextDataPreparingWindowApplication
{
    public partial class ThaiNguyenForm : Form
    {
        public ThaiNguyenForm()
        {
            InitializeComponent();
        }

        private void btnExecuteCleanText_Click(object sender, EventArgs e)
        {
            var cleanText = ThaiNguyenScriptTextClean.CleanScript(txtScriptRawText.Text);

            cleanText = new VietnameseTextProcessor().ProcessVietnameseText(cleanText);
            
            
            cleanText =  VietnameseTextNormalizer.NormalizeForSpeech(cleanText);
            

            txtScriptCleanText.Text = cleanText;
        }
    }
}
