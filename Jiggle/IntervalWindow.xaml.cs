using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using Jiggle.Properties;

namespace Jiggle {
    public partial class IntervalWindow : Window {
        public IntervalWindow() {
            InitializeComponent();
            ActivityIntervalTextBox.Text = Settings.Default.ActivityInterval.ToString();
            PauseIntervalTextBox.Text = Settings.Default.PauseInterval.ToString();
        }

        private void IntegerTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e) {
            e.Handled = !uint.TryParse(e.Text, out _);
        }

        private void IntegerTextBox_TextChanged(object sender, TextChangedEventArgs e) {
            TextBox textBox = sender as TextBox;
            if (!string.IsNullOrWhiteSpace(textBox.Text)) {
                textBox.Text = uint.Parse(textBox.Text).ToString();
                textBox.Select(textBox.Text.Length, 0);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e) {
            if (!string.IsNullOrWhiteSpace(ActivityIntervalTextBox.Text) && !ActivityIntervalTextBox.Text.Equals("0"))
                Settings.Default.ActivityInterval = uint.Parse(ActivityIntervalTextBox.Text);
            if (!string.IsNullOrWhiteSpace(PauseIntervalTextBox.Text) && !PauseIntervalTextBox.Text.Equals("0"))
                Settings.Default.PauseInterval = uint.Parse(PauseIntervalTextBox.Text);
            Settings.Default.Save();
            Close();
        }
    }
}
