using SkiaSharp;

namespace Program
{
    public static class WaterMark
    {
        public static void CompareWatermarks(string watermark1, string watermark2)
        {
            var w1 = WaterMark.getWatermark(watermark1);
            var w2 = WaterMark.getWatermark(watermark2);

            int size1, size2;
            size1 = w1.GetLength(0);
            size2 = w2.GetLength(1);

            double ber = 0;
            for (int i = 0; i < size1; i++)
            {
                for (int j = 0; j < size2; j++)
                {
                    if (w1[i, j] != w2[i, j])
                    {
                        ber++;
                    }
                }
            }

            ber /= size1 * size2;
            ber *= 100;

            ber = Math.Round(ber, 2);

            Console.WriteLine("\nComparison of watermarks:\n  full BER = " + ber + "%");
        }

        public static void ComparePictures(string pictures1, string pictures2)
        {
            var pic1 = getPixels(pictures1);
            var pic2 = getPixels(pictures2);

            int size1, size2;
            size1 = pic1.GetLength(0);
            size2 = pic2.GetLength(1);

            Console.WriteLine("\nComparison of pictures:");
            Console.WriteLine("  MSE = {0}\n  PSNR = {1}\n  SSIM = {2}",
                Functions.MSE(pic1, pic2),
                Functions.PSNR(pic1, pic2),
                Functions.SSIM(pic1, pic2));
        }

