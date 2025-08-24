using MediaApp.DAL.Entities;
using System.Windows;
using System.Windows.Input;
using video_media_player;

namespace MediaApp
{
    public partial class StartWindow : Window
    {
        public TbUser? AuthenticatedUser { get; set; }
        public StartWindow()
        {
            InitializeComponent();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MP3_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            MainWindow mainWindow = new();
            mainWindow.ShowDialog();
            this.Show();
        }

        private void MP4_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            //MusicVideosPage musicVideosPage = new();
            //musicVideosPage.ShowDialog();
            EditVideoWindow mainWindow = new EditVideoWindow();
            mainWindow.ShowDialog();
            this.Show();
        }

        private void Storage_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            StorePage storePage = new();
            storePage.ShowDialog();
            this.Show();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (AuthenticatedUser == null)
            {
                LoginPage loginPage = new();
                loginPage.ShowDialog();
            }

            ToggleWindowStateButton.IsEnabled = false;
            TxtUserName.Text = AuthenticatedUser?.UserName;
            TxtEmail.Text = AuthenticatedUser?.Email;

            if (AuthenticatedUser?.Role.Trim() == "user")
            {
                Storage.Visibility = Visibility.Collapsed;
            }
        }

        private void UserIcon_Click(object sender, RoutedEventArgs e)
        {
            LogoutPopup.IsOpen = !LogoutPopup.IsOpen;
        }
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        { 
            MessageBox.Show("You have been logged out.", "Logout", MessageBoxButton.OK, MessageBoxImage.Information); 
            LogoutPopup.IsOpen = false;
            AuthenticatedUser = null;
            this.Hide();
            LoginPage loginPage = new();
            loginPage.ShowDialog();
        }
    }
}
