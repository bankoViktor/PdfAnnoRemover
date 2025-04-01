using System.IO;
using iText.Kernel.Exceptions;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Annot;

namespace PdfAnnoRemover
{
    internal static class AnnoRemover
    {
        public const string AutoCadAuthorName = "AutoCAD SHX Text";
        public const string NullAuthorName = "NULL";

        private static string GetAuthorKey(PdfAnnotation anno) => anno.GetPdfObject()?.GetAsString(PdfName.T)?.ToUnicodeString() ?? NullAuthorName;

        public static int? RemoveDrawingsFromPDF(string inputPath, CancellationToken cancellationToken)
        {
            var removedCount = 0;
            var tempFilename = inputPath + ".tmp";

            try
            {
                using var reader = new PdfReader(inputPath);
                using var writer = new PdfWriter(tempFilename);
                using var pdfDoc = new PdfDocument(reader, writer);

                var pageCount = pdfDoc.GetNumberOfPages();
                for (var pageIndex = 1; pageIndex <= pageCount; pageIndex++)
                {
                    var page = pdfDoc.GetPage(pageIndex);

                    var pageAnnotations = page.GetAnnotations();
                    var groupedByAuthors = pageAnnotations.GroupBy(GetAuthorKey);
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
                File.Move(tempFilename, inputPath);
            }
            catch (BadPasswordException pwExc)
            {
                throw new Exception("Password protected", pwExc);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (File.Exists(tempFilename))
                {
                    File.Delete(tempFilename);
                }
            }

            return removedCount;
        }
    }
}
