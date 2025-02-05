﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Net;
using SistemaVenta.BLL.Interfaces;
using SistemaVenta.DAL.Interfaces;
using SistemaVenta.Entity.Models;

namespace SistemaVenta.BLL.Implementacion
{
  public class UsuarioService : IUsuarioService
  {
    private static HttpClient Client = new HttpClient();
    private readonly IGenericRepository<Usuario> _repositorio;
    private readonly IFireBaseService _fireBaseService;
    private readonly IUtilidadesService _utilidadesService;
    private readonly ICorreoService _correoService;

    public UsuarioService(IGenericRepository<Usuario> repository, IFireBaseService fireBaseService, IUtilidadesService utilidadesService, ICorreoService correoService)
    {
      _repositorio = repository;
      _fireBaseService = fireBaseService;
      _utilidadesService = utilidadesService;
      _correoService = correoService;
    }

    public async Task<List<Usuario>> Lista()
    {
      IQueryable<Usuario> query = await _repositorio.Consultar();
      return query.Include(rol => rol.IdRolNavigation).ToList();
    }

    public async Task<Usuario> Crear(Usuario entidad, Stream foto = null, string nombreFoto = "", string urlPlantillaCorreo = "")
    {
      Usuario usuarioExiste = await _repositorio.Obtener(u => u.Correo == entidad.Correo);

      if (usuarioExiste != null)
        throw new TaskCanceledException("O e-mail já existe");

      try
      {
        string claveGenerada = _utilidadesService.GenerarClave();
        entidad.Clave = _utilidadesService.ConvertirSha256(claveGenerada);
        entidad.NombreFoto = nombreFoto;

        if (foto != null)
        {
          string urlFoto = await _fireBaseService.SubirStorage(foto, "carpeta_usuario", nombreFoto);
          entidad.UrlFoto = urlFoto;
        }

        Usuario usuarioCreado = await _repositorio.Crear(entidad);

        if (usuarioCreado.IdUsuario == 0)
          throw new TaskCanceledException("Não foi possível criar o usuário");

        if (urlPlantillaCorreo != "")
        {
          urlPlantillaCorreo = urlPlantillaCorreo.Replace("[correo]", usuarioCreado.Correo).Replace("[clave]", claveGenerada);
          string htmlCorreo = "";

          //HttpWebRequest request = (HttpWebRequest)WebRequest.Create(urlPlantillaCorreo);
          //HttpWebResponse response = (HttpWebResponse)request.GetResponse();


          //if (response.StatusCode == HttpStatusCode.OK)
          //{
          //  using (Stream dataStream = response.GetResponseStream())
          //  {
          //    StreamReader reader = null;
          //    if (response.CharacterSet == null)
          //      reader = new StreamReader(dataStream);
          //    else
          //      reader = new StreamReader(dataStream, Encoding.GetEncoding(response.CharacterSet));


          //    htmlCorreo = reader.ReadToEnd();
          //    response.Close();
          //    reader.Close();
          //  }

          //}

          var response = await Client.GetAsync(urlPlantillaCorreo);

          if (response.IsSuccessStatusCode)
          {
            htmlCorreo = await response.Content.ReadAsStringAsync();
          }

          if (htmlCorreo != "")
            await _correoService.EnviarCorreo(usuarioCreado.Correo, "Conta criada", htmlCorreo);
        }

        IQueryable<Usuario> query = await _repositorio.Consultar(u => u.IdUsuario == usuarioCreado.IdUsuario);
        usuarioCreado = query.Include(r => r.IdRolNavigation).First();
        return usuarioCreado;
      }
      catch (Exception ex)
      {
        throw;
      }
    }

    public async Task<Usuario> Editar(Usuario entidad, Stream foto = null, string nombreFoto = "")
    {
      Usuario usuarioExiste = await _repositorio.Obtener(u => u.Correo == entidad.Correo && u.IdUsuario != entidad.IdUsuario);

      if (usuarioExiste != null)
        throw new TaskCanceledException("O e-mail já existe");

      try
      {
        IQueryable<Usuario> queryUsuario = await _repositorio.Consultar(u => u.IdUsuario == entidad.IdUsuario);
        Usuario usuarioEditar = queryUsuario.First();

        usuarioEditar.Nombre = entidad.Nombre;
        usuarioEditar.Correo = entidad.Correo;
        usuarioEditar.Telefono = entidad.Telefono;
        usuarioEditar.IdRol = entidad.IdRol;
        usuarioEditar.EsActivo = entidad.EsActivo;

        if (usuarioEditar.NombreFoto == "")
          usuarioEditar.NombreFoto = nombreFoto;

        if (foto != null)
        {
          string urlFoto = await _fireBaseService.SubirStorage(foto, "carpeta_usuario", usuarioEditar.NombreFoto);
          usuarioEditar.UrlFoto = urlFoto;
        }

        bool respuesta = await _repositorio.Editar(usuarioEditar);

        if (!respuesta)
          throw new TaskCanceledException("Não foi possível alterar o usuário");

        Usuario usuarioEditado = queryUsuario.Include(r => r.IdRolNavigation).First();

        return usuarioEditado;
      }
      catch
      {
        throw;
      }

    }

