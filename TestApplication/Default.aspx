<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="TestApplication._Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>JavaScript JWT Test</title>
</head>
<body>
    <form id="form1" runat="server">
    <div>

        <a href="https://stackoverflow.com/questions/38552003/how-to-decode-jwt-token-in-javascript" target="_blank">
            https://stackoverflow.com/questions/38552003/how-to-decode-jwt-token-in-javascript
        </a>
        <br /><br />

        <code>
            function parseJwt (token) <br />
            {<br />
                var base64Url = token.split('.')[1];<br />
                var base64 = base64Url.replace('-', '+').replace('_', '/');<br />
                return JSON.parse(window.atob(base64));<br />
            };<br />
        </code>
        <br /><br /><br />

        Unfortunately this doesn't seem to work with unicode text.<br />
        You can use jwt-decode, so then you could write:
        <br /><br />
        <code>
var token = 'eyJ0eXAiO.../// jwt token';<br />

var decoded = jwt_decode(token);<br />
console.log(decoded);<br />
// {exp: 10012016 name: john doe, scope:['admin']} <br />
        </code>

    </div>
         <br /><br />
        <h1>
            <a href="JOSE/index.html">See JOSE documentation</a>
        </h1>
        
        <br />
        <a href="CookieWriter.ashx" target="_blank">Write Auth-Cookie</a><br />
        <a href="CookieReader.ashx" target="_blank">Read Auth-Cookie</a><br />
        <a href="CookieDeleter.ashx" target="_blank">Delete Auth-Cookie</a><br />

    </form>


</body>
</html>
