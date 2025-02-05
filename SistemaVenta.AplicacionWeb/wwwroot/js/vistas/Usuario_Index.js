﻿
// Esto hace referencia a las propiedades del View Model Usuario
const MODELO_BASE = {
  idUsuario: 0,
  nombre: "",
  correo: "",
  telefono: "",
  idRol: 0,
  esActivo: 1,
  urlFoto: ""
}

let tablaData;
// FUNCION PRINCIPAL PARA MOSTRAR LA DATA
$(document).ready(function () {

  // FETCH A LA LISTA DE ROLES PARA MOSTRAR EN EL COMBOBOX

  fetch("/Usuario/ListaRoles")
    .then(response => {
      return response.ok ? response.json() : Promise.reject(response);
    })
    .then(responseJson => {
      if (responseJson.length > 0) {
        responseJson.forEach((item) => {
          $("#cboRol").append(
            $("<option>").val(item.idRol).text(item.descripcion)
          )
        })
      }
    })

  // MOSTRAR DATATABLE CON LOS DATOS DEL USUARIO
  tablaData = $('#tbdata').DataTable({
    responsive: true,
    "ajax": {
      "url": '/Usuario/Lista',
      "type": "GET",
      "datatype": "json"
    },
    "columns": [
      { "data": "idUsuario", "visible": false, "searchable": false },
      {
        "data": "urlFoto", render: function (data) {
          return `<img style="height:60px" src=${data} class="rounded mx-auto d-block"/>`
        }
      },
      { "data": "nombre" },
      { "data": "correo" },
      { "data": "telefono" },
      { "data": "nombreRol" },
      {
        "data": "esActivo", render: function (data) {
          if (data === 1)
            return '<span class="badge badge-info">Activo</span>'
          else
            return '<span class="badge badge-danger">No Activo</span>'
        }
      },
      {
        "defaultContent": '<button class="btn btn-primary btn-editar btn-sm mr-2"><i class="fas fa-pencil-alt"></i></button>' +
          '<button class="btn btn-danger btn-eliminar btn-sm"><i class="fas fa-trash-alt"></i></button>',
        "orderable": false,
        "searchable": false,
        "width": "80px"
      }
    ],
    order: [[0, "desc"]],
    dom: "Bfrtip",
    buttons: [
      {
        text: 'Exportar para Excel',
        extend: 'excelHtml5',
        title: '',
        filename: 'Reporte Usuarios',
        exportOptions: {
          columns: [2, 3, 4, 5, 6] //Columnas que se exportaran siguiendo el orden de "columns"
        }
      }, 'pageLength'
    ],
    language: {
      url: "https://cdn.datatables.net/plug-ins/1.11.5/i18n/es-ES.json"
    },
  });
})

// MOSTRAR MODAL

function mostrarModal(modelo = MODELO_BASE) {
  $("#txtId").val(modelo.idUsuario)
  $("#txtNombre").val(modelo.nombre)
  $("#txtCorreo").val(modelo.correo)
  $("#txtTelefono").val(modelo.telefono)
  $("#cboRol").val(modelo.idRol === 0 ? $("#cboRol option:first").val() : modelo.idRol)
  $("#cboEstado").val(modelo.esActivo)
  $("#txtFoto").val("")
  $("#imgUsuario").attr("src", modelo.urlFoto)

  $("#modalData").modal("show")
}
// Boton para mostrar el modal
$("#btnNuevo").click(function () {
  mostrarModal()
})

// Crear un nuevo Usuario
$("#btnGuardar").click(function () {

  const inputs = $("input.input-validar").serializeArray();
  const inputs_sin_valor = inputs.filter(item => item.value.trim() == "");

  if (inputs_sin_valor.length > 0) {
    const mensaje = `Debe completar el campo: "${inputs_sin_valor[0].name}"`;
    toastr.warning("", mensaje)
    $(`input[name="${inputs_sin_valor[0].name}"]`).focus()
    return
  }

  const modelo = structuredClone(MODELO_BASE);
  modelo["idUsuario"] = parseInt($("#txtId").val())
  modelo["nombre"] = $("#txtNombre").val()
  modelo["correo"] = $("#txtCorreo").val()
  modelo["telefono"] = $("#txtTelefono").val()
  modelo["idRol"] = $("#cboRol").val()
  modelo["esActivo"] = $("#cboEstado").val()

  const inputFoto = document.getElementById("txtFoto")

  const formData = new FormData();

  formData.append("foto", inputFoto.files[0])
  formData.append("modelo", JSON.stringify(modelo))

  $("#modalData").find("div.modal-content").LoadingOverlay("show")

  if (modelo.idUsuario === 0) {
    fetch("/Usuario/Crear", {
      method: "POST",
      body: formData
    })
      .then(response => {
        $("#modalData").find("div.modal-content").LoadingOverlay("hide")
        return response.ok ? response.json() : Promise.reject(response);
      })
      .then(responseJson => {
        if (responseJson.estado) {
          tablaData.row.add(responseJson.objeto).draw(false)
          $("#modalData").modal("hide")
          swal("Resultado!", "Usuario criado com sucesso", "success")
        } else {
          swal("Lo sentimos", responseJson.mensaje, "error")
        }
      })
  } else {
    fetch("/Usuario/Editar", {
      method: "PUT",
      body: formData
    })
      .then(response => {
        $("#modalData").find("div.modal-content").LoadingOverlay("hide")
        return response.ok ? response.json() : Promise.reject(response);
      })
      .then(responseJson => {
        if (responseJson.estado) {
          tablaData.row(filaSeleccionada).data(responseJson.objeto).draw(false)
          filaSeleccionada = null
          $("#modalData").modal("hide")
          swal("Resultado!", "Usuario alterado com sucesso", "success")
        } else {
          swal("Lamentamos", responseJson.mensaje, "error")
        }
      })
  }

})

// Editar un Usuario
let filaSeleccionada;
$("#tbdata tbody").on("click", ".btn-editar", function () {
  if ($(this).closest("tr").hasClass("child")) {
    filaSeleccionada = $(this).closest("tr").prev()
  } else {
    filaSeleccionada = $(this).closest("tr")
  }

  const data = tablaData.row(filaSeleccionada).data()
  //console.log(data)
  mostrarModal(data);

})

// Eliminar un Usuario

$("#tbdata tbody").on("click", ".btn-eliminar", function () {
  let fila
  if ($(this).closest("tr").hasClass("child")) {
    fila = $(this).closest("tr").prev()
  } else {
    fila = $(this).closest("tr")
  }

  const data = tablaData.row(fila).data()
  //console.log(data)

  swal({
    title: "tens certeza?",
    text: `Eliminar usuario "${data.nombre}"`,
    type: "warning",
    showCancelButton: true,
    confirmButtonClass: "btn-danger",
    confirmButtonText: "Sim, eliminar",
    cancelButtonText: "Não, cancelar",
    closeOnConfirm: false,
    closeOnCancel: true
  },
    function (respuesta) {
      if (respuesta) {
        $(".showSweetAlert").LoadingOverlay("show")
        
        fetch(`/Usuario/Eliminar?idUsuario=${data.idUsuario}`, {
          method: "DELETE"
        })

          .then(response => {
            $(".showSweetAlert").LoadingOverlay("hide")
            return response.ok ? response.json() : Promise.reject(response);
            
          })
          .then(responseJson => {
            if (responseJson.estado) {

              tablaData.row(fila).remove().draw()

              swal("Resultado!", "Usuario eliminado com sucesso", "success")
            } else {
              swal("Lamentamos!", responseJson.mensaje, "error")
            }
          })
      }
    }
  )

})

