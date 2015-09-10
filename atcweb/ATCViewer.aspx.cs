using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ATCMaster;
using System.ServiceModel;
using System.Configuration;
using System.Web.UI.HtmlControls;
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
public partial class _ATCViewer : System.Web.UI.Page, IATCMasterControllerCallback
{
    /// <summary>
    /// Connect to Master server and construct a table of the airports
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {
            //Connect to the Master server
            IATCMasterController m_ATCMaster;
            DuplexChannelFactory<IATCMasterController> IATCMasterFactory = ConnectToMaster(ConfigurationManager.AppSettings["address"]);
            m_ATCMaster = IATCMasterFactory.CreateChannel();

            //Get list of airports from the server
            List<Airport> m_airports = m_ATCMaster.GetAirportData();

            //close the connection
            IATCMasterFactory.Close();

            //Build the table
            Table table = new Table();
            TableRow headerRow = new TableRow();
            TableCell headerCell = new TableCell();
            headerCell.Text = "Airports";
            headerRow.Cells.Add(headerCell);
            foreach (Airport airport in m_airports)
            {
                TableRow tr = new TableRow();
                TableCell tc1 = new TableCell();
                tc1.Text = airport.name;
                tr.Cells.Add(tc1);
                TableCell tc2 = new TableCell();
                Button btn = new Button();
                btn.Text = "View " + airport.name;
                btn.ID = airport.airportID.ToString();
                btn.Click += new EventHandler(viewAirport);
                btn.Style["margin-right"] = "40px;";
                tc2.Controls.AddAt(0, btn);
                tr.Cells.Add(tc2);
                table.Rows.Add(tr);
            }

            //add the table to the main form
            frmMain.Controls.AddAt(0, table);

        }   //output error messages if exception is thrown
        catch (EndpointNotFoundException exception)
        {
            Context.Response.Write("Unable to connect!\n\n" + exception.Message);
        }
        catch (NullReferenceException exception)
        {
            Context.Response.Write("Server is not in a state to accept clients - check to see if all your slaves are connected!\n\n" + exception.Message);
        }
        catch (TimeoutException exception)
        {
            Context.Response.Write("Server timed out. Try to fix the problem then reconnect.\n\n" + exception.Message);
        }
        catch (CommunicationException exception)
        {
            Context.Response.Write("Communication Exception - try restarting the Master/Slaves\n\n" + exception.Message);
        }
        catch (Exception exception)
        {
            Context.Response.Write(exception.Message);
        }
    }

    /// <summary>
    /// Initiated when "15 Min Step" is pressed. Connects to Master and 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void nextStep(object sender, EventArgs e)
    {
        try
        {
            //connect to master
            IATCMasterController m_ATCMaster;
            DuplexChannelFactory<IATCMasterController> IATCMasterFactory = ConnectToMaster(ConfigurationManager.AppSettings["address"]);
            m_ATCMaster = IATCMasterFactory.CreateChannel();

            //call next step
            m_ATCMaster.NextStep();

            //close connection to server
            IATCMasterFactory.Close();
        }   //output error messages if exception is thrown
        catch (EndpointNotFoundException exception)
        {
            Context.Response.Write("Unable to connect!\n\n" + exception.Message);
        }
        catch (NullReferenceException exception)
        {
            Context.Response.Write("Server is not in a state to accept clients - check to see if all your slaves are connected!\n\n" + exception.Message);
        }
        catch (TimeoutException exception)
        {
            Context.Response.Write("Server timed out. Try to fix the problem then reconnect.\n\n" + exception.Message);
        }
        catch (CommunicationException exception)
        {
            Context.Response.Write("Communication Exception - try restarting the Master/Slaves\n\n" + exception.Message);
        }
        catch (Exception exception)
        {
            Context.Response.Write(exception.Message);
        }
    }

    /// <summary>
    /// Connect to master server, get the list of airports and display the list of airplanes in the airport whose button was click in a table
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void viewAirport(object sender, EventArgs e)
    {
        try
        {
            //connect to master server
            IATCMasterController m_ATCMaster;
            DuplexChannelFactory<IATCMasterController> IATCMasterFactory = ConnectToMaster(ConfigurationManager.AppSettings["address"]);
            m_ATCMaster = IATCMasterFactory.CreateChannel();

            //get the selected airport
            Airport airport = m_ATCMaster.GetAirportData().Find(x => x.airportID == Convert.ToInt32(((Button)sender).ID));

            //close connection to server
            IATCMasterFactory.Close();

            //construct list of all planes belonging to the airport
            List<Airplane> airplaneList = new List<Airplane>();
            airplaneList.AddRange(airport.planeDepartedList);
            airplaneList.AddRange(airport.planeQueuedList);
            airplaneList.AddRange(airport.planeLandedList);

            //construct the table
            Table table = new Table();
            TableRow headerRow = new TableRow();
            TableCell headerCell = new TableCell();
            headerCell.Text = airport.name;
            headerRow.Cells.Add(headerCell);
            table.Rows.Add(headerRow);
            TableRow descRow = new TableRow();
            TableCell dc1 = new TableCell();
            TableCell dc2 = new TableCell();
            TableCell dc3 = new TableCell();
            TableCell dc4 = new TableCell();
            dc1.Text = "Airplane ID";
            dc2.Text = "State";
            dc3.Text = "Type";
            dc4.Text = "Fuel";
            descRow.Cells.AddRange(new TableCell[] { dc1, dc2, dc3, dc4 });
            table.Rows.Add(descRow);
            foreach (Airplane airplane in airplaneList)
            {
                TableRow airplaneRow = new TableRow();
                TableCell c1 = new TableCell();
                TableCell c2 = new TableCell();
                TableCell c3 = new TableCell();
                TableCell c4 = new TableCell();
                c1.Text = airplane.airplaneID.ToString();
                c2.Text = airplane.state.ToString();
                c3.Text = airplane.type;
                c4.Text = airplane.fuel.ToString();
                airplaneRow.Cells.AddRange(new TableCell[] { c1, c2, c3, c4 });
                table.Rows.Add(airplaneRow);
            }

            //construct a button to go back to main screen
            Button btn = new Button();
            btn.Text = "Back";
            btn.ID = "Back";
            btn.Click += new EventHandler(Page_Load);
            btn.Style["margin-right"] = "40px;";

            //add table and button to main form
            frmMain.Controls.Clear();
            frmMain.Controls.Add(table);
            frmMain.Controls.Add(btn);
        }   //output error messages if exception is thrown
        catch (EndpointNotFoundException exception)
        {
            Context.Response.Write("Unable to connect!\n\n" + exception.Message);
        }
        catch (NullReferenceException exception)
        {
            Context.Response.Write("Server is not in a state to accept clients - check to see if all your slaves are connected!\n\n" + exception.Message);
        }
        catch (TimeoutException exception)
        {
            Context.Response.Write("Server timed out. Try to fix the problem then reconnect.\n\n" + exception.Message);
        }
        catch (CommunicationException exception)
        {
            Context.Response.Write("Communication Exception - try restarting the Master/Slaves\n\n" + exception.Message);
        }
        catch (Exception exception)
        {
            Context.Response.Write(exception.Message);
        }
    }

    /// <summary>
    /// Connects to master server and returns the ChannelFactory
    /// </summary>
    /// <param name="address"></param>
    /// <returns>ChannelFactory of the Master server</returns>
    private DuplexChannelFactory<IATCMasterController> ConnectToMaster(string address)
    {

        //connect to the server
        NetTcpBinding tcpBinding = new NetTcpBinding();
        tcpBinding.MaxReceivedMessageSize = System.Int32.MaxValue;
        tcpBinding.ReaderQuotas.MaxArrayLength = System.Int32.MaxValue;
        string sURL = "net.tcp://" + address;
        //return the channel factory
        return new DuplexChannelFactory<IATCMasterController>(this, tcpBinding, sURL);

    }
    public void OnNextStepComplete(IAsyncResult asyncResult)
    {
        throw new NotImplementedException("You called a function designed for a slave in the GUI");
    }
    public void AddPlane(Airplane plane)
    {
        throw new NotImplementedException("You called a function designed for a slave in the GUI");
    }
    public void Reset()
    {
        throw new NotImplementedException("You called a function designed for a slave in the GUI");
    }

    public Airport GetAirportData()
    {
        throw new NotImplementedException("You called a function designed for a slave in the GUI");
    }

    void IATCMasterControllerCallback.NextStep()
    {
        throw new NotImplementedException("You called a function designed for a slave in the GUI");
    }
}
