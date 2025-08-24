using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace video_media_player
{
    /// <summary>
    /// Interaction logic for SaveDialog.xaml
    /// </summary>
    public partial class SaveDialog : Window
    {
        public string SelectedFolderPath { get; private set; }

        public SaveDialog()
        {
            InitializeComponent();
            SelectedFolderPath = "";
        }

        private void QuickFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string folderType)
            {
                string folderPath = GetSpecialFolderPath(folderType);
                if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
                {
                    SelectedFolderPath = folderPath;
                    CurrentPathTextBlock.Text = folderPath;
                    OKButton.IsEnabled = true;
                }
                else
                {
                    MessageBox.Show($"Thư mục {folderType} không tồn tại!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private string GetSpecialFolderPath(string folderType)
        {
            try
            {
                switch (folderType)
                {
                    case "Desktop":
                        return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    case "Downloads":
                        return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";
                    case "Documents":
                        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    case "Pictures":
                        return Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                    default:
                        return "";
                }
            }
            catch
            {
                return "";
            }
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Title = "Chọn thư mục để lưu video",
                    ValidateNames = false,
                    CheckFileExists = false,
                    CheckPathExists = true,
                    FileName = "Chọn thư mục này"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string folderPath = Path.GetDirectoryName(openFileDialog.FileName);
                    if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
                    {
                        SelectedFolderPath = folderPath;
                        CurrentPathTextBlock.Text = folderPath;
                        OKButton.IsEnabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi chọn thư mục: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(SelectedFolderPath) && Directory.Exists(SelectedFolderPath))
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một thư mục hợp lệ!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
} 