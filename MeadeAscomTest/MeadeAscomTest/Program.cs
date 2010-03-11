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
             * hours + minutes/60 + seconds/3600
             */
            double MinutesInHour = 60;   //avoiding magic numbers... might be needed
            double SecondsInHour = 3600; //if you are observing on Mars...?
            return (Hour + (Minutre / MinutesInHour) + (Second / SecondsInHour));
        }

        static double timeToRA(DateTime Time)
        {
            /*
             * Gets the hour minute and second of a date time variable 
             * and passes those values to the getRA method
             */
            return getRA(Time.Hour, Time.Minute, Time.Second);
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
                myScope.Connected = true; // connect to the telescope

                double RightAscension = -1; // variables for later
                double Declination;
                DateTime RA;

                if (myScope.AtPark)
                {
                    myScope.Unpark();
                }
                if (myScope.CanSetTracking)
                {
                    myScope.Tracking = true;
                }

                // Set up done, checking inputs to find out what we want to do.

                if (args.Length == 0)
                {
                    //testing mode do something smart here
                    Console.Out.WriteLine("Please use the program by providing the coordinates to point the telescope to or the " + 
                        "the name of the object from the MiniSAC catalog.");
                }
                else if (args.Length == 2 && DateTime.TryParse(args[0], out RA) && double.TryParse(args[1], out Declination))
                {
                    // normal coordinates entered
                    Console.Out.WriteLine("Normal Mode");
                    
                    if (RA != DateTime.MinValue)
                    {
                        RightAscension = timeToRA(RA);
                    }
                    else
                    {
                        Console.Error.WriteLine("Could not convert the value {0} to a time.", args[0]);
                    }
                    myScope.SlewToCoordinates(RightAscension, Declination);
                }
                else
                {
                    // location name entered - one or more arg values combine them into 1 arg
                    Console.Out.WriteLine("Special Mode");
                    string theArg = "";
                    for (int n = 0; n < args.Length; n++)
                    {
                        if (string.Compare(theArg, "") != 0)
                            theArg += " ";
                        theArg += args[n];
                    }
                    Console.Out.WriteLine("Got Arg: '" + theArg + "'");
                    //Console.Out.WriteLine("==PARK?: " + (string.Compare(theArg, "PARK") == 0));
                    //Console.Out.WriteLine("==park?: " + (string.Compare(theArg, "park") == 0));
                    //Console.Out.WriteLine("==Park: " + (string.Compare(theArg, "Park") == 0));
                    //Console.Out.WriteLine("can park?: " + myScope.CanPark);
                    //Console.Out.WriteLine("So then we can park?: " + (string.Compare(theArg, "PARK") == 0 || string.Compare(theArg, "park") == 0 || string.Compare(theArg, "Park") == 0));
                    if ((string.Compare(theArg, "PARK") == 0 || string.Compare(theArg, "park") == 0 || string.Compare(theArg, "Park") == 0)
                        && myScope.CanPark)
                    {
                        Console.Out.WriteLine("Going to Park");
                        if (myScope.CanSetPark)
                        {
                            myScope.SetPark();
                        }
                        myScope.Park();
                    }
                        else if ((string.Compare(theArg, "UNPARK") == 0 || string.Compare(theArg, "unpark") == 0 || string.Compare(theArg, "Unpark") == 0) 
                        && myScope.AtPark)
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
                    else
                    {
                        Console.Out.WriteLine("Going To Move");

                        MiniSAC.CatalogClass miniCatalog = new MiniSAC.CatalogClass();
                        miniCatalog.SelectObject(theArg);
                            float tempRA = miniCatalog.RightAscension;
                            float tempDec = miniCatalog.Declination;
                         Console.Out.WriteLine("Moving to: {0}, {1}", tempRA, tempDec);
                        Console.Out.WriteLine(miniCatalog.RightAscension);
                        Console.Out.WriteLine(miniCatalog.Declination);
                        myScope.SlewToCoordinates(tempRA, tempDec);
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
