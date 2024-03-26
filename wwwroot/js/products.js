$(Document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "ajax": { url: '/product/getall' },
        "columns": [ // Corrected property name to "columns"
            { data: 'title' },
            { data: 'ISBN' },
            { data: 'Price' },
            { data: 'Author' }
        ] // Added a closing curly brace for the "columns" property
    });
}

