$(document).ready(function () {

    function applyPagination(table) {
        let maxRows = parseInt($('#maxRows').val());
        let rows = table.find('tbody tr');
        let totalRows = rows.length;

        // Reset
        rows.show();
        $('.pagination').html('');

        if (maxRows >= totalRows) {
            $('.rows_count').text(`Showing 1 to ${totalRows} of ${totalRows} entries`);
            return;
        }

        rows.hide();
        rows.slice(0, maxRows).show();

        let pageCount = Math.ceil(totalRows / maxRows);

        let pagination = $('.pagination');
        pagination.append('<li class="page-item disabled"><a class="page-link">Previous</a></li>');

        for (let i = 1; i <= pageCount; i++) {
            pagination.append(`<li class="page-item"><a class="page-link">${i}</a></li>`);
        }

        pagination.append('<li class="page-item"><a class="page-link">Next</a></li>');

        updateRowsCount(1, maxRows, totalRows);

        $('.pagination li').on('click', function () {
            let pageNum = $(this).text();

            if (pageNum === "Previous") {
                let current = $('.pagination li.active a').text();
                if (parseInt(current) > 1) pageNum = parseInt(current) - 1;
                else return;
            }

            if (pageNum === "Next") {
                let current = $('.pagination li.active a').text();
                if (parseInt(current) < pageCount) pageNum = parseInt(current) + 1;
                else return;
            }

            $('.pagination li').removeClass('active');
            $(this).addClass('active');

            let start = (pageNum - 1) * maxRows;
            let end = start + maxRows;

            rows.hide().slice(start, end).show();

            updateRowsCount(start + 1, Math.min(end, totalRows), totalRows);
        });

        $('.pagination li:nth-child(2)').addClass('active'); // first page number
    }

    function updateRowsCount(start, end, total) {
        $('.rows_count').text(`Showing ${start} to ${end} of ${total} entries`);
    }

    // SEARCH FUNCTION
    window.FilterkeyWord_all_table = function () {
        let input = $('#search_input_all').val().toLowerCase();
        let table = $('#myTable');
        let rows = table.find('tbody tr');

        rows.each(function () {
            let rowText = $(this).text().toLowerCase();
            $(this).toggle(rowText.indexOf(input) >= 0);
        });
    };

    // Initialize when table loads
    setTimeout(() => {
        applyPagination($('#myTable'));
    }, 300);

    // When user changes max rows
    $(document).on('change', '#maxRows', function () {
        applyPagination($('#myTable'));
    });

});
