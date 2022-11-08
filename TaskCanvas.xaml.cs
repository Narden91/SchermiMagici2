using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static System.Windows.Forms.LinkLabel;
using MessageBox = System.Windows.MessageBox;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for TaskCanvas.xaml
    /// </summary>
    public partial class TaskCanvas : Window
    {
        private string _imageWithBackgroundFolderPath;
        //private string _imageWithoutBackgroundFolderPath;
        private int _widthCanvas;
        private int _heightCanvas;
        private double dpi = 96d;

        private int _secondScreenWidthCanvas;
        private int _secondScreenHeightCanvas;

        /// <summary>
        /// Costruttore della classe responsabile per la creazione 
        /// di una nuova finestra contenente l'immagine del Task attuale
        /// </summary>
        /// <param name="task"></param>
        //public TaskCanvas(string task, string imageWithBackgroundPath, string imageWithoutBackgroundPath)
        public TaskCanvas(string task, string imageWithBackgroundPath)
        {

            InitializeComponent();

            Loaded += Window_Loaded;

            _imageWithBackgroundFolderPath = imageWithBackgroundPath;
            //_imageWithoutBackgroundFolderPath = imageWithoutBackgroundPath;

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

                _secondScreenWidthCanvas = workingArea.Width;
                _secondScreenHeightCanvas= workingArea.Height;
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
            SaveImageWithBackground();

            //SaveImageWithoutBackground();

        }

        /// <summary>
        /// Funzione che alla chiusura della finestra Task effettua il salvataggio dell'Immagine con lo sfondo
        /// </summary>
        private void SaveImageWithBackground()
        {
            //RenderTargetBitmap rtb = new RenderTargetBitmap(_widthCanvas, _heightCanvas, dpi, dpi, PixelFormats.Default);

            // Per secondo schermo
            RenderTargetBitmap rtb = new RenderTargetBitmap(_secondScreenWidthCanvas, _secondScreenHeightCanvas, dpi, dpi, PixelFormats.Default);

            rtb.Render(inkCanvasTask);
            DrawingVisual dvInk = new DrawingVisual();
            DrawingContext dcInk = dvInk.RenderOpen();
            SolidColorBrush brs = new SolidColorBrush(Colors.Black);
            //dcInk.DrawRectangle(brs, null, new Rect(0d, 0d, _widthCanvas, _heightCanvas));

            // Per secondo schermo
            dcInk.DrawRectangle(brs, null, new Rect(0d, 0d, _secondScreenWidthCanvas, _secondScreenHeightCanvas));


            foreach (System.Windows.Ink.Stroke stroke in inkCanvasTask.Strokes)
            {
                stroke.Draw(dcInk);
            }
            dcInk.Close();

            FileStream fs = File.Open(_imageWithBackgroundFolderPath, FileMode.OpenOrCreate);//save bitmap to file
            PngBitmapEncoder encoder1 = new PngBitmapEncoder();
            encoder1.Frames.Add(BitmapFrame.Create(rtb));
            encoder1.Save(fs);
            fs.Close();
        }

        /// <summary>
        /// Funzione che alla chiusura della finestra Task effettua il salvataggio dell'Immagine senza lo sfondo
        /// </summary>
        //private void SaveImageWithoutBackground()
        //{

        //    // Create a render bitmap and push the surface to it
        //    RenderTargetBitmap renderBitmap =
        //        new RenderTargetBitmap(_widthCanvas, _heightCanvas, dpi, dpi,
        //                               PixelFormats.Pbgra32);

        //    DrawingVisual drawingVisual = new DrawingVisual();
        //    using (DrawingContext drawingContext = drawingVisual.RenderOpen())
        //    {
        //        VisualBrush visualBrush = new VisualBrush(inkCanvasTask);
        //        drawingContext.DrawRectangle(visualBrush, null,
        //          new Rect(new Point(), new Size(Width,Height)));
        //    }

        //    renderBitmap.Render(drawingVisual);

        //    // Create a file stream for saving image
        //    using (FileStream outStream = new FileStream(_imageWithoutBackgroundFolderPath, FileMode.Create))
        //    {
        //        BmpBitmapEncoder encoder = new BmpBitmapEncoder();
        //        // push the rendered bitmap to it
        //        encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
        //        // save the data to the stream
        //        encoder.Save(outStream);

        //        outStream.Close();
        //    }

        //}

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
