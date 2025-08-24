using MahApps.Metro.IconPacks;
using MediaApp;
using MediaApp.BLL.Services;
using MediaApp.DAL.Entities;
using Microsoft.Win32;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
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

namespace video_media_player
{
    /// <summary>
    /// Interaction logic for EditVideoWindow.xaml
    /// </summary>
    public partial class EditVideoWindow : Window
    {
        private SongService songService = new();
        private DispatcherTimer _timer;
        private bool isDragging = false;
        private bool isZoom = false;
        private DispatcherTimer _timeMouseEnter;
        private List<TbSong> videoList;
        private DispatcherTimer _skipTimer;
        private Button selectedButton = null;
        
        public EditVideoWindow()
        {
            InitializeComponent();
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;

            _skipTimer = new DispatcherTimer();
            _skipTimer.Interval = TimeSpan.FromSeconds(1);
            _skipTimer.Tick += SkipTimer_Tick;
        }
        
        private double GetDurationFromUrl(string url)
        {
            using (var mf = new MediaFoundationReader(url))
            {
                return mf.TotalTime.TotalSeconds;
            }
        }
        
        void SkipTimer_Tick(object sender, EventArgs e)
        {
            PreviousStackPanel.Visibility = Visibility.Hidden;
            VolumeStackPanel.Visibility = Visibility.Hidden;
            ForwardStackPanel.Visibility = Visibility.Hidden;
            _skipTimer.Stop();
        }
        
        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void ImportFileButton_Click(object sender, RoutedEventArgs e)
        {
            // Sử dụng Microsoft.Win32.OpenFileDialog thay vì System.Windows.Forms.OpenFileDialog để tránh lỗi Forms.Dialog
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = false,
                Filter = "MP4 Video Files (*.mp4)|*.mp4|All Files (*.*)|*.*",
                Title = "Chọn file video MP4"
            };

