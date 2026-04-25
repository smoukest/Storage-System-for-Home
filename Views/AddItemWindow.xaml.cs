using System;
using System.Collections.Generic;
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
        public Container SelectedContainer { get; set; }

        public AddItemWindow()
        {
            InitializeComponent();
            Focus();
        }

        public void SetContainers(List<Container> containers)
        {
            if (ContainerComboBox != null)
            {
                ContainerComboBox.ItemsSource = containers;
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
            SelectedContainer = ContainerComboBox.SelectedItem as Container;

            DialogResult = true;
            Close();
        }
    }
}
