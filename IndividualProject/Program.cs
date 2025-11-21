using Week4.Common.Mapping;
using Week4.Common.Middleware;
using Week4.Common.Logging;
using Week4.Features.Products;
using Week4.Validators;
using Week4.Persistence;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using AutoMapper;
using Week4;

var builder = WebApplication.CreateBuilder(args);

// SWAGGER
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DATABASE (InMemory)
builder.Services.AddDbContext<ApplicationContext>(options =>
{
    options.UseInMemoryDatabase("ProductsDb");
});

// AUTOMAPPER
builder.Services.AddAutoMapper(
    cfg => { cfg.AddProfile<AdvancedProductMappingProfile>(); },
    typeof(AdvancedProductMappingProfile)
);

// VALIDATION
builder.Services.AddScoped<CreateProductProfileValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductProfileValidator>();

// HANDLERS + CACHE
builder.Services.AddScoped<CreateProductHandler>();
builder.Services.AddMemoryCache();

// LOGGING
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

// SWAGGER MIDDLEWARE (required)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// CORRELATION MIDDLEWARE
app.UseMiddleware<CorrelationMiddleware>();


app.MapPost("/products", async (
    CreateProductProfileRequest request,
    CreateProductHandler handler,
    CancellationToken token) =>
{
    var result = await handler.Handle(request, token);
    return Results.Created($"/products/{result.Id}", result);
})
.WithName("CreateProduct")
.WithSummary("Creates a new product with advanced mapping, validation, logging & metrics.")
.Produces<ProductProfileDto>(201)
.Produces(400);

app.Run();
