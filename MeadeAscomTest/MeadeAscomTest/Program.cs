using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EDSDKLib;
using System.Windows.Forms;
using System.Diagnostics;

namespace MeadeAscomTest
{
    class Program
    {
        //private static string[] fakeArgs = { "-I", "test", "-R", "12:30:00", "-D", "20", "-d", "30", "-T" };
        public static int debug = 1;
        public static int imageCount = 0;
        public static int millisecondsInASecond = 1000;
        public static int cameraWaitTime = 5 * millisecondsInASecond;
        private static string CTRCHAR = "-";
        private static bool CMDwasPARKING = false;
        private static string scheduleID = "";

        public static double getRA(double Hour, double Minutre, double Second)
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

        public static double timeToRA(DateTime Time)
        {
            /*
             * Gets the hour minute and second of a date time variable 
             * and passes those values to the getRA method
             */
            return getRA(Time.Hour, Time.Minute, Time.Second);
        }

        public static void Main(string[] args)
        {
            // we have some arguments we should interpert them
            // loop through all args and break them into key/value pairs
            // we look for string starting with / then the next character
            // is the key. all args up to the next time we find a /
            // are the value part of the pair.
            //args = fakeArgs;
            List<KeyValuePair<string, string>> commands = new List<KeyValuePair<string, string>>();
            string theKey = "";
            string itsValue = "";
            for (int i = 0; i < args.Length; i++)
            {
                string thisArg = args[i];
                if (thisArg.StartsWith(CTRCHAR))
                {
                    // we found a new command. Terminate old command and start a new one.
                    if (theKey.CompareTo("") != 0)
                    {
                        // first time through we don't have any command yet
                        commands.Add(new KeyValuePair<string, string>(theKey, itsValue));
                        itsValue = "";
                    }
                    theKey = thisArg.Substring(1);
                }
                else
                {
                    // if we don't have a command charater at the start we just add it to the 
                    // value for this command. There could be a problem if we don't start with
                    // a command character first so error check that
                    if (theKey.CompareTo("") != 0)
                    {
                        if (itsValue.CompareTo("") == 0)
                            itsValue += thisArg;
                        else
                            itsValue += " " + thisArg;
                    }
                }
            }
            // after the loop we still have not saved the last command. lets do that now.
            commands.Add(new KeyValuePair<string, string>(theKey, itsValue));

            // now lets test this out by outputting a nice formated list:
            // ok the list thing worked after a bug fix or 2 (forgot to reset the value
            // after each key is saved
            // now we need to check for Keys we recognize like R or D or O and then
            // assign the property values based on that.

            int numberOfPics = 1; // minimum of one pic
            bool foundNumPics = false;

            int duration = 0;
            bool foundDuration = false;

            uint shutterSpd = 0;
            bool foundShutterSpeed = false;

            uint isoFormat = 0;
            bool foundISOFormat = false;

            uint exposure = 0;
            bool foundExposure = false;

            string rightAsention = "";
            bool foundRightAsention = false;

            string declination = "";
            bool foundDeclination = false;

            string objectID = "";
            bool foundObjectID = false;
            bool isTesting = false;

            //string scheduleID = ""; // Schedule ID is now a global variable for the program
            bool foundScheduleID = false;

            // create a writer and open the file
            TextWriter tw = new StreamWriter("c:\\temp\\data.txt");


            foreach (KeyValuePair<string, string> pair in commands)
            {
                switch (pair.Key)
                {
                    case "n":
                        {
                            // if we have not found the property already
                            // we check that we have not already set a conflicting property
                            // then we set this property
                            if (!foundNumPics)
                            {
                                if (!foundDuration)
                                {
                                    // write a line of text to the file
                                    tw.WriteLine(pair.Key + ": " + pair.Value);
                                    numberOfPics = int.Parse(pair.Value);
                                    foundNumPics = true;
                                    Console.Out.WriteLine("number of pictures to take: " + pair.Value);
                                }
                                else
                                {
                                    Console.Out.WriteLine("Can't set number of pictures. A duration is already set");
                                }
                            }
                            else
                            {
                                Console.Out.WriteLine("Can't set number of pictures. A number of pictures is already set");
                            }
                            break;
                        }
                    case "d":
                        {
                            if (!foundDuration)
                            {
                                if (!foundNumPics)
                                {
                                    // write a line of text to the file
                                    tw.WriteLine(pair.Key + ": " + pair.Value);
                                    duration = int.Parse(pair.Value);
                                    foundDuration = true;
                                    Console.Out.WriteLine("duration: " + pair.Value);
                                }
                                else
                                {
                                    Console.Out.WriteLine("Can't set duration. A number of pictures is already set");
                                }
                            }
                            else
                            {
                                Console.Out.WriteLine("Can't set duration. A duration is already set");
                            }
                            break;
                        }
                    case "s":
                        {
                            if (!foundShutterSpeed)
                            {
                                // write a line of text to the file
                                tw.WriteLine(pair.Key + ": " + pair.Value);
                                switch (pair.Value)
                                {
                                    case "1/1000": { shutterSpd = 0x88; break; }
                                    case "1/500": { shutterSpd = 0x80; break; }
                                    case "1/250": { shutterSpd = 0x78; break; }
                                    case "1/125": { shutterSpd = 0x70; break; }
                                    case "1/60": { shutterSpd = 0x68; break; }
                                    case "1/30": { shutterSpd = 0x60; break; }
                                    case "1/15": { shutterSpd = 0x58; break; }
                                    case "1/8": { shutterSpd = 0x50; break; }
                                    case "1/4": { shutterSpd = 0x48; break; }
                                    case "1/2": { shutterSpd = 0x25; break; } // 1/2 doesn't seem to be valid for this camera. 
                                    case "1": { shutterSpd = 0x38; break; }

                                }
                                //shutterSpd = pair.Value;
                                foundShutterSpeed = true;
                                Console.Out.WriteLine("shutter speed: " + pair.Value);

                            }
                            else
                            {
                                Console.Out.WriteLine("Can't set shutter speed. A shutter speed is already set");
                            }
                            break;
                        }
                    case "i":
                        {
                            if (!foundISOFormat)
                            {
                                // write a line of text to the file
                                tw.WriteLine(pair.Key + ": " + pair.Value);
                                switch (pair.Value)
                                {
                                    case "6": { isoFormat = 0x00000028; break; }
                                    case "12": { isoFormat = 0x00000030; break; }
                                    case "25": { isoFormat = 0x00000038; break; }
                                    case "50": { isoFormat = 0x00000040; break; }
                                    case "100": { isoFormat = 0x00000048; break; }
                                    case "125": { isoFormat = 0x0000004b; break; }
                                    case "160": { isoFormat = 0x0000004d; break; }
                                    case "200": { isoFormat = 0x00000050; break; }
                                    case "250": { isoFormat = 0x00000053; break; }
                                    case "320": { isoFormat = 0x00000055; break; }
                                    case "400": { isoFormat = 0x00000058; break; }
                                    case "500": { isoFormat = 0x0000005b; break; }
                                    case "640": { isoFormat = 0x0000005d; break; }
                                    case "800": { isoFormat = 0x00000060; break; }
                                    case "1000": { isoFormat = 0x00000063; break; }
                                    case "1250": { isoFormat = 0x00000065; break; }
                                    case "1600": { isoFormat = 0x00000068; break; }
                                    case "3200": { isoFormat = 0x00000070; break; }
                                    case "6400": { isoFormat = 0x00000078; break; }
                                    case "12800": { isoFormat = 0x00000080; break; }
                                    case "25600": { isoFormat = 0x00000088; break; }
                                    case "51200": { isoFormat = 0x00000090; break; }
                                    case "102400": { isoFormat = 0x00000098; break; }
                                    default:
                                        {
                                            break;
                                        }
                                }


                                //isoFormat = pair.Value;
                                foundISOFormat = true;
                                Console.Out.WriteLine("ISO format: " + pair.Value);
                            }
                            else
                            {
                                Console.Out.WriteLine("Can't set ISO format. A ISO format is already set");
                            }
                            break;
                        }
                    case "e":
                        {
                            if (!foundExposure)
                            {
                                // write a line of text to the file
                                tw.WriteLine(pair.Key + ": " + pair.Value);
                                //exposure = pair.Value;
                                foundExposure = true;
                                Console.Out.WriteLine("exposure: " + pair.Value);
                            }
                            else
                            {
                                Console.Out.WriteLine("Can't set exposure. A exposure is already set");
                            }
                            break;
                        }
                    case "R":
                        {
                            if (!foundRightAsention)
                            {
                                if (!foundObjectID)
                                {
                                    // write a line of text to the file
                                    tw.WriteLine(pair.Key + ": " + pair.Value);
                                    rightAsention = pair.Value;
                                    foundRightAsention = true;
                                    Console.Out.WriteLine("right asention: " + pair.Value);
                                }
                                else
                                {
                                    Console.Out.WriteLine("Can't set right asention. A object ID is already set");
                                }
                            }
                            else
                            {
                                Console.Out.WriteLine("Can't set right asention. A right asention is already set");
                            }
                            break;
                        }
                    case "D":
                        {
                            if (!foundDeclination)
                            {
                                if (!foundObjectID)
                                {
                                    // write a line of text to the file
                                    tw.WriteLine(pair.Key + ": " + pair.Value);
                                    declination = pair.Value;
                                    foundDeclination = true;
                                    Console.Out.WriteLine("declination: " + pair.Value);
                                }
                                else
                                {
                                    Console.Out.WriteLine("Can't set declination. A object ID is already set");
                                }
                            }
                            else
                            {
                                Console.Out.WriteLine("Can't set declination. A declination is already set");
                            }
                            break;
                        }
                    case "O":
                        {
                            if (!foundObjectID)
                            {
                                if (!foundDeclination && !foundRightAsention)
                                {
                                    // write a line of text to the file
                                    tw.WriteLine(pair.Key + ": " + pair.Value);
                                    objectID = pair.Value;
                                    foundObjectID = true;
                                    Console.Out.WriteLine("object ID: " + pair.Value);
                                }
                                else
                                {
                                    Console.Out.WriteLine("Can't set object ID. A right asention and/or declination is already set");
                                }
                            }
                            else
                            {
                                Console.Out.WriteLine("Can't set object ID. A object ID is already set");
                            }
                            break;
                        }
                    case "I":
                        {
                            if (!foundScheduleID)
                            {
                                // write a line of text to the file
                                tw.WriteLine(pair.Key + ": " + pair.Value);
                                scheduleID = pair.Value;
                                foundScheduleID = true;
                                Console.Out.WriteLine("schedule ID: " + pair.Value);
                            }
                            else
                            {
                                Console.Out.WriteLine("Can't set schedule ID. A schedule ID is already set");
                            }
                            break;
                        }
                    case "T":
                        {
                            isTesting = true;
                            break;
                        }
                    default:
                        {
                            Console.Out.WriteLine(pair.Key + ": " + pair.Value);
                            break;
                        }
                }
            }
            // close the stream
            tw.Close();
            Console.Out.WriteLine("foundScheduleID " + foundScheduleID);
            Console.Out.WriteLine("foundRightAsention " + foundRightAsention);
            Console.Out.WriteLine("foundDeclination " + foundDeclination);
            Console.Out.WriteLine("foundObjectID " + foundObjectID);
            Console.Out.WriteLine("foundNumPics " + foundNumPics);
            Console.Out.WriteLine("foundDuration " + foundDuration);
            if (foundScheduleID)
            {
                if ((foundRightAsention && foundDeclination) || foundObjectID)
                {
                    TelescopeStuff(rightAsention, foundRightAsention,
                        declination, foundDeclination,
                        objectID, foundObjectID, isTesting);
                    if (foundNumPics || foundDuration)
                    {
                        if (!CMDwasPARKING)
                        {
                            int convertedDuration = foundDuration ? duration : numberOfPics;
                            CameraStuff(convertedDuration, foundDuration,
                                shutterSpd, foundShutterSpeed,
                                isoFormat, foundISOFormat,
                                exposure, foundExposure);
                        }
                    }
                    else
                    {
                        Console.Out.WriteLine("please specify the number of pictures to take or a duration to capture images for using -n or -d and then a number");
                    }
                }
                else
                {
                    Console.Out.WriteLine("you must specify an object (using -O then the object ID)");
                    Console.Out.WriteLine("or a right asention and declination (-RA and then a time and -D and a value)");
                }
            }
            else
            {
                Console.Out.WriteLine("please specify a schedule ID using -I and then an ID");
            }
        }

