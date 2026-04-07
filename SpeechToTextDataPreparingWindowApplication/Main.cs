namespace SpeechToTextDataPreparingWindowApplication
{
    public partial class Main : Form
    {
        ThaiNguyenForm _ThaiNguyenForm;
        public Main()
        {
            InitializeComponent();
        }

        private void btnThaiNguyen_Click(object sender, EventArgs e)
        {
            this.Invoke(new Action(() =>
            {

                if (_ThaiNguyenForm == null)
                {
                    _ThaiNguyenForm = new ThaiNguyenForm();
                    _ThaiNguyenForm.MdiParent = this;
                    _ThaiNguyenForm.Show();
                }
                else
                {
                    if (_ThaiNguyenForm.IsDisposed)
                    {
                        _ThaiNguyenForm = new ThaiNguyenForm();
                        _ThaiNguyenForm.MdiParent = this;
                        _ThaiNguyenForm.Show();
                    }
                    else
                    {
                        _ThaiNguyenForm.Activate();
                    }
                }
            }));
        }
    }
}
