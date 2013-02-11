using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PlayerCSharpAI.api;

namespace PlayerCSharpAI.AI
{
    /// <summary>
    /// A generalized range between a min and max.
    /// 
    /// From http://stackoverflow.com/questions/5343006/is-there-a-c-sharp-type-for-representing-an-integer-range
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Range<T> where T : IComparable<T>
    {
        /// <summary>
        /// Minimum value of the range
        /// </summary>
        public T Minimum { get; set; }

        /// <summary>
        /// Maximum value of the range
        /// </summary>
        public T Maximum { get; set; }

        /// <summary>
        /// Constructor takes min and max values.
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        public Range(T min, T max)
        {
            Minimum = min;
            Maximum = max;
        }

        /// <summary>
        /// Presents the Range in readable format
        /// </summary>
        /// <returns>String representation of the Range</returns>
        public override string ToString() { return String.Format("[{0} - {1}]", Minimum, Maximum); }

        /// <summary>
        /// Determines if the range is valid
        /// </summary>
        /// <returns>True if range is valid, else false</returns>
        public Boolean IsValid() { return Minimum.CompareTo(Maximum) <= 0; }

        /// <summary>
        /// Determines if the provided value is inside the range
        /// </summary>
        /// <param name="value">The value to test</param>
        /// <returns>True if the value is inside Range, else false</returns>
        public Boolean ContainsValue(T value)
        {
            return (Minimum.CompareTo(value) <= 0) && (value.CompareTo(Maximum) <= 0);
        }

        /// <summary>
        /// Determines if this Range is inside the bounds of another range
        /// </summary>
        /// <param name="Range">The parent range to test on</param>
        /// <returns>True if range is inclusive, else false</returns>
        public Boolean IsInsideRange(Range<T> Range)
        {
            return this.IsValid() && Range.IsValid() && Range.ContainsValue(this.Minimum) && Range.ContainsValue(this.Maximum);
        }

        /// <summary>
        /// Determines if another range is inside the bounds of this range
        /// </summary>
        /// <param name="Range">The child range to test</param>
        /// <returns>True if range is inside, else false</returns>
        public Boolean ContainsRange(Range<T> Range)
        {
            return this.IsValid() && Range.IsValid() && this.ContainsValue(Range.Minimum) && this.ContainsValue(Range.Maximum);
        }
    }

    /// <summary>
    /// A state in the game.
    /// 
    /// Each state encompasses:
    /// where each passenger currently is,
    /// what their next destination is,
    /// the distance to each company (split into ranges),
    /// the distance to the desired destination of each passenger (split into ranges),
    /// the probability that an opponent will pick up each passenger before we can get to them,
    /// the porbability that an enemy of the passenger would be at the next desintation by the time we drop
    /// 
    /// </summary>
    public class State
    {
        /// <summary>
        /// As per the rules, when a Passenger is in a Limo.
        /// </summary>
        public static String LimoLocLabel = "limo";
        
        /// <summary>
        /// As per the rules, when a Passenger is at their final destionation.
        /// </summary>
        public static String NoMoreLocLabel = "finished";

        public static int numPassengers = 12;
        public static int numCompanies = 12;
        public static int numLocations = numCompanies + 1;

        private static List<Range<Double>> DistIntervals = new List<Range<Double>> {
            new Range<Double>(0, 33),
            new Range<Double>(34, 66),
            new Range<Double>(66, Double.PositiveInfinity),
        };

        private int[] passLoc = new int[numPassengers];
        private int[] passDest = new int[numPassengers];
        private int[] playerCompRange = new int[numCompanies];
        private int[] passDestRange = new int[numPassengers];

        public static List<byte[]> GetInitPassLoc(byte[] array, int index, int size)
        {
            List<byte[]> ret = new List<byte[]>();
            if (index == size)
            {
                return new List<byte[]> { array };
            }
            for (int i = 0; i < numLocations; i++)
            {
                byte[] new_array = null;
                // if last loop, don't copy array
                if ((i + 1) == numLocations)
                {
                    new_array = array;
                }
                else
                {
                    new_array = new byte[size];
                    // copy all previous values into new array
                    for (int j = 0; j < index; j++)
                    {
                        new_array[j] = array[j];
                    }
                }
                // set current value
                new_array[index] = (byte) i;
                // pass the new array down to have the remaining indexes populated
                List<byte[]> tmp = GetInitPassLoc(new_array, index + 1, size);
                foreach (byte[] a in tmp)
                {
                    ret.Add(a);
                }
            }
            return ret;
        }
    }

    public class QLearner
    {
        public Boolean IsInitialized { get; private set; }
        public List<State> Q { get; private set; }

        public QLearner()
        {
            IsInitialized = false;
            Q = new List<State>();
        }

        /// <summary>
        /// Initialize the QLearner game state space with empty states.
        /// TODO: add load from file, save to file.
        /// </summary>
        public void Initialize(List<Passenger> passengers, List<Player> players, List<Company> companies)
        {
            if (IsInitialized)
            {
                return;
            }
            else
            {
                IsInitialized = true;
            }

            List<byte[]> tmp = State.GetInitPassLoc(new byte[State.numPassengers], 0, State.numPassengers);
            foreach (byte[] array in tmp)
            {
                foreach (byte a in array)
                {
                    Console.Write(((int)a).ToString() + " ");
                }
                Console.Write("\n");
            }

        }

    }
}