        // Sorry for this method decleration being so stupidly long.
        public static void TelescopeStuff(string rightAsention, bool foundRightAsention,
            string declination, bool foundDeclination,
            string objectID, bool foundObjectID, bool Testing)
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

                if (myScope.AtPark)
                {
                    myScope.Unpark();
                }
                if (myScope.CanSetTracking)
                {
                    myScope.Tracking = true;
                }

                // Set up done, checking inputs to find out what we want to do.

                /* 
                 * We are using a a set of control characters to indicate what values we are passing in.
                 * We start by breaking the arguments into commands based on the control character.
                 * We then check the control character for each command and assign variables acordingly
                 */

                if (foundRightAsention && foundDeclination)
                {
                    // normal coordinates entered easy mode!
                    Console.Out.WriteLine("Normal Mode");
                    double internalRightAscension = -1; // the = -1 is to ensure that a value has been asigned
                    double internalDeclination;
                    DateTime RA;

                    DateTime.TryParse(rightAsention, out RA);
                    double.TryParse(declination, out internalDeclination);

                    if (RA != DateTime.MinValue)
                    {
                        internalRightAscension = timeToRA(RA);
                    }
                    else
                    {
                        Console.Error.WriteLine("Could not convert the value {0} to a time.", rightAsention);
                    }
                    myScope.SlewToCoordinates(internalRightAscension, internalDeclination);
                    CMDwasPARKING = false;
                }
                else if (foundObjectID)
                {
                    // location name entered - one or more arg values combine them into 1 arg
                    Console.Out.WriteLine("Special Mode");
                    string theArg = objectID;

                    if ((string.Compare(theArg, "PARK") == 0 || string.Compare(theArg, "park") == 0 || string.Compare(theArg, "Park") == 0)
                        && myScope.CanPark)
                    {
                        Console.Out.WriteLine("Going to Park");
                        if (myScope.CanSetPark)
                        {
                            myScope.SetPark();
                        }
                        myScope.Park();
                        CMDwasPARKING = true;
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
                        CMDwasPARKING = true;
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
                        CMDwasPARKING = false;
                    }
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error: " + e.Message);
                System.Threading.Thread.Sleep(3000);
            }
        }

