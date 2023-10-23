using System;

namespace MessageSender
{
    public static class GenerateRandom
    {
        //random locking
        static readonly Random random = new Random();
        static readonly object randomNumberLock = new object();

        public static int RandomNumberLock(int min, int max)
        {
            lock (randomNumberLock)
            { // synchronize
                return random.Next(min, max);
            }
        }

        public static long RandomChqAmt(int LowerLimit, int UpperLimit)
        {
            long res;
            string str = "";

            for (int i = 0; i < 2; i++)
            {
                res = GenerateRandom.RandomNumberLock(LowerLimit, UpperLimit);
                str += (res).ToString();
                UpperLimit /= 10;
            }
            return Convert.ToInt64(str);
        }
    }
}
