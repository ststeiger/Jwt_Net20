<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="TestApplication._Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <a href="https://stackoverflow.com/questions/38552003/how-to-decode-jwt-token-in-javascript">
            https://stackoverflow.com/questions/38552003/how-to-decode-jwt-token-in-javascript
        </a>
        <code>
            function parseJwt (token) 
            {
                var base64Url = token.split('.')[1];
                var base64 = base64Url.replace('-', '+').replace('_', '/');
                return JSON.parse(window.atob(base64));
            };
        </code>


        Unfortunately this doesn't seem to work with unicode text.

        You can use jwt-decode, so then you could write:
        <code>
var token = 'eyJ0eXAiO.../// jwt token';

var decoded = jwt_decode(token);
console.log(decoded);
// {exp: 10012016 name: john doe, scope:['admin']} 
        </code>

    </div>
    </form>
</body>
</html>