        public static void Embed(string picture, string watermark)
        {
            int[,] pixels = getPixels(picture);
            int height = pixels.GetLength(1);
            int width = pixels.GetLength(0);
            int[,] bits = getWatermark(watermark);
            int sync_blocks_count = Globals.sync_blocks_count;
            int block_size = Globals.block_size;
            int q = Globals.q;

            int[,,] sync_blocks = new int[sync_blocks_count, block_size, block_size];
            int random_seed = 0;
            if (Globals.form_sync_blocks == Enums.FORM_SYNC_BLOCKS.YES)
            {
                for (int i_start = 0; i_start < sync_blocks_count * block_size; i_start += block_size)
                {
                    int[,] sync_block = Operations.SyncForm(pixels, i_start, 0);
                    for (int i1 = 0; i1 < block_size; i1++)
                    {
                        for (int j1 = 0; j1 < block_size; j1++)
                        {
                            sync_blocks[i_start / block_size, i1, j1] = sync_block[i1, j1];
                        }
                    }
                    Console.WriteLine("{0, -3} sync block powered!", i_start / block_size + 1);
                }
            }

            // Выбираем блоки, на основе которых будем встраивать
            else
            {
                for (int i_start = 0; i_start < sync_blocks_count * block_size; i_start += block_size)
                {
                    for (int i = 0; i < block_size; i++)
                    {
                        for (int j = 0; j < block_size; j++)
                        {
                            if (Globals.use_mid_freq == Enums.USE_MID_FREQ.NO || (i + j >= (double)block_size - Globals.m & i + j <= (double)block_size + Globals.p))
                            {
                                double current_coeff = Operations.countDCTCoeff(pixels, i_start, 0, i, j, block_size);
                                sync_blocks[i_start / block_size, i, j] = Operations.qimGet(current_coeff, q);
                            }
                            else
                            {
                                sync_blocks[i_start / block_size, i, j] = -1;
                            }
                        }
                    }
                }
            }

            
            int current_iteration = 0;
            for (int y_block = block_size; y_block < (Globals.low_pixel_count_test_bool ? 160 : height); y_block += block_size)
            {
                for (int x_block = 0; x_block < width; x_block += block_size)
                {
                    // Проверка на то, какой бит извлекается без преобразований
                    // запись картинки
                    if (Globals.use_skip_optimization == Enums.USE_SKIP_OPTIMISATION.YES)
                    {
                        int[,] block_image = new int[block_size, block_size];
                        for (int i = 0; i < block_size; i++)
                        {
                            for (int j = 0; j < block_size; j++)
                            {
                                block_image[i, j] = pixels[i + x_block, j + y_block];
                            }
                        }

                        // нахождение коэффициентов
                        double[,] coeffs = new double[block_size, block_size];
                        for (int i = 0; i < block_size; i++)
                        {
                            for (int j = 0; j < block_size; j++)
                            {
                                coeffs[i, j] = Operations.countDCTCoeff(block_image, 0, 0, i, j, block_size);
                            }
                        }
                        double best_diff = 0.5;
                        for (int sync_block = 0; sync_block < sync_blocks_count; sync_block++)
                        {
                            double current_diff = 0;
                            for (int i = 0; i < block_size; i++)
                            {
                                for (int j = 0; j < block_size; j++)
                                {
                                    if (Operations.qimGet(coeffs[i, j], q) != sync_blocks[sync_block, i, j])
                                    {
                                        current_diff++;
                                    }
                                }
                            }

                            current_diff /= block_size * block_size;

                            if (Math.Abs(current_diff - 0.5) > Math.Abs(best_diff - 0.5))
                            {
                                best_diff = current_diff;
                            }

                        }

                        if (best_diff < 0.5 - Globals.skip_threshold && bits[x_block / block_size, y_block / block_size] == 0
                            || best_diff > 0.5 + Globals.skip_threshold && bits[x_block / block_size, y_block / block_size] == 1)
                        {
                            Console.WriteLine("Current iteration: {0, 9})    -Skipped", current_iteration);
                            current_iteration++;
                            if (Globals.form_sync_blocks == Enums.FORM_SYNC_BLOCKS.YES)
                            {
                                for (int i = 0; i < block_size; i++)
                                {
                                    for (int j = 0; j < block_size; j++)
                                    {
                                        if (Globals.use_mid_freq == Enums.USE_MID_FREQ.NO
                                            || ((i + j >= (double)block_size - Globals.m)
                                                & (i + j <= (double)block_size + Globals.p)))
                                        {
                                            coeffs[i, j] = Operations.qimSet(coeffs[i, j],
                                                Operations.qimGet(coeffs[i, j], Globals.q),
                                                Globals.q);
                                        }
                                    }
                                }

                                for (int i = 0; i < block_size; i++)
                                {
                                    for (int j = 0; j < block_size; j++)
                                    {
                                        double result = Operations.countInverseDCTCoeff(coeffs, 0, 0, i, j, block_size);
                                        result = Math.Clamp(result, 0, 255);
                                        pixels[i + x_block, j + y_block] = (byte)result;
                                    }
                                }
                            }
                            continue;
                        }
                    }

                    // Начало GA
                    PopulationGA current_population = new PopulationGA(
                        ref random_seed,
                        pixels,
                        sync_blocks,
                        x_block,
                        y_block,
                        q, bits[x_block/block_size, y_block/block_size]);

                    if (Globals.random_type == Enums.RANDOM.SEED)
                    {
                        Console.WriteLine("Current iteration: {0, 9})    seed: {1, -10}"
                            + "     BER = {2, -5} \t PSNR = {3} \t fitness = {4}",
                            current_iteration++,
                            random_seed,
                            Math.Round(current_population.individuals[0].ber, 5),
                            Math.Round(current_population.individuals[0].psnr, 2),
                            current_population.individuals[0].fitness_result);
                    }
                    else
                    {
                        Console.WriteLine("Current iteration: {0, 9})"
                            + "                seed: no seed      BER = {1, -5}"
                            + "\t PSNR = {2} \t fitness = {3}",
                            current_iteration++,
                            Math.Round(current_population.individuals[0].ber, 5),
                            Math.Round(current_population.individuals[0].psnr, 3),
                            current_population.individuals[0].fitness_result);
                    }

                    random_seed += 100;
                }
            }

            // Сохранение картинки
            var image = SKBitmap.Decode(picture);

            for (int y = block_size; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    image.SetPixel(x, y,
                        new SKColor(
                            (byte)pixels[x, y],
                            image.GetPixel(x, y).Green,
                            image.GetPixel(x, y).Blue));
                }
            }

            using var skImage = SKImage.FromBitmap(image);
            using var data = skImage.Encode(SKEncodedImageFormat.Png, 100);

