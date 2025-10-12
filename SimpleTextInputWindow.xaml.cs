using System.Windows;

namespace ImageAndMp4WebBuilder
{
    public partial class SimpleTextInputWindow : Window
    {
        public string InputText => InputBox.Text;
        public SimpleTextInputWindow(string prompt, string? defaultValue = null)
        {
            InitializeComponent();
            PromptText.Text = prompt;
            if (!string.IsNullOrWhiteSpace(defaultValue))
            {
                InputBox.Text = defaultValue;
                InputBox.SelectAll();
                InputBox.Focus();
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