        public static void CameraStuff(int duration, bool isTime,
            uint shutterSpeed, bool foundShutterSpeed, uint isoSetting, bool foundISOFormat, uint exposureSetting, bool foundExposure)
        {
            /*
             * The basic out line of this program is as follows:
             * 
             * start the sdk
             * get a list of all cameras connected to this computer
             * get the first camera
             * register some event handlers to receive messages from the camera
             * open a session with the camera
             * tell the camera to save images to the host compter
             * start a loop
             * tell the camera to take an image
             * close and reopen the session
             * handle the image transfer event that is raised
             * release all used variables
             * close the sdk
             * 
             * we use the global variable debug to control debugging output
             * 
             */

            // This needs to be called at the start of using the camera
            if (debug == 1) Console.WriteLine("Starting the SDK");
            EDSDK.EdsInitializeSDK();
            if (debug == 1) Console.WriteLine("SDK started");

            // We want to get a list of all cameras connected to this computer so 
            // we can find the camera to use. This should be easy, just use camera 1
            IntPtr cameras;
            // it should be noted that IntPtr and uint are used quite a bit in this code
            // IntPtr is a pointer to a memory location. It is used to pass objects to
            // the SDK. uint is used for return codes mostly. Check the documentation
            // or the EDSDK.cs file for more details.

            // ALL SDK methods return an error code. 
            // An error code of EDS_ERR_OK means it was ok otherwise something broke.
            uint errCode;

            if (debug == 1) Console.WriteLine("getting EdsGetCameraList");
            errCode = EDSDK.EdsGetCameraList(out cameras);
            if (debug == 1) Console.WriteLine("got EdsGetCameraList");

            if (errCode == EDSDK.EDS_ERR_OK)
            {
                // We found cameras!
                // here we want to check the child count. It should be 1 if not break for now.
                int i; // int to save the count to.

                if (debug == 1) Console.WriteLine("getting EdsGetChildCount");
                errCode = EDSDK.EdsGetChildCount(cameras, out i);
                if (debug == 1) Console.WriteLine("got EdsGetChildCount");

                if (errCode == EDSDK.EDS_ERR_OK)
                {
                    if (i == 1)
                    {
                        // then get the first camera connected to the computer
                        // this should be at index 0 and should be our camera
                        IntPtr theCamera;
                        errCode = EDSDK.EdsGetChildAtIndex(cameras, 0, out theCamera);
                        if (errCode == EDSDK.EDS_ERR_OK)
                        {
                            // we are done with the camera list. Release it.
                            EDSDK.EdsRelease(cameras);

                            // we now have the camera; time to register event handlers.
                            // This was a copy paste from the sample code in the documentation
                            // I did however make some small fixes to what looked like typos


                            // this nothing variable is used to because the set methods need an IntPtr
                            // but we don't have anything useful to put there.
                            IntPtr nothing = IntPtr.Zero;

                            if (debug == 1) Console.WriteLine("setting Object Event Handler");
                            // This bit of code basically means:
                            // Set the cameras object event handler to the method handleObjectEvent
                            // it should be called for all object events. there is no context.
                            if (errCode == EDSDK.EDS_ERR_OK)
                            {
                                errCode = EDSDK.EdsSetObjectEventHandler(theCamera, EDSDK.ObjectEvent_All,
                                (EDSDK.EdsObjectEventHandler)handleObjectEvent, nothing);
                                if (debug == 1) Console.WriteLine("Success");
                            }

                            if (debug == 1) Console.WriteLine("setting Property Event Handler");
                            // This bit of code basically means:
                            // Set the cameras property event handler to the method handlePropertyEvent
                            // it should be called for all property events. there is no context.
                            if (errCode == EDSDK.EDS_ERR_OK)
                            {
                                errCode = EDSDK.EdsSetPropertyEventHandler(theCamera, EDSDK.PropertyEvent_All,
                                (EDSDK.EdsPropertyEventHandler)handlePropertyEvent, nothing);
                                if (debug == 1) Console.WriteLine("Success");
                            }

                            if (debug == 1) Console.WriteLine("setting State Event Handler");
                            // This bit of code basically means:
                            // Set the cameras state event handler to the method handleStateEvent
                            // it should be called for all state events. there is no context.
                            if (errCode == EDSDK.EDS_ERR_OK)
                            {
                                errCode = EDSDK.EdsSetCameraStateEventHandler(theCamera, EDSDK.StateEvent_All,
                                (EDSDK.EdsStateEventHandler)handleSateEvent, nothing);
                                if (debug == 1) Console.WriteLine("Success");
                            }
                            if (debug == 1) Console.WriteLine("Handelers set starting session");

                            // then open a session with the camera
                            errCode = EDSDK.EdsOpenSession(theCamera);

                            if (errCode == EDSDK.EDS_ERR_OK)
                            {
                                /*
                                 * Here we will set the camera settings specified by the user.
                                 * Those should include shutter speed, ISO speed and exposure.
                                 */

                                if (foundShutterSpeed)
                                {
                                    //uint newSpeed;
                                    //if (uint.TryParse(shutterSpeed, out newSpeed))
                                    errCode = setShutterSpeed(theCamera, shutterSpeed);
                                }
                                if (foundISOFormat)
                                {
                                    //uint newISO;
                                    //if (uint.TryParse(isoSetting, out newISO))
                                    errCode = setISOSpeed(theCamera, isoSetting);
                                }
                                if (foundExposure)
                                {
                                    //uint newExposure;
                                    //if (uint.TryParse(exposureSetting, out newExposure))
                                    errCode = setExposure(theCamera, exposureSetting);
                                }


                                // here we compute the numbr of pictures to take. If we pass in a number of pictures
                                // just use that if we specify that it is a time we are passing then we convert it 
                                // to milliseconds and then calculate the number of pictures that fit in that time.
                                int picsToTake = duration;

                                if (isTime)
                                {
                                    picsToTake = (duration * millisecondsInASecond) / cameraWaitTime;
                                }

                                // now we loop through the picture taking code once for each picture we will take.
                                for (int j = 0; j < picsToTake; j++)
                                {
                                    // here we configure the camera to send all picutres directly to the host computer
                                    // instead of saving them on the camera.
                                    EDSDK.EdsSaveTo toHost = EDSDK.EdsSaveTo.Host;
                                    errCode = setSaveLocation(theCamera, toHost);

                                    // Standard error code checking and debugging message
                                    if (errCode == EDSDK.EDS_ERR_OK)
                                    {
                                        if (debug == 1) Console.WriteLine("set to Save to Computer");
                                    }
                                    else
                                    {
                                        Console.WriteLine("error: 0x{0:X8}", errCode);
                                    }

                                    // eds send command (to camera, take picture enum, 0) // the 0 is an input parameter
                                    // the take picture command ignores it but we still need something there.
                                    // the send status commands are to lock the user interface on the camera while we use it
                                    // so no one can mess with the camera controls
                                    // this is only needed for cameras using protocal 1

                                    EDSDK.EdsSendStatusCommand(theCamera, EDSDK.CameraState_UILock, 0);
                                    errCode = EDSDK.EdsSendCommand(theCamera, EDSDK.CameraCommand_TakePicture, 0);
                                    EDSDK.EdsSendStatusCommand(theCamera, EDSDK.CameraState_UIUnLock, 0);

                                    if (errCode == EDSDK.EDS_ERR_OK)
                                    {
                                        // Provide message for debugging.
                                        if (debug == 1) Console.WriteLine("Click");

                                        // originally I thought we needed to process the download here but it seems
                                        // that an object event is raised once the image has been successfully taken
                                        // so instead we just wait here for a small delay to give the camera time
                                        // to capture the image. 
                                        System.Threading.Thread.Sleep(cameraWaitTime);

                                        // Once we have finished waiting for the camera to take the image we need to
                                        // tell the aplication to process any waiting events. This causes the call back
                                        // methods to get called and our waiting image will get downloaded!

                                        Application.DoEvents(); // requires System.Windows.Forms

                                    }
                                    else
                                    {
                                        // Could not send the take picture command
                                        Console.WriteLine("camera error: Could not send the take picture command");
                                        Console.WriteLine("error: 0x{0:X8}", errCode);
                                    }
                                }
                                errCode = EDSDK.EdsCloseSession(theCamera);
                                if (errCode == EDSDK.EDS_ERR_OK)
                                {
                                    // we most likely need to release some stuff.
                                    EDSDK.EdsRelease(theCamera);
                                }
                                else
                                {
                                    // Could not close session
                                    Console.WriteLine("camera error: Could not close session");
                                    Console.WriteLine("error: 0x{0:X8}", errCode);
                                }
                            }
                            else
                            {
                                // Could not open session
                                Console.WriteLine("camera error: Could not open session");
                                Console.WriteLine("error: 0x{0:X8}", errCode);
                            }
                        }
                        else
                        {
                            // Could not get the first child
                            Console.WriteLine("camera error: Could not get the first camera");
                            Console.WriteLine("error: 0x{0:X8}", errCode);
                        }
                    }
                    else
                    {
                        //too many or too few cameras connected.
                        Console.WriteLine("camera error: There are too many or too few cameras connected. ");
                        Console.WriteLine("error: 0x{0:X8}", errCode);
                    }
                }
                else
                {
                    // Could not find out how many cameras connected
                    Console.WriteLine("camera error: Could not find how many cameras were connected");
                    Console.WriteLine("error: 0x{0:X8}", errCode);
                }
            }
            else
            {
                // Could not get a list of cameras
                Console.WriteLine("camera error: Could not get a list of cameras");
                Console.WriteLine("error: 0x{0:X8}", errCode);
            }

            // this needs to be called after we are done using the camera.
            if (debug == 1) Console.WriteLine("Stopping the SDK");
            EDSDK.EdsTerminateSDK();
            if (debug == 1) Console.WriteLine("SDK stopped");
        }


