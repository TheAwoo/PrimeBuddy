using System.IO;
using System.Windows;

namespace PrimeBuddy
{
    public partial class MainWindow
    {
        void ChkDebug_Checked(object sender, RoutedEventArgs e)
        {
            TabDebug.Visibility = Visibility.Visible;
        }

        void ChkDebug_Unchecked(object sender, RoutedEventArgs e)
        {
            TabDebug.Visibility = Visibility.Hidden;
        }

        void onClose(object sender, System.ComponentModel.CancelEventArgs e)
        {
            LogFile.Close();
        }

        private void BtnClearDrops_Click(object sender, RoutedEventArgs e)
        {
            ItemList.Items.Clear();
        }
    }
}
