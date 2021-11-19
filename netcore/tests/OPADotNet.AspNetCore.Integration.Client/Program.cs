using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services; ;

services.AddOpa(x => x.AddSync(sync => sync
    .UseLocal(local =>
    {
        var policies = Directory.GetFiles("Policies", "*.rego");
        foreach (var policy in policies)
        {
            local.AddPolicy(File.ReadAllText(policy));
        }
    })
));

services.AddAuthorization(opt =>
{
    opt.AddPolicy("testpolicy", x => x.RequireOpaPolicy("MyTestPolicy"));
});

var app = builder.Build();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("secured-get", GetSecuredTask);

[Authorize("testpolicy")]
async Task GetSecuredTask(HttpContext context)
{
    await Task.CompletedTask;
}

app.Run();