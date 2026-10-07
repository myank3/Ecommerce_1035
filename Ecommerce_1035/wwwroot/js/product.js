var dataTable;

$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "ajax": {
            "url": "/Admin/Product/GetAll"
        },
        "columns": [
            { "data": "title", "width": "15%" },
            { "data": "description", "width": "20%" },
            { "data": "author", "width": "15%" },
            { "data": "isbn", "width": "15%" },
            { "data": "price", "width": "15%" },
            {
                "data": "id",
                "render": function (data) {
                    return `
                        <div class="text-center">
                            <a href="/Admin/Product/Upsert/${data}" class="btn btn-info text-white" style="cursor:pointer">
                                <i class="fas fa-edit"></i> Edit
                            </a>
                            <a class="btn btn-danger text-white" style="cursor:pointer" onclick="DeleteCat('/Admin/Product/Delete/${data}')"> 
                                <i class="fas fa-trash"></i> Delete
                            </a> 
                        </div>
                    `;
                },
                "width": "20%"
            }
        ],
        "lengthMenu": [5, 10, 15, 20],
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