    public async Task<bool> Eliminar(int idUsuario)
    {
      try
      {
        Usuario usuarioEncontrado = await _repositorio.Obtener(u => u.IdUsuario == idUsuario);

        if (usuarioEncontrado == null)
          throw new TaskCanceledException("O usuário não existe");

        string nombreFoto = usuarioEncontrado.NombreFoto;
        bool respuesta = await _repositorio.Eliminar(usuarioEncontrado);

        if (respuesta)
          await _fireBaseService.EliminarStorage("carpeta_usuario", nombreFoto);

        return true;
      }
      catch
      {
        throw;
      }
    }

    public async Task<Usuario> ObtenerPorCredenciales(string correo, string clave)
    {
      string claveEncriptada = _utilidadesService.ConvertirSha256(clave);
      Usuario usuarioEncontrado = await _repositorio.Obtener(u => u.Correo.Equals(correo) && u.Clave.Equals(claveEncriptada));

      return usuarioEncontrado;
    }

    public async Task<Usuario> ObtenerPorId(int idUsuario)
    {
      IQueryable<Usuario> query = await _repositorio.Consultar(u => u.IdUsuario == idUsuario);
      Usuario resultado = query.Include(r => r.IdRolNavigation).FirstOrDefault();

      return resultado;
    }

    public async Task<bool> GuardarPerfil(Usuario entidad)
    {
      try
      {
        Usuario usuarioEncontrado = await _repositorio.Obtener(u => u.IdUsuario == entidad.IdUsuario);
        if (usuarioEncontrado == null)
          throw new TaskCanceledException("O usuário não existe");

        usuarioEncontrado.Correo = entidad.Correo;
        usuarioEncontrado.Telefono = entidad.Telefono;

        bool respuesta = await _repositorio.Editar(usuarioEncontrado);
        return respuesta;
      }
      catch
      {
        throw;
      }
    }

    public async Task<bool> CambiarClave(int idUsuario, string claveActual, string claveNueva)
    {
      try
      {
        Usuario usuarioEncontrado = await _repositorio.Obtener(u => u.IdUsuario == idUsuario);
        if (usuarioEncontrado == null)
          throw new TaskCanceledException("O usuário não existe");

        if (usuarioEncontrado.Clave != _utilidadesService.ConvertirSha256(claveActual))
          throw new TaskCanceledException("A senha digitada não está correta");

        usuarioEncontrado.Clave = _utilidadesService.ConvertirSha256(claveNueva);

        bool respuesta = await _repositorio.Editar(usuarioEncontrado);
        return respuesta;
      }
      catch
      {
        throw;
      }
    }

    public async Task<bool> RestablecerClave(string correo, string urlPlantillaCorreo)
    {
      try
      {
        Usuario usuarioEncontrado = await _repositorio.Obtener(u => u.Correo == correo);

        if (usuarioEncontrado == null)
          throw new TaskCanceledException("Não encontramos nenhum usuário associado ao e-mail");

        string claveGenerada = _utilidadesService.GenerarClave();
        usuarioEncontrado.Clave = _utilidadesService.ConvertirSha256(claveGenerada);

        urlPlantillaCorreo = urlPlantillaCorreo.Replace("[clave]", claveGenerada);
        string htmlCorreo = "";

        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(urlPlantillaCorreo);
        HttpWebResponse response = (HttpWebResponse)request.GetResponse();

        if (response.StatusCode == HttpStatusCode.OK)
        {
          using (Stream dataStream = response.GetResponseStream())
          {
            StreamReader reader = null;
            if (response.CharacterSet == null)
              reader = new StreamReader(dataStream);
            else
              reader = new StreamReader(dataStream, Encoding.GetEncoding(response.CharacterSet));

            htmlCorreo = reader.ReadToEnd();
            response.Close();
            reader.Close();
          }
        }

        bool correoEnviado = false;

        if (htmlCorreo != "")
          correoEnviado = await _correoService.EnviarCorreo(correo, "Redefinição de senha", htmlCorreo);

        if (correoEnviado)
          throw new TaskCanceledException("Temos problemas. Por favor, tente novamente mais tarde");

        bool respuesta = await _repositorio.Editar(usuarioEncontrado);

        return respuesta;
      }
      catch
      {
        throw;
      }
    }
  }
}
