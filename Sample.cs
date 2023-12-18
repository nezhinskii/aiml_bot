using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIMLTGBot
{
    public enum SymbolType : byte { Alpha = 0, Beta, Gamma, Epsilon, Iota, Lambda, Pi, Sigma, Tau, Psi, Undef };
    public class Sample
    {
        /// <summary>
        /// Входной вектор
        /// </summary>
        public double[] input = null;

        /// <summary>
        /// Вектор ошибки, вычисляется по какой-нибудь хитрой формуле
        /// </summary>
        public double[] error = null;

        /// <summary>
        /// Действительный класс образа. Указывается учителем
        /// </summary>
        public SymbolType actualClass;

        /// <summary>
        /// Распознанный класс - определяется после обработки
        /// </summary>
        public SymbolType recognizedClass;

        /// <summary>
        /// Конструктор образа - на основе входных данных для сенсоров, при этом можно указать класс образа, или не указывать
        /// </summary>
        /// <param name="inputValues"></param>
        /// <param name="sampleClass"></param>
        public Sample(double[] inputValues, int classesCount, SymbolType sampleClass = SymbolType.Undef)
        {
            double[] inputWithLines = new double[inputValues.Length + 96];
            for (int i = 0; i < inputValues.Length; ++i)
            {
                inputWithLines[i] = inputValues[i];
            }
            for (int i = 0; i < 48; ++i) {
                double transitions = 0;
                for (int j = 1; j < 48; ++j)
                {
                    if (inputValues[i * 48 + j] != inputValues[i * 48 + j - 1])
                    {
                        transitions++;
                    }
                }
                inputWithLines[inputValues.Length + i] = transitions / 48;
            }

            for (int j = 0; j < 48; ++j)
            {
                double transitions = 0;
                for (int i = 1; i < 48; ++i)
                {
                    if (inputValues[i * 48 + j] != inputValues[(i - 1) * 48 + j])
                    {
                        transitions++;
                    }
                }
                inputWithLines[inputValues.Length + 48 + j] = transitions / 48;
            }
            //  Клонируем массивчик
            input = (double[])inputWithLines.Clone();
            Output = new double[classesCount];
            if (sampleClass != SymbolType.Undef) Output[(int)sampleClass] = 1;


            recognizedClass = SymbolType.Undef;
            actualClass = sampleClass;
        }

        /// <summary>
        /// Выходной вектор, задаётся извне как результат распознавания
        /// </summary>
        public double[] Output { get; private set; }

        /// <summary>
        /// Обработка реакции сети на данный образ на основе вектора выходов сети
        /// </summary>
        public SymbolType ProcessPrediction(double[] neuralOutput)
        {
            Output = neuralOutput;
            if (error == null)
                error = new double[Output.Length];

            //  Нам так-то выход не нужен, нужна ошибка и определённый класс
            recognizedClass = 0;
            for (int i = 0; i < Output.Length; ++i)
            {
                error[i] = (Output[i] - (i == (int)actualClass ? 1 : 0));
                if (Output[i] > Output[(int)recognizedClass]) recognizedClass = (SymbolType)i;
            }

            return recognizedClass;
        }

        /// <summary>
        /// Вычисленная суммарная квадратичная ошибка сети. Предполагается, что целевые выходы - 1 для верного, и 0 для остальных
        /// </summary>
        /// <returns></returns>
        public double EstimatedError()
        {
            double Result = 0;
            for (int i = 0; i < Output.Length; ++i)
                Result += System.Math.Pow(error[i], 2);
            return Result;
        }

        /// <summary>
        /// Добавляет к аргументу ошибку, соответствующую данному образу (не квадратичную!!!)
        /// </summary>
        /// <param name="errorVector"></param>
        /// <returns></returns>
        public void updateErrorVector(double[] errorVector)
        {
            for (int i = 0; i < errorVector.Length; ++i)
                errorVector[i] += error[i];
        }

        /// <summary>
        /// Представление в виде строки
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string result = "Sample decoding : " + actualClass.ToString() + "(" + ((int)actualClass).ToString() +
                            "); " + Environment.NewLine + "Input : ";
            for (int i = 0; i < input.Length; ++i) result += input[i].ToString() + "; ";
            result += Environment.NewLine + "Output : ";
            if (Output == null) result += "null;";
            else
                for (int i = 0; i < Output.Length; ++i)
                    result += Output[i].ToString() + "; ";
            result += Environment.NewLine + "Error : ";

            if (error == null) result += "null;";
            else
                for (int i = 0; i < error.Length; ++i)
                    result += error[i].ToString() + "; ";
            result += Environment.NewLine + "Recognized : " + recognizedClass.ToString() + "(" +
                      ((int)recognizedClass).ToString() + "); " + Environment.NewLine;


            return result;
        }

        /// <summary>
        /// Возвращает битовое изображение для вывода образа
        /// </summary>
        /// <returns></returns>
        public Bitmap GenBitmap()
        {
            Bitmap drawArea = new Bitmap(48, 48);
            for (int i = 0; i < 48; ++i)
                for (int j = 0; j < 48; ++j)
                    if (input[i * 48 + j] > 0.1)
                        drawArea.SetPixel(j, i, Color.White);
                    else
                        drawArea.SetPixel(j, i, Color.Black);
            return drawArea;
        }

        /// <summary>
        /// Правильно ли распознан образ
        /// </summary>
        /// <returns></returns>
        public bool Correct()
        {
            return actualClass == recognizedClass;
        }
    }

    /// <summary>
    /// Выборка образов. Могут быть как классифицированные (обучающая, тестовая выборки), так и не классифицированные (обработка)
    /// </summary>
    public class SamplesSet : IEnumerable
    {
        /// <summary>
        /// Накопленные обучающие образы
        /// </summary>
        public List<Sample> samples = new List<Sample>();

        /// <summary>
        /// Добавление образа к коллекции
        /// </summary>
        /// <param name="image"></param>
        public void AddSample(Sample image)
        {
            samples.Add(image);
        }

        public int Count => samples.Count;

        public IEnumerator GetEnumerator()
        {
            return samples.GetEnumerator();
        }

        /// <summary>
        /// Реализация доступа по индексу
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public Sample this[int i]
        {
            get => samples[i];
            set => samples[i] = value;
        }

        public double TestNeuralNetwork(BaseNetwork network)
        {
            double correct = 0;
            double wrong = 0;
            foreach (var sample in samples)
            {
                if (sample.actualClass == network.Predict(sample)) ++correct;
                else ++wrong;
            }
            return correct / (correct + wrong);
        }

        // Тут бы ещё сохранение в файл и чтение сделать, вообще классно было бы
    }
}
