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
        /// <returns></returns>
        public static double GetInterpolatedValue<T>(this IEnumerable<(long, double)> enumerable, T number) where T : INumber<T>
        {
            var numbers = (enumerable as List<(long, double)>) ?? enumerable.ToList();

            var match = enumerable.FirstOrDefault(p => p.Item1 == double.CreateChecked(number), (-1, -1));

            // no need to interpolate if data timestamp matches
            if (match.Item1 != -1)
            {
                return match.Item2;
            }

            var prev = numbers.SkipWhile(x => x.Item2 != double.CreateChecked(number)).Skip(1).DefaultIfEmpty(numbers[0]).FirstOrDefault();
            var next = numbers.TakeWhile(x => x.Item2 != double.CreateChecked(number)).DefaultIfEmpty(numbers[numbers.Count - 1]).LastOrDefault();

            var prevDifference = Math.Abs(double.CreateChecked(number) - double.CreateChecked(prev.Item1));
            var nextDifference = Math.Abs(double.CreateChecked(number) - double.CreateChecked(next.Item1));

            return double.CreateChecked(prev.Item2) + (double.CreateChecked(next.Item2) - double.CreateChecked(prev.Item2) * (prevDifference / nextDifference));
        }
    }
}
