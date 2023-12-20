using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Titan.Utilities.Extensions
{
    public static class ListExtensions
    {
        /// <summary>
        /// Computes the average difference for a given list
        /// </summary>
        /// <param name="numbers"></param>
        /// <returns>If list quantity is too small</returns>
        /// <exception cref="ArgumentException"></exception>
        public static double AverageDifference<T>(this IEnumerable<T> enumerable) where T : INumber<T>
        {
            var numbers = (enumerable as List<T>) ?? enumerable.ToList();

            if (numbers == null || numbers.Count < 2)
            {
                throw new ArgumentException("List must contain at least two elements to calculate the average difference.");
            }

            double sumDifferences = 0.0;

            for (int i = 1; i < numbers.Count; i++)
            {
                double difference = Math.Abs(double.CreateChecked(numbers[i]) - double.CreateChecked(numbers[i - 1]));
                sumDifferences += difference;
            }

            double averageDifference = sumDifferences / (numbers.Count - 1);

            return averageDifference;
        }

        /// <summary>
        /// Gets the interpolated value of a sequence of numbers
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        public static double GetInterpolatedValue(this IEnumerable<(long, double)> enumerable, long number)
        {
            var numbers = (enumerable as List<(long, double)>) ?? enumerable.ToList();

            var match = enumerable.FirstOrDefault(p => p.Item1 == number, (-1, -1));

            // no need to interpolate if data timestamp matches
            if (match.Item1 != -1)
            {
                return match.Item2;
            }

            var prev = numbers.TakeWhile(x => x.Item1 < number).LastOrDefault(numbers[0]);
            var next = numbers.SkipWhile(x => x.Item1 < number).FirstOrDefault(numbers[numbers.Count - 1]);
            
            if (prev == next)
            {
                return prev.Item2;
            }

            double prevDifference = number - prev.Item1;
            double totalDifference = next.Item1 - prev.Item1;

            return prev.Item2 + (next.Item2 - prev.Item2) * (prevDifference / totalDifference);
        }
    }
}
