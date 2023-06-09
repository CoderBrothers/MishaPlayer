﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Threading;

namespace MishaPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();          
            ReadMemory();
            ValueSlider.Value = 0.5;
            _timer.Tick += new EventHandler(timer_Tick);
            _timer.Interval = TimeSpan.FromSeconds(1);
        }

        private void timer_Tick(object? sender, EventArgs e)
        {
            if (_player.Source != null && _player.NaturalDuration.HasTimeSpan)
            {
                trackProgress.Minimum = 0;
                trackProgress.Maximum = _player.NaturalDuration.TimeSpan.TotalSeconds;
                trackProgress.Value = _player.Position.TotalSeconds;
                trackDuration.Text = TimeSpan.FromSeconds(_player.NaturalDuration.TimeSpan.TotalSeconds).ToString(@"hh\:mm\:ss");
            }
            if (_player.Position == _player.NaturalDuration)
            {
                NextBtn_Click(sender, null);
            }
        }

        private MediaPlayer _player = new();
        private List<string> _paths = new();
        private DispatcherTimer _timer = new();

        private void ReadMp3()
        {
            var tfile = TagLib.File.Create("kkk");
            string title = tfile.Tag.Title;
            TimeSpan duration = tfile.Properties.Duration;
            Console.WriteLine("Title: {0}, duration: {1}", title, duration);

            // change title in the file
            tfile.Tag.Title = "my new title";
            tfile.Save();
        }
        private void Hide_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void Close_Click(object sender, RoutedEventArgs e) => PlaylistMemory();
        private void Show_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Maximized;
        private void Window_MouseDown(object sender, MouseButtonEventArgs e) => DragMove();
        private void PreviousBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Playlist.SelectedIndex > 0) Playlist.SelectedIndex--;
            else Playlist.SelectedIndex = Playlist.Items.Count - 1;
        }
        private void PauseBtn_OnClick(object sender, RoutedEventArgs e) => _player.Pause();
        private void PlayBtn_OnClick(object sender, RoutedEventArgs e) => _player.Play();
        private void StopBtn_OnClick(object sender, RoutedEventArgs e) => _player.Stop();
        private void NextBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Playlist.SelectedIndex < Playlist.Items.Count - 1) Playlist.SelectedIndex++;
            else
            {
                Playlist.SelectedIndex = 0;
            }
        }
        private void Playlist_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Playlist.SelectedIndex == -1) _player.Stop();
            else
            {
                _player.Open(new Uri(_paths[Playlist.SelectedIndex], UriKind.RelativeOrAbsolute));
                _player.Play();
            }
            _timer.Start();
            ReadMp3();
        }
        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _player.Volume = ValueSlider.Value;
            VolumeTb.Text = (Math.Round(_player.Volume * 100)) + "%";
        }
        private void Grid_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            _player.Volume += e.Delta > 0 ? 0.01 : -0.01;
            VolumeTb.Text = (Math.Round(_player.Volume * 100)) + "%";
            ValueSlider.Value = _player.Volume;
        }
        private void trackProgress_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _player.Position = TimeSpan.FromSeconds((e.GetPosition(trackProgress).X * trackProgress.Maximum) / trackProgress.ActualWidth);
        }
        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.Multiselect = true;
            ofd.Filter = "mp3 files(*.mp3)|*.mp3|wav files(*.wav)|*.wav|aiff files(*.aiff)|*.aiff|ape files(*.ape)|*.ape|flac files(*.flac)|*.flac|ogg files(*.ogg)|*.ogg";
            if (ofd.ShowDialog() == false) return;
            foreach (var path in ofd.FileNames)
            {
                _paths.Add(path);
            }

            foreach (var name in ofd.SafeFileNames)
            {
                Playlist.Items.Add(name);
            }
        }
        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Playlist.SelectedItem != null)
            {
                _paths.RemoveAt(Playlist.SelectedIndex);
                Playlist.Items.RemoveAt(Playlist.SelectedIndex);
            }
        }
        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog();
            sfd.Filter = "Playlist files(*.pls)|*.pls";
            if (sfd.ShowDialog() == false) return;
            var writer = new StreamWriter(sfd.FileName);
            foreach (var item in _paths)
            {
                writer.WriteLine(item);
            }
            writer.Close();
            MessageBox.Show("Playlist saved");
        }
        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            Playlist.Items.Clear();
            _paths.Clear();
        }
        private void LoadBtn_Click(object sender, RoutedEventArgs e)
        {
            var opd = new OpenFileDialog();
            opd.Filter = "Playlist files(*.pls)|*.pls";
            if (opd.ShowDialog() == false) return;
            ClearBtn_Click(sender, e);
            using (StreamReader sr = new StreamReader(opd.FileName))
            {
                string? line;
                while ((line = sr.ReadLine()) != null)
                {
                    _paths.Add(line);
                    Playlist.Items.Add(System.IO.Path.GetFileName(line));
                }
            }
        }
        private void RandomBtn_Click(object sender, RoutedEventArgs e)
        {
            Random random = new Random();
            Playlist.SelectedIndex = random.Next(Playlist.Items.Count);
            _player.Play();
        }
        private void PlaylistMemory()
        {
            DriveInfo drive = DriveInfo.GetDrives()[0];
            string path = drive.Name + @"\MishaPlayer";
            DirectoryInfo directoryInfo = new(path);
            if (!directoryInfo.Exists) directoryInfo.Create();
            string file = drive.Name + @"\MishaPlayer\tmp.dat";
            File.WriteAllLines(file, _paths);
            Close();
        }
        private void ReadMemory()
        {
            DriveInfo drive = DriveInfo.GetDrives()[0];
            string path = drive.Name + @"\MishaPlayer\tmp.dat";
            try
            {
                using (FileStream fstream = File.OpenRead(path))
                {
                    string line;
                    StreamReader file = new(path);
                    while ((line = file.ReadLine()) != null)
                    {
                        _paths.Add(line);
                    }
                    file.Close();
                }
                foreach (var item in _paths)
                {
                    Playlist.Items.Add(System.IO.Path.GetFileName(item));
                }
            }
            catch (Exception e) { }
        }
    } 
}

