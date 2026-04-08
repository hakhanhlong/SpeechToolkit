namespace MFADatasetLexiconPrepareConsoleApplication
{
    internal class Program
    {
        static void Main(string[] args)
        {
            
            string rawDataFolder = @"/mnt/d/SPEECH_DATAPREPARING/audio_1";
            string cleanDataFolder = @"/mnt/d/SPEECH_DATAPREPARING/audio_1_clean";
            string lexiconFile = @"/mnt/d/SPEECH_DATAPREPARING/audio_1_clean/lexicon_sach.txt";
            PrepareMFADatasetLexiconVietnamese.PrepareMfaDataset(rawDataFolder, cleanDataFolder, lexiconFile);
        }
    }
}
