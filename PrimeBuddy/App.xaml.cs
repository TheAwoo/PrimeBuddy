using System.Windows;

namespace PrimeBuddy
{
    public partial class App : Application
    {
        private void AppStartup(object sender, StartupEventArgs e)
        {
            MainWindow hwnd = new MainWindow();
            hwnd.Show();
        }
    }
}
