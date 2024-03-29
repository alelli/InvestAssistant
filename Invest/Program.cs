var myAllowSpecificOrigins = "_myAllowSpecificOrigins";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

builder.Services.AddAuthentication("Cookies")
    .AddCookie(options => options.LoginPath = "/Account/Login");
builder.Services.AddAuthorization();


var app = builder.Build();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Stock}/{action=Shares}/{id?}");

app.UseCors(
    options => options
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader()
    );

app.UseCors(myAllowSpecificOrigins);


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.Run();
