using System.Windows;

namespace ApartmentInventory.Views
{
    public partial class AddRoomWindow : Window
    {
        public string RoomName { get; set; }

        public AddRoomWindow()
        {
            InitializeComponent();
            RoomNameTextBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(RoomNameTextBox.Text))
            {
                MessageBox.Show("Пожалуйста, введите название комнаты", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            RoomName = RoomNameTextBox.Text;
            this.DialogResult = true;
        }
    }
}
