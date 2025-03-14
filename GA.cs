using System.Linq.Expressions;
using System.Numerics;
using System.Security.Cryptography;

namespace Program
{
    public class Individual
    {
        public int[,] genes;
        public int intSeed = 0;
        public int mutation_chance = Globals.mutation_chance;
        public double ber;
        public double psnr;
        public double? fitness_result;

        public Individual(int block_size1, int block_size2)
        {
            genes = new int[block_size1, block_size2];
        }

        public Individual(int[,] genes)
        {
            this.genes = genes;
        }
        public Individual(int[,] genes, int mutation_chance)
        {
            this.genes = genes;
            this.mutation_chance = mutation_chance;
        }

        public Individual generate(ref int seed)
        {
            int length1 = genes.GetLength(0);
            int length2 = genes.GetLength(1);

            Random rnd = new Random();

            for (int i = 0; i < length1; i++)
            {
                for (int j = 0; j < length2; j++)
                {
                    if (Globals.random_type == Enums.RANDOM.SEED) rnd = new Random(seed);

                    genes[i, j] = rnd.Next(2);

                    seed += rnd.Next(100);
                }
            }

            return this;
        }

        public void setMutation(double fitness)
        {
            if (Globals.use_mutation_optimization == Enums.USE_MUTATION_OPTIMIZATION.YES)
            {
                int mutation = (int)(Math.Pow(Math.Exp(-1 * fitness / Globals.mutation_minimizer), 2.5) * 100);
                this.mutation_chance = mutation;
            }
        }

        public static Individual operator+ (Individual left, Individual right)
        {
            int length1, length2;
            length1 = left.genes.GetLength(0);
            length2 = left.genes.GetLength(1);
            int[,] genes = new int[length1, length2];

            for (int i = 0; i < length1; i++)
            {
                for (int j = 0; j < length2; j++)
                {
                    if (left.genes[i, j] == right.genes[i, j])
                    {
                        genes[i, j] = left.genes[i, j];
                        left.intSeed += 10;
                        right.intSeed += 10;
                        genes[i, j] = (genes[i, j] + Functions.random_with_seed(
                            (left.mutation_chance + right.mutation_chance)/2,
                            left.intSeed + right.intSeed)) % 2;
                    }
                    else
                    {
                        genes[i, j] = Functions.random_with_seed(50,
                            left.intSeed + right.intSeed);
                        left.intSeed += 1000;
                        right.intSeed += 2000;
                    }
                }
            }

            return new Individual(genes, (left.mutation_chance + right.mutation_chance) / 2);
        }
    }

    public class PopulationGA
    {
        public static int block_size = Globals.block_size;
        public System.Collections.Generic.List<Individual> individuals = new List<Individual>();
        public System.Collections.Generic.List<int[,]> target_zeros;
        public int[,] image = new int[block_size, block_size];
        public double[,] coeffs = new double[block_size, block_size];
        public int[,,] sync_blocks;

        public int population_size = Globals.population_size;
        int random_seed = Globals.seed;
        int cycles = Globals.cycles;
        int q = Globals.q;

        public PopulationGA(ref int random, int[,] new_image, int[,,] sync_blocks, int start_x, int start_y, int q, int bit)
        {
            this.q = q;
            this.sync_blocks = sync_blocks;
            // запись картинки
            for (int i = 0; i < block_size; i++)
            {
                for (int j = 0; j < block_size; j++)
                {
                    image[i, j] = new_image[i + start_x, j + start_y];
                }
            }

            // нахождение коэффициентов
            for (int i = 0; i < block_size; i++)
            {
                for (int j = 0; j < block_size; j++)
                {
                    coeffs[i, j] = Operations.countDCTCoeff(image, 0, 0, i, j, block_size);
                }
            }

            generate_population(population_size, ref random);

            for (int i = 0; i < cycles; i++)
            {
                count_fitness(bit);
                selection();
                resize();
                crossover();
            }

            count_fitness(bit);

            // тут встраивание
            Individual best = individuals.MaxBy(ind => ind.fitness_result);
            Operations.qimEmbed(best.genes, coeffs, q);
            for (int i = 0; i < block_size; i++)
            {
                for (int j = 0; j < block_size; j++)
                {
                    double result = Operations.countInverseDCTCoeff(coeffs, 0, 0, i, j, block_size);
                    result = Math.Clamp(result, 0, 255);
                    new_image[i + start_x, j + start_y] = (byte)Math.Round(result);
                }
            }
        }

