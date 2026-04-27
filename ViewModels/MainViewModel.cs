using System;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Input;
using ApartmentInventory.Models;
using ApartmentInventory.Data;

namespace ApartmentInventory.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;

        public void Execute(object parameter) => _execute(parameter);
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseContext _dbContext = new DatabaseContext();
        private ObservableCollection<Room> _rooms;
        private Room _selectedRoom;
        private Models.Container _selectedContainer;
        private Item _selectedItem;

        public ObservableCollection<Room> Rooms
        {
            get => _rooms;
            set
            {
                if (_rooms != value)
                {
                    _rooms = value;
                    OnPropertyChanged(nameof(Rooms));
                }
            }
        }

        public Room SelectedRoom
        {
            get => _selectedRoom;
            set
            {
                if (_selectedRoom != value)
                {
                    _selectedRoom = value;
                    OnPropertyChanged(nameof(SelectedRoom));
                }
            }
        }

        public Models.Container SelectedContainer
        {
            get => _selectedContainer;
            set
            {
                if (_selectedContainer != value)
                {
                    _selectedContainer = value;
                    OnPropertyChanged(nameof(SelectedContainer));
                }
            }
        }

        public Item SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    OnPropertyChanged(nameof(SelectedItem));
                }
            }
        }

        public ICommand RefreshCommand { get; }
        public ICommand AddRoomCommand { get; }
        public ICommand EditRoomCommand { get; }
        public ICommand DeleteRoomCommand { get; }
        public ICommand AddContainerCommand { get; }
        public ICommand EditContainerCommand { get; }
        public ICommand DeleteContainerCommand { get; }
        public ICommand AddItemCommand { get; }
        public ICommand EditItemCommand { get; }
        public ICommand DeleteItemCommand { get; }

        public MainViewModel()
        {
            _dbContext.Initialize();
            Rooms = _dbContext.GetAllRooms();

            RefreshCommand = new RelayCommand(_ => LoadRooms());
            AddRoomCommand = new RelayCommand(_ => RaiseAddRoomRequested());
            EditRoomCommand = new RelayCommand(_ => RaiseEditRoomRequested(), _ => SelectedRoom != null);
            DeleteRoomCommand = new RelayCommand(_ => DeleteSelectedRoom(), _ => SelectedRoom != null);
            AddContainerCommand = new RelayCommand(_ => RaiseAddContainerRequested(), _ => SelectedRoom != null);
            EditContainerCommand = new RelayCommand(_ => RaiseEditContainerRequested(), _ => SelectedContainer != null);
            DeleteContainerCommand = new RelayCommand(_ => DeleteSelectedContainer(), _ => SelectedContainer != null);
            AddItemCommand = new RelayCommand(_ => RaiseAddItemRequested(), _ => SelectedRoom != null || SelectedContainer != null);
            EditItemCommand = new RelayCommand(_ => RaiseEditItemRequested(), _ => SelectedItem != null);
            DeleteItemCommand = new RelayCommand(_ => DeleteSelectedItem(), _ => SelectedItem != null);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event Action<string> AddRoomRequested;
        public event Action<Room> EditRoomRequested;
        public event Action<Room, string> AddContainerRequested;
        public event Action<Models.Container> EditContainerRequested;
        public event Action<Room, Models.Container> AddItemRequested;
        public event Action<Item> EditItemRequested;

        private void LoadRooms()
        {
            Rooms = _dbContext.GetAllRooms();
        }

        private void RaiseAddRoomRequested()
        {
            AddRoomRequested?.Invoke("");
        }

        private void RaiseEditRoomRequested()
        {
            if (SelectedRoom != null)
                EditRoomRequested?.Invoke(SelectedRoom);
        }

        private void DeleteSelectedRoom()
        {
            if (SelectedRoom != null)
            {
                try
                {
                    _dbContext.DeleteRoom(SelectedRoom.Id);
                    LoadRooms();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Ошибка при удалении комнаты: {ex.Message}");
                }
            }
        }

        private void RaiseAddContainerRequested()
        {
            if (SelectedRoom != null)
                AddContainerRequested?.Invoke(SelectedRoom, "");
        }

        private void RaiseEditContainerRequested()
        {
            if (SelectedContainer != null)
                EditContainerRequested?.Invoke(SelectedContainer);
        }

        private void DeleteSelectedContainer()
        {
            if (SelectedContainer != null)
            {
                try
                {
                    _dbContext.DeleteContainer(SelectedContainer.Id);
                    LoadRooms();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Ошибка при удалении контейнера: {ex.Message}");
                }
            }
        }

        private void RaiseAddItemRequested()
        {
            if (SelectedContainer != null)
                AddItemRequested?.Invoke(SelectedContainer.Room, SelectedContainer);
            else if (SelectedRoom != null)
                AddItemRequested?.Invoke(SelectedRoom, null);
        }

        private void RaiseEditItemRequested()
        {
            if (SelectedItem != null)
                EditItemRequested?.Invoke(SelectedItem);
        }

        private void DeleteSelectedItem()
        {
            if (SelectedItem != null)
            {
                try
                {
                    _dbContext.DeleteItem(SelectedItem.Id);
                    LoadRooms();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Ошибка при удалении вещи: {ex.Message}");
                }
            }
        }

        public void AddRoom(string name)
        {
            try
            {
                _dbContext.AddRoom(name);
                LoadRooms();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        public void UpdateRoom(int id, string name)
        {
            try
            {
                _dbContext.UpdateRoom(id, name);
                LoadRooms();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        public void AddContainer(string name, int roomId, int? parentContainerId = null)
        {
            try
            {
                _dbContext.AddContainer(name, roomId, parentContainerId);
                LoadRooms();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error: {ex.Message}");
            }
        }

        public void UpdateContainer(int id, string name)
        {
            try
            {
                _dbContext.UpdateContainer(id, name);
                LoadRooms();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error: {ex.Message}");
            }
        }

        public void AddItem(string name, string itemType, string description, int roomId, int? containerId, string locationInRoom)
        {
            try
            {
                _dbContext.AddItem(name, itemType, description, roomId, containerId, locationInRoom);
                LoadRooms();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error: {ex.Message}");
            }
        }

        public void UpdateItem(int id, string name, string itemType, string description, int roomId, int? containerId, string locationInRoom)
        {
            try
            {
                _dbContext.UpdateItem(id, name, itemType, description, roomId, containerId, locationInRoom);
                LoadRooms();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error: {ex.Message}");
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
