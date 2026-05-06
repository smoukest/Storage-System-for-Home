using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace ApartmentInventory.Views
{
    public partial class AddContainerWindow : Window
    {
        public byte[] ImageData { get; private set; }
        private string _containerName;
        public string ContainerName
        {
            get => _containerName;
            set
            {
                _containerName = value;
                if (ContainerNameTextBox != null)
                    ContainerNameTextBox.Text = value ?? string.Empty;
            }
        }

        private string _containerType;
        public string ContainerType
        {
            get => _containerType;
            set
            {
                _containerType = value;
                if (ContainerTypeTextBox != null)
                    ContainerTypeTextBox.Text = value ?? string.Empty;
            }
        }

        private string _description;
        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                if (ContainerDescriptionTextBox != null)
                    ContainerDescriptionTextBox.Text = value ?? string.Empty;
            }
        }

        private string _locationInRoom;
        public string LocationInRoom
        {
            get => _locationInRoom;
            set
            {
                _locationInRoom = value;
                if (LocationTextBox != null)
                    LocationTextBox.Text = value ?? string.Empty;
            }
        }

        public AddContainerWindow()
        {
            InitializeComponent();
            ContainerNameTextBox.Focus();
        }

        public void LoadExistingImage(byte[] imageData)
        {
            ImageData = imageData;
            if (ImageData != null && ImageData.Length > 0)
            {
                LoadImageToPreview(ImageData);
            }
        }

        private void LoadImageToPreview(byte[] data)
        {
            try
            {
                using (var stream = new MemoryStream(data))
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = stream;
                    image.EndInit();
                    PhotoPreview.Source = image;
                }
            }
            catch { }
        }

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    string filePath = files[0];
                    ProcessImageFile(filePath);
                }
            }
        }

        private void PhotoPreview_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    string filePath = files[0];
                    ProcessImageFile(filePath);
                }
                e.Handled = true;
            }
        }

        private void PhotoPreview_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                ProcessImageFile(openFileDialog.FileName);
            }
        }

        private void ProcessImageFile(string filePath)
        {
            try
            {
                string ext = Path.GetExtension(filePath).ToLower();
                if (ext == ".jpg" || ext == ".jpeg" || ext == ".png")
                {
                    // Читаем файл в массив байт
                    ImageData = File.ReadAllBytes(filePath);
                    LoadImageToPreview(ImageData);
                }
                else
                {
                    MessageBox.Show("Пожалуйста, выберите изображение (JPG, PNG).", "Неверный формат", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке изображения: {ex.Message}");
            }
        }

        private void RemovePhoto_Click(object sender, RoutedEventArgs e)
        {
            ImageData = null;
            PhotoPreview.Source = null;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ContainerNameTextBox.Text))
            {
                MessageBox.Show("Пожалуйста, введите название контейнера", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ContainerName = ContainerNameTextBox.Text;
            ContainerType = ContainerTypeTextBox.Text;
            Description = ContainerDescriptionTextBox.Text;
            LocationInRoom = LocationTextBox.Text;

            this.DialogResult = true;
        }
    }
}
