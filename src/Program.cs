namespace doubleyewdee
{
    using System;
    using System.Diagnostics;

    public unsafe static class Benchmark
    {
        public static void Main(string[] args)
        {
            var random = false;
            var arraySize = -1;
            if (args.Length > 0)
            {
                random = bool.Parse(args[0]);
            }
            if (args.Length > 1)
            {
                arraySize = int.Parse(args[1]);
            }

            // Do some warmup to ensure JITing occurs
            RunBenchmark(1000, false, true);

            if (arraySize > 0)
            {
                RunBenchmark(arraySize, false, false);
            }
            else
            {
                for (var i = 10; i < 1000000; i *= 10)
                {
                    RunBenchmark(i, false, false);
                    RunBenchmark(i, true, false);
                }
            }
            Console.ReadLine();
        }

        private static void RunBenchmark(int size, bool random, bool silent)
        {
            var array = Generate(size, random);
            BenchmarkArray(silent ? null : "inline safe", array, random, InlineSafeSort);
            BenchmarkArray(silent ? null : "nested safe", array, random, NestedSafeSort);
            BenchmarkArray(silent ? null : "inline unsafe", array, random, InlineUnsafeSort);
            BenchmarkArray(silent ? null : "nested unsafe", array, random, NestedUnsafeSort);
        }

        private static void BenchmarkArray(string name, int[] array, bool random, Action<int[]> runner)
        {
            var arrayCopy = new int[array.Length];
            Buffer.BlockCopy(array, 0, arrayCopy, 0, array.Length);
            GC.Collect(2);

            var watch = new Stopwatch();
            watch.Start();
            runner(arrayCopy);
            watch.Stop();
            Validate(arrayCopy);

            if (name != null)
            {
                Console.WriteLine("{0},{1},{2},{3}", name, array.Length, random ? "random" : "inverted", watch.ElapsedMilliseconds);
            }
        }

        private static int[] Generate(int size, bool random)
        {
            var array = new int[size];
            var rng = new Random();

            for (var i = 0;i < size; ++i)
            {
                array[i] = (random ? rng.Next(size) : size - i);
            }

            return array;
        }

        private static void Validate(int[] array)
        {
            for (var i = 0;i < array.Length - 1; ++i)
            {
                if (array[i] > array[i + 1])
                {
                    throw new Exception("Sort fail");
                }
            }
        }

        private static void InlineSafeSort(int[] array)
        {
            for (var i = 0; i < array.Length; ++i)
            {
                for (var j = i + 1; j < array.Length; ++j)
                {
                    if (array[i] > array[j])
                    {
                        var temp = array[i];
                        array[i] = array[j];
                        array[j] = temp;
                    }
                }
            }
        }

        private static void NestedSafeSort(int[] array)
        {
            for (var i = 0; i < array.Length; ++i)
            {
                NestedSafeSortInner(array, i + 1);
            }
        }

        private static void NestedSafeSortInner(int[] array, int startIndex)
        {
            var valueIndex = startIndex - 1;
            for (var i = startIndex; i < array.Length; ++i)
            {
                if (array[valueIndex] > array[i])
                {
                    var temp = array[valueIndex];
                    array[valueIndex] = array[i];
                    array[i] = temp;
                }
            }
        }

        private static void InlineUnsafeSort(int[] array)
        {
            fixed (int *a = array)
            {
                var end = a + array.Length;
                for (var i = a; i < end; ++i)
                {
                    for (var j = i + 1; j < end; ++j)
                    {
                        if (*i > *j)
                        {
                            var temp = *i;
                            *i = *j;
                            *j = temp;
                        }
                    }
                }
            }
        }

        private static void NestedUnsafeSort(int[] array)
        {
            fixed (int *a = array)
            {
                var end = a + array.Length;
                for (var i = a; i < end; ++i)
                {
                    NestedUnsafeSortInner(i + 1, end);
                }
            }
        }

        private static void NestedUnsafeSortInner(int* start, int* end)
        {
            var value = start - 1;
            for (var i = start; i < end; ++i)
            {
                if (*value > *i)
                {
                    var temp = *value;
                    *value = *i;
                    *i = temp;
                }
            }
        }
    }
}
