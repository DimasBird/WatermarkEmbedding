using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Program
{
    public static class Functions
    {
        public static double BER(int[,] array1, int[,] array2)
        {
            int length1 = array1.GetLength(0);
            int length2 = array1.GetLength(1);
            int Length = 0;
            int block_size = Globals.block_size;
            double result = 0;

            for (int i = 0; i < length1; i++)
            {
                for (int j = 0; j < length2; j++)
                {
                    if (Globals.use_mid_freq == Enums.USE_MID_FREQ.NO || ((i + j >= (double)block_size - Globals.m) & (i + j <= (double)block_size + Globals.p)))
                    {
                        if (array1[i, j] != array2[i, j])
                        {
                            result += 1;
                        }
                        Length++;
                    }
                }
            }

            return result / Length;
        }

        public static double DIFF(int[,] target, int[,] variable, int block_size)
        {
            double diff = 0;
            int count = 0;
            for (int i = 0; i < block_size; i++)
            {
                for (int j = 0; j < block_size; j++)
                {
                    if (Globals.use_mid_freq == Enums.USE_MID_FREQ.NO || ((i + j >= (double)block_size - Globals.m) & (i + j <= (double)block_size + Globals.p)))
                    {
                        if (target[i, j] != variable[i, j]) diff++;
                        count++;
                    }
                }
            }

            diff /= count;
            return diff;
        }

        public static double fitness(double psnr, double ber)
        {
            double a = Globals.fitness_a;
            return psnr / a + (1 - ber);
        }

        public static double MSE(int[,] array1, int[,] array2)
        {
            int length1 = array1.GetLength(0);
            int length2 = array1.GetLength(1);

            double MSE = 0;

            for (int i = 0; i < length1; i++)
            {
                for (int j = 0; j < length2; j++)
                {
                    MSE += Math.Pow(array1[i, j] - array2[i, j], 2);
                }
            }

            return MSE / length1 / length2;
        }

        public static double PSNR(int[,] array1, int[,] array2)
        {
            double mse = MSE(array1, array2);

            return 10 * Math.Log10(255 * 255 / mse);
        }

        // returns 1 with chance p_chance
        public static int random_with_seed(int p_chance, int seed)
        {
            Random rnd = Globals.random_type == Enums.RANDOM.SEED ? new Random(seed) : new Random();
            int result = rnd.Next(100);

            if (result < p_chance)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        public static double SSIM(int[,] arr1, int[,] arr2)
        {
            double mid_x = 0, mid_y = 0;
            double var_x = 0, var_y = 0;
            double cov_xy = 0;
            int L = 255;
            double k1 = 0.01, k2 = 0.03;
            double c1 = Math.Pow(k1 * L, 2);
            double c2 = Math.Pow(k2 * L, 2);

            int Length = arr1.Length;

            // Средний x
            foreach (int n in arr1)
            {
                mid_x += n;
            }
            mid_x /= Length;

            // Средний y
            foreach(int n in arr2)
            {
                mid_y += n;
            }
            mid_y /= Length;

            // Дисперсия x
            foreach (int n in arr1)
            {
                var_x += Math.Pow(n - mid_x, 2);
            }
            var_x /= Length;

            // Дисперсия y
            foreach (int n in arr2)
            {
                var_y += Math.Pow(n - mid_y, 2);
            }
            var_y /= Length;

            // Ковариация x и y
            for (int i = 0; i < arr1.GetLength(0); i++)
            {
                for (int j = 0; j < arr2.GetLength(0); j++)
                {
                    cov_xy += (arr1[i, j] - mid_x) * (arr2[i, j] - mid_y);
                }
            }
            cov_xy /= Length;

            double ssim = 1;
            ssim *= 2 * mid_x * mid_y + c1;
            ssim *= 2 * cov_xy + c2;
            ssim /= Math.Pow(mid_x, 2) + Math.Pow(mid_y, 2) + c1;
            ssim /= var_x + var_y + c2;

            return ssim;
        }
    }
}