        public static void runImageTransferFile(string imagePath, string ScheduleID)
        {
            Process p = new Process();
            try
            {
                string targetDir;
                targetDir = string.Format(@"C:\stargazer");
                p.StartInfo.UseShellExecute = true;
                p.StartInfo.WorkingDirectory = targetDir;
                p.StartInfo.FileName = "stargazer_post_image.bat";
                p.StartInfo.Arguments = imagePath + " " + ScheduleID;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                p.WaitForExit();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception Occurred :{0},{1}", ex.Message, ex.StackTrace.ToString());
            }
        }

        public static uint handleObjectEvent(uint myEvent, IntPtr myObject, IntPtr context)
        {
            // This will be called any time we take a new picture (and restart the session???)
            // we want to confirm that the even is a item request transfer and if so
            // we want to pass the object to transfer on to the downloadImage method
            Console.Out.WriteLine("Object Event");
            Console.Out.WriteLine(" > 0x{0:X}", myEvent);
            if (myEvent == EDSDK.ObjectEvent_DirItemRequestTransfer)
            {
                downloadImage(myObject);
            }
            // ItemRequestTransferDT is alsmost the same as ItemRequestTransfer
            if (myEvent == EDSDK.ObjectEvent_DirItemRequestTransferDT)
            {
                downloadImage(myObject);
            }
            // once we have downloaded the image we can relse the image object.
            if (myObject != IntPtr.Zero)
            {
                EDSDK.EdsRelease(myObject);
            }
            // we always want to return err_OK if everything was ok.
            return EDSDK.EDS_ERR_OK;
        }
        public static uint handlePropertyEvent(uint myEvent, uint property, uint parameter, IntPtr context)
        {
            // do nothing. We don't really care about property events right now.
            //Console.Out.WriteLine("Property Event");
            //Console.Out.WriteLine(" > 0x{0:X}", myEvent);
            return EDSDK.EDS_ERR_OK;
        }
        public static uint handleSateEvent(uint myEvent, uint parameter, IntPtr context)
        {
            // do nothing. We don't really care about state events right now.
            //Console.Out.WriteLine("State Event");
            //Console.Out.WriteLine(" > 0x{0:X}", myEvent);
            return EDSDK.EDS_ERR_OK;
        }

