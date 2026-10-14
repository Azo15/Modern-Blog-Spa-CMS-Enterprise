using FluentValidation;
using BusinessLayer.CQRS.Behaviors;
using BusinessLayer.Abstract;
using BusinessLayer.Concrete;
using DataAccesLayer.Abstract;
using DataAccesLayer.EntityFramework;
using DataAccesLayer.Repository;
using Microsoft.Extensions.DependencyInjection;
using BusinessLayer.Security;
using BusinessLayer.Jobs;
using Microsoft.AspNetCore.Authorization;

namespace BusinessLayer.Container
{
    public static class Additions
    {
        public static void ContainerDependencies(this IServiceCollection services)
        {
            services.AddLocalization(options => options.ResourcesPath = "Resources");
            services.AddScoped<IActiveMemberService, ActiveMemberService>();
            services.AddScoped<IAuthorizationHandler, ActiveMemberAuthorizationHandler>();
            services.AddScoped<IContactSubmissionService, ContactSubmissionService>();
            services.AddScoped<IContactEmailQueue, HangfireContactEmailQueue>();
            services.AddScoped<ContactEmailJob>();
            services.AddScoped<ContactEmailRecoveryJob>();
            services.AddScoped<TrashMaintenanceJob>();
            services.AddScoped<IReportService, ReportService>();
            services.AddScoped(typeof(IGenericDal<>), typeof(GenericRepository<>));
            services.AddScoped(typeof(IGenericService<>), typeof(GenericManager<>));

            services.AddScoped<IFileHelper, FileHelper>();

            services.AddScoped<IMailService, MailService>();

            services.AddScoped<IBlogDal, EFBlogDal>();
            services.AddScoped<IBlogService, BlogManager>();
            services.AddScoped<IBlogWorkflowService, BlogWorkflowService>();
            services.AddScoped<IHtmlContentSanitizer, HtmlContentSanitizer>();

            services.AddScoped<IAboutDal, EFAboutDal>();
            services.AddScoped<IAboutService, AboutManager>();
            services.AddScoped<IAboutCardDal, EFAboutCardDal>();
            services.AddScoped<IAboutCardService, AboutCardManager>();

            services.AddScoped<IContactDal, EFContactDal>();
            services.AddScoped<IContactService, ContactManager>();

            services.AddScoped<IContactFormDal, EFContactFormDal>();
            services.AddScoped<IContactFormService, ContactFormManager>();

            services.AddScoped<ICatagoryDal, EFCatagoryDal>();
            services.AddScoped<ICatagoryService, CatagoryManager>();

            services.AddScoped<IGalaryDal, EFGalaryDal>();
            services.AddScoped<IGalaryService, GalaryManager>();

            services.AddScoped<ISliderDal, EFSliderDal>();
            services.AddScoped<ISliderService, SliderManager>();

            services.AddScoped<ISiteTableDal, EFSiteTableDal>();
            services.AddScoped<ISiteTableService, SiteTableManager>();

            services.AddScoped<IServiceDal, EFServiceDal>();
            services.AddScoped<IServiceService, ServiceManager>();

            // 📌 Distributed Cache Servisi
            services.AddSingleton<CacheFallbackStore>();
            services.AddSingleton<ICacheService, CacheManager>();
            services.AddScoped<IProfanityFilterService, ProfanityFilterService>();
            services.AddValidatorsFromAssembly(typeof(Additions).Assembly);

            // 📌 MediatR CQRS Entegrasyonu
            services.AddMediatR(cfg => { cfg.RegisterServicesFromAssembly(typeof(Additions).Assembly); cfg.AddOpenBehavior(typeof(ValidationBehavior<,>)); });
        }
    }
}
