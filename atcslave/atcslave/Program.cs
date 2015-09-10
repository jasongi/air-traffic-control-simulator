using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
/*  
    Copyright (C) 2015 Jason Giancono (jasongiancono@gmail.com)

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
*/
namespace ATCSlaveController
{
    class Program
    {
        static void Main(string[] args)
        {
            Start();
        }

        static private void Start()
        {
            ATCSlaveController slave;
            try
            {
                slave = new ATCSlaveController();

                System.Console.WriteLine("Press Enter to exit");
                //keep server running until enter is pressed
                System.Console.ReadLine();
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e.Message);
                Start();
            }
        }
    }
}
