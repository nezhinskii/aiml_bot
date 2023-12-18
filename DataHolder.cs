using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIMLTGBot
{
    public class DataHolder
    {
        /// <summary>
        /// Бинарное представление образа
        /// </summary>
        public bool[,] img = new bool[200, 200];

        //  private int margin = 50;
        private Random rand = new Random();

        /// <summary>
        /// Текущая сгенерированная фигура
        /// </summary>
        public SymbolType currentFigure = SymbolType.Undef;

        /// <summary>
        /// Количество классов генерируемых фигур (4 - максимум)
        /// </summary>
        public int FigureCount { get; set; } = 4;

        /// <summary>
        /// Диапазон смещения центра фигуры (по умолчанию +/- 20 пикселов от центра)
        /// </summary>
        public int FigureCenterGitter { get; set; } = 50;

        /// <summary>
        /// Диапазон разброса размера фигур
        /// </summary>
        public int FigureSizeGitter { get; set; } = 50;

        /// <summary>
        /// Диапазон разброса размера фигур
        /// </summary>
        public int FigureSize { get; set; } = 100;

        public SamplesSet trainData = new SamplesSet();
        public SamplesSet testData = new SamplesSet();

        public void loadData(string trainImagesPath, string testImagesPath, string trainMarkup, string testMarkup)
        {
            Dictionary<String, int> trainImageLabels = new Dictionary<String, int>();
            using (StreamReader reader = new StreamReader(trainMarkup))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string trimed = line.Trim();
                    if (trimed != "")
                    {
                        string[] parts = trimed.Split(':');
                        trainImageLabels[parts[0]] = int.Parse(parts[1]);
                    }
                }
            }
            string[] fileEntries = Directory.GetFiles(trainImagesPath);
            foreach (string filePath in fileEntries)
            {
                Bitmap image = new Bitmap(filePath);
                Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);
                BitmapData bmpData = image.LockBits(rect, ImageLockMode.ReadOnly, image.PixelFormat);
                string fileName = filePath.Split('\\').Last().Split('.').First();
                double[] input = new double[48 * 48];
                unsafe
                {
                    byte* ptr = (byte*)bmpData.Scan0;
                    int heightInPixels = bmpData.Height;
                    int widthInBytes = bmpData.Stride;
                    for (int y = 0; y < heightInPixels; y++)
                    {
                        for (int x = 0; x < 48; x = x + x + 1)
                        {
                            double grayValue = ptr[(y * bmpData.Stride) + x * (widthInBytes / 48)]/255.0;
                            input[(y * 48) + x] = grayValue;

                        }
                    }
                }
                Sample sample = new Sample(input, 10, (SymbolType)trainImageLabels[fileName]);
                trainData.AddSample(sample);
                image.Dispose();
            }


            Dictionary<String, int> testImageLabels = new Dictionary<String, int>();
            using (StreamReader reader = new StreamReader(testMarkup))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string trimed = line.Trim();
                    if (trimed != "")
                    {
                        string[] parts = trimed.Split(':');
                        testImageLabels[parts[0]] = int.Parse(parts[1]);
                    }
                }
            }
            fileEntries = Directory.GetFiles(testImagesPath);
            foreach (string filePath in fileEntries)
            {
                Bitmap image = new Bitmap(filePath);
                Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);
                BitmapData bmpData = image.LockBits(rect, ImageLockMode.ReadOnly, image.PixelFormat);
                string fileName = filePath.Split('\\').Last().Split('.').First();
                double[] input = new double[48 * 48];
                unsafe
                {
                    byte* ptr = (byte*)bmpData.Scan0;
                    int heightInPixels = bmpData.Height;
                    int widthInBytes = bmpData.Stride;
                    for (int y = 0; y < heightInPixels; y++)
                    {
                        for (int x = 0; x < 48; x = x + 1)
                        {
                            double grayValue = ptr[(y * bmpData.Stride) + x * (widthInBytes / 48)] /255.0;
                            input[(y * 48) + x] = grayValue;

                        }
                    }
                }
                Sample sample = new Sample(input, 10, (SymbolType)testImageLabels[fileName]);
                testData.AddSample(sample);
                image.Dispose();
            }
        }

        public Sample getRandomTestImage()
        {
            return testData[rand.Next(testData.Count)];
        }
    }
}
