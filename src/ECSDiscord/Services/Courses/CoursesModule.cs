using System;
using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ECSDiscord.Services.Courses;

public class CoursesModule : Module
{
    private readonly IConfiguration _configuration;

    public CoursesModule(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<CourseService>()
            .AsSelf()
            .As<IHostedService>()
            .SingleInstance();
    }
}