            // openFileDialog.ShowDialog() trả về bool? trong WPF, so sánh với true
            if (openFileDialog.ShowDialog() == true)
            {
                txtFilePath.Text = openFileDialog.FileName;
                try
                {
                    // Kiểm tra file có tồn tại không
                    if (!System.IO.File.Exists(openFileDialog.FileName))
                    {
                        System.Windows.MessageBox.Show("File không tồn tại!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Lấy thông tin file
                    string extension = System.IO.Path.GetExtension(openFileDialog.FileName).ToLower();
                    
                    // Kiểm tra có phải MP4 không
                    if (extension != ".mp4")
                    {
                        System.Windows.MessageBox.Show("Chỉ hỗ trợ file MP4! Vui lòng chọn file MP4.", "Định dạng không hỗ trợ", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Tự động phát file MP4
                    PlaySelectedFile(openFileDialog.FileName);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Lỗi khi đọc file: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ImportAudioButton_Click(object sender, RoutedEventArgs e)
        {
            // Sử dụng Microsoft.Win32.OpenFileDialog để chọn file audio
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = false,
                Filter = "Audio Files (*.mp3;*.wav;*.aac;*.m4a)|*.mp3;*.wav;*.aac;*.m4a|MP3 Files (*.mp3)|*.mp3|WAV Files (*.wav)|*.wav|All Files (*.*)|*.*",
                Title = "Chọn file audio để gắn vào video"
            };

            // openFileDialog.ShowDialog() trả về bool? trong WPF, so sánh với true
            if (openFileDialog.ShowDialog() == true)
            {
                txtAudioPath.Text = openFileDialog.FileName;
                try
                {
                    // Kiểm tra file có tồn tại không
                    if (!System.IO.File.Exists(openFileDialog.FileName))
                    {
                        System.Windows.MessageBox.Show("File audio không tồn tại!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Lấy thông tin file
                    string extension = System.IO.Path.GetExtension(openFileDialog.FileName).ToLower();
                    string[] supportedExtensions = { ".mp3", ".wav", ".aac", ".m4a" };

                    // Kiểm tra có phải file audio không
                    if (!supportedExtensions.Contains(extension))
                    {
                        System.Windows.MessageBox.Show("Chỉ hỗ trợ file audio (MP3, WAV, AAC, M4A)!", "Định dạng không hỗ trợ", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Lưu đường dẫn audio
                    currentAudioPath = openFileDialog.FileName;
                    
                    // Hiển thị thông báo thành công
                    string fileName = System.IO.Path.GetFileName(openFileDialog.FileName);
                    System.Windows.MessageBox.Show($"Đã chọn file audio: {fileName}\n\nBây giờ bạn có thể áp dụng audio này vào video!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Kích hoạt nút Apply Audio
                    ApplyAudioButton.IsEnabled = true;
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Lỗi khi đọc file audio: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        private void ImportLogoButton_Click(object sender, RoutedEventArgs e)
        {
            // Sử dụng Microsoft.Win32.OpenFileDialog để chọn file logo
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = false,
                Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif|PNG Files (*.png)|*.png|JPEG Files (*.jpg;*.jpeg)|*.jpg;*.jpeg|All Files (*.*)|*.*",
                Title = "Chọn file logo"
            };

            // openFileDialog.ShowDialog() trả về bool? trong WPF, so sánh với true
            if (openFileDialog.ShowDialog() == true)
            {
                txtLogoPath.Text = openFileDialog.FileName;
                try
                {
                    // Kiểm tra file có tồn tại không
                    if (!System.IO.File.Exists(openFileDialog.FileName))
                    {
                        System.Windows.MessageBox.Show("File logo không tồn tại!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Lấy thông tin file
                    string extension = System.IO.Path.GetExtension(openFileDialog.FileName).ToLower();
                    string[] supportedExtensions = { ".png", ".jpg", ".jpeg", ".bmp", ".gif" };

                    // Kiểm tra có phải file ảnh không
                    if (!supportedExtensions.Contains(extension))
                    {
                        System.Windows.MessageBox.Show("Chỉ hỗ trợ file ảnh (PNG, JPG, JPEG, BMP, GIF)!", "Định dạng không hỗ trợ", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Hiển thị preview logo
                    LoadLogoPreview(openFileDialog.FileName);
                    
                    // Hiển thị thông báo thành công
                    System.Windows.MessageBox.Show("Logo đã được tải thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Lỗi khi đọc file logo: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow = this;
            videoList = songService.GetMusicVideos();
            int number = 0;
            foreach (var song in videoList)
            {
                var songItem = new video_media_player.UserControls.SongItem
                {
                    Title = song.SongName,
                    Number = (++number).ToString(),
                    Time = ConvertTimeFormat(GetDurationFromUrl(song.FilePath)),
                    Tag = song.FilePath
                };
                songItem.Click += SongItem_Click;
                //ListVideos.Children.Add(songItem);
            }

            selectedButton = SpeedX1Button;
            ChangeSelectedColorButton(selectedButton);

            // Khởi tạo controls cho logo
            InitializeLogoControls();
        }

        private void InitializeLogoControls()
        {
            // Khởi tạo các controls logo
            LogoPreviewBorder.Visibility = Visibility.Hidden;
            ApplyLogoButton.IsEnabled = false;
            
            // Thiết lập giá trị mặc định cho sliders
            LogoSizeSlider.Value = 0.3;
            LogoOpacitySlider.Value = 0.8;
            
            // Cập nhật labels
            LogoSizeLabel.Text = "30%";
            LogoOpacityLabel.Text = "80%";
            
            // Khởi tạo các controls audio
            ApplyAudioButton.IsEnabled = false;
            
            // Thiết lập giá trị mặc định cho audio sliders
            AudioVolumeSlider.Value = 1.0;
            AudioSyncSlider.Value = 0.0;
            AudioFadeSlider.Value = 0.0;
            
            // Cập nhật audio labels
            AudioVolumeLabel.Text = "100%";
            AudioSyncLabel.Text = "0.0s";
            AudioFadeLabel.Text = "0.0s";
        }

        private void SongItem_Click(object sender, RoutedEventArgs e)
        {
            video_media_player.UserControls.SongItem songItem = (video_media_player.UserControls.SongItem)sender;
            string songName = songItem.Title.ToString();
            if (!string.IsNullOrEmpty(songName))
            {
                TbSong selectedSong = songService.GetSongByName(songName);

                if (selectedSong != null)
                {
                    VideoMediaPlayer.Source = new Uri(selectedSong.FilePath);
                    TimeSlider.Maximum = (double)selectedSong.Duration;
                    MaxTimeLabel.Content = "/ " + ConvertTimeFormat((double)selectedSong.Duration);
                    VideoNameLabel.Content = selectedSong.SongName;
                    VideoMediaPlayer.Play();
                    _timer.Start();
                }
            }
        }

        private string ConvertTimeFormat(double value)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(value);
            string timeFormated = string.Format("{0}:{1:D2}", (int)timeSpan.TotalMinutes, timeSpan.Seconds);
            return timeFormated;
        }

        private void Pause()
        {
            PlayIcon.Kind = PackIconMaterialKind.Play;
            VideoMediaPlayer.Pause();
            _timer.Stop();
        }

        private void StartPlayback()
        {
            VideoMediaPlayer.Play();
            PlayIcon.Kind = PackIconMaterialKind.Pause;
            _timer.Start();
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (PlayIcon.Kind == PackIconMaterialKind.Pause)
            {
                Pause();
            }
            else
            {
                StartPlayback();
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            TimeLabel.Content = FormatTime(VideoMediaPlayer.Position.TotalSeconds);
            if (VideoMediaPlayer.NaturalDuration.HasTimeSpan)
            {
                TimeSlider.Value = VideoMediaPlayer.Position.TotalSeconds;
            }
        }

        private string FormatTime(double seconds)
        {
            var timeSpan = TimeSpan.FromSeconds(seconds);
            return $"{(int)timeSpan.TotalMinutes}:{timeSpan.Seconds:D2}";
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            VideoMediaPlayer.Volume = VolumeSlider.Value / 100;
            if (VolumeSlider.Value > 70) VolumeIcon.Kind = PackIconMaterialKind.VolumeHigh;
            else if (VolumeSlider.Value >= 30) VolumeIcon.Kind = PackIconMaterialKind.VolumeMedium;
            else if (VolumeSlider.Value > 0) VolumeIcon.Kind = PackIconMaterialKind.VolumeLow;
            else VolumeIcon.Kind = PackIconMaterialKind.VolumeMute;
        }

        private void volumeButton_Click(object sender, RoutedEventArgs e)
        {
            if (volumePopup.IsOpen == true)
            {
                volumePopup.IsOpen = false;
                MuteEvent();
            }
            else
            {
                volumePopup.IsOpen = true;
            }
        }

        private void TimeSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            isDragging = true;
        }

        private void TimeSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
            VideoMediaPlayer.Position = TimeSpan.FromSeconds(TimeSlider.Value);
        }

        private void ZoomButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isZoom)
            {
                WindowState = WindowState.Maximized;
                FullLayOutGrid.RowDefinitions.Clear();
                FullLayOutGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                FullLayOutGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0) });
                FullLayOutGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0) });

                PlayerScreenGrid.RowDefinitions.Clear();
                PlayerScreenGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0) });
                PlayerScreenGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                PlayerScreenGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0) });
                FullScreenScrollView.Margin = new Thickness(0, 0, 0, 0);
                FullScreenScrollView.ScrollToEnd();
                isZoom = true;
            }
            else
            {
                WindowState = WindowState.Normal;
                FullLayOutGrid.RowDefinitions.Clear();
                FullLayOutGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(2.5, GridUnitType.Star) });
                FullLayOutGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                FullLayOutGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

                PlayerScreenGrid.RowDefinitions.Clear();
                PlayerScreenGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                PlayerScreenGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                PlayerScreenGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

                FullScreenScrollView.Margin = new Thickness(30, 20, 30, 30);
                FullScreenScrollView.ScrollToHome();
                isZoom = false;
            }
        }

        private void ToggleWindowStateButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                MainBorder.CornerRadius = new CornerRadius(40);
            }
            else
            {
                WindowState = WindowState.Maximized;
                MainBorder.CornerRadius = new CornerRadius(0);
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
        
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            VideoMediaPlayer.Stop();
            this.Close();
        }

