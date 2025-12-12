using System.Windows;

namespace HCIDE.Views
{
    /// <summary>
    /// Логика взаимодействия для SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Закрываем окно после сохранения
            this.DialogResult = true;
            this.Close();
        }
    }
}