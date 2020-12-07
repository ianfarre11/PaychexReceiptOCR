// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
function vendorSearch() {
    // Declare variables
    var input, filter, table, tr, td, i, txtValue;
    input = document.getElementById("myInput");
    filter = input.value.toUpperCase();
    table = document.getElementById("myTable");
    tr = table.getElementsByTagName("tr");

    // Loop through all table rows, and hide those who don't match the search query
    for (i = 0; i < tr.length; i++) {
        td = tr[i].getElementsByTagName("td")[1];
        if (td) {
            txtValue = td.textContent || td.innerText;
            if (txtValue.toUpperCase().indexOf(filter) > -1) {
                tr[i].style.display = "";
            } else {
                tr[i].style.display = "none";
            }
        }
    }
}

function priceSearch() {
    var input, filter, table, tr, td, i, txtValue, radioButton, txtValue1, txtValue2;
    input = document.getElementById("myInputPrice");
    filter = parseFloat(input.value);
    table = document.getElementById("myTable");
    tr = table.getElementsByTagName("tr");
    radioButton = document.querySelectorAll('input[name="groupOfMaterialRadios"]');
    for (const rb of radioButton) {
        if (rb.checked && rb.value == ">") {
            for (i = 0; i < tr.length; i++) {
                td = tr[i].getElementsByTagName("td")[2];
                if (td) {
                    txtValue = td.textContent || td.innerText;
                    if (txtValue == "Unknown") {
                        tr[i].style.display = "none";
                    }
                    else {
                        txtValue1 = txtValue.substring(1, txtValue.length);
                        txtValue2 = parseFloat(txtValue1);
                        if (txtValue2 > filter) {
                            tr[i].style.display = "";
                        } else {
                            tr[i].style.display = "none";
                        }
                    }
                }
            }
        }
        if (rb.checked && rb.value == "<") {
            for (i = 0; i < tr.length; i++) {
                td = tr[i].getElementsByTagName("td")[2];
                if (td) {
                    txtValue = td.textContent || td.innerText;
                    if (txtValue == "Unknown") {
                        tr[i].style.display = "none";
                    }
                    else {
                        txtValue1 = txtValue.substring(1, txtValue.length);
                        txtValue2 = parseFloat(txtValue1);
                        if (txtValue2 < filter) {
                            tr[i].style.display = "";
                        } else {
                            tr[i].style.display = "none";
                        }
                    }
                }
            }
        }
        if (rb.checked && rb.value == "=") {
            for (i = 0; i < tr.length; i++) {
                td = tr[i].getElementsByTagName("td")[2];
                if (td) {
                    txtValue = td.textContent || td.innerText;
                    if (txtValue == "Unknown") {
                        tr[i].style.display = "none";
                    }
                    else {
                        txtValue1 = txtValue.substring(1, txtValue.length)
                        txtValue2 = parseFloat(txtValue1);
                        if (txtValue2 == filter) {
                            tr[i].style.display = "";
                        } else {
                            tr[i].style.display = "none";
                        }
                    }
                }
            }
        }
    }
}

function resetTable() {
    // Declare variables
    var table, tr, td, i;
    table = document.getElementById("myTable");
    tr = table.getElementsByTagName("tr");

    // Loop through all table rows, and hide those who don't match the search query
    for (i = 0; i < tr.length; i++) {
        td = tr[i].getElementsByTagName("td")[1];
        if (td) {
            tr[i].style.display = "";
        }
    }
}