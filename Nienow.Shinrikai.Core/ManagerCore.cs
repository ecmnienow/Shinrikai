using System;

namespace Nienow.Shinrikai.Core
{
    public class ManagerCore
    {
        #region Public Methods

        public bool HasEnoughLevel(EJLPT userLevel, EJLPT contentLevel, bool restrict = false)
        {
            switch (userLevel)
            {
                case EJLPT.None:
                    if (restrict)
                    {
                        if (contentLevel == EJLPT.None)
                            return true;
                    }
                    else
                    {
                        if (contentLevel == EJLPT.None ||
                            contentLevel == EJLPT.N5)
                            return true;
                    }
                    break;

                case EJLPT.N5:
                    if (restrict)
                    {
                        if (contentLevel == EJLPT.None ||
                            contentLevel == EJLPT.N5)
                            return true;
                    }
                    else
                    {
                        if (contentLevel == EJLPT.None ||
                            contentLevel == EJLPT.N5 ||
                            contentLevel == EJLPT.N4)
                            return true;
                    }
                    break;

                case EJLPT.N4:
                    if (restrict)
                    {
                        if (contentLevel == EJLPT.None ||
                            contentLevel == EJLPT.N5 ||
                            contentLevel == EJLPT.N4)
                            return true;
                    }
                    else
                    {
                        if (contentLevel == EJLPT.None ||
                            contentLevel == EJLPT.N5 ||
                            contentLevel == EJLPT.N4 ||
                            contentLevel == EJLPT.N3)
                            return true;
                    }
                    break;

                case EJLPT.N3:
                    if (restrict)
                    {
                        if (contentLevel == EJLPT.None ||
                            contentLevel == EJLPT.N5 ||
                            contentLevel == EJLPT.N4 ||
                            contentLevel == EJLPT.N3)
                            return true;
                    }
                    else
                    {
                        if (contentLevel == EJLPT.None ||
                            contentLevel == EJLPT.N5 ||
                            contentLevel == EJLPT.N4 ||
                            contentLevel == EJLPT.N3 ||
                            contentLevel == EJLPT.N2)
                            return true;
                    }
                    break;

                case EJLPT.N2:
                    if(restrict)
                    {
                        if (contentLevel == EJLPT.None ||
                            contentLevel == EJLPT.N5 ||
                            contentLevel == EJLPT.N4 ||
                            contentLevel == EJLPT.N3 ||
                            contentLevel == EJLPT.N2)
                            return true;
                    }
                    else
                    {
                        return true;
                    }
                    break;
                case EJLPT.N1:
                    return true;
            }

            return false;
        }

        public int RandomNumber(int min, int max, ref int lastRandomNumber)
        {
            if (max == min)
                return min;

            Random random = new Random();

            int randomNumber = -1;
            int count = 0;

            while ((randomNumber < min || lastRandomNumber == randomNumber) && count < 5)
            {
                randomNumber = random.Next(min, max);
                randomNumber = randomNumber + DateTime.Now.Millisecond;
                randomNumber = randomNumber % max;

                count++;
            }

            lastRandomNumber = randomNumber;

            return randomNumber;
        }

        #endregion
    }
}
