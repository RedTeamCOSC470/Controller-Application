using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MeadeAscomTest
{
    class Program
    {
        static double getRA(double Hour, double Minutre, double Second)
        {
            /*
             * Gets the value for the Right Ascension from the hour, minute and second.
             * Telescope only acepts the hour value for the RA so minutes and seconds
             * need to be converted to fractions of an hour.
             * 20.00 = RA of 20:00:00
             * 21.5 = RA of 21:30:00
             * 30/60 = .5
             * 60*60 = 3600
             * hours + minutes/60 + seconds/3600
             */
            double MinutesInHour = 60;   //avoiding magiv numbers... might be needed
            double SecondsInHour = 3600; //if you are observing on Mars...?
            return (Hour + (Minutre / MinutesInHour) + (Second / SecondsInHour));
        }

        static double TimeToRA(DateTime time)
        {
            return getRA(time.Hour, time.Minute, time.Second);
        }
        static void Main(string[] args)
        {
            /*
             * The main part of the program! Writen by Robert Smith for the Red Team
             * Create an instance of the telescope, then connect.
             * Turn tracking on (if we can do that)
             * Stick it all in a try block to catch any errors and handle them
             * Tell telescope to slew to the coordinates we give it (RA and Dec)
             */
            //ScopeSim.TelescopeClass myScope = new ScopeSim.TelescopeClass();
            MeadeEx.TelescopeClass myScope = new MeadeEx.TelescopeClass();
            try
            {

                myScope.Connected = true;

                if (myScope.AtPark)
                {
                    myScope.Unpark();
                }
                if (myScope.CanSetTracking)
                {
                    myScope.Tracking = true;
                }

                double RightAscension = getRA(16, 45, 12);
                double Declination = 23.00;

                if (args.Length == 2)
                {
                    DateTime RA;
                    DateTime.TryParse(args[0], out RA);
                    if (RA != DateTime.MinValue)
                    {
                        RightAscension = TimeToRA(RA);
                        double.TryParse(args[1], out Declination);
                    }
                    else
                    {
                        Console.Error.WriteLine("Could not convert the value {0} to a time." ,args[0]);
                    }
                    myScope.SlewToCoordinates(RightAscension, Declination);
                }
                else if (args.Length == 1)
                {
                    if (args[0] == "PARK" && myScope.CanPark)
                    {
                        if (myScope.CanPark)
                        {
                            myScope.SetPark();
                            myScope.Park();
                        }
                        else
                        {
                            Console.Error.WriteLine("You can not park this type of telescope.");
                        }
                    }
                    if (args[0] == "UNPARK" && myScope.AtPark)
                    {
                        if (myScope.CanUnpark)
                        {
                            myScope.Unpark();
                        }
                        else
                        {
                            Console.Error.WriteLine("You can not unpark this type of telescope automatically.");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error: " + e.Message);
                System.Threading.Thread.Sleep(3000);
            }
        }
    }
}
