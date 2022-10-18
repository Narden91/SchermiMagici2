using System;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for TaskCanvas.xaml
    /// </summary>
    public partial class TaskCanvas : Window
    {

        /// <summary>
        /// Costruttore della classe responsabile per la creazione 
        /// di una nuova finestra contenente l'immagine del Task attuale
        /// </summary>
        /// <param name="task"></param>
        public TaskCanvas(string task)
        {

            InitializeComponent();

            Loaded += Window_Loaded;

            ImageBrush imageBrush = new ImageBrush();
            imageBrush.ImageSource = new BitmapImage(new Uri(@task, UriKind.Absolute));
            inkCanvasTask.Background = imageBrush;

        }

        /// <summary>
        /// Apre la finestra del Task direttamente sulla Wacom One a schermo pieno
        /// </summary>
        public void MaximizeToSecondaryMonitor()
        {
            var secondaryScreen = Screen.AllScreens.Where(s => !s.Primary).FirstOrDefault();

            if (secondaryScreen != null)
            {
                var workingArea = secondaryScreen.WorkingArea;
                this.Left = workingArea.Left;
                this.Top = workingArea.Top;
                this.Width = workingArea.Width;
                this.Height = workingArea.Height;

            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            MaximizeToSecondaryMonitor();
            WindowState = WindowState.Maximized;
        }

        /// <summary>
        /// Termina i Thread della finestra alla sua chiusura
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnWindowclose(object sender, EventArgs e)
        {
            Environment.Exit(Environment.ExitCode); // Prevent memory leak
            System.Windows.Application.Current.Shutdown();
        }
    }
}


//namespace WpfApp1
//{
//    /// <summary>
//    /// Interaction logic for TaskCanvas.xaml
//    /// </summary>
//    public partial class TaskCanvas : Window
//    {

//        /// <summary>
//        /// Costruttore della classe responsabile per la creazione 
//        /// di una nuova finestra contenente l'immagine del Task attuale
//        /// </summary>
//        /// <param name="task"></param>
//        public TaskCanvas(string task)
//        {

//            InitializeComponent();

//            ImageBrush imageBrush = new ImageBrush();
//            imageBrush.ImageSource = new BitmapImage(new Uri(@task, UriKind.Absolute));
//            inkCanvasTask.Background = imageBrush;

//        }

//        /// <summary>
//        /// Termina i Thread della finestra alla sua chiusura
//        /// </summary>
//        /// <param name="sender"></param>
//        /// <param name="e"></param>
//        private void OnWindowclose(object sender, EventArgs e)
//        {
//            Environment.Exit(Environment.ExitCode); // Prevent memory leak
//            Application.Current.Shutdown();
//        }
//    }
//}