        private void count_fitness(int bit = -1)
        {
            foreach (Individual ind in individuals)
            {
                if (ind.fitness_result == null)
                {
                    // для картинки найти новые коэффициенты с помощью qim
                    int[,] check_img = new int[block_size, block_size];
                    double[,] coeffs_with_qim = new double[block_size, block_size];
                    double[,] check_double = new double[block_size, block_size];

                    // проверка на соответствие биту
                    double best_check_diff = 0.5;
                    for (int i = 0; i < sync_blocks.GetLength(0); i++)
                    {
                        int[,] arr = new int[block_size, block_size];

                        for (int j = 0; j < sync_blocks.GetLength(1); j++)
                        {
                            for (int k = 0; k < sync_blocks.GetLength(2); k++)
                            {
                                arr[j, k] = sync_blocks[i, j, k];
                            }
                        }

                        double current_diff = Functions.DIFF(arr, ind.genes, block_size);
                        if (Math.Abs(current_diff - 0.5) > Math.Abs(best_check_diff - 0.5)) best_check_diff = current_diff;
                    }
                    if ((best_check_diff > 0.5 && bit == 0) || (best_check_diff < 0.5 && bit == 1)) inverse(ind);

                    // Пробное встраивание
                    for (int i = 0; i < block_size; i++)
                    {
                        for (int j = 0; j < block_size; j++)
                        {
                            coeffs_with_qim[i, j] = Operations.qimSet(coeffs[i, j], ind.genes[i, j], q);
                        }
                    }

                    // Обратное ДКП
                    for (int i = 0; i < block_size; i++)
                    {
                        for (int j = 0; j < block_size; j++)
                        {
                            check_img[i, j] = (byte)Math.Round(
                                Operations.countInverseDCTCoeff(coeffs_with_qim, 0, 0, i, j, block_size));
                        }
                    }

                    double psnr = Functions.PSNR(check_img, image);
                    ind.psnr = psnr;

                    // Прямое ДКП для вычисления робастности
                    int[,] genes_resulted = new int[block_size, block_size];
                    for (int i = 0; i < block_size; i++)
                    {
                        for (int j = 0; j < block_size; j++)
                        {
                            check_double[i, j] = Operations.
                                countDCTCoeff(check_img, 0, 0, i, j, block_size);
                            genes_resulted[i, j] = Operations.
                                qimGet(check_double[i, j], q);
                        }
                    }
                    double ber = Functions.BER(genes_resulted, ind.genes);
                    ind.ber = ber;

                    // на случай, если возвращается не тот бит
                    int mul = 1;
                    // Вычисление на соответствие паттерну
                    double best_diff = 0.5;
                    for (int i = 0; i < sync_blocks.GetLength(0); i++)
                    {
                        int[,] arr = new int[block_size, block_size];

                        for (int j = 0; j < sync_blocks.GetLength(1); j++)
                        {
                            for (int k = 0; k < sync_blocks.GetLength(2); k++)
                            {
                                arr[j, k] = sync_blocks[i, j, k];
                            }
                        }

                        double current_diff = Functions.DIFF(arr, genes_resulted, block_size);

                        if (Math.Abs(current_diff - 0.5) > Math.Abs(best_diff - 0.5)) best_diff = current_diff;
                    }

                    if (best_diff == 0.5) mul = 0;
                    else if (best_diff > 0.5 && bit == 0) mul = 0;
                    else if (best_diff < 0.5 && bit == 1) mul = 0;
                    else mul = 1;


                    double fitness = Functions.fitness(psnr, ber);
                    ind.fitness_result = mul * fitness;
                    ind.setMutation(fitness);
                }
            }
        }

        private void crossover()
        {
            int current_population_size = individuals.Count;
            while (individuals.Count < population_size)
            {
                Random rnd1 = new Random(), rnd2 = new Random();

                if (Globals.random_type == Enums.RANDOM.SEED)
                {
                    rnd1 = new Random(random_seed++);
                    random_seed += 3;
                    rnd2 = new Random(random_seed++);
                }


                Individual parent1 = individuals[rnd1.Next(current_population_size)];
                Individual parent2 = individuals[rnd2.Next(current_population_size)];

                Individual child = parent1 + parent2;

                individuals.Add(child);
            }
        }

        private void generate_population(int size, ref int random)
        {
            for (int i = 0; i < size; i++)
            {
                individuals.Add(new Individual(block_size, block_size).generate(ref random));
                random += 100;
            }
        }

        public static void inverse(Individual ind)
        {
            for (int i = 0; i < ind.genes.GetLength(0); i++)
            {
                for (int j = 0; j < ind.genes.GetLength(1); j++)
                {
                    ind.genes[i, j] = (ind.genes[i, j] + 1) % 2;
                }
            }
        }

        public void resize()
        {
            if (Globals.population_resize == Enums.USE_POPULATION_RESIZE_OPTIMIZATION.YES)
            {
                double best_fitness = (double)individuals.MaxBy(ind => ind.fitness_result).fitness_result;
                int population = (int)(Math.Pow(Math.Exp(-1 * best_fitness / Globals.population_resize_param), Globals.fitness_a / 15) * Globals.population_size);
            }
        }

        private void selection(Enums.SELECTIONS selection = Globals.selection)
        {
            if (selection == Enums.SELECTIONS.ELITE)
            {
                individuals = individuals.
                    OrderByDescending(ind => ind.fitness_result).
                    Take(Globals.population_size - Globals.population_size * Globals.child_rate / 100).
                    ToList();
            }
            if (selection == Enums.SELECTIONS.TOURNAMENT)
            {
                int battles = Globals.population_size - Globals.population_size * Globals.child_rate / 100;
                Individual[] warriors = new Individual[battles];
                warriors[0] = individuals.MaxBy(ind => ind.fitness_result);

                Random rnd = new Random();
                for (int round = 1; round < battles; round++)
                {
                    random_seed += 10;

                    int max_fitness_index = 0;
                    Individual[] battle = new Individual[battles];
                    for (int warrior = 0; warrior < Globals.warriors_in_battle; warrior++)
                    {
                        if (Globals.random_type == Enums.RANDOM.SEED)
                        {
                            rnd = new Random(random_seed);
                            random_seed += 100;
                        }

                        battle[warrior] = individuals[rnd.Next(individuals.Count)];

                        max_fitness_index = battle[warrior].fitness_result > battle[max_fitness_index].fitness_result
                            ? warrior : max_fitness_index;
                    }

                    warriors[round] = battle[max_fitness_index];
                }

                System.Collections.Generic.List<Individual> new_population = new List<Individual>();
                foreach (Individual ind in warriors)
                {
                    new_population.Add(ind);
                }

                individuals = new_population.OrderByDescending(ind => ind.fitness_result).ToList();
            }
        }
    }
}
