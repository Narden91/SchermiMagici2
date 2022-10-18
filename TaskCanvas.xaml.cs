using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MessageBox = System.Windows.MessageBox;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for TaskCanvas.xaml
    /// </summary>
    public partial class TaskCanvas : Window
    {
        private string _imagePath;
        private int _widthCanvas;
        private int _heightCanvas;

        /// <summary>
        /// Costruttore della classe responsabile per la creazione 
        /// di una nuova finestra contenente l'immagine del Task attuale
        /// </summary>
        /// <param name="task"></param>
        public TaskCanvas(string task, string imagePath)
        {

            InitializeComponent();

            _imagePath = imagePath;

            Loaded += Window_Loaded;

            _widthCanvas = (int)this.Width;
            _heightCanvas = (int)this.Height;

            ImageBrush imageBrush = new ImageBrush();
            imageBrush.ImageSource = new BitmapImage(new Uri(@task, UriKind.Absolute));
            inkCanvasTask.Background = imageBrush;

            Closing += OnWindowclose;

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

        /// <summary>
        /// Routine attivata al momento dell'apertura della finestra
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
            SaveImage();
            //Environment.Exit(Environment.ExitCode); // Prevent memory leak
            //System.Windows.Application.Current.Shutdown();
        }

        /// <summary>
        /// Funzione che alla chiusura della finestra Task effettua il salvataggio dell'Immagine
        /// </summary>
        private void SaveImage()
        {
            RenderTargetBitmap rtb = new RenderTargetBitmap(_widthCanvas, _heightCanvas, 96d, 96d, PixelFormats.Default);
            rtb.Render(inkCanvasTask);
            DrawingVisual dvInk = new DrawingVisual();
            DrawingContext dcInk = dvInk.RenderOpen();
            dcInk.DrawRectangle(inkCanvasTask.Background, null, new Rect(0d, 0d, _widthCanvas, _heightCanvas));
            foreach (System.Windows.Ink.Stroke stroke in inkCanvasTask.Strokes)
            {
                stroke.Draw(dcInk);
            }
            dcInk.Close();

            FileStream fs = File.Open(_imagePath, FileMode.OpenOrCreate);//save bitmap to file
            JpegBitmapEncoder encoder1 = new JpegBitmapEncoder();
            encoder1.Frames.Add(BitmapFrame.Create(rtb));
            encoder1.Save(fs);
            fs.Close();
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
