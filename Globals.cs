using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Program
{
    public static class Enums
    {
        public enum FORM_SYNC_BLOCKS
        {
            YES = 1,
            NO = 0
        }

        public enum RANDOM
        {
            REAL = 0,
            SEED = 1
        }

        public enum SELECTIONS
        {
            ELITE = 0,
            TOURNAMENT = 1,
        }

        public enum USE_MID_FREQ
        {
            YES = 1,
            NO = 0
        }

        public enum USE_MUTATION_OPTIMIZATION
        {
            YES = 1,
            NO = 0
        }

        public enum USE_POPULATION_RESIZE_OPTIMIZATION
        {
            YES = 1,
            NO = 0
        }

        public enum USE_SKIP_OPTIMISATION
        {
            YES = 1,
            NO = 0
        }
    }

    public class Globals
    {
        // Globals
            public const Enums.FORM_SYNC_BLOCKS form_sync_blocks = Enums.FORM_SYNC_BLOCKS.YES;
            public const Enums.RANDOM random_type = Enums.RANDOM.REAL;
            public const int sync_blocks_count = 4;
            public const Enums.USE_MID_FREQ use_mid_freq = Enums.USE_MID_FREQ.YES;
            public const int m = 3;
            public const int p = 1;

        // DCT settings
            public const int block_size = 8;

        // Embedding settings
            public const int q = 32;

        // Genetic Algorithm
            public const Enums.SELECTIONS selection = Enums.SELECTIONS.TOURNAMENT;
            public const Enums.USE_MUTATION_OPTIMIZATION use_mutation_optimization = Enums.USE_MUTATION_OPTIMIZATION.NO;
            public const Enums.USE_POPULATION_RESIZE_OPTIMIZATION population_resize = Enums.USE_POPULATION_RESIZE_OPTIMIZATION.YES;
            public const Enums.USE_SKIP_OPTIMISATION use_skip_optimization = Enums.USE_SKIP_OPTIMISATION.NO;
            public const double skip_threshold = 0.2;        // >= 0
            public const double mutation_minimizer = 0.1;      // >= 0
            public const double population_resize_param = 32;      // >= 0
            public const int mutation_chance = 30;
            public const int seed = 573257;
            public const int population_size = 68;
            public const int cycles = 30;
            public const double fitness_a = 12;
            public const int child_rate = 70;

        // Tournament
            public const int warriors_in_battle = 3;

        // Testing - bool
            public const bool low_pixel_count_test_bool = false;

        // Save settings
            public const string input_image      =       "Ship.png";
            public const string input_watermark  =   "watermark64.png";
            public const string output_image     = "check_3.png";
            public const string output_watermark = "check_wm_3.png";
    }
}
