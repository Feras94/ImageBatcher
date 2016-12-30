using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows.Forms;

namespace ImageBatcher
{
    public partial class MainForm : Form
    {
        private List<string> _imagesPathsList;
        private OperationParameters _operationParameters;
        private CancellationTokenSource _cancellationTokenSource;

        private SynchronizationContext _context;

        public MainForm()
        {
            InitializeComponent();

            comboSizeType.SelectedIndex = 1;
            comboImageType.SelectedIndex = 1;
        }

        private void btnBrowseInput_Click(object sender, EventArgs e)
        {
            if (inputFileDialoge.ShowDialog() != DialogResult.OK)
                return;

            _imagesPathsList = new List<string>(inputFileDialoge.FileNames);
            txtInput.Text = $"{_imagesPathsList.Count} Files Selected";
        }

        private async void btnStart_Click(object sender, EventArgs e)
        {
            // clearing the log window
            txtLog.Clear();

            if (_imagesPathsList == null)
            {
                LogMessage("No Files Selected");
                return;
            }

            var maxSize = -1;
            if (!int.TryParse(txtMaxSize.Text, out maxSize))
            {
                LogMessage("No Valid Size");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtOutput.Text))
            {
                LogMessage("No Output Path Chosen");
                return;
            }

            // setting up operation parameters
            _operationParameters = new OperationParameters
            {
                ImagesPathsList = _imagesPathsList,
                OutputPath = txtOutput.Text,
                MaxSize = maxSize,
                MaxSizeType = comboSizeType.SelectedIndex == 0 ? SizeType.Megabytes :
                                comboSizeType.SelectedIndex == 1 ? SizeType.Kilobytes : SizeType.Bytes,
                ImageType = comboImageType.SelectedIndex == 0 ? ImageType.Png : ImageType.Jpg,
            };

            if (!Directory.Exists(_operationParameters.OutputPath))
                Directory.CreateDirectory(_operationParameters.OutputPath);

            // canceling previous operation
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            // preparing the progress bar
            _context = SynchronizationContext.Current;
            progressBar.Minimum = 0;
            progressBar.Maximum = _imagesPathsList.Count;
            progressBar.Value = 0;

            // defining the action block to use
            var proccessImageBlock = new ActionBlock<string>((path) =>
            {
                ProccessImage(path);

                _context.Send((_) =>
                {
                    progressBar.Value++;

                }, null);

                GC.Collect();

            }, new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = _cancellationTokenSource.Token,
            });

            btnStart.Enabled = false;
            btnCancel.Enabled = true;

            Parallel.ForEach(_imagesPathsList, (path) => { proccessImageBlock.Post(path); });

            // finish submitting and wait for completion
            proccessImageBlock.Complete();
            await proccessImageBlock.Completion.ContinueWith(task =>
            {
                _context.Send((_) =>
                {
                    LogMessage("Done");
                    btnStart.Enabled = true;
                    btnCancel.Enabled = false;

                }, null);
            });
        }

        private void ProccessImage(string path)
        {
            if (_cancellationTokenSource.IsCancellationRequested)
                return;

            var img = Image.FromFile(path);

            var imageName = Path.GetFileNameWithoutExtension(path);
            var iteration = 1;

            while (true)
            {
                if (_cancellationTokenSource.IsCancellationRequested)
                    return;

                using (var memoryStream = new MemoryStream())
                {
                    img.Save(memoryStream,
                        _operationParameters.ImageType == ImageType.Png ? ImageFormat.Png : ImageFormat.Jpeg);

                    var size = memoryStream.Length;
                    if (ReachedTargetSize(size))
                    {
                        var savePath = Path.Combine(_operationParameters.OutputPath, imageName);
                        var fileName = _operationParameters.ImageType == ImageType.Png
                            ? $"{savePath}.png"
                            : $"{savePath}.jpg";

                        _context.Post((itr) => { LogMessage($"Saving image {imageName} after {itr} iterations"); }, iteration);
                        File.WriteAllBytes(fileName, memoryStream.GetBuffer());

                        break;
                    }
                }

                var newWidth = Math.Ceiling(img.Width * 0.75);
                var newHeight = Math.Ceiling(img.Height * 0.75);

                var newImg = new Bitmap((int)newWidth, (int)newHeight);
                using (var graphics = Graphics.FromImage(newImg))
                {
                    graphics.DrawImage(img, new RectangleF(0.0f, 0.0f, (float)newWidth, (float)newHeight));
                }

                img.Dispose();
                img = newImg;
                iteration++;
            }
        }

        private bool ReachedTargetSize(long size)
        {
            var convertedSize = ConvertToTargetSizeType(size);
            return convertedSize <= _operationParameters.MaxSize;
        }

        private long ConvertToTargetSizeType(long size)
        {
            if (_operationParameters.MaxSizeType == SizeType.Bytes)
                return size;
            if (_operationParameters.MaxSizeType == SizeType.Kilobytes)
                return size / 1024;
            if (_operationParameters.MaxSizeType == SizeType.Megabytes)
                return size / 1024 / 1024;

            throw new NotImplementedException();
        }

        private void btnBrowseOutput_Click(object sender, EventArgs e)
        {
            if (outputDialoge.ShowDialog() != DialogResult.OK)
                return;

            txtOutput.Text = outputDialoge.SelectedPath;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            _cancellationTokenSource?.Cancel();

            btnCancel.Enabled = false;
            btnStart.Enabled = true;
        }

        private void LogMessage(string message)
        {
            txtLog.Text = message + Environment.NewLine + txtLog.Text;
        }
    }
}