using SkiaSharp;
using static System.Net.Mime.MediaTypeNames;

namespace Program
{
    public static class Operations
    {

        public static double C(int n)
        {
            if (n == 0) return (1.0 / Math.Sqrt(2.0));
            else return 1.0;
        }

        public static double cosDCT(int size, int x, int i)
        {
            double arg = Math.PI / size * ((double)x + 1.0 / 2.0) * i;
            return Math.Cos(arg);
        }

        public static double countDCTCoeff(int[,] array, int startX, int startY, int i, int j, int sizeDCT)
        {
            double coeff = 0;

            for (int x = 0; x < sizeDCT; x++)
            {
                for (int y = 0; y < sizeDCT; y++)
                {
                    coeff += array[startX + x, startY + y]
                        * cosDCT(sizeDCT, x, i) * cosDCT(sizeDCT, y, j);
                }
            }
            coeff *= C(i) * C(j) * (2.0 / sizeDCT);

            return coeff;
        }

        public static double countInverseDCTCoeff(double[,] array, int startX, int startY, int x, int y, int sizeDCT)
        {
            double sum = 0;

            for (int i = 0; i < sizeDCT; i++)
            {
                for (int j = 0; j < sizeDCT; j++)
                {
                    sum += array[startX + i, startY + j]
                        * C(i) * C(j)
                        * cosDCT(sizeDCT, x, i) * cosDCT(sizeDCT, y, j);
                }
            }

            return sum * (2.0 / sizeDCT);
        }

        public static void qimEmbed(int[,] bits, double[,] coeffs, int q)
        {
            int block_size = Globals.block_size;
            for (int i = 0; i < coeffs.GetLength(0); i++)
            {
                for (int j = 0; j < coeffs.GetLength(1); j++)
                {
                    if (Globals.use_mid_freq == Enums.USE_MID_FREQ.NO || (i + j >= (double)block_size - Globals.m & i + j <= (double)block_size + Globals.p))
                    {
                        coeffs[i, j] = qimSet(coeffs[i, j], bits[i, j], q);
                    }
                }
            }
        }

        public static int qimGet(double coeff, int q)
        {
            if (Math.Abs(coeff - qimSet(coeff, 0, q)) < Math.Abs(coeff - qimSet(coeff, 1, q)))
            {
                return 0;
            }
            else
            {
                return 1;
            }
        }

        public static double qimSet(double coeff, int bit, int q)
        {
            coeff -= coeff % q;

            if (bit == 0)
            {
                return coeff;
            }
            else if (bit == 1)
            {
                if (coeff >= 0)
                {
                    coeff += q/2;
                }
                else
                {
                    coeff -= q/2;
                }
                return coeff;
            }
            else
            {
                return coeff;
            }
        }

        public static int[,] SyncForm(int[,] image, int start_x, int start_y)
        {
            int q = Globals.q;
            int block_size = Globals.block_size;
            double[,] coeffs = new double[block_size, block_size];

            // нахождение коэффициентов
            int[,] sync_block = new int[block_size, block_size];
            for (int i = 0; i < block_size; i++)
            {
                for (int j = 0; j < block_size; j++)
                {
                    if (Globals.use_mid_freq == Enums.USE_MID_FREQ.NO || (i + j >= (double)block_size - Globals.m & i + j <= (double)block_size + Globals.p))
                    {
                        coeffs[i, j] = countDCTCoeff(image, start_x, start_y, i, j, block_size);
                        coeffs[i, j] = qimSet(coeffs[i, j], qimGet(coeffs[i, j], q), q);
                        sync_block[i, j] = qimGet(coeffs[i, j], q);
                    }
                }
            }

            // возвращение пикселей
            for (int i = 0; i < block_size; i++)
            {
                for (int j = 0; j < block_size; j++)
                {
                    double result = countInverseDCTCoeff(coeffs, 0, 0, i, j, block_size);
                    result = Math.Clamp(result, 0, 255);
                    image[i + start_x, j + start_y] = (byte)(Math.Round(result));
                }
            }

            return sync_block;
        }
    }
}
