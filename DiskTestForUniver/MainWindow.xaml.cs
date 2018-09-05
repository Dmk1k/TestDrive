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
using Microsoft.WindowsAPICodePack.Dialogs;
using System.IO;
using System.Timers;

namespace DiskTestForUniver
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string _filename = "test_file.txt";
        string _folderpath;
        string _filepath;
        FileStream _file;
        System.Timers.Timer _writeTimer;
        System.Timers.Timer _readTimer;
        double _writeTick;
        double _readTick;
        System.Diagnostics.Process _openedFile;
        public MainWindow()
        {
            _folderpath = "C:\\";
            _filepath = _folderpath + _filename;
            _file = null;
            _openedFile = null;
            _writeTick = 100;
            _readTick = 100;
            _writeTimer = new System.Timers.Timer(_writeTick);
            _writeTimer.Elapsed += _writeTimer_Elapsed;
            _readTimer = new System.Timers.Timer(_readTick);
            _readTimer.Elapsed += _readTimer_Elapsed;
            InitializeComponent();
            DisplayStateTextBox.Text = "Непроинициализировано";
            FilePathTextBox.Text = _filepath;
            LogTextBox.Text = "Логирование";
        }

        private void _readTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                var value = (char)_file.ReadByte();
                if (value != '1')
                {
                    Dispatcher.Invoke(() => DisplayStateTextBox.Text = "Закончено чтение файла");
                    Dispatcher.Invoke(() => _readTimer.Stop());
                    return;
                }
                Dispatcher.Invoke(() => LogTextBox.AppendText(value.ToString()));
                Dispatcher.Invoke(() => LogTextBox.ScrollToEnd());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка чтения файла");
            }
        }

        private void _writeTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                var value = (byte)'1';
                _file.WriteByte(value);
                Dispatcher.Invoke(() => LogTextBox.AppendText("1"));
                Dispatcher.Invoke(() => LogTextBox.ScrollToEnd());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка записи в файл");
            }
        }

        private void ChangeDriveButton_onClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new CommonOpenFileDialog();
                dialog.IsFolderPicker = true;
                CommonFileDialogResult result = dialog.ShowDialog();
                if (result == CommonFileDialogResult.Ok)
                {
                    _folderpath = dialog.FileName;
                    _filepath = _folderpath + "\\" + _filename;
                    FilePathTextBox.Text = _filepath;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка выбора директории");
            }
        }

        private void WriteToDriveButton_onClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_filepath == null)
                {
                    MessageBox.Show("Необходимо задать путь", "Ошибка");
                    return;
                }

                if (_readTimer.Enabled)
                {
                    MessageBox.Show("Запущен процесс чтения", "Ошибка");
                    return;
                }
                if (_file == null)
                {
                    if (File.Exists(_filepath))
                    {
                        File.Delete(_filepath);
                    }
                    _file = File.Create(_filepath);
                }
                if (_writeTimer.Enabled)
                {
                    MessageBox.Show("Процесс записи уже запущен", "Ошибка");
                    return;
                }
                
                CloseOpenedFile();
                if (!_file.CanWrite)
                {
                    _file = File.OpenWrite(_filepath);
                }
                DisplayStateTextBox.Text = "Идет запись в файл";
                LogTextBox.Text = "";
                _writeTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка записи в файл");
            }
        }

        private void StopWriteButton_onClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_writeTimer.Enabled)
                {
                    MessageBox.Show("Нет активного процесса записи", "Ошибка");
                    return;
                }
                DisplayStateTextBox.Text = "Закончена запись в файл";
                StopWriting();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка остановки записи в файл");
            }
        }

        private void SessionStopButton_onClick(object sender, RoutedEventArgs e)
        {
            try
            {
                DisplayStateTextBox.Text = "Сессия закончена";
                LogTextBox.Text = "";
                if (_file == null)
                {
                    return;
                }
                EraseFile();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка остановки сессии");
            }
        }

        private void EraseFile()
        {
            CloseOpenedFile();
            StopWriting();
            StopReading();
            if (File.Exists(_filepath))
            {
                File.Delete(_filepath);
            }
            _file = null;
        }

        private void StopWriting()
        {
            if (_writeTimer != null)
            {
                _writeTimer.Stop();
            }
            if (_file != null)
            {
                _file.Close();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            EraseFile();
        }

        private void OpenFileButton_onClick(object sender, RoutedEventArgs e)
        {
            if (_filepath == null || !File.Exists(_filepath) || _file == null)
            {
                MessageBox.Show("Файл удален или еще не создан", "Ошибка");
                return;
            }
            if (_writeTimer.Enabled)
            {
                MessageBox.Show("Идет процесс записи в файл", "Ошибка открытия файла");
                return;
            }
            if (_readTimer.Enabled)
            {
                MessageBox.Show("Идет процесс чтения с файла", "Ошибка открытия файла");
                return;
            }
            CloseOpenedFile();
            try
            {
                _openedFile = System.Diagnostics.Process.Start(_filepath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка открытия файла");
            }
        }

        private void CloseOpenedFile()
        {
            if (_openedFile != null)
            {
                if (_openedFile.HasExited)
                {
                    _openedFile = null;
                    return;
                }
                _openedFile.CloseMainWindow();
                _openedFile = null;
            }

        }

        private void ReadFileButton_onClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_filepath == null)
                {
                    MessageBox.Show("Необходимо задать путь", "Ошибка");
                    return;
                }
                if (_file == null)
                {
                    MessageBox.Show("Файл не создан или удален", "Ошибка");
                    return;
                }
                if (_readTimer.Enabled)
                {
                    MessageBox.Show("Процесс чтения уже запущен", "Ошибка");
                    return;
                }
                if (_writeTimer.Enabled)
                {
                    MessageBox.Show("Запущен процесс записи", "Ошибка");
                    return;
                }
                CloseOpenedFile();
                _file = File.OpenRead(_filepath);
                DisplayStateTextBox.Text = "Идет чтения файла";
                LogTextBox.Text = "";
                _readTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка чтения файла");
            }
        }

        private void StopReading()
        {
            if (_readTimer != null)
            {
                _readTimer.Stop();
            }
        }

        private void StopReadFileButton_onClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_readTimer.Enabled)
                {
                    MessageBox.Show("Нет активного процесса чтения", "Ошибка");
                    return;
                }
                DisplayStateTextBox.Text = "Закончено чтение файла";
                StopReading();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка остановки чтения файла");
            }
        }
    }
}
