using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using ApartmentInventory.Models;

namespace ApartmentInventory.Views
{
    public partial class AddItemWindow : Window
    {
        public byte[] ImageData { get; private set; }
        public string ItemName { get; set; }
        public string ItemType { get; set; }
        public string Description { get; set; }
        public string LocationInRoom { get; set; }
        public Room SelectedRoom { get; set; }
        public Container SelectedContainer { get; set; }

        private List<Room> _rooms;
        private List<Container> _containers;

        public AddItemWindow()
        {
            InitializeComponent();
            Focus();
        }

        public void SetRooms(List<Room> rooms)
        {
            _rooms = rooms;
            if (RoomComboBox != null)
            {
                RoomComboBox.ItemsSource = rooms;
                RoomComboBox.SelectionChanged += RoomComboBox_SelectionChanged;
            }
        }

        public void SetContainers(List<Container> containers)
        {
            _containers = containers;
            if (ContainerComboBox != null)
            {
                // Добавляем опцию "Нет" (предмет без коробки)
                var containerList = new List<Container> { new Container { Id = -1, Name = "Нет" } };
                containerList.AddRange(containers);
                ContainerComboBox.ItemsSource = containerList;
                ContainerComboBox.SelectedIndex = 0;
            }
        }

        public void PreselectRoomAndContainer(Room room, Container container)
        {
            if (room != null && _rooms != null)
            {
                var matchingRoom = _rooms.FirstOrDefault(r => r.Id == room.Id);
                if (matchingRoom != null)
                {
                    RoomComboBox.SelectedItem = matchingRoom;

                    // Обновляем список контейнеров перед выбором контейнера.
                    // Вызов RoomComboBox.SelectedItem провоцирует вызов RoomComboBox_SelectionChanged
                    // Но на всякий случай устанавливаем напрямую. 
                    var roomContainers = matchingRoom.Containers?.ToList() ?? new List<Container>();
                    SetContainers(roomContainers);
                }
            }

            if (container != null && _containers != null)
            {
                // Ищем рекурсивно по id, так как список контейнеров комнаты может не иметь плоской структуры,
                // либо контейнер может лежать не напрямую в комнате, а в другом контейнере. 
                // Но у нас SetContainers получает только прямые контейнеры, 
                // поэтому нужно собрать все контейнеры комнаты в плоский список.
                var allRoomContainers = GetAllContainersInRoom(room ?? container.Room);
                SetContainers(allRoomContainers);

                var matchingContainer = ContainerComboBox.Items.Cast<Container>().FirstOrDefault(c => c.Id == container.Id);
                if (matchingContainer != null)
                {
                    ContainerComboBox.SelectedItem = matchingContainer;
                }
            }
        }

        private List<Container> GetAllContainersInRoom(Room room)
        {
            var result = new List<Container>();
            if (room?.Containers == null) return result;

            foreach (var container in room.Containers)
            {
                AddContainerAndChildren(container, result);
            }
            return result;
        }

        private void AddContainerAndChildren(Container container, List<Container> list)
        {
            list.Add(container);
            if (container.ChildContainers != null)
            {
                foreach (var child in container.ChildContainers)
                {
                    AddContainerAndChildren(child, list);
                }
            }
        }

        private void RoomComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var selectedRoom = RoomComboBox.SelectedItem as Room;
            if (selectedRoom != null)
            {
                // Обновляем список контейнеров в зависимости от выбранной комнаты (включая вложенные)
                var allContainers = GetAllContainersInRoom(selectedRoom);
                SetContainers(allContainers);
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ItemNameTextBox.Text))
            {
                MessageBox.Show("Название предмета не может быть пустым.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ItemName = ItemNameTextBox.Text;
            ItemType = ItemTypeTextBox.Text ?? string.Empty;
            Description = ItemDescriptionTextBox.Text ?? string.Empty;
            LocationInRoom = LocationTextBox.Text ?? string.Empty;
            SelectedRoom = RoomComboBox.SelectedItem as Room;
            SelectedContainer = ContainerComboBox.SelectedItem as Container;

            DialogResult = true;
            Close();
        }

        private void ContainerComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

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
    }
}
