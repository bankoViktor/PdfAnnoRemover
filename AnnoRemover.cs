using System.IO;
using iText.Kernel.Pdf;

namespace PdfAnnoRemover
{
    internal static class AnnoRemover
    {
        public const string AutoCadAuthorName = "AutoCAD SHX Text";
        public const string NullAuthorName = "NULL";


        public static int? RemoveDrawingsFromPDF(string inputPath, CancellationToken cancellationToken)
        {
            var removedCount = 0;

            using var reader = new PdfReader(inputPath);
            using var writer = new PdfWriter(inputPath + ".tmp");
            using var pdfDoc = new PdfDocument(reader, writer);

            var pageCount = pdfDoc.GetNumberOfPages();
            for (var pageIndex = 1; pageIndex <= pageCount; pageIndex++)
            {
                var page = pdfDoc.GetPage(pageIndex);

                var pageAnnotations = page.GetAnnotations();
                var groupedByAuthors = pageAnnotations
                    .GroupBy(x => x.GetPdfObject()?.GetAsString(PdfName.T)?.ToUnicodeString() ?? NullAuthorName);
                foreach (var group in groupedByAuthors)
                {
                    if (cancellationToken.IsCancellationRequested) return null;

                    if (group.Key == AutoCadAuthorName ||
                        group.Key == NullAuthorName)
                    {
                        continue;
                    }

                    foreach (var groupAnno in group)
                    {
                        page.RemoveAnnotation(groupAnno);
                        removedCount++;
                    }
                }
            }

            pdfDoc.Close();

            File.Delete(inputPath);
            File.Move(inputPath + ".tmp", inputPath);

            return removedCount;
        }
    }
}
