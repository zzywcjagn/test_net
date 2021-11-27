using IServer;
using IServer.IPageServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using NETJDC.Extensions;
using Server;
using Server.OpenCVServer;
using Server.PageServer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Systems;

namespace NETJDC
{
    public class Startup
    {
        public IServiceProvider serviceProvider { get; set; }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public  void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
           
            services.AddSingleton<MainConfig>();
            services.AddSingleton<OpenCVServer>();
            services.AddSingleton<IPageServer, PageServer>();
            services.AddControllers(options =>
            {
                options.Filters.Add(new CustomerExceptionFilter());
            });
            // ��������������Դ����
            services.AddCors(options => options.AddPolicy("CorsPolicy",
            builder =>
            {
                builder.SetPreflightMaxAge(TimeSpan.FromSeconds(1800L));
                builder.AllowAnyMethod()
                    .AllowAnyHeader()
                    .SetIsOriginAllowed(_ => true) // =AllowAnyOrigin()
                    .AllowCredentials();
            }));
           
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            serviceProvider = app.ApplicationServices;
            app.UseRouting();

            app.UseCors("CorsPolicy");
            var options = new DefaultFilesOptions();
            options.DefaultFileNames.Clear();
            options.DefaultFileNames.Add("index.html");
            app.UseDefaultFiles(options);
            app.UseStaticFiles();
            app.Use(async (HttpContext context, Func<Task> next) => {
                if (context.Request.Path.ToString() == "/login")
                {

                    context.Response.Redirect("/");
                    return;
                }
                await next();
            });

         app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
            IPageServer testServer = serviceProvider.GetService<IPageServer>();
            testServer.info();
            Console.WriteLine("��ʼ����Ŀ");
            Console.WriteLine("-------------------------------------------------------------------------------------");
            Console.WriteLine("��һ�����л�����Chromium,����ֻ�Ǽ���Ƿ�����");
            Console.WriteLine("�����....");
            var aa = testServer.BrowserInit().Result;
            Console.WriteLine(aa);
            Console.WriteLine("���������Լ� ");
            Console.WriteLine("��������������û��߼�������Ƿ����");
            var dd = testServer.CCHECK().Result;
            Console.WriteLine("���ɹ�");
            Console.WriteLine("NETJDC started");

        }

    }
}
