namespace PdfAnnoRemover.ViewModels
{
    public class FileViewModel : BaseViewModel
    {
        private readonly string _fullFilename;


        public string FullFilename => _fullFilename;
        public string Filename => System.IO.Path.GetFileName(_fullFilename);
        public string? Path => System.IO.Path.GetDirectoryName(_fullFilename);

        public int? RemovedCount
        {
            get => _RemovedCount;
            set
            {
                if (value != _RemovedCount)
                {
                    _RemovedCount = value;
                    OnPropertyChanged();
                }
            }
        }
        private int? _RemovedCount;

        public string? Comment
        {
            get => _Comment;
            set
            {
                if (value != _Comment)
                {
                    _Comment = value;
                    OnPropertyChanged();
                }
            }
        }
        private string? _Comment;

        public bool IsRunning
        {
            get => _IsRunning;
            set
            {
                if (_IsRunning != value)
                {
                    _IsRunning = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool _IsRunning;

        public CancellationTokenSource? CancellationTokenSource { get; private set; }


        public FileViewModel(string fullFilename)
        {
            if (string.IsNullOrEmpty(fullFilename)) throw new ArgumentNullException(nameof(fullFilename));

            _fullFilename = fullFilename;
        }

        public void RunRemoveAnnotations(CancellationToken cancellationToken)
        {
            if (IsRunning) throw new Exception("File is already running");

            IsRunning = true;
            Comment = "Processing";

            try
            {
                RemovedCount = AnnoRemover.RemoveDrawingsFromPDF(FullFilename, cancellationToken);
                Comment = cancellationToken.IsCancellationRequested ? "Aborted" : "Done";
            }
            catch (Exception exc)
            {
                Comment = exc.Message;
            }

            IsRunning = false;
        }
    }
}
