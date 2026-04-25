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
using ApartmentInventory.Models;
using ApartmentInventory.ViewModels;
using ApartmentInventory.Views;

namespace _25._04
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            this.DataContext = _viewModel;

            _viewModel.AddRoomRequested += OnAddRoomRequested;
            _viewModel.EditRoomRequested += OnEditRoomRequested;
            _viewModel.AddContainerRequested += OnAddContainerRequested;
            _viewModel.EditContainerRequested += OnEditContainerRequested;
            _viewModel.AddItemRequested += OnAddItemRequested;
            _viewModel.EditItemRequested += OnEditItemRequested;

            RefreshTreeView();
        }

        private void RefreshTreeView()
        {
            ItemsTreeView.Items.Clear();
            foreach (var room in _viewModel.Rooms)
            {
                var roomItem = CreateRoomTreeItem(room);
                ItemsTreeView.Items.Add(roomItem);
            }
        }

        private TreeViewItem CreateRoomTreeItem(Room room)
        {
            var roomItem = new TreeViewItem
            {
                Header = $"🏠 {room.Name}",
                Tag = room,
                IsExpanded = false
            };

            // Добавляем коробки
            foreach (var container in room.Containers)
            {
                var containerItem = new TreeViewItem
                {
                    Header = $"📦 {container.Name}",
                    Tag = container
                };

                foreach (var item in container.Items)
                {
                    var itemNode = new TreeViewItem
                    {
                        Header = $"📄 {item.Name} ({item.ItemType})",
                        Tag = item
                    };
                    containerItem.Items.Add(itemNode);
                }

                roomItem.Items.Add(containerItem);
            }

            // Добавляем вещи без контейнера
            foreach (var item in room.Items)
            {
                var itemNode = new TreeViewItem
                {
                    Header = $"📄 {item.Name} ({item.ItemType})",
                    Tag = item
                };
                roomItem.Items.Add(itemNode);
            }

            return roomItem;
        }

        private void ItemsTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selectedItem = ItemsTreeView.SelectedItem as TreeViewItem;
            if (selectedItem?.Tag is Room room)
            {
                _viewModel.SelectedRoom = room;
                _viewModel.SelectedContainer = null;
                _viewModel.SelectedItem = null;
                InfoTextBlock.Text = $"Комната: {room.Name}";
            }
            else if (selectedItem?.Tag is Container container)
            {
                _viewModel.SelectedContainer = container;
                _viewModel.SelectedRoom = container.Room;
                _viewModel.SelectedItem = null;
                InfoTextBlock.Text = $"Контейнер: {container.Name} (в комнате: {container.Room.Name})";
            }
            else if (selectedItem?.Tag is Item item)
            {
                _viewModel.SelectedItem = item;
                _viewModel.SelectedRoom = item.Room;
                _viewModel.SelectedContainer = item.Container;
                var location = string.IsNullOrEmpty(item.LocationInRoom) ? "не указано" : item.LocationInRoom;
                var container_name = item.Container?.Name ?? "нет";
                InfoTextBlock.Text = $"Вещь: {item.Name} | Вид: {item.ItemType} | Контейнер: {container_name} | Местоположение: {location} | Описание: {item.Description}";
            }
        }

        private void AddRoomButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddRoomWindow { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                _viewModel.AddRoom(dialog.RoomName);
                RefreshTreeView();
            }
        }

        private void AddContainerButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedRoom == null)
            {
                MessageBox.Show("Пожалуйста, выберите комнату");
                return;
            }

            var dialog = new AddContainerWindow { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                _viewModel.AddContainer(dialog.ContainerName, _viewModel.SelectedRoom.Id);
                RefreshTreeView();
            }
        }

        private void AddItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedRoom == null)
            {
                MessageBox.Show("Пожалуйста, выберите комнату или контейнер");
                return;
            }

            var dialog = new AddItemWindow { Owner = this };
            dialog.SetContainers(_viewModel.SelectedRoom.Containers.ToList());

            if (dialog.ShowDialog() == true)
            {
                int? containerId = dialog.SelectedContainer?.Id;
                if (dialog.SelectedContainer?.Name == "Нет")
                    containerId = null;

                _viewModel.AddItem(
                    dialog.ItemName,
                    dialog.ItemType,
                    dialog.Description,
                    _viewModel.SelectedRoom.Id,
                    containerId,
                    dialog.LocationInRoom
                );
                RefreshTreeView();
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedRoom != null)
            {
                var dialog = new AddRoomWindow { Owner = this, Title = "Редактировать комнату" };
                dialog.RoomName = _viewModel.SelectedRoom.Name;
                if (dialog.ShowDialog() == true)
                {
                    _viewModel.UpdateRoom(_viewModel.SelectedRoom.Id, dialog.RoomName);
                    RefreshTreeView();
                }
            }
            else if (_viewModel.SelectedContainer != null)
            {
                var dialog = new AddContainerWindow { Owner = this, Title = "Редактировать контейнер" };
                dialog.ContainerName = _viewModel.SelectedContainer.Name;
                if (dialog.ShowDialog() == true)
                {
                    _viewModel.UpdateContainer(_viewModel.SelectedContainer.Id, dialog.ContainerName);
                    RefreshTreeView();
                }
            }
            else if (_viewModel.SelectedItem != null)
            {
                var dialog = new AddItemWindow { Owner = this, Title = "Редактировать вещь" };
                dialog.ItemName = _viewModel.SelectedItem.Name;
                dialog.ItemType = _viewModel.SelectedItem.ItemType;
                dialog.Description = _viewModel.SelectedItem.Description;
                dialog.LocationInRoom = _viewModel.SelectedItem.LocationInRoom;

                var room = _viewModel.SelectedItem.Room;
                dialog.SetContainers(room.Containers.ToList());

                if (_viewModel.SelectedItem.Container != null)
                    dialog.SelectedContainer = _viewModel.SelectedItem.Container;

                if (dialog.ShowDialog() == true)
                {
                    int? containerId = dialog.SelectedContainer?.Id;
                    if (dialog.SelectedContainer?.Name == "Нет")
                        containerId = null;

                    _viewModel.UpdateItem(
                        _viewModel.SelectedItem.Id,
                        dialog.ItemName,
                        dialog.ItemType,
                        dialog.Description,
                        containerId,
                        dialog.LocationInRoom
                    );
                    RefreshTreeView();
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите элемент для редактирования");
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedRoom != null)
            {
                if (MessageBox.Show($"Удалить комнату '{_viewModel.SelectedRoom.Name}'?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    _viewModel.DeleteRoomCommand.Execute(null);
                    RefreshTreeView();
                }
            }
            else if (_viewModel.SelectedContainer != null)
            {
                if (MessageBox.Show($"Удалить контейнер '{_viewModel.SelectedContainer.Name}'?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    _viewModel.DeleteContainerCommand.Execute(null);
                    RefreshTreeView();
                }
            }
            else if (_viewModel.SelectedItem != null)
            {
                if (MessageBox.Show($"Удалить вещь '{_viewModel.SelectedItem.Name}'?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    _viewModel.DeleteItemCommand.Execute(null);
                    RefreshTreeView();
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите элемент для удаления");
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.RefreshCommand.Execute(null);
            RefreshTreeView();
        }

        private void OnAddRoomRequested(string name)
        {
            var dialog = new AddRoomWindow { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                _viewModel.AddRoom(dialog.RoomName);
                RefreshTreeView();
            }
        }

        private void OnEditRoomRequested(Room room)
        {
            var dialog = new AddRoomWindow { Owner = this, Title = "Редактировать комнату" };
            dialog.RoomName = room.Name;
            if (dialog.ShowDialog() == true)
            {
                _viewModel.UpdateRoom(room.Id, dialog.RoomName);
                RefreshTreeView();
            }
        }

        private void OnAddContainerRequested(Room room, string name)
        {
            var dialog = new AddContainerWindow { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                _viewModel.AddContainer(dialog.ContainerName, room.Id);
                RefreshTreeView();
            }
        }

        private void OnEditContainerRequested(Container container)
        {
            var dialog = new AddContainerWindow { Owner = this, Title = "Редактировать контейнер" };
            dialog.ContainerName = container.Name;
            if (dialog.ShowDialog() == true)
            {
                _viewModel.UpdateContainer(container.Id, dialog.ContainerName);
                RefreshTreeView();
            }
        }

        private void OnAddItemRequested(Room room, Container container)
        {
            var dialog = new AddItemWindow { Owner = this };
            dialog.SetContainers(room.Containers.ToList());
            if (dialog.ShowDialog() == true)
            {
                int? containerId = dialog.SelectedContainer?.Id;
                if (dialog.SelectedContainer?.Name == "Нет")
                    containerId = null;

                _viewModel.AddItem(
                    dialog.ItemName,
                    dialog.ItemType,
                    dialog.Description,
                    room.Id,
                    containerId,
                    dialog.LocationInRoom
                );
                RefreshTreeView();
            }
        }

        private void OnEditItemRequested(Item item)
        {
            var dialog = new AddItemWindow { Owner = this, Title = "Редактировать вещь" };
            dialog.ItemName = item.Name;
            dialog.ItemType = item.ItemType;
            dialog.Description = item.Description;
            dialog.LocationInRoom = item.LocationInRoom;

            var room = item.Room;
            dialog.SetContainers(room.Containers.ToList());

            if (item.Container != null)
                dialog.SelectedContainer = item.Container;

            if (dialog.ShowDialog() == true)
            {
                int? containerId = dialog.SelectedContainer?.Id;
                if (dialog.SelectedContainer?.Name == "Нет")
                    containerId = null;

                _viewModel.UpdateItem(
                    item.Id,
                    dialog.ItemName,
                    dialog.ItemType,
                    dialog.Description,
                    containerId,
                    dialog.LocationInRoom
                );
                RefreshTreeView();
            }
        }
    }
}
