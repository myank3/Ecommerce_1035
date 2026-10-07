var dataTable;

$(document).ready(function () {
    loadDataTable();

    $("#searchBtn").on("click", function () {
        var from = $("#fromDate").val();
        var to = $("#toDate").val();

        if (from && to && new Date(to) < new Date(from)) {
            swal("Invalid Date Range", "\"To\" date cannot be earlier than \"From\" date.", "error");
            return;
        }

        dataTable.destroy();
        loadDataTable();
    });
});

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "ajax": {
            "url": "/Admin/Order/GetAll",
            "data": {
                "fromDate": $("#fromDate").val(),
                "toDate": $("#toDate").val()
            }
        },
        "columns": [
            {
                "data": "orderDate",
                "render": function (data) {
                    return new Date(data).toLocaleDateString();
                }
            },
            { "data": "name" },
            { "data": "phoneNumber" },
            { "data": "applicationUser.email" },
            {
                "data": "orderTotal",
                "render": function (data) {
                    return "₹ " + parseFloat(data).toFixed(2);
                }
            },
            {
                "data": "id",
                "render": function (data) {
                    return `
                        <a href="/Admin/Order/Details/${data}" class="btn btn-info btn-sm">
                            View Detail
                        </a>
                    `;
                }
            }
        ],
        "pageLength": 10,
        "lengthMenu": [[10, 20, 50, -1], ["10", "20", "50", "All"]]
    });
}