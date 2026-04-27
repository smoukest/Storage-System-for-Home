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

        private List<TreeGridNode> _flatNodes = new List<TreeGridNode>();

        private void RefreshTreeView()
        {
            RefreshDataGrid();
        }

        private void RefreshDataGrid()
        {
            _flatNodes.Clear();
            foreach (var room in _viewModel.Rooms)
            {
                var roomNode = new TreeGridNode
                {
                    OriginalItem = room,
                    Name = $"🏠 {room.Name}",
                    ElementType = "Комната",
                    Level = 0,
                    IsExpanded = false,
                    IsVisible = true
                };
                _flatNodes.Add(roomNode);

                // Рекурсивный метод для добавления контейнеров
                void AddContainerNodes(Container container, TreeGridNode parentNode, int level)
                {
                    var indent = new string(' ', level * 4);
                    var containerNode = new TreeGridNode
                    {
                        OriginalItem = container,
                        Name = $"{indent}📦 {container.Name}",
                        ElementType = "Контейнер",
                        Level = level,
                        IsExpanded = false,
                        IsVisible = parentNode.IsExpanded,
                        Parent = parentNode
                    };
                    parentNode.Children.Add(containerNode);
                    _flatNodes.Add(containerNode);

                    // Сначала добавляем дочерние контейнеры
                    foreach (var childContainer in container.ChildContainers)
                    {
                        AddContainerNodes(childContainer, containerNode, level + 1);
                    }

                    // Затем вещи внутри этого контейнера
                    foreach (var item in container.Items)
                    {
                        var itemIndent = new string(' ', (level + 1) * 4);
                        var itemNode = new TreeGridNode
                        {
                            OriginalItem = item,
                            Name = $"{itemIndent}📄 {item.Name}",
                            ItemType = item.ItemType,
                            Description = item.Description,
                            LocationInRoom = item.LocationInRoom,
                            Container = container,
                            ElementType = "Вещь",
                            Level = level + 1,
                            IsVisible = containerNode.IsExpanded,
                            Parent = containerNode
                        };
                        containerNode.Children.Add(itemNode);
                        _flatNodes.Add(itemNode);
                    }
                }

                // Добавляем только корневые контейнеры
                foreach (var container in room.Containers)
                {
                    if (container.ParentContainerId == null)
                    {
                        AddContainerNodes(container, roomNode, 1);
                    }
                }

                foreach (var item in room.Items)
                {
                    var itemNode = new TreeGridNode
                    {
                        OriginalItem = item,
                        Name = $"    📄 {item.Name}",
                        ItemType = item.ItemType,
                        Description = item.Description,
                        LocationInRoom = item.LocationInRoom,
                        Container = null,
                        ElementType = "Вещь",
                        Level = 1,
                        IsVisible = roomNode.IsExpanded,
                        Parent = roomNode
                    };
                    roomNode.Children.Add(itemNode);
                    _flatNodes.Add(itemNode);
                }
            }
            ItemsDataGrid.ItemsSource = null;
            ItemsDataGrid.ItemsSource = _flatNodes;
        }

        private bool _ignoreNextSelectionChange = false;

        private void ItemsDataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                var hit = VisualTreeHelper.HitTest(ItemsDataGrid, e.GetPosition(ItemsDataGrid));
                if (hit != null)
                {
                    var row = GetVisualParent<DataGridRow>(hit.VisualHit);
                    if (row != null && row.Item is TreeGridNode node)
                    {
                        if (node.ElementType == "Комната" || node.ElementType == "Контейнер")
                        {
                            e.Handled = true;
                            _ignoreNextSelectionChange = true;
                            ItemsDataGrid.SelectedItem = node;

                            bool targetExpandedState = !node.IsExpanded;
                            node.IsExpanded = targetExpandedState;
                            ExpandRecursively(node, targetExpandedState);

                            UpdateVisibility(node);
                            ItemsDataGrid.ItemsSource = null;
                            ItemsDataGrid.ItemsSource = _flatNodes;
                            CheckAllExpandedState();

                            UpdateSelectionState(node);
                            _ignoreNextSelectionChange = false;
                        }
                    }
                }
            }
        }

        private void ExpandRecursively(TreeGridNode parentNode, bool isExpanded)
        {
            foreach (var child in parentNode.Children)
            {
                if (child.ElementType == "Комната" || child.ElementType == "Контейнер")
                {
                    child.IsExpanded = isExpanded;
                    ExpandRecursively(child, isExpanded);
                }
            }
        }

        private void ItemsDataGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var hit = VisualTreeHelper.HitTest(ItemsDataGrid, e.GetPosition(ItemsDataGrid));
            if (hit != null)
            {
                var row = GetVisualParent<DataGridRow>(hit.VisualHit);
                if (row != null && row.Item is TreeGridNode node)
                {
                    e.Handled = true;
                    _ignoreNextSelectionChange = true;
                    ItemsDataGrid.SelectedItem = node;

                    UpdateSelectionState(node);
                    _ignoreNextSelectionChange = false;

                    ContextMenu contextMenu = new ContextMenu();

                    if (node.ElementType == "Комната" || node.ElementType == "Контейнер")
                    {
                        MenuItem addContainerMenu = new MenuItem { Header = "➕ Добавить контейнер" };
                        addContainerMenu.Click += AddContainerButton_Click;
                        contextMenu.Items.Add(addContainerMenu);

                        MenuItem addItemMenu = new MenuItem { Header = "➕ Добавить вещь" };
                        addItemMenu.Click += AddItemButton_Click;
                        contextMenu.Items.Add(addItemMenu);

                        contextMenu.Items.Add(new Separator());
                    }

                    MenuItem editMenu = new MenuItem { Header = "✎ Редактировать" };
                    editMenu.Click += EditButton_Click;
                    contextMenu.Items.Add(editMenu);

                    MenuItem deleteMenu = new MenuItem { Header = "🗑 Удалить", Foreground = Brushes.Red };
                    deleteMenu.Click += DeleteButton_Click;
                    contextMenu.Items.Add(deleteMenu);

                    row.ContextMenu = contextMenu;
                    contextMenu.IsOpen = true;
                }
            }
        }

        private static T GetVisualParent<T>(DependencyObject element) where T : DependencyObject
        {
            while (element != null && !(element is T))
            {
                element = VisualTreeHelper.GetParent(element);
            }
            return element as T;
        }

        private void ItemsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_ignoreNextSelectionChange) 
                return;

            if (ItemsDataGrid.SelectedItem is TreeGridNode node)
            {
                if (node.ElementType == "Комната" || node.ElementType == "Контейнер")
                {
                    node.IsExpanded = !node.IsExpanded;
                    UpdateVisibility(node);
                    ItemsDataGrid.ItemsSource = null;
                    ItemsDataGrid.ItemsSource = _flatNodes;
                    CheckAllExpandedState();
                }

                UpdateSelectionState(node);
            }
        }

        private void UpdateSelectionState(TreeGridNode node)
        {
            if (node.OriginalItem is Room room)
            {
                _viewModel.SelectedRoom = room;
                _viewModel.SelectedContainer = null;
                _viewModel.SelectedItem = null;
                InfoTextBlock.Text = $"Комната: {room.Name}";
            }
            else if (node.OriginalItem is Container container)
            {
                _viewModel.SelectedContainer = container;
                _viewModel.SelectedRoom = container.Room;
                _viewModel.SelectedItem = null;
                InfoTextBlock.Text = $"Контейнер: {container.Name} (в комнате: {container.Room.Name})";
            }
            else if (node.OriginalItem is Item item)
            {
                _viewModel.SelectedItem = item;
                _viewModel.SelectedRoom = item.Room;
                _viewModel.SelectedContainer = item.Container;
                var location = string.IsNullOrEmpty(item.LocationInRoom) ? "не указано" : item.LocationInRoom;
                var container_name = item.Container?.Name ?? "нет";
                InfoTextBlock.Text = $"Вещь: {item.Name} | Вид: {item.ItemType} | Контейнер: {container_name} | Местоположение: {location} | Описание: {item.Description}";
            }
        }

        private void CheckAllExpandedState()
        {
            if (_flatNodes == null || _flatNodes.Count == 0) return;

            bool allExpanded = true;
            foreach (var node in _flatNodes)
            {
                if ((node.ElementType == "Комната" || node.ElementType == "Контейнер") && node.HasChildren)
                {
                    if (!node.IsExpanded)
                    {
                        allExpanded = false;
                        break;
                    }
                }
            }

            _isExpanded = allExpanded;
            ExpandAllButton.Content = _isExpanded ? "↕ Скрыть все" : "↕ Раскрыть все";
        }

        private void UpdateVisibility(TreeGridNode parentNode)
        {
            foreach (var child in parentNode.Children)
            {
                child.IsVisible = parentNode.IsExpanded;
                if (!parentNode.IsExpanded && child.IsExpanded)
                {
                    child.IsExpanded = false;
                }
                UpdateVisibility(child);
            }
        }

        public class TreeGridNode
        {
            public object OriginalItem { get; set; }
            public string Name { get; set; }
            public string ItemType { get; set; }
            public string Description { get; set; }
            public string LocationInRoom { get; set; }
            public Container Container { get; set; }
            public string ElementType { get; set; }
            public int Level { get; set; }
            public bool IsExpanded { get; set; }
            public bool IsVisible { get; set; }
            public bool HasChildren => Children != null && Children.Count > 0;
            public TreeGridNode Parent { get; set; }
            public List<TreeGridNode> Children { get; set; } = new List<TreeGridNode>();

            public string ExpandIndicator 
            { 
                get 
                {
                    if (HasChildren)
                    {
                        return IsExpanded ? "▼" : "▶";
                    }
                    return "";
                }
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
                int? parentContainerId = _viewModel.SelectedContainer?.Id;
                _viewModel.AddContainer(dialog.ContainerName, _viewModel.SelectedRoom.Id, parentContainerId);
                RefreshTreeView();
            }
        }

        private void AddItemButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddItemWindow { Owner = this };

            // Передаем список всех комнат
            dialog.SetRooms(_viewModel.Rooms.ToList());

            // Если выбрана комната, устанавливаем её по умолчанию
            if (_viewModel.SelectedRoom != null)
            {
                // Устанавливаем контейнеры для выбранной комнаты
                dialog.SetContainers(_viewModel.SelectedRoom.Containers.ToList());
            }

            if (dialog.ShowDialog() == true)
            {
                int? containerId = dialog.SelectedContainer?.Id;
                if (dialog.SelectedContainer?.Name == "Нет" || dialog.SelectedContainer?.Id == -1)
                    containerId = null;

                // Используем выбранную в диалоге комнату
                var roomId = dialog.SelectedRoom?.Id ?? _viewModel.SelectedRoom?.Id;
                if (roomId.HasValue)
                {
                    _viewModel.AddItem(
                        dialog.ItemName,
                        dialog.ItemType,
                        dialog.Description,
                        roomId.Value,
                        containerId,
                        dialog.LocationInRoom
                    );
                    RefreshTreeView();
                }
                else
                {
                    MessageBox.Show("Пожалуйста, выберите комнату");
                }
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
                dialog.SetRooms(_viewModel.Rooms.ToList());
                dialog.SetContainers(room.Containers.ToList());

                if (_viewModel.SelectedItem.Container != null)
                    dialog.SelectedContainer = _viewModel.SelectedItem.Container;

                if (dialog.ShowDialog() == true)
                {
                    int? containerId = dialog.SelectedContainer?.Id;
                    if (dialog.SelectedContainer?.Name == "Нет" || dialog.SelectedContainer?.Id == -1)
                        containerId = null;

                    var roomId = dialog.SelectedRoom?.Id ?? room.Id;
                    _viewModel.UpdateItem(
                        _viewModel.SelectedItem.Id,
                        dialog.ItemName,
                        dialog.ItemType,
                        dialog.Description,
                        roomId,
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

        private bool _isExpanded = false;

        private void ExpandAllButton_Click(object sender, RoutedEventArgs e)
        {
            _isExpanded = !_isExpanded;
            ExpandAllButton.Content = _isExpanded ? "↕ Скрыть все" : "↕ Раскрыть все";

            foreach (var node in _flatNodes)
            {
                if (node.ElementType == "Комната" || node.ElementType == "Контейнер")
                {
                    node.IsExpanded = _isExpanded;
                    UpdateVisibility(node);
                }
            }

            ItemsDataGrid.ItemsSource = null;
            ItemsDataGrid.ItemsSource = _flatNodes;
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.RefreshCommand.Execute(null);
            _isExpanded = false;
            ExpandAllButton.Content = "↕ Раскрыть все";
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
            dialog.SetRooms(_viewModel.Rooms.ToList());
            dialog.SetContainers(room.Containers.ToList());
            if (dialog.ShowDialog() == true)
            {
                int? containerId = dialog.SelectedContainer?.Id;
                if (dialog.SelectedContainer?.Name == "Нет" || dialog.SelectedContainer?.Id == -1)
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
            dialog.SetRooms(_viewModel.Rooms.ToList());
            dialog.SetContainers(room.Containers.ToList());

            if (item.Container != null)
                dialog.SelectedContainer = item.Container;

            if (dialog.ShowDialog() == true)
            {
                int? containerId = dialog.SelectedContainer?.Id;
                if (dialog.SelectedContainer?.Name == "Нет" || dialog.SelectedContainer?.Id == -1)
                    containerId = null;

                var roomId = dialog.SelectedRoom?.Id ?? item.Room?.Id ?? 1;
                _viewModel.UpdateItem(
                    item.Id,
                    dialog.ItemName,
                    dialog.ItemType,
                    dialog.Description,
                    roomId,
                    containerId,
                    dialog.LocationInRoom
                );
                RefreshTreeView();
            }
        }
    }
}
