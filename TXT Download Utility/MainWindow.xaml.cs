using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.ComponentModel;
using System.Reflection;
using Downloader;
using System.Net;

namespace Multi_Threading_Download
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private System.Threading.Timer aTimer;
        public MainWindow()
        {
            InitializeComponent();
            SetTimer();
        }
        public string[] filename=null;
        public string foldPath=null;
        private void SetTimer()
        {
            aTimer = new System.Threading.Timer(ChangeProgressBarEvent, null, Timeout.Infinite, Timeout.Infinite);
        }
        private void ChangeProgressBarEvent(Object state)
        {
            aTimer.Change(1000, Timeout.Infinite);
            this.Dispatcher.BeginInvoke(new Action(() => this.DownloadPercentage.Value=tot), System.Windows.Threading.DispatcherPriority.Normal, null);
        }
        int tot=0;
        private void TXTSelect_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Multiselect = false;
            fileDialog.Title = "Please Select Your File";
            fileDialog.Filter = "TXT File(*.txt)|*.txt";
            if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string[] name = fileDialog.FileNames;
                filename= System.IO.File.ReadAllLines(name[0]);
            }

            if (filename != null && foldPath != null)
            {
                StartDownload.IsEnabled = true;
            }
        }

        private void FolderSelect_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "请选择文件路径";

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                foldPath = dialog.SelectedPath;
            }
            if (filename != null && foldPath!=null)
            {
                StartDownload.IsEnabled = true;
            }
        }

        private async void StartDownload_Click(object sender, RoutedEventArgs e)
        {
            tot = 0;
            DownloadPercentage.Maximum = filename.Length;
            DownloadPercentage.Value = 0;
            /*
            for (int i = 0; i < filename.Length; i++)
            {
                string[] saveUrl = filename[i].Split('/');
                var TaskAsync= startDownloadAsync(filename[i], foldPath + "\\" + saveUrl[saveUrl.Length - 1],int.Parse(ThreadCountTextbox.Text), saveUrl[saveUrl.Length - 1]);
                if (await TaskAsync)
                {
                    continue;
                }
            }
            OutputLabel.Text += "\n" +"End";
            */
            aTimer.Change(0, Timeout.Infinite);
            StopDownload.IsEnabled = true;
            TXTSelect.IsEnabled = false;
            FolderSelect.IsEnabled = false;
            await AllocDownloadAsync();
        }
        private async Task AllocDownloadAsync()
        {
            if (filename.Length < 4)
            {
                OutputLabel2.Text = "Nothing to Download";
                OutputLabel3.Text = "Nothing to Download";
                OutputLabel4.Text = "Nothing to Download";
                await StartDownloadAsync(0, filename.Length,1);
                OutputLabel1.Text += "\n" + "End";
            }
            else
            {
                var task1= StartDownloadAsync(0, filename.Length/4,1);
                var task2 = StartDownloadAsync(filename.Length/4, filename.Length / 2,2);
                var task3 = StartDownloadAsync(filename.Length / 2, 3*filename.Length / 4,3);
                var task4 = StartDownloadAsync(3*filename.Length / 4, filename.Length,4);
                await Task.WhenAll(task1, task2, task3, task4);
                System.Windows.MessageBox.Show("Download Process End");
            }
        }
        private async Task StartDownloadAsync(int start,int end,int tasknumber)
        {
            for (int i = start; i < end; i++)
            {
                string[] saveUrl = filename[i].Split('/');
                if (!File.Exists(foldPath + "\\" + saveUrl[saveUrl.Length - 1]))
                {
                    var TaskAsync = startDownloadAsync(filename[i], foldPath + "\\" + saveUrl[saveUrl.Length - 1], int.Parse(ThreadCountTextbox.Text), saveUrl[saveUrl.Length - 1],tasknumber);
                    if (await TaskAsync)
                    {
                        ModifyOutput(saveUrl[saveUrl.Length - 1],tasknumber,false);
                        continue;
                    }
                }
            }
        }
        private void ModifyOutput(string labeltext,int tasknumber,bool isException)
        {
            if (!isException)
            {
                switch (tasknumber)
                {
                    case 1:
                        OutputLabel1.Text += labeltext + "Success \n";
                        break;
                    case 2:
                        OutputLabel2.Text += labeltext + "Success \n";
                        break;
                    case 3:
                        OutputLabel3.Text += labeltext + "Success \n";
                        break;
                    case 4:
                        OutputLabel4.Text += labeltext + "Success \n";
                        break;
                }
                tot++;
                //DownloadPercentage.Value += 1;
            }
            else if (isException)
            {
                switch (tasknumber)
                {
                    case 1:
                        OutputLabel1.Text += labeltext + "Failed \n";
                        break;
                    case 2:
                        OutputLabel2.Text += labeltext + "Failed \n";
                        break;
                    case 3:
                        OutputLabel3.Text += labeltext + "Failed \n";
                        break;
                    case 4:
                        OutputLabel4.Text += labeltext + "Failed \n";
                        break;
                }
            }
        }
        private async Task<bool> startDownloadAsync(string URLName,string FoldPath,int ThreadCount,string FileName,int tasknumber)
        {
            var downloadOpt = new DownloadConfiguration()
            {
                BufferBlockSize = 10240, // 通常，主机最大支持8000字节，默认值为8000。
                ChunkCount = ThreadCount, // 要下载的文件分片数量，默认值为1
                MaximumBytesPerSecond = 0, // 下载速度限制为1MB/s，默认值为零或无限制
                MaxTryAgainOnFailover = int.MaxValue, // 失败的最大次数
                OnTheFlyDownload = true, // 是否在内存中进行缓存？ 默认值是true
                ParallelDownload = true, // 下载文件是否为并行的。默认值为false
                TempDirectory = System.IO.Path.GetTempPath(), // 设置用于缓冲大块文件的临时路径，默认路径为Path.GetTempPath()。
                Timeout = 1000, // 每个 stream reader  的超时（毫秒），默认值是1000
                RequestConfiguration = // 定制请求头文件
            {
                Accept = "*/*",
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                CookieContainer =  new CookieContainer(), // Add your cookies
                Headers = new WebHeaderCollection(), // Add your custom headers
                KeepAlive = false,
                ProtocolVersion = HttpVersion.Version11, // Default value is HTTP 1.1
                UseDefaultCredentials = false,
                UserAgent = $"DownloaderSample/{Assembly.GetExecutingAssembly().GetName().Version.ToString(3)}"
            }
            };
            try
            {
                var downloader = new DownloadService(downloadOpt);
                await downloader.DownloadFileTaskAsync(URLName, FoldPath);
            }
            catch (Exception ex)
            {
                ModifyOutput(URLName+" "+ex.ToString(),tasknumber,true);
            }
            //OutputLabel.Text = tot.ToString();
            //DownloadPercentage.Value += 1;
            return true;
        }

        private void StopDownload_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
