﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;
using Rcv.LabelTool.Backend.Configuration;
using Rcv.LabelTool.Backend.Statics;
using Rcv.LabelTool.Logic.Utilities;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;

namespace Rcv.LabelTool.Backend
{
    public class Startup
    {
        #region constants

        /// <summary>
        /// Name of CORS policy to use.
        /// </summary>
        private const string CORS_POLICY_NAME = "AllowAllOrigins";

        /// <summary>
        /// Application version.
        /// </summary>
        private const string APPLICATION_VERSION = "v1";

        /// <summary>
        /// Name of application.
        /// </summary>
        private const string APPLICATION_NAME = "RCV Labeltool LITE Backend";

        #endregion

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            OnPremiseConfiguration onPremiseConfiguration = Configuration.GetOnPremiseConfiguration();
            services.BindOnPremiseFileContainer(onPremiseConfiguration);

            ImagePathUtil.ValidImageExtensions = new List<string>() { ".png", ".jpg", ".jpeg" };

            services.BindRepositories();
            services.BindServices();

            // accessor to use httpcontext inside of methods
            services.AddHttpContextAccessor();

            services.AddMvc(options =>
            {
                options.CacheProfiles.Add(CacheProfileNames.Default,
                    new CacheProfile()
                    {
                        Duration = 60
                    });
                options.CacheProfiles.Add(CacheProfileNames.Never,
                    new CacheProfile()
                    {
                        Location = ResponseCacheLocation.None,
                        NoStore = true
                    });
            }).SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
                .AddJsonOptions(options => options.SerializerSettings.ContractResolver = new DefaultContractResolver());

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc(APPLICATION_VERSION, new Swashbuckle.AspNetCore.Swagger.Info()
                {
                    Title = APPLICATION_NAME,
                    Version = APPLICATION_VERSION,
                    Description = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString()
                });

                //c.OperationFilter<FormFileOperationFilter>();

                // Set the comments path for the Swagger JSON and UI.                
                string xmlPath = Path.Combine(AppContext.BaseDirectory, "Documentation", "Rcv.LabelTool.Backend.xml");
                c.IncludeXmlComments(xmlPath);

                c.DescribeAllEnumsAsStrings();
            });

            // add CORS rules
            services.AddCors(options =>
            {
                options.AddPolicy(CORS_POLICY_NAME, builder =>
                {
                    builder
                        .AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint($"/swagger/{APPLICATION_VERSION}/swagger.json", $"{APPLICATION_NAME} {APPLICATION_VERSION}");
            });

            Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(Configuration).CreateLogger();
            Log.Information($"Start logging for {APPLICATION_NAME}");

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors(CORS_POLICY_NAME);
            app.UseMvc();
        }
    }
}
