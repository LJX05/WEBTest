﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceDLL;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using WEB.Middleware;
using WEB.Models;

namespace WEB
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
           
        }
        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            string connection = @"Data Source=LAPTOP-IKHOQFAC;Initial Catalog=MyDemo;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
            services.AddDbContext<DataContext>(options => options.UseSqlServer(connection));
            //域生命周期的UserService
            services.AddScoped<IUserService, UserService>();
            services.AddDistributedMemoryCache();
            services.AddSession();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1); 
            //使用缓存服务
            services.AddMemoryCache();
           
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            #region 初始化缓存
            var webCache = app.ApplicationServices.GetService<IMemoryCache>();
            IList<WebSocket>  cacheWebSocket  =new List<WebSocket>();
            webCache.Set("webSocketCache", cacheWebSocket);
            #endregion

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }
            //app.UseFileServer();
            app.UseHttpsRedirection();
            app.UseMiddleware<RequesultIPMiddleware>();
            app.UseSession();

            #region UseWebSocketsOptionsAO

            var webSocketOptions = new WebSocketOptions()

            {
                KeepAliveInterval = TimeSpan.FromSeconds(120),

                ReceiveBufferSize = 4 * 1024
            };
            app.UseWebSockets(webSocketOptions);

            #endregion UseWebSocketsOptionsAO

            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/ws")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        var cache =app.ApplicationServices.GetService<IMemoryCache>();
                        var webSocketController = new Controllers.WebSocketController(cache);
                        await webSocketController.Echo(context, webSocket);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                    }
                }
                else
                {
                    await next();
                }
            });
            app.UseMvc();
        }

       
    }
}