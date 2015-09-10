using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;

namespace ATCMaster
{
    /// <summary>
    /// represents a route that an airplane takes between two airports
    /// </summary>
    [DataContract]
    public class AirRoute
    {
        [DataMember]
        public int airRouteID;

        /// <summary>
        /// ID of the originating airport
        /// </summary>
        [DataMember]
        public int fromAirportID;

        /// <summary>
        /// Destination airport ID
        /// </summary>
        [DataMember]
        public int toAirportID;

        /// <summary>
        /// Distance of the route in Kilometers
        /// </summary>
        [DataMember]
        public double distanceKM;

        public AirRoute(int airRouteID, int fromAirportID, int toAirportID, double distanceKM)
        {
            this.airRouteID = airRouteID;
            this.fromAirportID = fromAirportID;
            this.toAirportID = toAirportID;
            this.distanceKM = distanceKM;
        }
    }
}
