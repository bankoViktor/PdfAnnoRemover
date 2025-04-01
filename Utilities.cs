using System.Diagnostics;

namespace PdfAnnoRemover
{
    internal static class Utilities
    {
        internal static void OpenFolderAndSelectFile(string fullFilename)
        {
            var argument = $"/select,\"{fullFilename}\"";
            Process.Start("explorer.exe", argument);
        }
    }
}