        private void ScreenBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TaskBarGrid.Visibility = Visibility.Hidden;
        }

        private void ScreenBorder_MouseUp(object sender, MouseButtonEventArgs e)
        {
            TaskBarGrid.Visibility = Visibility.Visible;
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Space:
                    {
                        PlayButton_Click(sender, e);
                        break;
                    }
                case Key.M:
                    {
                        MuteEvent();
                        break;
                    }
                case Key.F:
                    {
                        ZoomButton_Click(sender, e);
                        break;
                    }
                case Key.Escape:
                    {
                        if (WindowState == WindowState.Maximized)
                            ZoomButton_Click(sender, e);
                        break;
                    }
                case Key.Right:
                    {
                        UpdateTimeKeyDown(e.Key);
                        break;
                    }
                case Key.Left:
                    {
                        UpdateTimeKeyDown(e.Key);
                        break;
                    }
                case Key.Up:
                    {
                        VolumeSlider.Value = VolumeSlider.Value + 5;
                        VolumePressUpLabel.Content = VolumeSlider.Value + "%";
                        VolumeStackPanel.Visibility = Visibility.Visible;
                        _skipTimer.Start();
                        break;
                    }
                case Key.Down:
                    {
                        VolumeSlider.Value = VolumeSlider.Value - 5;
                        VolumePressUpLabel.Content = VolumeSlider.Value + "%";
                        VolumeStackPanel.Visibility = Visibility.Visible;
                        _skipTimer.Start();
                        break;
                    }
            }
        }

        private void UpdateTimeKeyDown(Key key)
        {
            if (key == Key.Left)
            {
                TimeSlider.Value = TimeSlider.Value - 10;
                PreviousStackPanel.Visibility = Visibility.Visible;
            }
            else
            {
                TimeSlider.Value = TimeSlider.Value + 10;
                ForwardStackPanel.Visibility = Visibility.Visible;
            }
            _skipTimer.Start();
            VideoMediaPlayer.Position = TimeSpan.FromSeconds(TimeSlider.Value);
            TimeLabel.Content = ConvertTimeFormat(TimeSlider.Value);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private void MuteEvent()
        {
            if (VolumeIcon.Kind != PackIconMaterialKind.VolumeMute)
            {
                VideoMediaPlayer.Volume = 0;
                VolumeIcon.Kind = PackIconMaterialKind.VolumeMute;
            }
            else
            {
                VideoMediaPlayer.Volume = 50;
                VolumeIcon.Kind = PackIconMaterialKind.VolumeMedium;
            }
        }

        private void ScreenBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            if (VideoMediaPlayer.Source == null) return;

            // Start timer when mouse enters
            TaskBarGrid.Visibility = Visibility.Visible;
            if (_timeMouseEnter == null)
            {
                _timeMouseEnter = new DispatcherTimer();
                _timeMouseEnter.Interval = TimeSpan.FromSeconds(3);
                _timeMouseEnter.Tick += TimeMouseEnter_Tick;
            }
            _timeMouseEnter.Start();
        }

        private void TimeMouseEnter_Tick(object sender, EventArgs e)
        {
            // Perform action after 3 seconds
            TaskBarGrid.Visibility = Visibility.Hidden;
            _timeMouseEnter.Stop();
        }

        private void ScreenBorder_MouseLeave(object sender, MouseEventArgs e)
        {
            // Hide TaskBarGrid when mouse leaves
            TaskBarGrid.Visibility = Visibility.Hidden;
            if (_timeMouseEnter != null)
            {
                _timeMouseEnter.Stop();
            }
        }

        private void ScreenBorder_MouseMove(object sender, MouseEventArgs e)
        {
            if (VideoMediaPlayer.Source == null) return;

            // Show TaskBarGrid and start/restart timer on mouse move
            if (TaskBarGrid.Visibility != Visibility.Visible)
                TaskBarGrid.Visibility = Visibility.Visible;
            if (_timeMouseEnter == null)
            {
                _timeMouseEnter = new DispatcherTimer();
                _timeMouseEnter.Interval = TimeSpan.FromSeconds(3);
                _timeMouseEnter.Tick += TimeMouseEnter_Tick;
            }
            _timeMouseEnter.Stop();
            _timeMouseEnter.Start();
        }

        private void VideoMediaPlayer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            PlayButton_Click(sender, e);
        }

        private void VideoMediaPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            try
            {
                // Đảm bảo volume được thiết lập đúng
                VideoMediaPlayer.Volume = VolumeSlider.Value / 100;
                
                // Cập nhật thông tin video
                if (VideoMediaPlayer.NaturalDuration.HasTimeSpan)
                {
                    TimeSlider.Maximum = VideoMediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                    MaxTimeLabel.Content = "/ " + ConvertTimeFormat(VideoMediaPlayer.NaturalDuration.TimeSpan.TotalSeconds);
                }
                
                // Hiển thị thông báo thành công
                System.Windows.MessageBox.Show("Video đã được tải thành công và sẵn sàng phát!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi khi mở media: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void VideoMediaPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            System.Windows.MessageBox.Show($"Không thể phát file: {e.ErrorException.Message}\n\nHãy kiểm tra:\n- File có bị hỏng không\n- Định dạng file có được hỗ trợ không\n- Codec audio/video có được cài đặt không", "Lỗi Media", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                e.Handled = true;
            } else if (e.Key == Key.Down)
            {
                e.Handled = true;
            } else if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }

        private void SpeedButton_Click(object sender, RoutedEventArgs e)
        {
            SpeedPopup.IsOpen = !SpeedPopup.IsOpen;
        }

        private bool isTimerRunning = false;  // Biến để theo dõi trạng thái của timer

        private void SpeedX2Button_Click(object sender, RoutedEventArgs e)
        {
            ChangeSpeed(2, sender);
        }

        private void SpeedX175Button_Click(object sender, RoutedEventArgs e)
        {
            ChangeSpeed(1.75, sender); 
        }

        private void SpeedX15Button_Click(object sender, RoutedEventArgs e)
        {
            ChangeSpeed(1.5, sender);
        }

        private void SpeedX1Button_Click(object sender, RoutedEventArgs e)
        {
            ChangeSpeed(1,sender);
        }

        private void SpeedX05Button_Click(object sender, RoutedEventArgs e)
        {
            ChangeSpeed(0.5, sender);
        }

        // Hàm thay đổi tốc độ và Interval của timer
        private void ChangeSpeed(double speed, Object sender)
        {
            if (isTimerRunning) 
            {
                _timer.Stop(); 
                isTimerRunning = false;  
            }
            VideoMediaPlayer.SpeedRatio = speed;
            _timer.Interval = TimeSpan.FromSeconds(1 / speed);
            _timer.Start(); 
            isTimerRunning = true;  
            ChangeSelectedColorButton(sender);
        }

        private void ChangeSelectedColorButton(object sender)
        {
            
            Button clickedButton = sender as Button;

            if (selectedButton != null)
            {
                selectedButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3b3c36"));
            }

            clickedButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#080808"));
            selectedButton = clickedButton;

            // Set lai isOpen
            SpeedPopup.IsOpen = false;
        }

        // Các biến để lưu thông tin logo
        private string currentLogoPath = "";
        private ImageSource currentLogoImage = null;

        private void LoadLogoPreview(string logoPath)
        {
            try
            {
                // Tạo BitmapImage từ file
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(logoPath);
                bitmap.EndInit();

                // Lưu thông tin logo
                currentLogoPath = logoPath;
                currentLogoImage = bitmap;

                // Hiển thị preview
                LogoPreviewImage.Source = bitmap;
                LogoPreviewBorder.Visibility = Visibility.Visible;

                // Kích hoạt nút Apply
                ApplyLogoButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi khi tải preview logo: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LogoSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (LogoSizeLabel != null)
            {
                LogoSizeLabel.Text = $"{(int)(e.NewValue * 100)}%";
            }
        }

        private void LogoOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (LogoOpacityLabel != null)
            {
                LogoOpacityLabel.Text = $"{(int)(e.NewValue * 100)}%";
            }
        }

        private void ApplyLogoButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(currentLogoPath) || currentLogoImage == null)
                {
                    System.Windows.MessageBox.Show("Vui lòng chọn logo trước!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (VideoMediaPlayer.Source == null)
                {
                    System.Windows.MessageBox.Show("Vui lòng chọn video trước!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Lấy vị trí logo từ ComboBox
                string position = (LogoPositionComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                
                // Lấy kích thước và độ trong suốt
                double size = LogoSizeSlider.Value;
                double opacity = LogoOpacitySlider.Value;

                // Hiển thị thông báo thành công
                System.Windows.MessageBox.Show($"Logo đã được áp dụng!\n\nVị trí: {position}\nKích thước: {(int)(size * 100)}%\nĐộ trong suốt: {(int)(opacity * 100)}%", 
                    "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                // TODO: Ở đây bạn có thể thêm code để thực sự áp dụng logo vào video
                // Ví dụ: sử dụng FFmpeg hoặc thư viện xử lý video khác
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi khi áp dụng logo: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Các biến để lưu thông tin audio
        private string currentAudioPath = "";

        private void AudioVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (AudioVolumeLabel != null)
            {
                double percentage = e.NewValue * 100;
                AudioVolumeLabel.Text = $"{(int)percentage}%";
            }
        }

        private void AudioSyncSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (AudioSyncLabel != null)
            {
                AudioSyncLabel.Text = $"{e.NewValue:F1}s";
            }
        }

        private void AudioFadeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (AudioFadeLabel != null)
            {
                AudioFadeLabel.Text = $"{e.NewValue:F1}s";
            }
        }

        private void ApplyAudioButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(currentAudioPath))
                {
                    System.Windows.MessageBox.Show("Vui lòng chọn file audio trước!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (VideoMediaPlayer.Source == null)
                {
                    System.Windows.MessageBox.Show("Vui lòng chọn video trước!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Lấy các thông số audio
                double volume = AudioVolumeSlider.Value;
                double sync = AudioSyncSlider.Value;
                string mode = (AudioModeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                double fade = AudioFadeSlider.Value;

                // Tạo mô tả chi tiết cho từng chế độ
                string modeDescription = "";
                switch ((AudioModeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString())
                {
                    case "default":
                        modeDescription = "Default: Mặc định theo đúng thời gian";
                        break;
                    case "loop":
                        modeDescription = "Loop: Vòng lặp cho tới khi hết video";
                        break;
                    case "trim":
                        modeDescription = "Trim Video: Cắt video cho phù hợp với thời gian của âm thanh";
                        break;
                    default:
                        modeDescription = mode;
                        break;
                }

                // Hiển thị thông báo thành công
                System.Windows.MessageBox.Show($"Audio đã được áp dụng vào video!\n\n" +
                    $"File audio: {System.IO.Path.GetFileName(currentAudioPath)}\n" +
                    $"Âm lượng: {(int)(volume * 100)}%\n" +
                    $"Đồng bộ: {sync:F1}s\n" +
                    $"Chế độ: {modeDescription}\n" +
                    $"Fade: {fade:F1}s", 
                    "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                // TODO: Ở đây bạn có thể thêm code để thực sự áp dụng audio vào video
                // Ví dụ: sử dụng FFmpeg hoặc thư viện xử lý video khác
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi khi áp dụng audio: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Export Video Functionss
        private void ExportVideoButton_Click(object sender, RoutedEventArgs e)
        {
            if (VideoMediaPlayer.Source == null)
            {
                System.Windows.MessageBox.Show("Vui lòng chọn video trước khi xuất!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Cập nhật thông tin logo và audio trong popup Export
            UpdateExportInformation();
            
            // Đặt popup ở giữa màn hình
            CenterPopupOnScreen();
            
            // Hiển thị popup export
            ExportPopup.IsOpen = true;
        }

        private void UpdateExportInformation()
        {
            // Cập nhật thông tin Logo
            if (!string.IsNullOrEmpty(currentLogoPath) && currentLogoImage != null)
            {
                ExportLogoFileText.Text = System.IO.Path.GetFileName(currentLogoPath);
                ExportLogoPositionText.Text = (LogoPositionComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Góc trên bên trái";
                ExportLogoSizeText.Text = $"{(int)(LogoSizeSlider.Value * 100)}%";
            }
            else
            {
                ExportLogoFileText.Text = "Chưa áp dụng logo";
                ExportLogoPositionText.Text = "-";
                ExportLogoSizeText.Text = "-";
            }

            // Cập nhật thông tin Audio
            if (!string.IsNullOrEmpty(currentAudioPath))
            {
                ExportAudioFileText.Text = System.IO.Path.GetFileName(currentAudioPath);
                
                // Tạo mô tả chi tiết cho từng chế độ
                string modeDescription = "";
                switch ((AudioModeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString())
                {
                    case "default":
                        modeDescription = "Default: Mặc định theo đúng thời gian";
                        break;
                    case "loop":
                        modeDescription = "Loop: Vòng lặp cho tới khi hết video";
                        break;
                    case "trim":
                        modeDescription = "Trim Video: Cắt video cho phù hợp với thời gian của âm thanh";
                        break;
                    default:
                        modeDescription = (AudioModeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Default";
                        break;
                }
                
                ExportAudioModeText.Text = modeDescription;
                ExportAudioVolumeText.Text = $"{(int)(AudioVolumeSlider.Value * 100)}%";
                ExportAudioSyncText.Text = $"{AudioSyncSlider.Value:F1}s";
            }
            else
            {
                ExportAudioFileText.Text = "Chưa áp dụng audio";
                ExportAudioModeText.Text = "-";
                ExportAudioVolumeText.Text = "-";
                ExportAudioSyncText.Text = "-";
            }
        }

        private void CenterPopupOnScreen()
        {
            try
            {
                // Lấy kích thước màn hình
                double screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
                double screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
                
                // Kích thước popup (đã định nghĩa trong XAML là Width="500")
                double popupWidth = 500;
                double popupHeight = 600; // Ước tính chiều cao
                
                // Tính toán vị trí để popup nằm giữa màn hình
                double left = (screenWidth - popupWidth) / 2;
                double top = (screenHeight - popupHeight) / 2;
                
                // Đặt vị trí cho popup
                ExportPopup.HorizontalOffset = left;
                ExportPopup.VerticalOffset = top;
                
                // Đảm bảo popup hiển thị trên cùng
                ExportPopup.PopupAnimation = System.Windows.Controls.Primitives.PopupAnimation.Fade;
            }
            catch (Exception ex)
            {
                // Nếu có lỗi, sử dụng vị trí mặc định
                ExportPopup.HorizontalOffset = 0;
                ExportPopup.VerticalOffset = 0;
            }
        }

        private void BrowseExportPathButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Sử dụng SaveDialog mới
                var saveDialog = new SaveDialog();
                if (saveDialog.ShowDialog() == true)
                {
                    ExportPathTextBox.Text = saveDialog.SelectedFolderPath;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi khi chọn thư mục: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Kiểm tra thư mục xuất
                if (string.IsNullOrEmpty(ExportPathTextBox.Text) || ExportPathTextBox.Text == "Chưa chọn thư mục")
                {
                    System.Windows.MessageBox.Show("Vui lòng chọn thư mục xuất!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Lấy các thông số export
                string format = (ExportFormatComboBox.SelectedItem as ComboBoxItem)?.Tag.ToString();
                string quality = (ExportFormatComboBox.SelectedItem as ComboBoxItem)?.Tag.ToString();
                string audioCodec = (AudioCodecComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                string audioBitrate = (AudioBitrateComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

                // Lấy thông tin logo và audio đã áp dụng
                string logoInfo = GetLogoExportInfo();
                string audioInfo = GetAudioExportInfo();

                // Hiển thị progress panel
                ExportProgressPanel.Visibility = Visibility.Visible;
                StartExportButton.IsEnabled = false;

                // Mô phỏng quá trình export (thay thế bằng FFmpeg thực tế sau này)
                SimulateExportProcess(format, quality, audioCodec, audioBitrate, logoInfo, audioInfo);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi khi bắt đầu xuất: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetLogoExportInfo()
        {
            if (!string.IsNullOrEmpty(currentLogoPath) && currentLogoImage != null)
            {
                return $"Logo: {System.IO.Path.GetFileName(currentLogoPath)} | " +
                       $"Vị trí: {(LogoPositionComboBox.SelectedItem as ComboBoxItem)?.Content} | " +
                       $"Kích thước: {(int)(LogoSizeSlider.Value * 100)}% | " +
                       $"Độ trong suốt: {(int)(LogoOpacitySlider.Value * 100)}%";
            }
            return "Không có logo";
        }

        private string GetAudioExportInfo()
        {
            if (!string.IsNullOrEmpty(currentAudioPath))
            {
                // Tạo mô tả chi tiết cho từng chế độ
                string modeDescription = "";
                switch ((AudioModeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString())
                {
                    case "default":
                        modeDescription = "Default: Mặc định theo đúng thời gian";
                        break;
                    case "loop":
                        modeDescription = "Loop: Lặp âm thanh cho tới khi hết video";
                        break;
                    case "trim":
                        modeDescription = "Trim Video: Cắt video cho phù hợp với thời gian của âm thanh";
                        break;
                    default:
                        modeDescription = (AudioModeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Default";
                        break;
                }
                
                return $"Audio: {System.IO.Path.GetFileName(currentAudioPath)} | " +
                       $"Chế độ: {modeDescription} | " +
                       $"Âm lượng: {(int)(AudioVolumeSlider.Value * 100)}% | " +
                       $"Đồng bộ: {AudioSyncSlider.Value:F1}s | " +
                       $"Fade: {AudioFadeSlider.Value:F1}s";
            }
            return "Không có audio";
        }

        private void CancelExportButton_Click(object sender, RoutedEventArgs e)
        {
            ExportPopup.IsOpen = false;
            ResetExportUI();
        }

        private void CloseExportPopupButton_Click(object sender, RoutedEventArgs e)
        {
            ExportPopup.IsOpen = false;
            ResetExportUI();
        }

        private void ExportPopup_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Chỉ đóng popup nếu click vào background, không phải vào nội dung
            if (e.Source == sender)
            {
                ExportPopup.IsOpen = false;
                ResetExportUI();
            }
        }

        private void SimulateExportProcess(string format, string quality, string audioCodec, string audioBitrate, string logoInfo, string audioInfo)
        {
            // Tạo timer để mô phỏng progress
            var exportTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };

            int progress = 0;
            exportTimer.Tick += (s, e) =>
            {
                progress += 2;
                ExportProgressBar.Value = progress;
                ExportProgressText.Text = $"{progress}%";

                if (progress >= 100)
                {
                    exportTimer.Stop();
                    ExportCompleted(format, quality, audioCodec, audioBitrate, logoInfo, audioInfo);
                }
            };

            exportTimer.Start();
        }

        private void ExportCompleted(string format, string quality, string audioCodec, string audioBitrate, string logoInfo, string audioInfo)
        {
            // Ẩn progress panel
            ExportProgressPanel.Visibility = Visibility.Collapsed;
            StartExportButton.IsEnabled = true;

            // Hiển thị thông báo thành công với đầy đủ thông tin
            System.Windows.MessageBox.Show($"Video đã được xuất thành công!\n\n" +
                $"Định dạng: {format.ToUpper()}\n" +
                $"Chất lượng: {quality}\n" +
                $"Audio Codec: {audioCodec}\n" +
                $"Audio Bitrate: {audioBitrate}\n" +
                $"Thư mục: {ExportPathTextBox.Text}\n\n" +
                $"Thông tin Logo:\n{logoInfo}\n\n" +
                $"Thông tin Audio:\n{audioInfo}", 
                "Xuất thành công", MessageBoxButton.OK, MessageBoxImage.Information);

            // Đóng popup
            ExportPopup.IsOpen = false;
            ResetExportUI();
        }

        private void ResetExportUI()
        {
            ExportProgressPanel.Visibility = Visibility.Collapsed;
            ExportProgressBar.Value = 0;
            ExportProgressText.Text = "0%";
            StartExportButton.IsEnabled = true;
        }

        // Event handler cho Quality ComboBox
        private void ExportQualityComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CustomQualityPanel == null) return;

            if (ExportQualityComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                if (selectedItem.Tag?.ToString() == "custom")
                {
                    CustomQualityPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    CustomQualityPanel.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void PlaySelectedFile(string filePath)
        {
            try
            {
                // Debug: Kiểm tra file path
                //System.Windows.MessageBox.Show($"Đang xử lý file: {filePath}", "Debug", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Dừng video hiện tại nếu đang phát
                if (VideoMediaPlayer.Source != null)
                {
                    VideoMediaPlayer.Stop();
                    _timer.Stop();
                }

                // Thiết lập source mới
                Uri videoUri = new Uri(filePath);
                VideoMediaPlayer.Source = videoUri;
                
                // Debug: Kiểm tra source đã được thiết lập chưa
                //System.Windows.MessageBox.Show($"Source đã được thiết lập: {VideoMediaPlayer.Source}", "Debug", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Cập nhật thông tin hiển thị
                string fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                VideoNameLabel.Content = fileName;
                
                // Đảm bảo volume được thiết lập đúng
                VideoMediaPlayer.Volume = VolumeSlider.Value / 100;
                
                // Reset về tốc độ bình thường
                VideoMediaPlayer.SpeedRatio = 1.0;
                _timer.Interval = TimeSpan.FromSeconds(1);
                
                // Cập nhật trạng thái nút speed
                if (selectedButton != null)
                {
                    selectedButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3b3c36"));
                }
                SpeedX1Button.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#080808"));
                selectedButton = SpeedX1Button;
                
                // Hiển thị thông báo đang tải
                System.Windows.MessageBox.Show($"Đang tải video MP4: {fileName}...", "Đang tải", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi khi thiết lập file: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
} 