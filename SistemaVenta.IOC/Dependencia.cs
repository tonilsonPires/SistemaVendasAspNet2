﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SistemaVenta.DAL.DBContext;
using Microsoft.EntityFrameworkCore;
using SistemaVenta.DAL.Interfaces;
using SistemaVenta.DAL.Implementacion;
using SistemaVenta.BLL.Interfaces;
using SistemaVenta.BLL.Implementacion;


namespace SistemaVenta.IOC
{
  public static class Dependencia
  {
    // Inyectamos la dependencia del DBContext
    public static void InyectarDependencia(this IServiceCollection services, IConfiguration configuration)
    {
      services.AddDbContext<DBVENTAContext>(options =>
      {
        options.UseSqlServer(configuration.GetConnectionString("CadenaSQL"));
      });

      // Sirve para poder permitir cualquier entidad que respete la interfaz y la clase
      services.AddTransient(typeof(IGenericRepository<>), typeof(GenericRepository<>));

      // Inyectamos la dependencia para el servicio de Venta
      services.AddScoped<IVentaRepository, VentaRepository>();

      // Inyectamos la dependencia para el servicio de correo
      services.AddScoped<ICorreoService, CorreoService>();

      // Inyectamos la dependencia para el servicio de FireBase
      services.AddScoped<IFireBaseService, FireBaseService>();

      // Inyectamos la dependencia para las utilidades
      services.AddScoped<IUtilidadesService, UtilidadesService>();

      // Inyectamos la dependencia para los roles
      services.AddScoped<IRolService, RolService>();

      // Inyectamos la dependencia para los usuarios
      services.AddScoped<IUsuarioService, UsuarioService>();

      // Inyectamos la dependencia para el negocio
      services.AddScoped<INegocioService, NegocioService>();

      // Inyectamos la dependencia para la categoria
      services.AddScoped<ICategoriaService, CategoriaService>();

      // Inyectamos la dependencia para los productos
      services.AddScoped<IProductoService, ProductoService>();

      // Inyectamos la dependencia para los tipos de Documentos de venta
      services.AddScoped<ITipoDocumentoVentaService, TipoDocumentoVentaService>();

      // Inyectamos la dependencia para los tipos de Ventas
      services.AddScoped<IVentaService, VentaService>();

      // Inyectamos la dependencia para el DashBoard
      services.AddScoped<IDashBoardService, DashBoardService>();

    }
  }
}
