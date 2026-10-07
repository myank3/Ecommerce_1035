var dataTable;
$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
 dataTable = $('#tblData').DataTable({
        "ajax": {
            "url": "/Admin/Category/GetAll"
        },
     "columns": [
         {
             "data": "id",
             "render": function (data) {
                 return `
                        <div class="text-center">
                            <a href="/Admin/Category/Upsert/${data}" class="btn btn-info">
                                <i class="fas fa-edit"></i> Edit
                            </a>
                            <a class="btn btn-danger" onclick=DeleteCat('/Admin/Category/Delete/${data}')> 
                                <i class="fas fa-trash"></i> Delete
                            </a> 
                        </div>
                    `;
             }
         },
            { "data": "name", "width": "70%" },
            
        ], "lengthMenu" : [2,4,6,8],
    });
}

function DeleteCat(url) {
    //alert(url);
    swal({
        title: "Want to Delete Data??",
        text: "Delete Information",
        icon: "warning",
        buttons: true,
        dangerModel: true,

    }).then((willDelete) => {
        if (willDelete) {
            $.ajax({
                url: url,
                type: "DELETE",
                success: function (data) {
                    if (data.success) {
                        toastr.success(data.message);
                        dataTable.ajax.reload();
                    }
                    else {
                        toastr.error(data.message);
                    }
                }

            })
        }
    })
}