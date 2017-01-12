using MathNet.Numerics.IntegralTransforms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Globalization;
using System.Windows.Ink;

namespace FourierDraw
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        class ComplexImage
        {
            public readonly Complex[] Data;
            public readonly int Height;
            public readonly int Width;

            public ComplexImage(Complex[] data, int height, int width)
            {
                Data = data;
                Height = height;
                Width = width;
            }

            /// <summary>
            /// Creates a empty ComplexImage with the same size like template
            /// </summary>
            public static ComplexImage CreateEmpty(ComplexImage template)
            {
                return new ComplexImage(new Complex[template.Data.Length], template.Height, template.Width);
            }
        }


        private BitmapImage inputBitmap;
        private ComplexImage inputFreqSpace;

        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var uri = new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, ((App)Application.Current).StartArgs[0]));
            inputBitmap = new BitmapImage(uri);

            sourceImage.Source = inputBitmap;

            ComplexImage input = BitmapSourceToComplexImage(inputBitmap);
            inputFreqSpace = fftshift(fft2(input));

            double[] pixelDataFreq = inputFreqSpace.Data.Select(_ => _.Magnitude).ToArray();
            double[] pixelDataFreqNorm = Normalize(pixelDataFreq);

            frequenciesImage.Source = BitmapSourceFromArray(pixelDataFreqNorm, inputBitmap);

            var inverse = ifft2(ifftshift(inputFreqSpace));
            var inversePixelData = inverse.Data.Select(_ => _.Magnitude).ToArray();

            resultImage.Source = BitmapSourceFromArray(inversePixelData, inputBitmap);

            inkCanvas.CenterFactor = new Point(
                (inputBitmap.PixelWidth / 2 + 0.5) / inputBitmap.PixelWidth,
                (inputBitmap.PixelHeight / 2 + 0.5) / inputBitmap.PixelHeight);
        }

        private ComplexImage fft2(ComplexImage img)
        {
            Complex[] data = (Complex[])img.Data.Clone();

            var width = img.Width;
            var height = img.Height;

            // Rows
            for (int y = 0; y < height; y++)
            {
                Complex[] stride = data.Skip(y * width).Take(width).ToArray();
                Fourier.Forward(stride, FourierOptions.Matlab);

                for (int x = 0; x < width; x++)
                {
                    data[y * width + x] = stride[x];
                }
            }

            // Columns
            for (int x = 0; x < width; x++)
            {
                Complex[] col = new Complex[height];
                for (int y = 0; y < height; y++)
                {
                    col[y] = data[y * width + x];
                }

                Fourier.Forward(col, FourierOptions.Matlab);

                for (int y = 0; y < height; y++)
                {
                    data[y * width + x] = col[y];
                }
            }

            return new ComplexImage(data, img.Height, img.Width);
        }

        private ComplexImage ifft2(ComplexImage img)
        {
            Complex[] data = (Complex[])img.Data.Clone();

            var width = img.Width;
            var height = img.Height;

            // Rows
            for (int y = 0; y < height; y++)
            {
                Complex[] stride = data.Skip(y * width).Take(width).ToArray();
                Fourier.Inverse(stride, FourierOptions.Matlab);
                
                for (int x = 0; x < width; x++)
                {
                    data[y * width + x] = stride[x];
                }
            }

            // Columns
            for (int x = 0; x < width; x++)
            {
                Complex[] col = new Complex[height];
                for (int y = 0; y < height; y++)
                {
                    col[y] = data[y * width + x];
                }

                Fourier.Inverse(col, FourierOptions.Matlab);

                for (int y = 0; y < height; y++)
                {
                    data[y * width + x] = col[y];
                }
            }

            return new ComplexImage(data, img.Height, img.Width);
        }

        private ComplexImage fftshift(ComplexImage img)
        {
            Complex[] result = new Complex[img.Data.Length];
            Complex[] result2 = new Complex[img.Data.Length];

            var width = img.Width;
            var height = img.Height;

            var halfX = width / 2;
            var halfY = height / 2;
          
            for (int y = 0; y < height; y++)
            {
                Array.Copy(img.Data, y * width, result, ((halfY + y) % height) * width, width);
            }

            for (int y = 0; y < height; y++)
            {
                Array.Copy(result, y * width, result2, y * width + halfX, halfX);
                Array.Copy(result, y * width + halfX, result2, y * width, halfX);
            }

            return new ComplexImage(result2, height, width);
        }

        private ComplexImage ifftshift(ComplexImage img)
        {
            return fftshift(img);
        }

        private double[] Normalize(double[] values)
        {
            var temp = values.Select(x => Math.Log(x + 1));
            var max = temp.Max();

            return temp.Select(x => x / max).ToArray();
        }

        private double[] BitmapSourceToArray(BitmapSource bitmapSource)
        {
            // Stride = (width) x (bytes per pixel)
            int stride = bitmapSource.PixelWidth * (bitmapSource.Format.BitsPerPixel / 8);
            byte[] pixels = new byte[bitmapSource.PixelHeight * stride];

            bitmapSource.CopyPixels(pixels, stride, 0);

            return pixels.Select(x => ((double)x)/255).ToArray();
        }

        private ComplexImage BitmapSourceToComplexImage(BitmapSource bitmapSource)
        {
            return new ComplexImage(
                 BitmapSourceToArray(bitmapSource)?.Select(_ => new Complex(_, 0)).ToArray(),
                 bitmapSource.PixelHeight,
                 bitmapSource.PixelWidth);
        }

        private BitmapSource BitmapSourceFromArray(double[] pixels, int width, int height, PixelFormat? pixelFormat = null)
        {
            pixelFormat = pixelFormat ?? PixelFormats.Bgra32;


            byte[] pixelData = pixels.Select(x => (byte)(x * 255)).ToArray();

            WriteableBitmap bitmap = new WriteableBitmap(width, height, 72, 72, pixelFormat.Value, null);

            bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * (bitmap.Format.BitsPerPixel / 8), 0);

            return bitmap;
        }

        private BitmapSource BitmapSourceFromArray(double[] pixels, BitmapSource template)
        {
            byte[] pixelData = pixels.Select(x => (byte)(x * 255)).ToArray();

            WriteableBitmap bitmap = new WriteableBitmap(template.PixelWidth, template.PixelHeight, template.DpiX, template.DpiY, template.Format, null);

            bitmap.WritePixels(new Int32Rect(0, 0, template.PixelWidth, template.PixelHeight), pixelData, template.PixelWidth* (bitmap.Format.BitsPerPixel / 8), 0);
          
            return bitmap;
        }

        private void inkCanvas_StrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
        {
            // Render ink to bitmap
            RenderTargetBitmap renderTarget = new RenderTargetBitmap(inputBitmap.PixelWidth, inputBitmap.PixelHeight, inputBitmap.DpiX, inputBitmap.DpiY, PixelFormats.Pbgra32);
            renderTarget.Render(inkCanvas);
            
            int stride = inputBitmap.PixelWidth * (PixelFormats.Pbgra32.BitsPerPixel / 8);
            byte[] pixels = new byte[inputBitmap.PixelHeight * stride];
            renderTarget.CopyPixels(pixels, stride, 0);

            // Gather ink pixels
            double[] inkPixels = new double[pixels.Length / 4];
            for (int i = 0, j = 0; i < pixels.Length; i+=4, j++)
            {
                // bgr == 0
                double alpha = pixels[i + 3] / 255.0;
                inkPixels[j] = alpha;
            }

            // Combine frequencies and ink
            ComplexImage modifiedInputFreq = ComplexImage.CreateEmpty(inputFreqSpace);
            Debug.Assert(inkPixels.Length == modifiedInputFreq.Data.Length);

            for (int i = 0; i < inkPixels.Length; i++)
            {
                if (inkPixels[i] > 0)
                {
                    // scale frequency by ink stroke
                    modifiedInputFreq.Data[i] = inputFreqSpace.Data[i] * (1-inkPixels[i]);
                }
                else
                {
                    modifiedInputFreq.Data[i] = inputFreqSpace.Data[i];
                }
            }

            var inverse = ifft2(ifftshift(modifiedInputFreq));
            var inversePixelData = inverse.Data.Select(_ => _.Magnitude).ToArray();

            resultImage.Source = BitmapSourceFromArray(inversePixelData, inputBitmap);
        }
    }

    public class DrawingAttributesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double? input = value as double?;
            if (input == null) return new DrawingAttributes { Color = Colors.Black };
            byte byteInput = (byte)input.Value;
            return new DrawingAttributes { Color = Color.FromRgb(byteInput, byteInput, byteInput) };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
