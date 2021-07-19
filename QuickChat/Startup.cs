using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Security.Cryptography;
using CSL.Encryption;
using System.Security.Principal;

namespace QuickChat
{
    public class Startup
    {

        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseWebSockets()
                .Use((HttpContext context, Func<Task> next ) =>
                {
                    if(context.Request.Headers.ContainsKey("Security-Wrapper-User"))
                    {
                        context.User = new GenericPrincipal(new GenericIdentity(context.Request.Headers["Security-Wrapper-User"]), new string[] { });
                    }
                    return next();
                })
                .Use(async (HttpContext context, Func<Task> next) =>
                {
                    if(context.WebSockets.IsWebSocketRequest)
                    {
                        byte[] RandomName = new byte[3];
                        RandomNumberGenerator.Fill(RandomName);
                        string AltUserName = Passwords.Adjectives[RandomName[0]] + " " + Passwords.Adjectives[RandomName[1]] + " " + Passwords.Pokemon[RandomName[2]];
                        WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();
                        using (User user = new User(context.User.Identity.Name ?? AltUserName, socket))
                        {
                            await user.Send();
                            while (user.MsgHandler(await user.Receive())) ;
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = 200;
                        context.Response.ContentType = "text/html";
                        await context.Response.WriteAsync("<!DOCTYPE html>" +
                        "<html>" +
                        "<head>" +
                        "<title>QuickChat</title>" +
                        "<meta content='text/html;charset=utf-8' http-equiv='Content-Type'><meta content='utf-8'>" +
                        "<style>" +
                        "html,body{height: 100%; width: 100%; margin:0; padding:0; overflow:hidden;}" +
                        "body{background-color: hsl(200,100%,95%); font-family: sans-serif; display: flex; flex-flow: column;}" +
                        "#chatbox{margin:0; width:100%; padding: 8px; overflow-x:hidden; overflow-y:scroll; background: white; border-radius: 3px; flex: 1 1 auto; box-sizing: border-box;}" +
                        "input{margin:0; display:block; width: 100%; border: none;border-radius: 3px; padding-left: 8px; flex: 0 0 30px;}" +
                        "p{color:white;text-align:center;}" +
                        "</style>" +
                        "</head>" +
                        "<body>" +
                        "<div id='chatbox'>" +
                        "</div>" +
                        "<input id='msgbox' type='text' placeholder='Message'></input>" +
                        "<script>" +
                        "var msgbox=document.getElementById('msgbox'),chatbox=document.getElementById('chatbox');" +
                        "msgbox.focus();" +
                        "var loc=window.location,proto='wss://';" +
                        "'https:'!==loc.protocol&&(proto='ws://');" +
                        "var socket=new WebSocket(proto+loc.host+loc.pathname+loc.search);" +
                        "msgbox.onkeydown=function(o){13===o.keyCode&&(socket.send(msgbox.value),msgbox.value='')};" +
                        "var lastMessage=null;" +
                        "socket.onmessage=function(o){" +
                        "JSON.parse(o.data).forEach(function(o){" +
                        "if(null===lastMessage||lastMessage+1===o.id){" +
                        "var e=document.createElement('div');" +
                        "e.textContent=o.user + ':' + o.msg,chatbox.appendChild(e),lastMessage=o.id,chatbox.scrollTop=chatbox.scrollHeight" +
                        "}})};" +
                        "</script>" +
                        "</body>" +
                        "</html>");
                    }
                });
        }
    }
}