        // These are some get and set methods for important properties.

        public static uint getShutterSpeed(IntPtr theCamera, out uint Tv)
        {
            uint tmp = 0;
            if (debug == 1) Console.WriteLine("getting Shutter Speed");
            uint err = EDSDK.EDS_ERR_OK;
            // I don't think we need the next 5 lines or so but the example in the documentation
            // had them so I don't think I will remove them right now.
            EDSDK.EdsDataType dataType;
            int dataSize;
            err = EDSDK.EdsGetPropertySize(theCamera, EDSDK.PropID_Tv, 0, out dataType, out dataSize);
            if (err == EDSDK.EDS_ERR_OK)
            {
                // This is the real working line. it gets the property TV into the variable tmp.
                err = EDSDK.EdsGetPropertyData(theCamera, EDSDK.PropID_Tv, 0, out tmp);
            }
            else
            {
                Console.WriteLine("Could not get property");
                Console.WriteLine("Error: " + err);
            }
            // this whole TV = tmp bit is a bit silly. Since TV is listed as an output variable
            // we need to assign the value of TV at some point. the compiler didn't like it when
            // I just used Tv in the get propery size method so I used this as a work around.
            Tv = tmp;
            return err;
        }

        public static uint setShutterSpeed(IntPtr theCamera, uint TvValue)
        {
            uint err;
            err = EDSDK.EdsSetPropertyData(theCamera, EDSDK.PropID_Tv, 0, sizeof(uint), new IntPtr(TvValue));
            return err;
        }


