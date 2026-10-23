var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();


builder.Services.AddOpenApi();

string dbConnectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    + "Database=mysmartwardrobe;";

builder.Services.AddSingleton(
    new DAL.UserDB(dbConnectionString)
);
var app = builder.Build();

// שליפת מחרוזת החיבור מ-appsettings.json / User Secrets
// והפעלת אתחול בסיס הנתונים של PawPals
string serverConnectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found."
    );

// יצירת בסיס הנתונים, הטבלאות ונתוני ברירת המחדל
await DAL.DatabaseInitializer.InitializeAsync(
    serverConnectionString
);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();