using System.Windows;

namespace ApartmentInventory.Views
{
    public partial class AddContainerWindow : Window
    {
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
