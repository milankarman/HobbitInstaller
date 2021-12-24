using System.Windows;

namespace HobbitInstaller
{
    public partial class Success : Window
    {
        public Success()
        {
            InitializeComponent();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
