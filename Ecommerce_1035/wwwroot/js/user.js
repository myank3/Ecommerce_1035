var dataTable;

$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    dataTable = $('#tblData').DataTable({

        "ajax": {
            "url": "/Admin/User/GetAll"
        },
        "columns": [
            { "data": "name", "width": "15%" },
            { "data": "email", "width": "15%" },
            { "data": "phoneNumber", "width": "10%" },
            { "data": "company.name", "width": "15%" },
            { "data": "role", "width": "15%" },
            {
                "data": { "id": "id", "lockoutEnd": "lockoutEnd" },
                "render": function (data) {
                    var today = new Date().getTime();
                    var lockOut = new Date(data.lockoutEnd).getTime();

                    var lockUnlockBtn = lockOut > today
                        ? `<a class="btn btn-success" onclick=LockUnLock('${data.id}')>Unlock</a>`
                        : `<a class="btn btn-danger" onclick=LockUnLock('${data.id}')>Lock</a>`;

                    return `
                        <div class="text-center">
                            ${lockUnlockBtn}
                        </div>`;
                },
                "width": "30%"
            }
        ]

    });

}

function LockUnLock(id) {
    $.ajax({
        url: "/Admin/User/LockUnlock",
        type: "POST",
        data: JSON.stringify(id),
        contentType: "application/json",
        success: function (data) {
            if (data.success) {
                toastr.success(data.message);
                dataTable.ajax.reload();
            } else {
                toastr.error(data.message);
            }
        }
    });
}