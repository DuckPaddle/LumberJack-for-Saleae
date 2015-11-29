//#define DEMO_SETTING
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;
using Saleae.SocketApi;

namespace LumberJackConsole
{
    class LumberJack
    {
        static void Main(string[] args)
        {
            LumberJack app = new LumberJack();
            app.Run();
        }


        SaleaeClient Client;


        public void Run()
        {
            // Data saved to filenames
            String s_salted_filename, s_complete_filename;
            // Just a subdirectory off the root
            String s_dest_directory = "c:/traces/";
            // Used in creation of "salted" filename, prevents name clashing
            UInt32 i_Seq = 0;
            DateTime dt_current;
            // Simple Performance Measurements
            TimeSpan ts_temp0, ts_temp1;

            //Set this variable to have all text socket commands printed to the console.
            SaleaeClient.PrintCommandsToConsole = false;

            //Make sure to enable the socket server in the Logic software preferences, and make sure that it is running!

            //This demo is designed to show some common socket commands, and interacts best with either the simulation or real Logic 8, Logic Pro 8, or Logic Pro 16.

            //lets run a quick demo!
            Console.WriteLine("Logic Socket API demonstation application.\n");

            Console.WriteLine("enter host IP address, or press enter for localhost");
            String host = Console.ReadLine();
            if (host.Length == 0)
                host = "127.0.0.1";
            Console.WriteLine("enter host port, or press enter for default ( 10429 )");
            String port_str = Console.ReadLine();
            if (port_str.Length == 0)
                port_str = "10429";
            int port = int.Parse(port_str);

            Console.WriteLine("Connecting...");
            try
            {
                Client = new SaleaeClient(host, port);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while connecting: " + ex.Message);
                Console.ReadLine();
                System.Environment.Exit(3);
            }
            StringHelper.WriteLine("Connected");
            Console.WriteLine("");

            Console.WriteLine("enter full directory path, or press enter for " + s_dest_directory);
            { // Gather User Directory Choice 
                String lcl_dest = Console.ReadLine();
                if (lcl_dest.Length > 0) { s_dest_directory = lcl_dest; }
            }

            if (host.Equals("127.0.0.1"))
            { // logic to test and make directory on local machine
                if (!Directory.Exists(s_dest_directory))
                {
                    Console.WriteLine("Directory " + s_dest_directory + " does not exist, attempting to make it.");
                    Directory.CreateDirectory(s_dest_directory);
                    if (!Directory.Exists(s_dest_directory))
                    {
                        Console.WriteLine("Could not create " + s_dest_directory + ", giving up.");
                        System.Environment.Exit(3);

                    }
                }

            }
            else
            { // Warn User about remote machine
                Console.WriteLine("Make sure the directory " + s_dest_directory + " exists on target machine.");

            }

            var devices = Client.GetConnectedDevices();
            var active_device = devices.Single(x => x.IsActive == true);

            Console.WriteLine("currently availible devices:");
            devices.ToList().ForEach(x => Console.WriteLine(x.Name));

            Console.WriteLine("currently active device: " + active_device.Name);
            Console.WriteLine("");
#if DEMO_SETTING
            Console.WriteLine("Press Enter to Continue");
            Console.ReadLine();
#endif
            // Get hardware list
            var analyzers = Client.GetAnalyzers();

            if (analyzers.Any())
            {
                Console.WriteLine("Current analyzers:");
                analyzers.ToList().ForEach(x => Console.WriteLine(x.AnalyzerType));
                Console.WriteLine("");
#if DEMO_SETTING
                Console.WriteLine("Press Enter to Start Capture");
                Console.ReadLine();
#endif
            }

            // This code sets the Saleae configurations
            // I prefer to set my own..
#if DEMO_SETTING
            if (active_device.DeviceType == DeviceType.Logic8 || active_device.DeviceType == DeviceType.LogicPro8 || active_device.DeviceType == DeviceType.LogicPro16)
            {
                Console.WriteLine("changing active channels");
                //Client.SetActiveChannels( new int[] { 2, 5, 6, 7 }, new int[] { 0, 1 } );
                Client.SetActiveChannels(new int[] { 0, 1 }, new int[] { });
                Console.WriteLine("");
                Console.WriteLine("Press Enter to Continue");
                Console.ReadLine();

                var possible_sample_rates = Client.GetAvailableSampleRates();

                /*if( possible_sample_rates.Any( x => x.AnalogSampleRate == 125000 ) )
				{
					Console.WriteLine( "Changing sample rate" );
					Client.SetSampleRate( possible_sample_rates.First( x => x.AnalogSampleRate == 125000 ) );
					Console.WriteLine( "" );
					Console.WriteLine( "Press Enter to Continue" );
					Console.ReadLine();
				}
                */
                // set digital sample rate to 2500000
                if (possible_sample_rates.Any(x => x.DigitalSampleRate == 2500000))
                {
                    Console.WriteLine("Changing sample rate");
                    Client.SetSampleRate(possible_sample_rates.First(x => x.DigitalSampleRate == 2500000));
                    Console.WriteLine("");
                    Console.WriteLine("Press Enter to Continue");
                    Console.ReadLine();
                }


                //set trigger. There are 4 digital channels. all need to be specified.
                Console.WriteLine("setting trigger");
                //Client.SetTrigger(new Trigger[] { Trigger.None, Trigger.PositivePulse, Trigger.Low, Trigger.High }, 1E-6, 5E-3);
                Client.SetTrigger(new Trigger[] { Trigger.None, Trigger.FallingEdge });
                Console.WriteLine("");
                Console.WriteLine("Press Enter to Continue");
                Console.ReadLine();


            }
            else
            {
                Console.WriteLine("to see more cool features demoed by this example, please switch to a Logic 8, Logic Pro 8, or Logic Pro 16. Physical or simulation");
            }

            Console.WriteLine("setting capture time");
            Client.SetCaptureSeconds(0.25);
            Console.WriteLine("");
            Console.WriteLine("Press Enter to Continue");
            Console.ReadLine();

            Console.WriteLine("starting capture");

            //Client.Capture();
            //Console.WriteLine( "" );
            //Console.WriteLine( "Press Enter to Exit" );
            //Console.ReadLine();
#endif
            // New Stuff
            Console.WriteLine("Capture starting");
            // Filename sequence number
            i_Seq = 0;
            while (true)
            {
                // loop here forever (or canceled from Saleae)
                try
                { // a little more elegant exit
                    Client.Capture();
                }
                catch (Saleae.SocketApi.SaleaeSocketApiException)
                {
                    // calling this a normal exit for now..
                    Console.WriteLine("End of Session");
                    System.Environment.Exit(1);
                }



                dt_current = DateTime.Now;
                Console.WriteLine("Captured, Processing Started");

                // Build a file name of year_JulianDay_time_sequenceNumber
                // That funny parce statement sets the Julian Day count to start on January first of the current year.
                s_salted_filename = dt_current.ToString("yyyy") + "_" + // Year
                    JulianDate(new DateTime(Int32.Parse(dt_current.ToString("yyyy")), 1, 1)).ToString() + "_" + // Days since January first
                    dt_current.ToString("HHmmss") + //hours minutes seconds - 24 hour local time
                    "_sq_" + i_Seq.ToString(); // sequence number to prevent a potential name conflict

                // Yep this could be combined to save a variable, but s_salted_filename is already too complex
                s_complete_filename = s_dest_directory + s_salted_filename + ".logicdata";
                // Test for done Processing
                while (!Client.IsProcessingComplete())
                {
                    // Only check 4 times a second
                    System.Threading.Thread.Sleep(250);
                    // Let them know it hasn't gotten stuck
                    Console.Write(".");
                }
                // Calculate and display elapsed time
                ts_temp0 = DateTime.Now.Subtract(dt_current);
                Console.WriteLine("");
                Console.WriteLine("Saleae Processing took " + ts_temp0.ToString("c"));
                // show destination 
                Console.WriteLine("Writing File " + s_complete_filename);
                // tell Saleae to write the file out
                Client.SaveToFile(s_complete_filename);
                // tell Saleae to close the tabs
                Client.CloseAllTabs();
                // Calculate and display elapsed time
                ts_temp1 = DateTime.Now.Subtract(dt_current);
                Console.WriteLine("Total Processing and File Save took " + ts_temp1.ToString("c"));
                // bump the sequence
                i_Seq++;
            }



        }

        public static int JulianDate(DateTime baseDate)
        {
            TimeSpan j = DateTime.Now - baseDate;
            return j.Days;

        }
    }
}