        public static uint getISOSpeed(IntPtr theCamera, out uint ISO)
        {
            uint tmp = 0;
            if (debug == 1) Console.WriteLine("getting ISO Speed");
            uint err = EDSDK.EDS_ERR_OK;
            EDSDK.EdsDataType dataType;
            int dataSize;
            err = EDSDK.EdsGetPropertySize(theCamera, EDSDK.PropID_ISOSpeed, 0, out dataType, out dataSize);
            if (err == EDSDK.EDS_ERR_OK)
            {
                err = EDSDK.EdsGetPropertyData(theCamera, EDSDK.PropID_ISOSpeed, 0, out tmp);
            }
            else
            {
                Console.WriteLine("Could not get property");
                Console.WriteLine("Error: " + err);
            }
            ISO = tmp;
            return err;
        }

        public static uint setISOSpeed(IntPtr theCamera, uint ISOValue)
        {
            uint err;
            err = EDSDK.EdsSetPropertyData(theCamera, EDSDK.PropID_ISOSpeed, 0, sizeof(uint), new IntPtr(ISOValue));
            return err;
        }


        public static uint getExposure(IntPtr theCamera, out uint exp)
        {
            uint tmp = 0;
            if (debug == 1) Console.WriteLine("getting Exposure setting");
            uint err = EDSDK.EDS_ERR_OK;
            EDSDK.EdsDataType dataType;
            int dataSize;
            err = EDSDK.EdsGetPropertySize(theCamera, EDSDK.PropID_ExposureCompensation, 0, out dataType, out dataSize);
            if (err == EDSDK.EDS_ERR_OK)
            {
                err = EDSDK.EdsGetPropertyData(theCamera, EDSDK.PropID_ExposureCompensation, 0, out tmp);
            }
            else
            {
                Console.WriteLine("Could not get property");
                Console.WriteLine("Error: " + err);
            }
            exp = tmp;
            return err;
        }

