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

namespace FourierDraw
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var uri = new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, "Lenna.png"));
            var bitmap = new BitmapImage(uri);

            sourceImage.Source = bitmap;

            double[] pixelData = BitmapSourceToArray(bitmap);

            //var pixelDataCompelx = pixelData.Select(_ => new System.Numerics.Complex(_, 0)).ToArray();
            var pixelDataCompelx = fftshift(fft2(pixelData, bitmap.PixelWidth, bitmap.PixelHeight), bitmap.PixelWidth, bitmap.PixelHeight);

            double[] pixelDataFreq = pixelDataCompelx.Select(_ => _.Magnitude).ToArray();
            double[] pixelDataFreqNorm = Normalize(pixelDataFreq);

            frequenciesImage.Source = BitmapSourceFromArray(pixelDataFreqNorm, bitmap);


            var pixelDataInverse = ifft2(ifftshift(pixelDataCompelx, bitmap.PixelWidth, bitmap.PixelHeight), bitmap.PixelWidth, bitmap.PixelHeight);
            resultImage.Source = BitmapSourceFromArray(pixelDataInverse.Select(_ => _.Magnitude).ToArray(), bitmap);
        }

        private Complex[] fft2(double[] pixelData, int width, int height)
        {
            Complex[] data = pixelData.Select(_ => new Complex(_, 0)).ToArray();

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

            return data;
        }

        private Complex[] ifft2(Complex[] pixelData, int width, int height)
        {
            Complex[] data = pixelData;

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

            return data;
        }

        private Complex[] fftshift(Complex[] data, int width, int height)
        {
            Complex[] result = new Complex[data.Length];
            Complex[] result2 = new Complex[data.Length];

            var halfX = width / 2;
            var halfY = height / 2;
          
            for (int y = 0; y < height; y++)
            {
                Array.Copy(data, y * width, result, ((halfY + y) % height) * width, width);
            }

            for (int y = 0; y < height; y++)
            {
                Array.Copy(result, y * width, result2, y * width + halfX, halfX);
                Array.Copy(result, y * width + halfX, result2, y * width, halfX);
            }

            return result2;
        }

        private Complex[] ifftshift(Complex[] data, int width, int height)
        {
            return fftshift(data, width, height);
        }

        private double[] Normalize(double[] values)
        {
            var min = values.Min();         
            Debug.Assert(min > 0);

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
    }
}
