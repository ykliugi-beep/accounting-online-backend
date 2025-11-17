using ERPAccounting.Application.DTOs;
using ERPAccounting.Application.Services;
using ERPAccounting.Application.Services.Contracts;
using ERPAccounting.Application.Validators;
using ERPAccounting.Infrastructure.Data;
using ERPAccounting.Infrastructure.Repositories;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IDocumentLineItemService, DocumentLineItemService>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("ERPAccounting"));
builder.Services.AddScoped<IDocumentLineItemRepository, DocumentLineItemRepository>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IValidator<CreateLineItemDto>, CreateLineItemValidator>();
builder.Services.AddScoped<IValidator<PatchLineItemDto>, PatchLineItemValidator>();
builder.Services.AddScoped<IStoredProcedureService, StoredProcedureService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
