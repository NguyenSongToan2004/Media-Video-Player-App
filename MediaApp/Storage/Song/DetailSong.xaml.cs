using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using MediaApp.BLL.Services;
using MediaApp.DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TagLib;

namespace MediaApp
{
    public partial class DetailSong : Window
    {
        private Cloudinary _cloudinary;
        private SongService _service = new();
        private ArtistService _artistService = new();
        private AlbumService _albumService = new();
        public TbSong EditSong { get; set; }
        public DetailSong()
        {
            InitializeComponent();
            SetupCloudinary();
        }
        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult answer = System.Windows.MessageBox.Show("Are You Sure?", "Cancel", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer == MessageBoxResult.Yes)
            {
                this.Close();
            }
        }
        private void SetupCloudinary()
        {
            _cloudinary = new Cloudinary(new Account("dpfj7qsae", "464938966635639", "U4sYEHIN4mLuQ4abxneX08e49qs"));
        }
        //private string UploadFileToCloudinary(string filePath, string songName)
        //{
        //    var uploadParams = new VideoUploadParams()
        //    {
        //        File = new FileDescription(filePath),
        //        PublicId = songName.Trim(),
        //        EagerTransforms = new List<Transformation>()
        //        {
        //        new EagerTransformation().Width(300).Height(300).Crop("pad").AudioCodec("none"),
        //        new EagerTransformation().Width(160).Height(100).Crop("crop").Gravity("south").AudioCodec("none"),
        //        },
        //        EagerAsync = true,
        //        EagerNotificationUrl = "https://mysite.example.com/my_notification_endpoint"
        //    };
        //    var uploadResult = _cloudinary.Upload(uploadParams);
        //    string fileUrl = uploadResult.Url.ToString();
        //    return fileUrl;
        //}
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSongName.Text))
            {
                System.Windows.MessageBox.Show("Song Name cannot be empty. Please enter a name for the song.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;  // Dừng quá trình lưu nếu tên bài hát trống
            }

            TbSong song = EditSong ?? new TbSong();
            song.SongName = txtSongName.Text;
            song.FilePath = txtFilePath.Text;
            var file = TagLib.File.Create(txtFilePath.Text);
            TimeSpan duration = file.Properties.Duration;
            song.Duration = (decimal)duration.TotalSeconds;
            song.Type = txtFileType.Text.ToString();
            song.Plays = 0;
            song.ArtistId = Convert.ToInt32(ArtistCombobox.SelectedValue.ToString());
            song.AlbumId = AlbumCombobox.SelectedValue != null ? Convert.ToInt32(AlbumCombobox.SelectedValue.ToString()) : (int?)null;

            if (EditSong == null)
            {
                //song.FilePath = UploadFileToCloudinary(txtFilePath.Text, txtSongName.Text);
                _service.Create(song);
            }
            else
            {
                //song.FilePath = UploadFileToCloudinary(txtFilePath.Text, txtSongName.Text);
                _service.Update(song);
            }

            System.Windows.MessageBox.Show("Song has been saved", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }


        private void ImportFileButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Audio Files (*.mp3;*.wav)|*.mp3;*.wav | Video Files (*.mp4)|*.mp4"
            };

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtFilePath.Text = openFileDialog.FileName;
                try
                {
                    var file = TagLib.File.Create(txtFilePath.Text);
                    TimeSpan duration = file.Properties.Duration;
                    txtDuration.Text = ConvertTimeFormat(duration.TotalSeconds);

                    // Xác định loại file dựa trên phần mở rộng
                    string extension = System.IO.Path.GetExtension(openFileDialog.FileName).ToLower();
                    txtFileType.Text = extension switch
                    {
                        ".mp3" or ".wav" => "mp3",
                        ".mp4" => "mp4",
                        _ => "Unknown"
                    };
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Lỗi khi đọc file: " + ex.Message);
                }
            }

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ArtistCombobox.ItemsSource = _artistService.GetAll();

            ArtistCombobox.DisplayMemberPath = "ArtistName";
            ArtistCombobox.SelectedValuePath = "ArtistId";

            AlbumCombobox.ItemsSource = _albumService.GetAllAlbums();
            AlbumCombobox.DisplayMemberPath = "Title";
            AlbumCombobox.SelectedValuePath = "AlbumId";

            //TypeCombobox.ItemsSource = new List<string> { "mp3", "mp4" };
            //TypeCombobox.SelectedIndex = 0;
            if (EditSong != null)
            {
                txtSongName.Text = EditSong.SongName;
                txtDuration.Text = EditSong.Duration.ToString();
                txtFilePath.Text = EditSong.FilePath;
                txtFileType.Text = EditSong.Type;
                ArtistCombobox.SelectedValue = EditSong.ArtistId;
                AlbumCombobox.SelectedValue = EditSong.AlbumId;
                Header.Text = $"Update Song {EditSong.SongName}";
            }
        }
        private string ConvertTimeFormat(double value)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(value);
            string timeFormated = string.Format("{0}:{1:D2}", (int)timeSpan.TotalMinutes, timeSpan.Seconds);
            return timeFormated;
        }
    }
}
