using Microsoft.Extensions.DependencyInjection;
using Our.Umbraco.TheDashboard.Controllers.OpenApi;
using Our.Umbraco.TheDashboard.Counters.Collections;
using Our.Umbraco.TheDashboard.Counters.Implement;
using Our.Umbraco.TheDashboard.Extensions;
using Our.Umbraco.TheDashboard.Services;
using Umbraco.Cms.Api.Common.OpenApi;
using Umbraco.Cms.Api.Management.OpenApi;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Our.Umbraco.TheDashboard;

public class TheDashboardComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {

        builder.Services.AddTransient<ITheDashboardService, TheDashboardService>();

        builder.WithCollectionBuilder<DashboardCountersCollectionBuilder>()
            .Append<ContentTotalContentItemsDashboardCounter>()
            .Append<ContentInRecycleBinDashboardCounter>()
            .Append<MembersTotalDashboardCounter>();

        // Just using this to make sure that it works and are used in the package
        builder.TheDashboardCounters().Append<MembersNewLastWeekDashboardCounter>();

#if DEBUG
        builder.AddBackOfficeOpenApiDocument(
            TheDashboardApiConfiguration.ApiName,
            document => document
                .WithTitle(TheDashboardApiConfiguration.ApiTitle)
                .WithBackOfficeAuthentication()
                .ConfigureOpenApiOptions(options => options.AddOperationTransformer((operation, context, _) =>
                {
                    // Extracts the action name to use as operation id.
                    var routeValues = context.Description.ActionDescriptor.RouteValues;

                    if (routeValues.TryGetValue("action", out var actionName) &&
                        !string.IsNullOrWhiteSpace(actionName))
                    {
                        operation.OperationId = actionName;
                    }

                    return Task.CompletedTask;
                }))
        );
#endif

    }
}