            using var stream = File.OpenWrite(Globals.output_image);
            data.SaveTo(stream);
        }

        public static void Extract(string picture)
        {
            int[,] pixels = getPixels(picture);
            int height = pixels.GetLength(1);
            int width = pixels.GetLength(0);
            int sync_blocks_count = Globals.sync_blocks_count;
            int block_size = Globals.block_size;
            int q = Globals.q;

            // Извлечение блоков синхронизации
            int[,,] sync_blocks = new int[sync_blocks_count, block_size, block_size];
            for (int i_start = 0; i_start < sync_blocks_count * block_size; i_start += block_size)
            {
                for (int i = 0; i < block_size; i++)
                {
                    for (int j = 0; j < block_size; j++)
                    {
                        double current_coeff = Operations.countDCTCoeff(pixels, i_start, 0, i, j, block_size);
                        sync_blocks[i_start / block_size, i, j] = Operations.qimGet(current_coeff, q);
                    }
                }
            }

            // Непосредственное извлечение
            int[,] bits = new int[width / block_size, height / block_size];
            for (int y_block = block_size; y_block < height; y_block += block_size)
            {
                for (int x_block = 0; x_block < width; x_block += block_size)
                {
                    // Извлечение паттерна
                    int[,] arr = new int[block_size, block_size];
                    for (int i = 0; i < block_size; i++)
                    {
                        for (int j = 0; j < block_size; j++)
                        {
                            arr[i, j] = Operations.qimGet(
                                Operations.countDCTCoeff(pixels, x_block, y_block, i, j, block_size), q);
                        }
                    }

                    // Поиск близкого синхронизационного блока
                    double best_diff = 0.5;
                    for (int sync = 0; sync < sync_blocks_count; sync++)
                    {
                        int[,] check_arr = new int[block_size, block_size];
                        for (int i = 0; i < block_size; i++)
                        {
                            for (int j = 0; j < block_size; j++)
                            {
                                check_arr[i, j] = sync_blocks[sync, i, j];
                            }
                        }
                        double current_diff = Functions.DIFF(arr, check_arr, block_size);

                        if (Math.Abs(current_diff - 0.5) > Math.Abs(best_diff - 0.5)) best_diff = current_diff;
                    }

                    if (best_diff > 0.5) bits[x_block / block_size, y_block / block_size] = 0;
                    else bits[x_block / block_size, y_block / block_size] = 1;
                }
            }

            // Сохранение водяного знака
            var wm = new SKBitmap(width / block_size, height / block_size);
            for (int x = 0; x < width / block_size; x++)
            {
                wm.SetPixel(x, 0, new SKColor(255, 255, 255));
            }
            for (int y = 1; y < height / block_size; y++)
            {
                for (int x = 0; x < width / block_size; x++)
                {
                    wm.SetPixel(x, y,
                        new SKColor(
                            (byte)(bits[x, y] * 255),
                            (byte)(bits[x, y] * 255),
                            (byte)(bits[x, y] * 255))
                        );
                }
            }

            using var skImage = SKImage.FromBitmap(wm);
            using var data = skImage.Encode(SKEncodedImageFormat.Png, 100);

            using var stream = File.OpenWrite(Globals.output_watermark);
            data.SaveTo(stream);
        }

        public static int[,] getPixels(string filename)
        {
            var image = SKBitmap.Decode(filename);

            int height = image.Height;
            var width = image.Width;

            int[,] pixels = new int[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var current_pixel = image.GetPixel(x, y);
                    pixels[x, y] = current_pixel.Red;
                }
            }

            return pixels;
        }

        // белый == 0, чёрный == 1
        public static int[,] getWatermark(string watermark)
        {
            var image = SKBitmap.Decode(watermark);

            int height = image.Height;
            var width = image.Width;

            int[,] pixels = new int[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var current_pixel = image.GetPixel(x, y);
                    pixels[x, y] = current_pixel.Red;
                    if (pixels[x, y] > 255 / 2) pixels[x, y] = 0;
                    else pixels[x, y] = 1;
                }
            }

            return pixels;
        }
    }
}
