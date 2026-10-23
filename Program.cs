using InventarioApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using InventarioApi.Services;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);//Constructor de aplicacion, permite configurar la aplicacion

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));//Se configura SQLite  como motor de base de datos y busca el string de conexion en Appsettings.json

builder.Services.AddControllers();//Se añade los controladores MVC de tipo API
// Configuración de autenticación JWT
// Autenticacion != Autorizacion
var jwtSection = builder.Configuration.GetSection("Jwt"); //lee la seccion Jwt de appsettings
var jwtKey = jwtSection.GetValue<string>("Key");          //
var issuer = jwtSection.GetValue<string>("Issuer");       //
var audience = jwtSection.GetValue<string>("Audience");   //

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;//Establecemos Jwt Bearer como metodo de autenticacion
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;   //responde cuando se envia una solicitud sin un token valido
})
    .AddJwtBearer(options =>//Registro y configuracion de autenticacion JWT Bearer 
    {       
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,      //Se debe validar el emisor del token
            ValidateAudience = true,    //Se debe validar el destinatario del token
            ValidateIssuerSigningKey = true,// Validar clave de firma para verificar firma criptografica
            ValidIssuer = issuer,           //Emisor autorizado
            ValidAudience = audience,       //Destinatario autorizado
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),//Crea el objeto que representa la clave simetrica
            //luego convierte la clave en un arreglo de bytes
            RoleClaimType = ClaimTypes.Role, //Especifica que el tipo de reclamo para roles es ClaimTypes.Role

        };
    });

builder.Services.AddAuthorization();

builder.Services.AddSingleton<TokenService>();//Se registra el servicio de token como singleton, lo que significa que se crea una única instancia durante toda la vida de la aplicación
                                              //y se comparte entre todos los controladores y servicios que lo necesiten.

builder.Services.AddEndpointsApiExplorer();//Registra servicios que permiten descubrir y describir los endpoints de la aplicación para su documentación.

//Inicio de la configuracion de swagger mediante especificacion de openAPI
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo//Definicion de version y de los documentos de la API en formato OpenAPI
    {
        Title = "API de Gestión de Inventario",
        Version = "v1",
        Description = "CRUD de productos con ASP.NET Core y EF Core"
    });
    var securityscheme = new OpenApiSecurityScheme//Definicion del esquema de seguridad para autenticacion en Swagger UI
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese 'Bearer <token>'"
    };
    c.AddSecurityDefinition("Bearer", securityscheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});


var app = builder.Build(); //Se construye la app a partir de la configuracion antes especificada

//Desde este punto en adelante se especifica que sucede cuando la API recibe una solicitud
//Existen distintos ciclos de vida de las instancias: TRANSIENT, SCOPED Y SINGLETON
using (var scope = app.Services.CreateScope())//utilizando el contenedor de servicios se crea un ambito nuevo para obtener servicios con ciclo de vida Scoped como AppDbContext
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();//Se obtiene el contexto de la base de datos asociado al ambito, el proveedor de servicios permite administrarlos
                                                                      //Con el ciclo de vida apropiado. No se deben resolver servicios directamente desde app.services

    db.Database.Migrate();//Una vez obtenido el contexto de la base de datos se hacen las migraciones pendientes
}//cuando finaliza el bloque using(){} se libera el ambito y el contexto puede ser eliminado(scope.Dispose()), mas no la conexion SQLite
//En resumen se crea un ambito temporal para actualizar la base de datos antes de que la API comience a atender solicitudes

app.UseSwagger(); //Activacion del middleware que expone el documento openAPI generado por swagger
app.UseSwaggerUI(c =>//Configuracion de Swagger UI
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventario API v1");//direccion de la documentacion
});

app.UseHttpsRedirection(); //redirige HTTP a HTTPS
app.UseAuthentication();
app.UseAuthorization();    //Activacion de middleware de autorizacion
app.MapControllers();      //Conectar rutas declaradas con el sistema de enrutamiento de ASP.NET Core

app.Run();                 //Inicio de la aplicacion, comienza a atender peticiones
