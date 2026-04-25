using System.Windows;

namespace ApartmentInventory.Views
{
    public partial class AddContainerWindow : Window
    {
        public string ContainerName { get; set; }

        public AddContainerWindow()
        {
            InitializeComponent();
            ContainerNameTextBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ContainerNameTextBox.Text))
            {
                MessageBox.Show("Пожалуйста, введите название контейнера", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ContainerName = ContainerNameTextBox.Text;
            this.DialogResult = true;
        }
    }
}
