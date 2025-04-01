using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;

namespace PdfAnnoRemover.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private CancellationTokenSource? _cts;


        public string? Status
        {
            get => _Status;
            set
            {
                if (_Status != value)
                {
                    _Status = value;
                    OnPropertyChanged();
                }
            }
        }
        private string? _Status;

        public bool IsError
        {
            get => _IsError;
            set
            {
                if (_IsError != value)
                {
                    _IsError = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool _IsError;

        public Visibility ProgressBarVisibility
        {
            get => _ProgressBarVisibility;
            set
            {
                if (_ProgressBarVisibility != value)
                {
                    _ProgressBarVisibility = value;
                    OnPropertyChanged();
                }
            }
        }
        private Visibility _ProgressBarVisibility;

        public double Progress
        {
            get => _Progress;
            set
            {
                if (_Progress != value)
                {
                    _Progress = value;
                    OnPropertyChanged();
                }
            }
        }
        private double _Progress;

        public string? SourceDirectory
        {
            get => _SourceDirectory;
            set
            {
                if (_SourceDirectory != value)
                {
                    _SourceDirectory = value;
                    OnPropertyChanged();
                    StartOrStopCommand.RaiseCanExecuteChanged();

                    if (!Directory.Exists(SourceDirectory))
                    {
                        Status = "Selected directory not exists";
                    }
                    else
                    {
                        Status = "Ready";
                    }
                }
            }
        }
        private string? _SourceDirectory;

        public bool IsRecursive
        {
            get => _IsRecursive;
            set
            {
                if (_IsRecursive != value)
                {
                    _IsRecursive = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool _IsRecursive;

        public bool IsRunning
        {
            get => _IsRunning;
            set
            {
                if (_IsRunning != value)
                {
                    _IsRunning = value;
                    OnPropertyChanged();

                    StartOrStopCommand.RaiseCanExecuteChanged();
                    BrowseCommand.RaiseCanExecuteChanged();
                    ExportCommand.RaiseCanExecuteChanged();
                }
            }
        }
        private bool _IsRunning;

        public int FilesCount
        {
            get => _FilesCount;
            set
            {
                if (_FilesCount != value)
                {
                    _FilesCount = value;
                    OnPropertyChanged();
                }
            }
        }
        private int _FilesCount;

        public ObservableCollection<FileViewModel>? Files
        {
            get => _Files;
            set
            {
                if (_Files != value)
                {
                    _Files = value;
                    OnPropertyChanged();

                    FilesCount = Files?.Count ?? 0;
                }
            }
        }
        private ObservableCollection<FileViewModel>? _Files;

        public FileViewModel? SelectedFile
        {
            get => _SelectedFile;
            set
            {
                if (_SelectedFile != value)
                {
                    _SelectedFile = value;
                    OnPropertyChanged();

                    OpenFolderCommand.RaiseCanExecuteChanged();
                }
            }
        }
        private FileViewModel? _SelectedFile;

        public DelegateCommand StartOrStopCommand
        {
            get
            {
                _StartOrStopCommand ??= new DelegateCommand(StartOrStopCommandExecute, CanStartOrStopCommandExecute);
                return _StartOrStopCommand;
            }
        }
        DelegateCommand? _StartOrStopCommand;

        public DelegateCommand BrowseCommand
        {
            get
            {
                _BrowseCommand ??= new DelegateCommand(BrowseCommandExecute, x => !IsRunning);
                return _BrowseCommand;
            }
        }
        DelegateCommand? _BrowseCommand;

        public DelegateCommand OpenFolderCommand
        {
            get
            {
                _OpenFolderCommand ??= new DelegateCommand(OpenFolderCommandExecute, x => SelectedFile != null);
                return _OpenFolderCommand;
            }
        }
        DelegateCommand? _OpenFolderCommand;

        public DelegateCommand ExportCommand
        {
            get
            {
                _ExportCommand ??= new DelegateCommand(ExportCommandExecute, x => !IsRunning && Files != null && Files.Count > 0);
                return _ExportCommand;
            }
        }
        DelegateCommand? _ExportCommand;


        public MainViewModel()
        {
            SourceDirectory = @"D:\";
            IsRecursive = false;
            IsRunning = false;

            Files = [];
            Status = "Ready";
            ProgressBarVisibility = Visibility.Collapsed;
            Progress = 0;
        }

        private bool CanStartOrStopCommandExecute(object? parameter)
        {
            if (string.IsNullOrEmpty(SourceDirectory))
            {
                return false;
            }

            return Directory.Exists(SourceDirectory);
        }

        private void StartOrStopCommandExecute(object? parameter)
        {
            if (IsRunning)
            {
                _cts?.Cancel();
            }
            else
            {
                StartFilesProcessingAsync();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Проверка совместимости платформы", Justification = "<Ожидание>")]
        private void BrowseCommandExecute(object? parameter)
        {
            using var dialog = new FolderBrowserDialog()
            {
                UseDescriptionForTitle = true,
                Description = "Pick a folder",
                InitialDirectory = SourceDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                ShowNewFolderButton = true,
            };

            if (dialog.ShowDialog() == DialogResult.OK &&
                Directory.Exists(dialog.SelectedPath))
            {
                SourceDirectory = dialog.SelectedPath;
            }
        }

        private void OpenFolderCommandExecute(object? parameter)
        {
            var vm = SelectedFile ?? throw new Exception("File not selected");
            Utilities.OpenFolderAndSelectFile(vm.FullFilename);
        }

        private void ExportCommandExecute(object? parameter)
        {
            if (Files == null) throw new Exception("Files not found");

            var saveFileDialog = new SaveFileDialog()
            {
                Filter = "CSV files|*.csv|All files|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            };

            if (saveFileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            var sb = new StringBuilder();

            // Header
            sb.AppendLine("File name, Removed annotations, Status");

            foreach (var file in Files)
            {
                sb.AppendLine($"\"{file.FullFilename}\", {file.RemovedCount}, {file.Comment}");
            }

            var csvFilename = saveFileDialog.FileName;
            File.WriteAllText(csvFilename, sb.ToString());
        }

        private void ProcesFiles(IProgress<int> progress, CancellationToken cancellationToken)
        {
            var dir = SourceDirectory ?? throw new Exception("Source directory not selected");

            Files = [];

            var files = Directory.GetFiles(dir, "*.PDF", new EnumerationOptions
            {
                RecurseSubdirectories = IsRecursive,
                ReturnSpecialDirectories = false,
            });

            var models = files.Select(x => new FileViewModel(x));
            Files = new ObservableCollection<FileViewModel>(models);

            var counter = 0;
            foreach (var fileVM in Files)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                fileVM.RunRemoveAnnotations(cancellationToken);
                counter++;

                progress.Report((int)(counter / (double)files.Length * 100));
            }
        }

        private async void StartFilesProcessingAsync()
        {
            // Prepere UI
            IsError = false;
            Status = "Running 0%";
            IsRunning = true;
            ProgressBarVisibility = Visibility.Visible;
            var progress = new Progress<int>(value =>
            {
                Status = $"Running {value}%";
                Progress = value;
            });
            _cts = new CancellationTokenSource();

            try
            {
                // Long time operation
                await Task.Run(() =>
                {
                    ProcesFiles(progress, _cts.Token);
                }, _cts.Token);

                Status = "Completed";
            }
            catch (TaskCanceledException)
            {
                Status = "Canceled";
            }
            catch (Exception exc)
            {
                IsError = true;
                Status = "ERROR: " + exc.Message;
            }

            // Restore UI
            IsRunning = false;
            ProgressBarVisibility = Visibility.Collapsed;

            if (!_cts.IsCancellationRequested)
            {
                var fielsCount = Files?.Count ?? 0;
                var totalRemoved = Files?.Sum(x => x.RemovedCount) ?? 0;
                var message = $"Successfully removed {totalRemoved} annotation items from {fielsCount} files.";
                System.Windows.MessageBox.Show(App.Current.MainWindow, message, App.Current.MainWindow.Title, MessageBoxButton.OK, MessageBoxImage.Information);
            }

            _cts = null;
        }
    }
}
