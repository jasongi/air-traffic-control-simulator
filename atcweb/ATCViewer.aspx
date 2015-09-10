<%@ Page Title="Home Page" Language="C#" AutoEventWireup="true" CodeFile="ATCViewer.aspx.cs"
    Inherits="_ATCViewer" %>

<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
    <title>Air Traffic Control Web Viewer</title>
</head>
<body>
    <form id="frmMain" runat="server">
    <input id="btnStep" type="button" value="15 Min Step" runat="server" onserverclick="nextStep" />
    </form>
    <div runat="server" id="divMsg">
    </div>
</body>
</html>
