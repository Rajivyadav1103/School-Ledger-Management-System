$(document).ready(function () {
    loadData();
});

// SEARCH
$(document).on('click', '.btnSearch', function () {
    loadData();
});

// OPEN MODAL
$(document).on('click', '.btnCreate', function () {
    clearForm();
    $('#mdlStudent').show();
});

// CANCEL
$(document).on('click', '#btnCancel', function () {
    if (confirm("Cancel?")) {
        clearForm();
        $('#mdlStudent').hide();
    }
});

// SAVE
$(document).on('click', '.btnSave', function () {

    var data = {
        id: $('.hdnId').val(),
        rollNo: $('.txtRollNo').val(),
        fullName: $('.txtFullName').val(),
        class: $('.txtClass').val(),
        phone: $('.txtPhone').val()
    };

    $.ajax({
        method: 'GET',
        url: '/Student/Create',
        data: data,
        success: function (resp) {
            alert(resp.message);
            clearForm();
            $('#mdlStudent').hide();
            loadData();
        }
    });
});

// LOAD DATA
function loadData() {

    var filters = {
        rollNo: $('.txtSearchRoll').val(),
        name: $('.txtSearchName').val()
    };

    $.ajax({
        method: 'GET',
        url: '/Student/GetAll',
        data: filters,
        success: function (data) {

            var html = "";

            $.each(data, function (i, item) {

                html += `
                <tr>
                    <td>${i + 1}</td>
                    <td>${item.rollNo}</td>
                    <td>${item.fullName}</td>
                    <td>${item.class}</td>
                    <td>${item.phone}</td>
                    <td>
                        <button class="btnEdit" data-id="${item.studentID}">Edit</button>
                        <button class="btnDelete" data-id="${item.studentID}">Delete</button>
                    </td>
                </tr>`;
            });

            $('.main-data').html(html);
        }
    });
}

// EDIT
$(document).on('click', '.btnEdit', function () {

    var id = $(this).data('id');

    $.ajax({
        url: '/Student/GetById',
        data: { id: id },
        success: function (resp) {

            $('.txtRollNo').val(resp.data.rollNo);
            $('.txtFullName').val(resp.data.fullName);
            $('.txtClass').val(resp.data.class);
            $('.txtPhone').val(resp.data.phone);
            $('.hdnId').val(resp.data.studentID);

            $('#mdlStudent').show();
        }
    });
});

// DELETE
$(document).on('click', '.btnDelete', function () {

    if (!confirm("Delete this student?")) return;

    var id = $(this).data('id');

    $.ajax({
        url: '/Student/Delete',
        data: { id: id },
        success: function () {
            loadData();
        }
    });
});

// CLEAR FORM
function clearForm() {
    $('.txtRollNo').val('');
    $('.txtFullName').val('');
    $('.txtClass').val('');
    $('.txtPhone').val('');
    $('.hdnId').val('0');
}