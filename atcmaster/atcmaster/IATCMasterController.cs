using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
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
namespace ATCMaster
{
    /// <summary>
    /// Interface for the Master Controller
    /// </summary>
    [ServiceContract(CallbackContract = typeof(IATCMasterControllerCallback))]
    public interface IATCMasterController
    {
        /// <summary>
        /// //Add callback to Master and get allocated airport
        /// </summary>
        /// <returns>Airport assigned to the slave</returns>
        [OperationContract]
        Airport InitialiseSlave();

        /// <summary>
        /// Step the entire simulation by 15 mins. Calls each of the slaves in paralell.
        /// </summary>
        /// <returns>List of the Airport objects at the end of the step</returns>
        [OperationContract]
        List<Airport> NextStep();

        /// <summary>
        /// Get the current state of the airports
        /// </summary>
        /// <returns>List of Airport objects in their current state</returns>
        [OperationContract]
        List<Airport> GetAirportData();

        /// <summary>
        /// Called by a slave to pass a Airplane from the slave to another slave
        /// </summary>
        /// <param name="airplane">The Airplane to pass</param>
        /// <param name="airportID"></param>
        [OperationContract]
        void PassPlane(Airplane airplane, int airportID);
    }

    /// <summary>
    /// Interface for a Client of the master controller (needed for callbacks)
    /// </summary>
    [ServiceContract]
    public interface IATCMasterControllerCallback
    {
        /// <summary>
        /// Completes a 15 minute step in the simulation
        /// </summary>
        [OperationContract]
        void NextStep();

        /// <summary>
        /// reset the slave due to an error ruining the state of the simulation
        /// </summary>
        [OperationContract(IsOneWay = true)]
        void Reset();

        /// <summary>
        /// Callback for when the GUI is calling NextStep asyncronously
        /// </summary>
        /// <param name="asyncResult">the result of the call</param>
        [OperationContract]
        void OnNextStepComplete(IAsyncResult asyncResult);

        /// <summary>
        /// Gets the airport object
        /// </summary>
        /// <returns>the up-to-date airport state that the slave reperesents</returns>
        [OperationContract]
        Airport GetAirportData();

        /// <summary>
        /// Add plane from another slave. This function is called when the plane comes within 300KM of the airport 
        /// </summary>
        /// <param name="airplane">The airplane to add</param>
        [OperationContract]
        void AddPlane(Airplane airplane);
    }
}