        public static uint setExposure(IntPtr theCamera, uint expValue)
        {
            uint err;
            err = EDSDK.EdsSetPropertyData(theCamera, EDSDK.PropID_ExposureCompensation, 0, sizeof(uint), new IntPtr(expValue));
            return err;
        }

        public static uint setSaveLocation(IntPtr theCamera, EDSDK.EdsSaveTo expValue)
        {
            uint err;
            err = EDSDK.EdsSetPropertyData(theCamera, EDSDK.PropID_SaveTo, 0, sizeof(uint), new IntPtr((uint)expValue));
            return err;
        }

        public static uint downloadImage(IntPtr directoryItem)
        {
            // this is the code for transfering the image from the cameras buffer to 
            // the host computer.
            if (debug == 1) Console.WriteLine("downloading image");
            uint err = EDSDK.EDS_ERR_OK;
            // this is the output path we will save the file to. Notice the variable
            // we add to the filename so the images don't overwrite each other.
            string fileName = String.Format("c:\\temp\\TestImage" + scheduleID + "{0:D4}.jpg", imageCount++);
            IntPtr stream = IntPtr.Zero;

            // Get directory item information
            EDSDK.EdsDirectoryItemInfo dirItemInfo;
            err = EDSDK.EdsGetDirectoryItemInfo(directoryItem, out dirItemInfo);

            // Create file stream for transfer destination
            if (err == EDSDK.EDS_ERR_OK)
            {
                // let the SDK create the file access stream.
                // it seems to know what it is doing.
                err = EDSDK.EdsCreateFileStream(fileName,
                    EDSDK.EdsFileCreateDisposition.CreateAlways,
                    EDSDK.EdsAccess.ReadWrite, out stream);
            }

            // Download image
            if (err == EDSDK.EDS_ERR_OK)
            {
                err = EDSDK.EdsDownload(directoryItem, dirItemInfo.Size, stream);
            }

            // Issue notification that download is complete
            // this is needed to tell the camera it can remove the image from the buffer.
            if (err == EDSDK.EDS_ERR_OK)
            {
                err = EDSDK.EdsDownloadComplete(directoryItem);
            }
            else
            {
                // if we didn't download the image successfully, we need to
                // cancel the image download to remove the image from the buffer.
                Console.Out.WriteLine("There was an error downloading the image");
                Console.WriteLine(err);
                err = EDSDK.EdsDownloadCancel(directoryItem);
                Console.Out.WriteLine("download canceled");
                Console.WriteLine(err);

            }
            // Release stream
            if (stream != IntPtr.Zero)
            {
                EDSDK.EdsRelease(stream);
                stream = IntPtr.Zero;
            }
            runImageTransferFile(fileName, scheduleID);
            return err;
        }


    }
}
