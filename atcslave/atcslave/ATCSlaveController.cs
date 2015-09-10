using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Text;
using System.ServiceModel;
using ATCMaster;
using System.Runtime.CompilerServices;
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
    /// <summary>
    /// Controller for the slave server in the Business Tier
    /// </summary>
    [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
    class ATCSlaveController : IATCMasterControllerCallback
    {
        /// <summary>
        /// RPC interface to the server
        /// </summary>
        private IATCMasterController m_ATCMaster;

        /// <summary>
        /// The airport that the slave owns
        /// </summary>
        private Airport m_Airport;

        /// <summary>
        /// internal clock to keep track of the time (used to check against plane objects as they may be on a different time if they have just been passed from a different slave
        /// </summary>
        private int m_SimTime;

        /// <summary>
        /// Address of the master server it connects to, set to default but can be changed on startup
        /// </summary>
        private string address = "localhost:50002/ATCMaster";

        /// <summary>
        /// Connects to the Master server and initialises the slave, getting back the allocated airport
        /// </summary>
        private void Initialise()
        {
            try
            {
                //connect to the server
                DuplexChannelFactory<IATCMasterController> IATCMasterFactory;
                NetTcpBinding tcpBinding = new NetTcpBinding();
                tcpBinding.MaxReceivedMessageSize = System.Int32.MaxValue;
                tcpBinding.ReaderQuotas.MaxArrayLength = System.Int32.MaxValue;
                string sURL = "net.tcp://" + address;
                IATCMasterFactory = new DuplexChannelFactory<IATCMasterController>(this, tcpBinding, sURL);
                m_ATCMaster = IATCMasterFactory.CreateChannel();
                //add callback to Master and get allocated airport
                m_Airport = m_ATCMaster.InitialiseSlave();

                //set simulation time to 0
                m_SimTime = 0;

                //display the allocated airport (for debug purposes)
                System.Console.WriteLine(m_Airport.name);
            }
            catch (EndpointNotFoundException exception)
            {
                //if you get one of these four exceptions, print the error and try again, asking for a new address
                System.Console.WriteLine("Server connection failed\n\n" + exception.Message + "\n\nInput Server Address (leave blank for localhost:50002/ATCMaster)");
                address = System.Console.ReadLine();
                if (address == "")
                {
                    address = "localhost:50002/ATCMaster";
                }
                Initialise();
            }
            catch (CommunicationObjectFaultedException exception)
            {
                System.Console.WriteLine("Server connection failed\n\n" + exception.Message + "\n\nInput Server Address (leave blank for localhost:50002/ATCMaster)");
                address = System.Console.ReadLine();
                if (address == "")
                {
                    address = "localhost:50002/ATCMaster";
                }
                Initialise();
            }
            catch (CommunicationObjectAbortedException exception)
            {
                System.Console.WriteLine("Server connection failed\n\n" + exception.Message + "\n\nInput Server Address (leave blank for localhost:50002/ATCMaster)");
                address = System.Console.ReadLine();
                if (address == "")
                {
                    address = "localhost:50002/ATCMaster";
                }
                Initialise();
            }
            catch (NullReferenceException exception)
            {
                //usually this exception happens when there is four slaves connected and the master rejects the InitialiseSlave call
                System.Console.WriteLine("Server connection failed - Maybe there are already the max slaves connected?\n\n" + exception.Message + "\n\nInput Server Address (leave blank for localhost:50002/ATCMaster)");
                address = System.Console.ReadLine();
                if (address == "")
                {
                    address = "localhost:50002/ATCMaster";
                }
                Initialise();
            }
        }
        /// <summary>
        /// Constructor: prompts for server address then initialises the connection
        /// </summary>
        public ATCSlaveController()
        {
            System.Console.WriteLine("Input Server Address (leave blank for localhost:50002/ATCMaster)");
            address = System.Console.ReadLine();
            if (address == "")
            {
                address = "localhost:50002/ATCMaster";
            }
            Initialise();
        }
        /// <summary>
        /// Gets the airport object
        /// </summary>
        /// <returns>the up-to-date airport state that the slave reperesents</returns>
        public Airport GetAirportData()
        {
            return m_Airport;
        }
        /// <summary>
        /// Completes a 15 minute step in the simulation
        /// </summary>
        public void NextStep()
        {
            //lander is the airport we (might) select to land in this step
            Airplane lander = null;

            //check to see if any plane is ready to take off, if so, take off and switch the lists it belongs to
            if (m_Airport.planeLandedList.Count > 0 && m_Airport.planeLandedList.Peek().timeLanded >= 60 && m_Airport.planeLandedList.Peek().simTime == m_SimTime)
            {
                Airplane airplane = m_Airport.planeLandedList.Dequeue();
                m_Airport.planeDepartedList.Add(airplane);
                TakeOff(airplane);
            }

            //runs the 15 minute step on each of the landed plane (basically just incrementing the timelanded counter)
            foreach (Airplane airplane in m_Airport.planeLandedList)
            {
                PlaneLandedStep(airplane);
            }

            //see if there are planes that can land this turn
            if (m_Airport.planeQueuedList.FindAll(x => x.simTime == m_SimTime && x.state != PlaneState.Crashed).Count > 0)
            {
                //iterate through each plane that has not had it's turn this step and is not crashed and check to see if it can land and if it has lower fuel than the current selected plane
                foreach (Airplane airplane in m_Airport.planeQueuedList.FindAll(x => x.simTime == m_SimTime && x.state != PlaneState.Crashed))
                {

                    //if there is no selected plane to land, just see if this one can land and if so then make it the landing plane (for now)
                    if (lander == null &&
                        ((airplane.currentAirRoute.distanceKM - airplane.distanceAlongRoute) - (airplane.cruisingKPH * 0.25) <= 0.0) &&
                        (airplane.fuel - ((airplane.fuelConsPerHour / airplane.cruisingKPH) * (airplane.currentAirRoute.distanceKM - airplane.distanceAlongRoute)) >= 0.00))
                    {
                        lander = airplane;
                    }

                    //if there is a selected plane, see if the current plane can land and has less fuel, if that is true then set the current plane as the landing plane
                    else if (lander != null && ((airplane.fuel < lander.fuel) &&
                        ((airplane.currentAirRoute.distanceKM - airplane.distanceAlongRoute) - (airplane.cruisingKPH * 0.25) <= 0.0) &&
                        (airplane.fuel - ((airplane.fuelConsPerHour / airplane.cruisingKPH) * (airplane.currentAirRoute.distanceKM - airplane.distanceAlongRoute)) >= 0.00)))
                    {
                        lander = airplane;
                    }
                }

                //if we have a plane that can land, change it's state to landed, update fields and change the list it belongs to
                if (lander != null)
                {
                    lander.state = PlaneState.Landed;
                    lander.fuel = lander.fuel - (lander.fuelConsPerHour / lander.cruisingKPH) * (lander.currentAirRoute.distanceKM - lander.distanceAlongRoute);
                    lander.timeLanded = 0;
                    lander.distanceAlongRoute = 0;
                    lander.currentAirRouteID = -1;
                    lander.currentAirportID = lander.currentAirRoute.toAirportID;
                    m_Airport.planeQueuedList.Remove(lander);
                    m_Airport.planeLandedList.Enqueue(lander);
                    lander.simTime = lander.simTime + 15;
                }
            }

            //runs the 15 minute step on each of the queued (incoming) planes, updating ths state
            foreach (Airplane airplane in m_Airport.planeQueuedList.FindAll(x => x.simTime == m_SimTime))
            {
                PlaneQueuedStep(airplane);
            }

            //runs the 15 minute step on each of the departed planes, updating ths state and passing to other slave if they pass the distance threshold
            foreach (Airplane airplane in m_Airport.planeDepartedList.FindAll(x => x.simTime == m_SimTime))
            {
                PlaneDepartedStep(airplane);
            }

            //remove all the planes which have been transferred to another slave (cannot do this in above loop because it is a foreach)
            m_Airport.planeDepartedList.RemoveAll(x => x.currentAirRoute.distanceKM - x.distanceAlongRoute <= 300.00);

            //all the planes should now be incremented, so increment the timer
            m_SimTime = m_SimTime + 15;

        }
        /// <summary>
        /// Add plane from another slave. This function is called when the plane comes within 300KM of the airport 
        /// </summary>
        /// <param name="airplane">The airplane to add</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void AddPlane(Airplane airplane)
        {
            //add a reference to the plane's route
            airplane.currentAirRoute = m_Airport.IncomingRouteList.Find(x => x.airRouteID == airplane.currentAirRouteID);

            //check if the plane has entered circling, if so then change the state
            if (airplane.currentAirRoute.distanceKM - airplane.distanceAlongRoute < 0.0)
            {
                airplane.distanceAlongRoute = airplane.currentAirRoute.distanceKM;
                airplane.state = PlaneState.Circling;
            }

            //check if the plane has run out of fuel and crashed, if so then change the state
            if (airplane.fuel < 0)
            {
                airplane.fuel = 0;
                airplane.state = PlaneState.Crashed;
            }

            //add the plane to the queued (incoming) plane list
            m_Airport.planeQueuedList.Add(airplane);
        }

        /// <summary>
        /// Step 15 mins forward for a plane that has departed (outgoing)
        /// </summary>
        /// <param name="airplane">The plane to step forward</param>
        private void PlaneDepartedStep(Airplane airplane)
        {
            //add 15 minutes to the simulation clock of the plane
            airplane.simTime = airplane.simTime + 15;

            //update the distance travelled and remaining fuel
            airplane.distanceAlongRoute = airplane.distanceAlongRoute + airplane.cruisingKPH * 0.25;
            airplane.fuel = airplane.fuel - airplane.fuelConsPerHour * 0.25;

            //check if we need to transfer to another slave/airport
            if (airplane.currentAirRoute.distanceKM - airplane.distanceAlongRoute <= 300.00)
            {
                airplane.state = PlaneState.Entering;
                m_ATCMaster.PassPlane(airplane, airplane.currentAirRoute.toAirportID);
            }
        }

        /// <summary>
        /// Step 15 mins forward for a plane that has landed
        /// </summary>
        /// <param name="airplane"></param>
        private void PlaneLandedStep(Airplane airplane)
        {
            //check to make sure plane isn't ahead, and the increment timeLanded
            if (airplane.simTime == m_SimTime)
            {
                airplane.timeLanded = airplane.timeLanded + 15;
                airplane.simTime = airplane.simTime + 15;
            }
        }

        /// <summary>
        /// Updates a plane's state to take off and moves it into the departed queue
        /// </summary>
        /// <param name="airplane">the airplane that is taking off</param>
        private void TakeOff(Airplane airplane)
        {
            //pick the next route, update the airplane object and switch it into the departed queue
            airplane.simTime = airplane.simTime + 15;
            AirRoute route = m_Airport.DepartingRouteList.Peek();
            m_Airport.DepartingRouteList.Enqueue(m_Airport.DepartingRouteList.Dequeue());
            airplane.fuel = (route.distanceKM / airplane.cruisingKPH) * airplane.fuelConsPerHour * 1.15;
            airplane.distanceAlongRoute = 0;
            airplane.timeLanded = -1;
            airplane.currentAirRoute = route;
            airplane.currentAirRouteID = route.airRouteID;
            airplane.state = PlaneState.InTransit;
            airplane.currentAirportID = -1;
        }

        /// <summary>
        /// Step 15 mins forward for a plane that is queued (incoming)
        /// </summary>
        /// <param name="airplane"></param>
        private void PlaneQueuedStep(Airplane airplane)
        {
            airplane.simTime = airplane.simTime + 15;

            //don't do anything is the plane is crashed
            if (airplane.state != PlaneState.Crashed)
            {
                //update the distance travelled
                airplane.distanceAlongRoute = airplane.distanceAlongRoute + airplane.cruisingKPH * 0.25;

                //check if you are circling
                if (airplane.currentAirRoute.distanceKM - airplane.distanceAlongRoute < 0.0)
                {
                    airplane.distanceAlongRoute = airplane.currentAirRoute.distanceKM;
                    airplane.state = PlaneState.Circling;
                }

                //update the fuel
                airplane.fuel = airplane.fuel - airplane.fuelConsPerHour * 0.25;

                //check if you are out of fuel (therefore crashed)
                if (airplane.fuel < 0)
                {
                    airplane.fuel = 0;
                    airplane.state = PlaneState.Crashed;
                }
            }
        }

        /// <summary>
        /// reset the slave due to an error ruining the state of the simulation
        /// </summary>
        public void Reset()
        {
            System.Console.WriteLine("Master has called a reset. Waiting 20 seconds");
            Thread.Sleep(40000);
            Initialise();
        }

        /// <summary>
        /// Do not call: Not implemented
        /// </summary>
        /// <param name="asyncResult"></param>
        public void OnNextStepComplete(IAsyncResult asyncResult)
        {
            throw new NotImplementedException("You called a function designed for a GUI in the slave");
        }

    }
}
