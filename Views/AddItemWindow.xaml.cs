using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ApartmentInventory.Models;

namespace ApartmentInventory.Views
{
    public partial class AddItemWindow : Window
    {
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

        private void RoomComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var selectedRoom = RoomComboBox.SelectedItem as Room;
            if (selectedRoom != null)
            {
                // Обновляем список контейнеров в зависимости от выбранной комнаты
                var roomContainers = selectedRoom.Containers?.ToList() ?? new List<Container>();
                SetContainers(roomContainers);
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
    }
}
